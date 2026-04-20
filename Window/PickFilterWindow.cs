using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityPickFilter
{
    public class PickFilterWindow : EditorWindow
    {
        [MenuItem("Tools/RainRust/Pick Filter")]
        public static void Open()
        {
            GetWindow<PickFilterWindow>("Pick Filter").Show();
        }

        private PickFilterSettings m_Settings;
        private SerializedObject m_SerializedSettings;
        private ReorderableList m_SOList;
        private Vector2 m_Scroll;
        private string m_StatusMessage;
        private double m_StatusTime;

        private void OnEnable()
        {
            RefreshSettings();
        }

        private void RefreshSettings()
        {
            m_Settings = PickFilterSettings.GetOrCreate();
            m_SerializedSettings = new SerializedObject(m_Settings);
            BuildList();
        }

        private void BuildList()
        {
            var prop = m_SerializedSettings.FindProperty("RuleSets");
            m_SOList = new ReorderableList(m_SerializedSettings, prop, true, true, true, true);

            m_SOList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Rule Sets   (top = highest priority)");

            m_SOList.drawElementCallback = DrawSOElement;
            m_SOList.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 6f;

            m_SOList.onAddCallback = _ => CreateAndAddRuleSet();
            m_SOList.onChangedCallback = _ =>
            {
                m_SerializedSettings.ApplyModifiedProperties();
                if (m_Settings.AutoApply)
                    PickFilterProcessor.ForceApply(m_Settings);
            };
        }

        private void DrawSOElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var prop = m_SerializedSettings.FindProperty("RuleSets");
            var elem = prop.GetArrayElementAtIndex(index);
            var ruleSetSO = elem.objectReferenceValue as PickFilterRuleSO;

            float lh = EditorGUIUtility.singleLineHeight;
            float y = rect.y + 3f;
            float x = rect.x;
            float w = rect.width;

            float toggleW = 18f;
            float selectW = 56f;
            float gap = 4f;
            float fieldW = w - toggleW - selectW - gap * 2f;

            // Enabled toggle
            if (ruleSetSO != null)
            {
                EditorGUI.BeginChangeCheck();
                bool enabled = EditorGUI.Toggle(new Rect(x, y, toggleW, lh), ruleSetSO.Enabled);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(ruleSetSO, "Toggle Rule Set");
                    ruleSetSO.Enabled = enabled;
                    EditorUtility.SetDirty(ruleSetSO);
                    if (m_Settings.AutoApply)
                        PickFilterProcessor.ForceApply(m_Settings);
                }
            }
            else
            {
                EditorGUI.Toggle(new Rect(x, y, toggleW, lh), false);
            }

            // SO object field
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(
                new Rect(x + toggleW + gap, y, fieldW, lh),
                elem,
                GUIContent.none
            );
            if (EditorGUI.EndChangeCheck())
            {
                m_SerializedSettings.ApplyModifiedProperties();
                if (m_Settings.AutoApply)
                    PickFilterProcessor.ForceApply(m_Settings);
            }

            // Select button
            if (ruleSetSO != null)
            {
                if (
                    GUI.Button(new Rect(x + toggleW + gap + fieldW + gap, y, selectW, lh), "Select")
                )
                    Selection.activeObject = ruleSetSO;
            }
        }

        private void CreateAndAddRuleSet()
        {
            string defaultDir = "Assets/Settings/PickFilter";
            if (!System.IO.Directory.Exists(defaultDir))
                System.IO.Directory.CreateDirectory(defaultDir);

            string path = EditorUtility.SaveFilePanelInProject(
                "Create Rule Set",
                "NewRuleSet",
                "asset",
                "Choose where to save the new Rule Set",
                defaultDir
            );

            if (string.IsNullOrEmpty(path))
                return;

            var newSO = CreateInstance<PickFilterRuleSO>();
            AssetDatabase.CreateAsset(newSO, path);
            AssetDatabase.SaveAssets();

            m_SerializedSettings.Update();
            var prop = m_SerializedSettings.FindProperty("RuleSets");
            prop.arraySize++;
            prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = newSO;
            m_SerializedSettings.ApplyModifiedProperties();
        }

        private void OnGUI()
        {
            if (m_Settings == null || m_SerializedSettings == null)
            {
                RefreshSettings();
                return;
            }

            m_SerializedSettings.Update();

            DrawToolbar();
            DrawList();
            DrawAddExisting();
            DrawStatusBar();

            m_SerializedSettings.ApplyModifiedProperties();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            bool autoApply = EditorGUILayout.ToggleLeft(
                "Auto Apply",
                m_Settings.AutoApply,
                GUILayout.Width(88f)
            );
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_Settings, "Toggle Auto Apply");
                m_Settings.AutoApply = autoApply;
                EditorUtility.SetDirty(m_Settings);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Apply Now", EditorStyles.toolbarButton, GUILayout.Width(76f)))
            {
                PickFilterProcessor.ForceApply(m_Settings);
                ShowStatus("Rules applied.");
            }

            if (GUILayout.Button("Reset All", EditorStyles.toolbarButton, GUILayout.Width(68f)))
            {
                PickFilterProcessor.ResetAllPicking();
                ShowStatus("All picking re-enabled.");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawList()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll, GUILayout.ExpandHeight(true));
            EditorGUILayout.Space(2f);
            m_SOList.DoLayoutList();
            EditorGUILayout.EndScrollView();
        }

        private void DrawAddExisting()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Add Existing SO:", GUILayout.Width(106f));
            var dragged =
                EditorGUILayout.ObjectField(null, typeof(PickFilterRuleSO), false)
                as PickFilterRuleSO;
            if (dragged != null && !m_Settings.RuleSets.Contains(dragged))
            {
                Undo.RecordObject(m_Settings, "Add Rule Set");
                m_Settings.RuleSets.Add(dragged);
                EditorUtility.SetDirty(m_Settings);
                m_SerializedSettings.Update();
                if (m_Settings.AutoApply)
                    PickFilterProcessor.ForceApply(m_Settings);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2f);
        }

        private void DrawStatusBar()
        {
            if (string.IsNullOrEmpty(m_StatusMessage))
                return;

            if (EditorApplication.timeSinceStartup - m_StatusTime > 4.0)
            {
                m_StatusMessage = null;
                Repaint();
                return;
            }

            EditorGUILayout.HelpBox(m_StatusMessage, MessageType.Info);
        }

        private void ShowStatus(string msg)
        {
            m_StatusMessage = msg;
            m_StatusTime = EditorApplication.timeSinceStartup;
            Repaint();
        }
    }
}
