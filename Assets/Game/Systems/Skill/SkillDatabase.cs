using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Skill
{
    [CreateAssetMenu(menuName = "EffectInstance System/Database/Skill Database")]
    public class SkillDatabase : ScriptableObject
    {
        public List<SkillData> skills;

        private Dictionary<int, SkillData> lookupTable;

        public void Initialize()
        {
            lookupTable = new Dictionary<int, SkillData>();
            foreach (var s in skills)
                lookupTable[s.skillId] = s;
        }

        public SkillData GetSkill(int id)
        {
            if(lookupTable == null) Initialize();
            return lookupTable.TryGetValue(id, out var s) ? s : null;
        }
    }
}
