using Game.Core.Battle;
using Game.Core.Game;
using Game.Systems.Combat;
using Game.Systems.Effect;
using Game.Systems.Stat;
using Game.Systems.Tags;
using Game.Systems.Trigger;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Game.Core.Game.GameManager;

namespace Game.Systems.Skill
{
    [Serializable]
    public class AbilityData
    {
        public List<TriggerGroup> effects;
    }

    [Serializable]
    public class DamageOverrideData
    {
        public DamageType damageType;
        public CoreStats statOverride;
    }

    [Serializable]
    public class CounterOverrideData
    {
        public string requiredWeapon;
        public int rangeOverride;
    }

    public class SkillList
    {
        public List<int> activeSkills = new();
        public List<int> knownSkills = new();
        public int attackOverrideSkillId = -1;
        public int defenseOverrideSkillId = -1;
        public int counterAttackOverrideSkillId = -1;

        public int MAX_ACTIVE_SKILLS = 6;

        public bool LearnSkill(int skillId)
        {
            if(!knownSkills.Contains(skillId))
            {
                knownSkills.Add(skillId);
                return true;
            }
            return false;
        }

        public bool EquipSkill(int skillId)
        {
            if(!knownSkills.Contains(skillId)) return false;
            if (activeSkills.Contains(skillId)) return false;
            if(activeSkills.Count >= MAX_ACTIVE_SKILLS) return false;

            activeSkills.Add(skillId);
            return true;
        }

        public void UnequipSkill(int skillId)
        {
            activeSkills.Remove(skillId);
        }

        public bool EquipAttackOverrideSkill(int skillId)
        {
            attackOverrideSkillId = skillId;
            return true;
        }

        public bool EquipDefenseOverrideSkill(int skillId)
        {
            defenseOverrideSkillId = skillId;
            return true;
        }
        public bool EquipCounterAttackOverrideSkill(int skillId)
        {
            counterAttackOverrideSkillId = skillId;
            return true;
        }

        public int GetOverrideSkill(string slot)
        {
            return slot switch
            {
                "Attack" => attackOverrideSkillId,
                "Defense" => defenseOverrideSkillId,
                "Counter" => counterAttackOverrideSkillId,
                _ => -1,
            };
        }

    }

    public static class SkillAutoEquip
    {
        public static void AutoAssignSkills(SkillList skillList, List<int> skillIds,SkillDatabase db)
        {
            if(skillIds == null|| skillIds.Count == 0) return;
            foreach(int id in skillIds)
            {
                skillList.LearnSkill(id);
            }

            foreach(int id in skillIds)
            {
                if(skillList.activeSkills.Count < skillList.MAX_ACTIVE_SKILLS)
                    skillList.EquipSkill(id);
            }
            
            ChooseAttackOverride(skillList,skillIds,db);
            ChooseDefenseOverride(skillList,skillIds,db);
            ChooseCounterAttackOverride(skillList,skillIds,db);
        }

        public static void ChooseAttackOverride(SkillList skillList, List<int> skillIds,SkillDatabase db)
        {
            foreach (int id in skillIds)
            {
                var sd = db.GetSkill(id);
                if (sd != null && sd.attackOverride.enabled)
                {
                    skillList.EquipAttackOverrideSkill(id);
                }
            }
        }

        public static void ChooseDefenseOverride(SkillList skillList, List<int> skillIds, SkillDatabase db)
        {
            foreach (int id in skillIds)
            {
                var sd = db.GetSkill(id);
                if (sd != null && sd.defenceOverride.enabled)
                {
                    skillList.EquipAttackOverrideSkill(id);
                }
            }
        }

        public static void ChooseCounterAttackOverride(SkillList skillList, List<int> skillIds, SkillDatabase db)
        {
            foreach (int id in skillIds)
            {
                var sd = db.GetSkill(id);
                if (sd != null && sd.counterOverride.enabled)
                {
                    skillList.EquipAttackOverrideSkill(id);
                }
            }
        }
    }

    public class SkillManager 
    {
        public SkillDatabase skillDb;
        private readonly Dictionary<int, SkillList> skillRegistry = new();

        public void InitializeSystem(GameManager gameManager,BattleSystems battleSystems)
        {
            skillDb = gameManager.SkillDb;
            gameManager.triggerDispatcher.Register(new SkillListener(this));
        }

        public void RegisterUnit(int unitId, List<int> equipedSkillIds,List<int> knowSkillIds)
        {
            if (!skillRegistry.ContainsKey(unitId))
            {
                var skillList = new SkillList();

                SkillAutoEquip.AutoAssignSkills(skillList, equipedSkillIds,skillDb);

                foreach (int id in knowSkillIds) skillList.LearnSkill(id);
                skillRegistry[unitId] = skillList;
            }
            
        }

        public void LearnSkill(int unitId,int skillId) 
        {
            if (!skillRegistry.ContainsKey(unitId)) return;
            skillRegistry[unitId].LearnSkill(skillId);
        }

        public List<SkillData> GetActiveSkills(int unitId)
        {
            var list = new List<SkillData>();
            if (!skillRegistry.ContainsKey(unitId)) return null;
            var activeSkills = skillRegistry[unitId].activeSkills;
            foreach(var skill in activeSkills)
            {
                list.Add(skillDb.GetSkill(skill));
            }
            return list;
        }

        public SkillData GetOverrideSkill(string slot,int unitId)
        {
            var list = skillRegistry[unitId];
            var skillId = list.GetOverrideSkill(slot);

            if(skillId == -1) return null;
            return skillDb.GetSkill(skillId);

        }
    }

    public class SkillListener : TriggerListener
    {
        private readonly SkillManager skillManager;
        public SkillListener(SkillManager skillManager) 
        {
            this.skillManager = skillManager;
            priority = 0;
        }

        public override void Trigger(TriggerContext context)
        {
            if (context is not BattleContext baseCtx)
                return;

            int triggerOwnerId = baseCtx.SourceUnitId;

            var skills = skillManager.GetActiveSkills(triggerOwnerId);

            foreach(var sk in skills)
            {
                CheckSkill(sk, baseCtx.triggerCall, baseCtx);
            }
        }

        private void CheckSkill(SkillData skillData,TriggerKey triggerCall,BattleContext baseCtx)
        {
            foreach (var tg in skillData.abilityData.effects)
            {
                if (!tg.TriggerCall.Equals(triggerCall))
                    continue;

                var effectCtx = new EffectContext
                {
                    triggerCall = triggerCall,
                    battleSystems = baseCtx.battleSystems,

                    SourceUnitId = baseCtx.SourceUnitId,
                    TargetUnitId = baseCtx.TargetUnitId,

                    SourceCode = SourceType.Unit,
                    SourceId = baseCtx.SourceUnitId,

                    SourceRefCode = SourceType.Skill,
                    SourceRefId = skillData.skillId,

                    EffectInstance = null
                };

                ActionHelper.ExecuteActions(tg.Actions, effectCtx);
            }
        }
    }
}
