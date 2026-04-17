using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityPickFilter
{
    [InitializeOnLoad]
    public static class PickFilterProcessor
    {
        private static double m_PendingApplyTime = -1;
        private const double k_DebounceSeconds = 0.3;

        static PickFilterProcessor()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.hierarchyChanged += ScheduleApply;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += ApplyAllRules;
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            ApplyAllRules();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                ApplyAllRules();
        }

        private static void ScheduleApply()
        {
            m_PendingApplyTime = EditorApplication.timeSinceStartup + k_DebounceSeconds;
            EditorApplication.update -= CheckPendingApply;
            EditorApplication.update += CheckPendingApply;
        }

        private static void CheckPendingApply()
        {
            if (EditorApplication.timeSinceStartup < m_PendingApplyTime)
                return;

            EditorApplication.update -= CheckPendingApply;
            m_PendingApplyTime = -1;
            ApplyAllRules();
        }

        public static void ApplyAllRules()
        {
            var settings = PickFilterSettings.GetOrCreate();
            if (!settings.AutoApply)
                return;
            ForceApply(settings);
        }

        public static void ResetAllPicking()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                foreach (var root in scene.GetRootGameObjects())
                    SceneVisibilityManager.instance.EnablePicking(root, true);
            }
        }

        public static void ForceApply(PickFilterSettings settings = null)
        {
            if (settings == null)
                settings = PickFilterSettings.GetOrCreate();

            ResetAllPicking();

            var decisions = new Dictionary<GameObject, (PickAction action, bool includeChildren, string soName, string ruleName)>();
            var conflicts = new Dictionary<GameObject, List<(PickAction action, string soName, string ruleName)>>();

            foreach (var so in settings.RuleSets)
            {
                if (so == null || !so.Enabled)
                    continue;

                foreach (var rule in so.Rules)
                {
                    foreach (var (go, includeChildren) in FindMatchingObjects(rule))
                    {
                        if (!decisions.ContainsKey(go))
                        {
                            decisions[go] = (rule.Action, includeChildren, so.name, rule.RuleName);
                        }
                        else if (decisions[go].action != rule.Action)
                        {
                            if (!conflicts.ContainsKey(go))
                                conflicts[go] = new List<(PickAction, string, string)>();
                            conflicts[go].Add((rule.Action, so.name, rule.RuleName));
                        }
                    }
                }
            }

            foreach (var kvp in decisions)
            {
                var go = kvp.Key;
                var (action, includeChildren, _, _) = kvp.Value;

                if (action == PickAction.DisablePick)
                    SceneVisibilityManager.instance.DisablePicking(go, includeChildren);
                else
                    SceneVisibilityManager.instance.EnablePicking(go, includeChildren);
            }

            foreach (var kvp in conflicts)
            {
                var go = kvp.Key;
                var first = decisions[go];
                var sb = new StringBuilder();
                sb.AppendLine($"[PickFilter] Conflict on GameObject '{go.name}'  —  First-match wins.");
                sb.AppendLine($"  Applied  : {first.action} (SO: '{first.soName}', Rule: '{first.ruleName}')");
                foreach (var (action, soName, ruleName) in kvp.Value)
                    sb.AppendLine($"  Skipped  : {action} (SO: '{soName}', Rule: '{ruleName}')");
                Debug.LogWarning(sb.ToString(), go);
            }
        }

        private static List<(GameObject go, bool includeChildren)> FindMatchingObjects(PickFilterRule rule)
        {
            var result = new List<(GameObject, bool)>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                foreach (var root in scene.GetRootGameObjects())
                    CollectMatching(root, rule, result);
            }
            return result;
        }

        private static void CollectMatching(GameObject go, PickFilterRule rule, List<(GameObject, bool)> result)
        {
            if (rule.Matches(go))
            {
                bool includeChildren = rule.Scope == PickScope.Tree;
                result.Add((go, includeChildren));
                if (includeChildren)
                    return;
            }

            for (int i = 0; i < go.transform.childCount; i++)
                CollectMatching(go.transform.GetChild(i).gameObject, rule, result);
        }
    }

    public class PickFilterAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                if (AssetDatabase.LoadAssetAtPath<PickFilterRuleSO>(path) != null)
                {
                    PickFilterProcessor.ApplyAllRules();
                    return;
                }
            }
        }
    }
}
