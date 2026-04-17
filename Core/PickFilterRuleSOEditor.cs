using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityPickFilter
{
    [CustomEditor(typeof(PickFilterRuleSO))]
    public class PickFilterRuleSOEditor : Editor
    {
        private ReorderableList m_RulesList;
        private SerializedProperty m_EnabledProp;
        private SerializedProperty m_RulesProp;

        private void OnEnable()
        {
            m_EnabledProp = serializedObject.FindProperty("Enabled");
            m_RulesProp = serializedObject.FindProperty("Rules");
            BuildList();
        }

        private void BuildList()
        {
            m_RulesList = new ReorderableList(serializedObject, m_RulesProp, true, true, true, true);
            m_RulesList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Rules  (top = evaluated first)");
            m_RulesList.drawElementCallback = DrawRuleElement;
            m_RulesList.elementHeightCallback = _ => (EditorGUIUtility.singleLineHeight + 2f) * 6f + 6f;
            m_RulesList.onAddCallback = list =>
            {
                m_RulesProp.arraySize++;
                var elem = m_RulesProp.GetArrayElementAtIndex(m_RulesProp.arraySize - 1);
                elem.FindPropertyRelative("RuleName").stringValue = "New Rule";
                elem.FindPropertyRelative("Action").enumValueIndex = 0;
                elem.FindPropertyRelative("Scope").enumValueIndex = 0;
                serializedObject.ApplyModifiedProperties();
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_EnabledProp);
            EditorGUILayout.Space(4f);
            m_RulesList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRuleElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var rule = m_RulesProp.GetArrayElementAtIndex(index);
            float lh = EditorGUIUtility.singleLineHeight;
            float ls = lh + 2f;
            float x = rect.x;
            float w = rect.width;
            float y = rect.y + 2f;

            // Row 0: RuleName | Action | Scope
            float nameW = w * 0.38f;
            float actionW = w * 0.3f;
            float scopeW = w - nameW - actionW - 8f;
            EditorGUI.PropertyField(new Rect(x, y, nameW, lh), rule.FindPropertyRelative("RuleName"), GUIContent.none);
            EditorGUI.PropertyField(new Rect(x + nameW + 4f, y, actionW, lh), rule.FindPropertyRelative("Action"), GUIContent.none);
            EditorGUI.PropertyField(new Rect(x + nameW + actionW + 8f, y, scopeW, lh), rule.FindPropertyRelative("Scope"), GUIContent.none);
            y += ls;

            DrawCondition(ref y, x, w, lh, ls, rule, "UseNameFilter", "NamePattern", "Name (Regex)");
            DrawCondition(ref y, x, w, lh, ls, rule, "UseTagFilter", "Tag", "Tag");
            DrawCondition(ref y, x, w, lh, ls, rule, "UseLayerFilter", "Layer", "Layer");
            DrawCondition(ref y, x, w, lh, ls, rule, "UseHasComponent", "HasComponentType", "Has Component");
            DrawCondition(ref y, x, w, lh, ls, rule, "UseNotHasComponent", "NotHasComponentType", "Not Has Component");
        }

        private static void DrawCondition(
            ref float y, float x, float w, float lh, float ls,
            SerializedProperty rule,
            string toggleField, string valueField, string label)
        {
            float toggleW = 18f;
            float labelW = w * 0.3f;
            float valueX = x + toggleW + 4f + labelW + 4f;
            float valueW = w - toggleW - 4f - labelW - 4f;

            var toggleProp = rule.FindPropertyRelative(toggleField);
            EditorGUI.PropertyField(new Rect(x, y, toggleW, lh), toggleProp, GUIContent.none);

            using (new EditorGUI.DisabledScope(!toggleProp.boolValue))
            {
                EditorGUI.LabelField(new Rect(x + toggleW + 4f, y, labelW, lh), label);
                EditorGUI.PropertyField(
                    new Rect(valueX, y, valueW, lh),
                    rule.FindPropertyRelative(valueField),
                    GUIContent.none);
            }

            y += ls;
        }
    }
}
