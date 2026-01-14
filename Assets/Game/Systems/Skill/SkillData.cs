using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Skill
{
    [CreateAssetMenu(menuName = "EffectInstance System/EffectInstance/Skill Data")]
    public class SkillData : ScriptableObject
    {
        public int skillId;
        public string skillName;
        public AbilityData abilityData;

        public OptionalAttackOverride attackOverride;
        public OptionalAttackOverride defenceOverride;
        public OptionalCounterOverride counterOverride;
    }
    [System.Serializable]
    public class OptionalAttackOverride
    {
        public bool enabled;
        public DamageOverrideData data;
    }
    [System.Serializable]
    public class OptionalCounterOverride
    {
        public bool enabled;
        public CounterOverrideData data;
    }
}
