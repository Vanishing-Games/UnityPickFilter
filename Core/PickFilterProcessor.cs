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

        private static void OnSceneOpened(
            UnityEngine.SceneManagement.Scene scene,
            OpenSceneMode mode
        )
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

            var decisionsSelf =
                new Dictionary<
                    GameObject,
                    (PickAction action, string soName, string ruleName, int priority)
                >();
            var decisionsTree =
                new Dictionary<
                    GameObject,
                    (PickAction action, string soName, string ruleName, int priority)
                >();

            int priorityCounter = 0;
            foreach (var so in settings.RuleSets)
            {
                if (so == null || !so.Enabled)
                    continue;

                foreach (var rule in so.Rules)
                {
                    int currentPriority = priorityCounter++;
                    foreach (var go in FindAllMatchingObjects(rule))
                    {
                        var targetDict =
                            rule.Scope == PickScope.SingleObject ? decisionsSelf : decisionsTree;

                        bool shouldUpdate = false;
                        if (!targetDict.ContainsKey(go))
                        {
                            shouldUpdate = true;
                        }
                        else
                        {
                            // 使用规则自带的结合模式
                            if (rule.CombineMode == RuleCombineMode.Override)
                                shouldUpdate = true;
                        }

                        if (shouldUpdate)
                        {
                            targetDict[go] = (rule.Action, so.name, rule.RuleName, currentPriority);
                        }
                    }
                }
            }

            if (settings.OnlyApplyToLeaves)
            {
                ApplyToLeavesOnly(decisionsSelf, decisionsTree);
            }
            else
            {
                ApplyLegacy(decisionsSelf, decisionsTree);
            }
        }

        private static void ApplyToLeavesOnly(
            Dictionary<
                GameObject,
                (PickAction action, string soName, string ruleName, int priority)
            > decisionsSelf,
            Dictionary<
                GameObject,
                (PickAction action, string soName, string ruleName, int priority)
            > decisionsTree
        )
        {
            var allPickables = GetAllPickableObjects();
            foreach (var go in allPickables)
            {
                PickAction finalAction = PickAction.EnablePick;

                // 1. Check self decision (priority)
                // 2. Check tree decisions in ancestry (nearest wins)

                if (decisionsSelf.TryGetValue(go, out var selfDec))
                {
                    finalAction = selfDec.action;
                }
                else
                {
                    var curr = go;
                    while (curr != null)
                    {
                        if (decisionsTree.TryGetValue(curr, out var treeDec))
                        {
                            finalAction = treeDec.action;
                            break;
                        }
                        curr =
                            curr.transform.parent != null ? curr.transform.parent.gameObject : null;
                    }
                }

                if (finalAction == PickAction.DisablePick)
                    SceneVisibilityManager.instance.DisablePicking(go, false);
                else
                    SceneVisibilityManager.instance.EnablePicking(go, false);
            }
        }

        private static void ApplyLegacy(
            Dictionary<
                GameObject,
                (PickAction action, string soName, string ruleName, int priority)
            > decisionsSelf,
            Dictionary<
                GameObject,
                (PickAction action, string soName, string ruleName, int priority)
            > decisionsTree
        )
        {
            // In legacy mode, we just apply everything as they come.
            // To respect priority, we should sort all decisions.
            var all =
                new List<(GameObject go, PickAction action, bool includeChildren, int priority)>();
            foreach (var kvp in decisionsSelf)
                all.Add((kvp.Key, kvp.Value.action, false, kvp.Value.priority));
            foreach (var kvp in decisionsTree)
                all.Add((kvp.Key, kvp.Value.action, true, kvp.Value.priority));

            all.Sort((a, b) => a.priority.CompareTo(b.priority));

            foreach (var dec in all)
            {
                if (dec.action == PickAction.DisablePick)
                    SceneVisibilityManager.instance.DisablePicking(dec.go, dec.includeChildren);
                else
                    SceneVisibilityManager.instance.EnablePicking(dec.go, dec.includeChildren);
            }
        }

        private static HashSet<GameObject> GetAllPickableObjects()
        {
            var result = new HashSet<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                foreach (var root in scene.GetRootGameObjects())
                    CollectPickables(root, result);
            }
            return result;
        }

        private static void CollectPickables(GameObject go, HashSet<GameObject> result)
        {
            if (IsPotentiallyPickable(go))
                result.Add(go);

            for (int i = 0; i < go.transform.childCount; i++)
                CollectPickables(go.transform.GetChild(i).gameObject, result);
        }

        private static bool IsPotentiallyPickable(GameObject go)
        {
            return go.GetComponent<Renderer>() != null
                || go.GetComponent<Collider>() != null
                || go.GetComponent<Collider2D>() != null;
        }

        private static List<GameObject> FindAllMatchingObjects(PickFilterRule rule)
        {
            var result = new List<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                foreach (var root in scene.GetRootGameObjects())
                    CollectAllMatching(root, rule, result);
            }
            return result;
        }

        private static void CollectAllMatching(
            GameObject go,
            PickFilterRule rule,
            List<GameObject> result
        )
        {
            if (rule.Matches(go))
            {
                result.Add(go);
            }

            for (int i = 0; i < go.transform.childCount; i++)
                CollectAllMatching(go.transform.GetChild(i).gameObject, rule, result);
        }
    }

    public class PickFilterAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
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
