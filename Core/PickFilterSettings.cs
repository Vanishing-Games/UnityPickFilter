using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityPickFilter
{
    public enum PickFilterConflictPolicy
    {
        FirstMatchWins,
        LastMatchWins,
    }

    [CreateAssetMenu(menuName = "UnityPickFilter/Settings", fileName = "PickFilterSettings")]
    public class PickFilterSettings : ScriptableObject
    {
        private const string k_DefaultPath = "Assets/Settings/PickFilter/PickFilterSettings.asset";

        public bool AutoApply = true;
        public PickFilterConflictPolicy ConflictPolicy = PickFilterConflictPolicy.FirstMatchWins;
        public bool OnlyApplyToLeaves = true;
        public List<PickFilterRuleSO> RuleSets = new List<PickFilterRuleSO>();

        private static PickFilterSettings s_Instance;

        public static PickFilterSettings GetOrCreate()
        {
            if (s_Instance != null)
                return s_Instance;

            var guids = AssetDatabase.FindAssets("t:PickFilterSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                s_Instance = AssetDatabase.LoadAssetAtPath<PickFilterSettings>(path);
                if (s_Instance != null)
                    return s_Instance;
            }

            var dir = Path.GetDirectoryName(k_DefaultPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            s_Instance = CreateInstance<PickFilterSettings>();
            AssetDatabase.CreateAsset(s_Instance, k_DefaultPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return s_Instance;
        }
    }
}
