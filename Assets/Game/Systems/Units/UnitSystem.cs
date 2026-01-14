using Game.Core.Battle;
using Game.Systems.BattleMap;
using Game.Systems.Combat;
using Game.Systems.Effect;
using Game.Systems.Item;
using Game.Systems.Job;
using Game.Systems.Stat;
using Game.Systems.Tags;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEditor;
using UnityEngine;
using static Game.Core.Game.GameManager;
using static Game.Core.Game.GameManager.GameConst;
using static Game.Systems.Combat.CombatHelper;

namespace Game.Systems.Units
{
    [Serializable]
    public class UnitSaveData
    {
        public string OverrideName;
        public int templateId;
        public bool hasFactionOverride;
        public int OverrideFactionId;
        public int OverrideCurrentLevel;
        public int OverrideCurrentExp;
        public bool hasStatOverride;
        public UnitStatsData OverrideUnitStatData;
        public JobSave OverrideJob;
        public List<JobSave> OverrideJobs;
        public List<ItemSave> Overrideitems;
        public List<int> OverrideAttacks;
        public List<int> OverrideEquippedSkills;
        public List<int> OverrideKnowSkills;
        public Vector2Int OverrideTilePostion;

    }

    public static class UnitIdGenerator
    {
        public static int _nextId = 100;

        public static int GenNextId() { return _nextId++; }

        public static void Reset()
        {
            _nextId = 100;
        }
    }

    public class UnitFactory
    {
        private  int _nextId = 1;

        private UnitTemplateDatabase _database;
        private BattleSystems _battleSystems;

        public void Initialize(UnitTemplateDatabase unitTemplateDatabase,BattleSystems battleSystems)
        {
            _database = unitTemplateDatabase;
            _battleSystems = battleSystems;
        }

        public UnitInstance CreateUnit(UnitSaveData unitSaveData)
        {
            var instanceId = _nextId++;
            var template = _database.GetTeamplate(unitSaveData.templateId);
            var unit = new UnitInstance
            {
                instanceId = instanceId,
                unitName = String.IsNullOrEmpty(unitSaveData.OverrideName) ? template.name : unitSaveData.OverrideName,
                factionId = unitSaveData.hasFactionOverride ? unitSaveData.OverrideFactionId: template.factionId,
                statsData = unitSaveData.hasStatOverride ? unitSaveData.OverrideUnitStatData : template.unitStatData,
                effectList = CreateEffectList(instanceId),
                equippedSkillsId = unitSaveData.OverrideEquippedSkills,
                knowSkillsID = unitSaveData.OverrideKnowSkills,
                knowAttacksId = unitSaveData.OverrideAttacks,
                itemsOwned = unitSaveData.Overrideitems
            };
 

            return unit;
        }

        public List<UnitInstance>CreateUnitsFromBattleConfig(BattleConfigData config)
        {
            var units = new List<UnitInstance>();
            foreach(var u in config.UnitsInMap)
            {
                units.Add(CreateUnit(u));
            }
            return units;
        }


        public EffectList CreateEffectList(int id)
        {
            var effectList = new EffectList();

            _battleSystems.EffectManager.RegisterUnit(id, effectList);
            return effectList;
        }

    }

    public class UnitInstance
    {
        public int instanceId;
        public int templateId;
        public string unitName;
        public int factionId;
        public UnitStatsData statsData;
        public List<int> knowSkillsID;
        public List<int> equippedSkillsId;
        public List<int> knowAttacksId;
        public List<ItemSave> itemsOwned;
        public EffectList effectList;
        public float currentHp;
        public bool isAlive => currentHp >= 0;
        public bool hasActed;
    }

    public class UnitRunTimeManager
    {
        private readonly Dictionary<int, UnitInstance> unitRegistry = new();
        private BattleSystems battleSystems;

        public void Initialize(BattleSystems battleSystems)
        {
            this.battleSystems = battleSystems;
        }
        public void RegisterUnit(int unitId, UnitInstance unitInstance)
        {
            unitRegistry[unitInstance.instanceId] = unitInstance;
            battleSystems.StatManager.RegisterUnit(unitId, unitInstance.statsData);
            battleSystems.EffectManager.RegisterUnit(unitId,unitInstance.effectList);
            battleSystems.SkillManager.RegisterUnit(unitId, unitInstance.equippedSkillsId, unitInstance.knowSkillsID);
            battleSystems.AttackLoadOutManager.RegisterUnit(unitId, unitInstance.knowAttacksId);
            battleSystems.InventoryManager.RegisterUnit(unitId, unitInstance.itemsOwned);

        }

        public float GetUnitDamage(int unitId, DamageType damageType)
        {
            CoreStats stat = CombatHelper.ResolveStat(unitId,damageType,OverrideType.Attack,CoreStats.Attack,CoreStats.Magic,battleSystems);
            return battleSystems.StatManager.GetFinalStat(unitId, stat);
        }

        public float GetUnitDefence(int unitId, DamageType damageType)
        {
            CoreStats stat = CombatHelper.ResolveStat(unitId, damageType, OverrideType.Defense, CoreStats.Defence, CoreStats.Resistance, battleSystems);
            bool ignoreModifiers = damageType == DamageType.TruePhysical || damageType == DamageType.TrueMagical;

            return battleSystems.StatManager.GetStat(unitId, stat, ignoreModifiers);
        }

        public float GetStat(int unitId,CoreStats stat, bool isBaseValue)
        {
            if (!unitRegistry.ContainsKey(unitId)) return 0f;
            return battleSystems.StatManager.GetStat(unitId,stat,isBaseValue);
        }
        //Used to get Hit, avoid and Move
        public float GetOtherStat(int unitId, OtherStats otherStats,bool ignoreModifiers)
        {
            if (!unitRegistry.ContainsKey(unitId)) return 0f;

            return battleSystems.StatManager.GetStat(unitId, otherStats, ignoreModifiers);
        }

        public void TakeDamage(int unitId, bool isHealing, float value)
        {
            if (!unitRegistry.TryGetValue(unitId, out var unit) || unit == null)
                return;

            if (isHealing)
            {
                float maxHealth = battleSystems.StatManager.GetFinalStat(unitId, CoreStats.Health);

                unit.currentHp = Mathf.Min(unit.currentHp + value, maxHealth);
            }
            else
            {
                unit.currentHp = Mathf.Max(unit.currentHp - value, 0f);
            }
        }

        public AttackData GetDefaultAttack(int unitId)
        {
            if (!unitRegistry.ContainsKey(unitId)) return null;

            var defualtAttack = battleSystems.AttackLoadOutManager.GetDefaultAttack(unitId);

            return defualtAttack;
        }

        public AttackData GetSelectedAttack(int unitId)
        {
            if (!unitRegistry.ContainsKey(unitId)) return null;

            var defualtAttack = battleSystems.AttackLoadOutManager.GetSelectedAttack(unitId);

            return defualtAttack;
        }

        public MoveType GetUnitCurrentMoveType(int unitId)
        {
            if (!unitRegistry.ContainsKey(unitId)) return MoveType.None;

            var moveProfile = battleSystems.JobManager.GetUnitCurrentMoveProfile(unitId);

            if(moveProfile == null) return MoveType.None;

            return moveProfile.MoveType;
        }

        public void TickUnit(int unitId)
        {
            if (!unitRegistry.ContainsKey(unitId)) return;

            battleSystems.EffectManager.TickUnit(unitId, battleSystems);
        }

        public void SetHasActed(int unitId,bool hasActed)
        {
            if (unitRegistry.TryGetValue(unitId,out var unitInstance)) return;
            unitInstance.hasActed = hasActed;
        }

        public bool CanAct(int unitId)
        {
            if (unitRegistry.TryGetValue(unitId, out var unitInstance)) return false;
            return unitInstance.hasActed;
        }

        public UnitInstance GetUnit(int unitId)
        {
            return unitRegistry[unitId];
        }
        public void PrintUnit(int unitid)
        {
            var unit = unitRegistry[unitid];
            Debug.Log(unit.unitName);
            Debug.Log("statsData: " + battleSystems.StatManager.PrintCoreStats(unitid));
            Debug.Log("other: " + battleSystems.StatManager.PrintOtherStats(unitid));
        }

        public void SaveStats(int unitid)
        {
            if (unitRegistry.TryGetValue(unitid, out var unitInstance)) return;
            var statsSaveData = battleSystems.StatManager.ToUnitStatsData(unitid);

            if (statsSaveData == null) return;
            unitInstance.statsData = statsSaveData;
        }
    }
}
    

