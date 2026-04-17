using System.Collections.Generic;
using UnityEngine;

namespace UnityPickFilter
{
    [CreateAssetMenu(menuName = "UnityPickFilter/Rule Set", fileName = "NewRuleSet")]
    public class PickFilterRuleSO : ScriptableObject
    {
        public bool Enabled = true;
        public List<PickFilterRule> Rules = new List<PickFilterRule>();
    }
}
