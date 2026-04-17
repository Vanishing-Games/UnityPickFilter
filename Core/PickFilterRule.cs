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

    [Serializable]
    public class PickFilterRule
    {
        public string RuleName = "New Rule";
        public PickAction Action = PickAction.DisablePick;
        public PickScope Scope = PickScope.SingleObject;

        public bool UseNameFilter;
        public string NamePattern = "";

        public bool UseTagFilter;
        public string Tag = "Untagged";

        public bool UseLayerFilter;
        public LayerMask Layer;

        public bool UseHasComponent;
        public string HasComponentType = "";

        public bool UseNotHasComponent;
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

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            var t = Type.GetType(typeName);
            if (t != null) return t;

            t = Type.GetType($"UnityEngine.{typeName}, UnityEngine");
            if (t != null) return t;

            t = Type.GetType($"UnityEngine.{typeName}, UnityEngine.CoreModule");
            if (t != null) return t;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(typeName);
                if (t != null) return t;
            }

            return null;
        }
    }
}
