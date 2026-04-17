#if !UNITY_PICK_FILTER_NO_ODIN
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
#endif
using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityPickFilter
{
    public enum PickAction
    {
        DisablePick,
        EnablePick,
    }

    public enum PickScope
    {
        SingleObject,
        Tree,
    }

    public enum RuleCombineMode
    {
        Override,    // 覆盖：如果匹配，则强制应用此规则，不管之前是否有匹配
        FollowFirst, // 沿用：如果之前已经有规则匹配了该物体，则跳过此规则
    }

    [Serializable]
    public class PickFilterRule
    {
#if !UNITY_PICK_FILTER_NO_ODIN
        [HorizontalGroup("Header"), LabelWidth(74)]
#endif
        public string RuleName = "New Rule";

#if !UNITY_PICK_FILTER_NO_ODIN
        [HorizontalGroup("Header"), LabelWidth(50)]
#endif
        public PickAction Action = PickAction.DisablePick;

#if !UNITY_PICK_FILTER_NO_ODIN
        [HorizontalGroup("Header"), LabelWidth(42)]
#endif
        public PickScope Scope = PickScope.SingleObject;

#if !UNITY_PICK_FILTER_NO_ODIN
        [HorizontalGroup("Header"), LabelWidth(90)]
#endif
        public RuleCombineMode CombineMode = RuleCombineMode.Override;

#if !UNITY_PICK_FILTER_NO_ODIN
        [HorizontalGroup("NameRow", 0.05f), HideLabel]
#endif
        public bool UseNameFilter;

#if !UNITY_PICK_FILTER_NO_ODIN
        [HorizontalGroup("NameRow"), EnableIf("UseNameFilter"), LabelText("Name (Regex)"), LabelWidth(96)]
#endif
        public string NamePattern = "";

#if !UNITY_PICK_FILTER_NO_ODIN
        [HorizontalGroup("TagRow", 0.05f), HideLabel]
#endif
        public bool UseTagFilter;

#if !UNITY_PICK_FILTER_NO_ODIN
        [HorizontalGroup("TagRow"), EnableIf("UseTagFilter"), LabelText("Tag"), LabelWidth(96)]
#endif
        public string Tag = "Untagged";

#if !UNITY_PICK_FILTER_NO_ODIN
        [HorizontalGroup("LayerRow", 0.05f), HideLabel]
#endif
        public bool UseLayerFilter;

#if !UNITY_PICK_FILTER_NO_ODIN
        [HorizontalGroup("LayerRow"), EnableIf("UseLayerFilter"), LabelText("Layer"), LabelWidth(96)]
#endif
        public LayerMask Layer;

#if !UNITY_PICK_FILTER_NO_ODIN
        [HorizontalGroup("HasCompRow", 0.05f), HideLabel]
#endif
        public bool UseHasComponent;

#if !UNITY_PICK_FILTER_NO_ODIN
        [HorizontalGroup("HasCompRow"), EnableIf("UseHasComponent"), LabelText("Has Component"), LabelWidth(96)]
        [ValueDropdown("GetComponentTypeNames")]
#endif
        public string HasComponentType = "";

#if !UNITY_PICK_FILTER_NO_ODIN
        [HorizontalGroup("NotHasCompRow", 0.05f), HideLabel]
#endif
        public bool UseNotHasComponent;

#if !UNITY_PICK_FILTER_NO_ODIN
        [HorizontalGroup("NotHasCompRow"), EnableIf("UseNotHasComponent"), LabelText("Not Has Component"), LabelWidth(96)]
        [ValueDropdown("GetComponentTypeNames")]
#endif
        public string NotHasComponentType = "";

        public bool Matches(GameObject go)
        {
            bool anyEnabled = UseNameFilter || UseTagFilter || UseLayerFilter
                              || UseHasComponent || UseNotHasComponent;
            if (!anyEnabled)
                return true;

            if (UseNameFilter && !string.IsNullOrEmpty(NamePattern))
            {
                try
                {
                    if (!Regex.IsMatch(go.name, NamePattern, RegexOptions.IgnoreCase))
                        return false;
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }

            if (UseTagFilter && !string.IsNullOrEmpty(Tag))
            {
                try
                {
                    if (!go.CompareTag(Tag))
                        return false;
                }
                catch (UnityException)
                {
                    return false;
                }
            }

            if (UseLayerFilter)
            {
                if ((Layer.value & (1 << go.layer)) == 0)
                    return false;
            }

            if (UseHasComponent && !string.IsNullOrEmpty(HasComponentType))
            {
                var type = ResolveType(HasComponentType);
                if (type == null || go.GetComponent(type) == null)
                    return false;
            }

            if (UseNotHasComponent && !string.IsNullOrEmpty(NotHasComponentType))
            {
                var type = ResolveType(NotHasComponentType);
                if (type != null && go.GetComponent(type) != null)
                    return false;
            }

            return true;
        }

        private static Dictionary<string, Type> s_TypeCache;

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            if (s_TypeCache == null)
            {
                s_TypeCache = new Dictionary<string, Type>(StringComparer.Ordinal);
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var type in asm.GetTypes())
                        {
                            if (typeof(Component).IsAssignableFrom(type))
                            {
                                // Store by short name and full name
                                s_TypeCache[type.Name] = type;
                                s_TypeCache[type.FullName] = type;
                            }
                        }
                    }
                    catch { /* Some assemblies might not be accessible */ }
                }
            }

            if (s_TypeCache.TryGetValue(typeName, out var t))
                return t;

            return null;
        }

#if !UNITY_PICK_FILTER_NO_ODIN
        private static List<string> s_ComponentTypeNames;

        private static IEnumerable<string> GetComponentTypeNames()
        {
            if (s_ComponentTypeNames != null)
                return s_ComponentTypeNames;

            // Ensure cache is populated
            ResolveType("Component");

            s_ComponentTypeNames = s_TypeCache.Values
                .Where(t => !t.IsAbstract && !t.IsGenericType)
                .Select(t => t.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            return s_ComponentTypeNames;
        }
#endif
    }
}
