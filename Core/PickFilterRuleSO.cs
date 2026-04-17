#if !UNITY_PICK_FILTER_NO_ODIN
using Sirenix.OdinInspector;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace UnityPickFilter
{
    [CreateAssetMenu(menuName = "UnityPickFilter/Rule Set", fileName = "NewRuleSet")]
    public class PickFilterRuleSO : ScriptableObject
    {
        public bool Enabled = true;

#if !UNITY_PICK_FILTER_NO_ODIN
        [ListDrawerSettings(DraggableItems = true, ShowIndexLabels = true, ShowItemCount = true)]
#endif
        public List<PickFilterRule> Rules = new List<PickFilterRule>();
    }
}
