using Game.Core.Battle;
using Game.Core.Game;
using Game.Systems.Effect;
using Game.Systems.Logging;
using Game.Systems.Tags;
using Game.Systems.Trigger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking.Types;
using static Game.Core.Game.GameManager;
using Random = UnityEngine.Random;

namespace Game.Systems.Stat
{
    public enum CoreStats
    {
        Health,
        Attack,
        Defence,
        Magic,
        Resistance,
        Speed,
        Luck
    }

    public enum OtherStats
    {
        Hit_Rate,
        Avoid_Rate,
        Move
    }
    public enum StatType
    {
        Core,
        Other
    }
    [Serializable]
    public readonly struct StatKey : IEquatable<StatKey>
    {
        public readonly StatType Type;
        public readonly int Value;

        public StatKey(CoreStats stat)
        {
            Type = StatType.Core;
            Value = (int)stat;
        }

        public StatKey(OtherStats stat)
        {
            Type = StatType.Other;
            Value = (int)stat;
        }

        public bool Equals(StatKey other)
        {
            return Type == other.Type && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is StatKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Value);
        }

        public override string ToString()
        {
            return $"{Type}:{Value}";
        }

        public static bool operator ==(StatKey left, StatKey right) => left.Equals(right);
        public static bool operator !=(StatKey left, StatKey right) => !left.Equals(right);
    }
    [Serializable]
    public enum ModifierType
    {
        Flat,
        Percent,
        PercentAdd,
        Cap,
    }

    public readonly struct StatModifier
    {
        public readonly ModifierType Type;
        public readonly float Value;
        public readonly int Order;

        public StatModifier(ModifierType type, float value)
        {
            Type = type;
            Value = value;
            Order = GetPriority(type);
        }

        private static int GetPriority(ModifierType type) => type switch
        {
            ModifierType.Cap => 0,
            ModifierType.Percent => 1,
            ModifierType.PercentAdd => 2,
            ModifierType.Flat => 3,
            _ => 0
        };
    }

    public sealed class StatModifierOrderComparer : IComparer<StatModifier>
    {
        public static readonly StatModifierOrderComparer Instance = new();

        public int Compare(StatModifier x, StatModifier y)
            => x.Order.CompareTo(y.Order);
    }
    public class Stat
    {
        private float baseValue;
        private readonly List<StatModifier> modifiers = new();

        private bool isDirty = true;
        private float finalValue;

        public Stat(float baseValue)
        {
            this.baseValue = baseValue;
            isDirty = true;
        }

        public float BaseValue => baseValue;

        public void IncreaseBaseValue(float value)
        {
            baseValue += value;
            isDirty = true;
        }
        public void SetBase(float value)
        {
            baseValue = value;
            isDirty = true;
        }

        public void AddModifier(in StatModifier mod)
        {
            int index = modifiers.BinarySearch(
                mod,
                StatModifierOrderComparer.Instance
            );

            if (index < 0)
                index = ~index;

            modifiers.Insert(index, mod);
            isDirty = true;
        }

        public void RemoveModifier(in StatModifier mod)
        {
            modifiers.Remove(mod);
            isDirty = true;
        }

        public void RemoveAllModifiers()
        {
            modifiers.Clear();
            isDirty = true;
        }

        public float GetFinalValue()
        {
            if (isDirty)
            {
                Recalculate();
                isDirty = false;
            }
            return finalValue;
        }

        private void Recalculate()
        {
            float value = baseValue;
            float percentAdd = 0f;


            for (int i = 0; i < modifiers.Count; i++)
            {
                var mod = modifiers[i];

                switch (mod.Type)
                {
                    case ModifierType.Flat:
                        value += mod.Value;
                        break;
                    case ModifierType.Percent:
                        value *= 1 + mod.Value;
                        break;
                    case ModifierType.PercentAdd:
                        percentAdd += mod.Value;
                        break;
                    case ModifierType.Cap:
                        value = Mathf.Clamp(value, 0, mod.Value);
                        break;
                }
            }
            if (percentAdd != 0f)
                value *= (1 + percentAdd);

            finalValue = Mathf.Round(value);
            isDirty = false;
        }

    }



    [Serializable]
    public class StatData
    {
        public CoreStats stat;
        public float baseStatValue = 0;
        public int growthRate = 0;

        public override string ToString()
        {
            return $"{stat}:{baseStatValue}(Growth:{growthRate})";
        }
    }

    [Serializable]
    public class UnitStatsData
    {
        public List<StatData> coreStats;
    }

    public readonly struct AppliedModifier
    {
        public readonly StatModifier Modifier;
        public readonly int ModId;
        public readonly int SourceId;
        public readonly int SourceRefId;
        public readonly StatKey TargetStat;

        public AppliedModifier(in StatModifier modifier,
            int modId,
            int sourceId,
            int sourceRefId,
            StatKey targetStat)
        {
            Modifier = modifier;
            ModId = modId;
            SourceId = sourceId;
            SourceRefId = sourceRefId;
            TargetStat = targetStat;
        }

    }

    public class ActiveModifierList
    {
        public List<AppliedModifier> activeMods = new();

        public bool TryGetModifer(int modId,int sourceId,int sourceRef,out AppliedModifier result)
        {
            foreach (var mod in activeMods)
            {
                if(mod.ModId == modId && mod.ModId == sourceId && mod.SourceRefId == sourceRef)
                {
                    result = mod;
                    return true;
                }
            }
            result = default;
            return false;
        }

        public void AddModifier(in AppliedModifier appliedModifier) => activeMods.Add(appliedModifier);
        public void RemoveModifier(in AppliedModifier appliedModifier) => activeMods.Remove(appliedModifier);

        public AppliedModifier GetModifier(int modId, int sourceId, int sourceRefId)
        {
            return activeMods.FirstOrDefault(
                m => m.ModId == modId && m.SourceId == sourceId && m.SourceRefId == sourceRefId
            );
        }

        public bool IsModifierPresent(int modId, int sourceId, int sourceRefId)
        {
            return activeMods.Any(m => m.ModId == modId && m.SourceId == sourceId && m.SourceRefId == sourceRefId);
        }
    }

    public class StatEntry
    {
        public Stat stat;
        public int growthRate;

        public StatEntry(float baseVaule, int growthRate)
        {
            stat = new Stat(baseVaule);
            this.growthRate = growthRate;
        }

        public float GetBaseStat() => stat.BaseValue;
        public float GetFinalValue() => stat.GetFinalValue();

        public void AddModifer(StatModifier modifier) => stat.AddModifier(modifier);
        public void RemoveModifer(StatModifier modifier) => stat.RemoveModifier(modifier);
        public void IncreaseBaseStat(float value) => stat.IncreaseBaseValue(value);
        public void SetBaseStat(float value) => stat.SetBase(value);

        public void RollGrowth()
        {
            if (growthRate < 0) return;
            if (Random.Range(0, 100) < growthRate)
                IncreaseBaseStat(1);
        }

        public override string ToString()
        {
            return $"{stat.BaseValue}({stat.GetFinalValue()})(Growth:{growthRate})";
        }
    }

    public class UnitStats
    {
        private Dictionary<StatKey, StatEntry> stats = new();
        public ActiveModifierList ActiveMods = new();

        public void AddStat(StatKey key, StatEntry entry)
        {
            stats[key] = entry;
        }
        public StatEntry Get(CoreStats stat)
        {
            var key = new StatKey(stat);
            if (!stats.TryGetValue(key, out var entry))
                throw new Exception($"Missing CoreStat {stat}");
            return entry;
        }
        public StatEntry Get(OtherStats stat)
        {
            var key = new StatKey(stat);
            if (!stats.TryGetValue(key, out var entry))
                throw new Exception($"Missing OtherStat {stat}");
            return entry;
        }
        public float GetStatValue(CoreStats stat) => Get(stat).GetFinalValue();
        public float GetStatValue(OtherStats stat) => Get(stat).GetFinalValue();

        public float GetStatBaseValue(CoreStats stat) => Get(stat).GetBaseStat();
        public float GetStatBaseValue(OtherStats stat) => Get(stat).GetBaseStat();

        public void SetOtherStat(float val,OtherStats stat) => Get(stat).SetBaseStat(val);

        public bool IsModiferPresent(int modId, int sourceId, int sourceRefId) => ActiveMods.IsModifierPresent(modId, sourceId, sourceRefId);
        public void AddModifier(AppliedModifier appliedModifier)
        {
            stats[appliedModifier.TargetStat].AddModifer(appliedModifier.Modifier);
            ActiveMods.AddModifier(appliedModifier);
        }

        public void RemoveModifier(AppliedModifier appliedModifier)
        {
            stats[appliedModifier.TargetStat].RemoveModifer(appliedModifier.Modifier);
            ActiveMods.RemoveModifier(appliedModifier);
        }
        public string CoreStatsToString()
        {
            var sb = new StringBuilder();
            foreach (CoreStats stat in Enum.GetValues(typeof(CoreStats)))
            {
                sb.Append($"{stat} {Get(stat)}");
            }
            return sb.ToString();
        }
        public string OtherStatsToString()
        {
            var sb = new StringBuilder();
            foreach (OtherStats stat in Enum.GetValues(typeof(OtherStats)))
            {
                sb.Append($"{stat} {Get(stat)}");
            }
            return sb.ToString();
        }

        public UnitStatsData SaveStats()
        {
            var savedStats = new UnitStatsData();
            foreach (CoreStats stat in Enum.GetValues(typeof(CoreStats)))
            {
                var statEntry = Get(stat);
                var statSave = new StatData
                {
                    stat = stat,
                    baseStatValue = statEntry.GetBaseStat(),
                    growthRate = statEntry.growthRate
                };
                savedStats.coreStats.Add(statSave);
            }
            
            return savedStats;
        }
    }

    public class StatManager
    {
        public TagCategory statTags;

        private readonly Dictionary<int, UnitStats> statRegistry = new();

        public void InitializeSystem(TagDatabase tagDatabase)
        {
            statTags = tagDatabase.GetCategory("Stats");
            Debug.Log("System Up");
        }

        public void RegisterUnit(int unitId, UnitStatsData statsData)
        {
            UnitStats unitStats = new();

            foreach (var statData in statsData.coreStats)
            {
                var entry = new StatEntry(statData.baseStatValue, statData.growthRate);

                unitStats.AddStat(new StatKey(statData.stat), entry);
            }

            CreateOtherStats(unitStats);

            statRegistry[unitId] = unitStats;
        }

        private void CreateOtherStats(UnitStats unitStats)
        {
            unitStats.AddStat(
                new StatKey(OtherStats.Hit_Rate),
                new StatEntry(0, 0));
            unitStats.AddStat(
                new StatKey(OtherStats.Avoid_Rate),
                new StatEntry(0, 0));
            unitStats.AddStat(
                new StatKey(OtherStats.Move),
                new StatEntry(0, 0));
        }

        public float GetStat(int unitId, CoreStats stat, bool ignoreModifiers)
        {
            return ignoreModifiers
                ? GetBaseStat(unitId, stat)
                : GetFinalStat(unitId, stat);
        }
        public float GetStat(int unitId, OtherStats stat, bool ignoreModifiers)
        {
            return ignoreModifiers
                ? GetBaseStat(unitId, stat)
                : GetFinalStat(unitId, stat);
        }

        public float GetFinalStat(int unitId, CoreStats stat)
        {
            if (!statRegistry.TryGetValue(unitId, out var unitStats)) return 0f;

            return unitStats.GetStatValue(stat);
        }

        public float GetFinalStat(int unitId, OtherStats stat)
        {
            if (!statRegistry.TryGetValue(unitId, out var unitStats)) return 0f;

            return unitStats.GetStatValue(stat);
        }
        public float GetBaseStat(int unitId,CoreStats stat)
        {
            if (!statRegistry.TryGetValue(unitId, out var unitStats)) return 0f;

            return unitStats.GetStatBaseValue(stat);
        }

        public float GetBaseStat(int unitId, OtherStats stat)
        {
            if (!statRegistry.TryGetValue(unitId, out var unitStats)) return 0f;

            return unitStats.GetStatBaseValue(stat);
        }

        public void SetOtherStat(int unitId, float value,OtherStats stat)
        {
            if (!statRegistry.TryGetValue(unitId, out var unitStats)) return;

            unitStats.SetOtherStat(value, stat);
        }


        public void RollLevelUp(int unitId)
        {
            var unitStats = statRegistry[unitId];

            foreach (CoreStats stat in Enum.GetValues(typeof(CoreStats)))
            {
                unitStats.Get(stat).RollGrowth();
            }
        }

        public bool IsModifierPresent(int unitId, int modifierId, int sourceId, int sourceRefId)
        {
            if (!statRegistry.ContainsKey(unitId)) return false;

            return statRegistry[unitId].IsModiferPresent(modifierId, sourceId, sourceRefId);
        }

        public bool AddModifier(int id, ModifierEffectData modData, int modId, int sourceId, int sourceRefId)
        {
            if (!statRegistry.TryGetValue(id, out var uStats))
                return false;

            if (uStats.IsModiferPresent(modId, sourceId, sourceRefId))
                return false;

            var mod = new StatModifier(modData.Type, modData.Value);
            var activeModifer = new AppliedModifier(mod,modId, sourceId, sourceRefId,modData.GetStatKey());
            uStats.AddModifier(activeModifer);
            return true;

        }

        public bool RemoveModifer(int unitId,int modId,int sourceId,int sourceRefId)
        {
            if (!statRegistry.TryGetValue(unitId, out var stats)) return false;

            if(stats.ActiveMods.TryGetModifer(modId, sourceId, sourceRefId, out var activeModifer))return false;

            stats.RemoveModifier(activeModifer);
            return true;
        }

        public void RemoveModifiersBySource(int unitId, int sourceId, int sourceRefId)
        {
            if (!statRegistry.TryGetValue(unitId, out var stats))return;

            var toRemove = stats.ActiveMods.activeMods
                .Where(m => m.SourceId == sourceId && m.SourceRefId == sourceRefId)
                .ToList();

            foreach (var applied in toRemove)
            {
                stats.RemoveModifier(applied);
            }
        }

        public string PrintCoreStats(int id)
        {
            return $"{statRegistry[id].CoreStatsToString()}";
        }
        public string PrintOtherStats(int id)
        {
            return $"{statRegistry[id].OtherStatsToString()}";
        }

        public UnitStatsData ToUnitStatsData(int unitId)
        {
            if (statRegistry.TryGetValue(unitId, out var unitStats)) return null;

            return unitStats.SaveStats();
        }
    }

    [System.Serializable]
    public class ModifierEffectData
    {
        public StatKey TargetStat;
        public StatType StatType;
        public CoreStats CoreStat;
        public OtherStats OtherStat;
        public ModifierType Type;
        public float Value;

        public StatKey GetStatKey()
        {
            return StatType == StatType.Core
                ? new StatKey(CoreStat)
                : new StatKey(OtherStat);
        }
    }

    [Serializable]
    public class AddStatModifierActionData : ActionData
    {
        public ModifierEffectData Effect;
        public int ModId;
        public TargetType TargetType;
        public override void Execute(TriggerContext context)
        {
            if (context is not EffectContext ctx) return;
            var sm = ctx.battleSystems.StatManager;
            var modId = HashCode.Combine(ctx.SourceRefId, ModId);
            var sourceId = ctx.SourceId;
            var sourceRefId = ctx.SourceRefId;

            var targetId = ctx.battleSystems.ResolveTarget(TargetType, ctx);

            if (sm.IsModifierPresent(targetId, modId, sourceId, sourceRefId)) return;

            sm.AddModifier(targetId, Effect, modId, sourceId, sourceRefId);
            ctx.EffectInstance.modifierTargets.Add(targetId);
            ctx.battleSystems.PrintApplyModifierLog(ctx,Effect);

        }
    }
    [Serializable]
    public class AddStatModifierFromStacksActionData : ActionData
    {
        public ModifierEffectData Effect;
        public int ModId;
        public TargetType TargetType;

        public override void Execute(TriggerContext context)
        {
            if (context is not EffectContext ctx) return;

            var stacks = ctx.EffectInstance.CurrentStackCount;
            if (stacks <= 0) return;

            var scaledEffect = new ModifierEffectData
            {
                StatType = Effect.StatType,
                CoreStat = Effect.CoreStat,
                OtherStat = Effect.OtherStat,
                Type = Effect.Type,
                Value = Effect.Value * stacks
            };

            var sm = ctx.battleSystems.StatManager;
            var targetId = ctx.battleSystems.ResolveTarget(TargetType, ctx);

            var modId = HashCode.Combine(ctx.SourceRefId, ModId);
            if (sm.IsModifierPresent(targetId, modId, ctx.SourceId, ctx.SourceRefId))
                return;

            sm.AddModifier(targetId, scaledEffect, modId, ctx.SourceId, ctx.SourceRefId);
            ctx.EffectInstance.modifierTargets.Add(targetId);
        }
    }

    public class RemoveStatModifierActionData : ActionData
    {
        public int ModifierId;
        public override void Execute(TriggerContext context)
        {
            if (context is not EffectContext ctx)
                return;

            var sm = ctx.battleSystems.StatManager;

            // Must match AddStatModifierAction identity
            int modId = HashCode.Combine(ctx.SourceRefId, ModifierId);
            int sourceId = ctx.SourceId;
            int sourceRefId = ctx.SourceRefId;

            // Default behavior: remove from the same Target the effect was applied to
            int targetId = ctx.TargetUnitId;

            if (!sm.IsModifierPresent(targetId, modId, sourceId, sourceRefId))
                return;

            sm.RemoveModifer(targetId, modId, sourceId, sourceRefId);
        }
    }
}