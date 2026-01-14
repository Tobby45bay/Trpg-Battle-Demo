using Game.Systems.Logging;
using Game.Systems.Skill;
using Game.Systems.Stat;
using Game.Systems.BattleMap;
using Game.Systems.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using Game.Systems.Tags;
using Game.Core.Battle;
using static Game.Core.Game.GameManager;
using Game.Systems.Trigger;
using static Game.Core.Game.GameManager.GameConst;
using Mono.Cecil.Cil;
namespace Game.Systems.Effect
{
    [Serializable]
    public enum StackRuleType { Add, Transform, Expire }

    [Serializable]
    public class StackRule
    {
        public int stackRequirement;
        public int stackCost;
        public StackRuleType ruleType;
        public int effectId;
    }

    [Serializable]
    public class StackEffectTable
    {
        public List<StackRule> rules;
    }

    public readonly struct EffectKey : IEquatable<EffectKey>
    {
        public readonly int EffectId;
        public readonly int SourceUnitId;
        public readonly SourceType SourceCode;
        public readonly int SourceId;
        public readonly SourceType SourceRefCode;
        public readonly int SourceRefId;

        public EffectKey(
            int effectId,
            int sourceUnitId,
            SourceType sourceCode,
            int sourceId,
            SourceType sourceRefCode,
            int sourceRefId)
        {
            EffectId = effectId;
            SourceUnitId = sourceUnitId;
            SourceCode = sourceCode;
            SourceId = sourceId;
            SourceRefCode = sourceRefCode;
            SourceRefId = sourceRefId;
        }

        public bool Equals(EffectKey other) =>
            EffectId == other.EffectId &&
            SourceUnitId == other.SourceUnitId &&
            SourceCode == other.SourceCode &&
            SourceId == other.SourceId &&
            SourceRefCode == other.SourceRefCode &&
            SourceRefId == other.SourceRefId;

        public override bool Equals(object obj) =>
            obj is EffectKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(
                EffectId,
                SourceUnitId,
                SourceCode,
                SourceId,
                SourceRefCode,
                SourceRefId
            );
    }

    public class EffectInstance
    {
        public EffectKey Key;

        public int CurrentDuration;
        public int TargetUnitId;

        public StackEffectWrapper StackEffect;
        public HashSet<int> modifierTargets = new();

        public EffectInstance(EffectKey key, int targetUnitId)
        {
            Key = key;
            TargetUnitId = targetUnitId;
        }

        public EffectSO GetEffectData(EffectManager em)
            => em.GetEffectData(Key.EffectId);

        public bool IsPermanent => CurrentDuration == -1;
        public int CurrentStackCount;
    }

    public class StackEffectWrapper
    {
        public int currentStack;
        public int currentEffectId;
        public List<EffectInstance> stackedEffects = new();
    }

    public class EffectList
    {
        private readonly Dictionary<EffectKey, EffectInstance> effects = new();

        public void AddEffect(EffectInstance effect)
        {
            effects.Add(effect.Key, effect);
        }

        public EffectInstance GetEffect(EffectKey key)
        {
            effects.TryGetValue(key, out var effect);
            return effect;
        }

        public void RemoveEffect(EffectKey key, BattleSystems bs)
        {
            if (!effects.TryGetValue(key, out var effect))
                return;

            // EffectRemoved trigger
            var effectData = bs.EffectManager.GetEffectData(key.EffectId);

            var effectCtx = new EffectContext
            {
                battleSystems = bs,
                triggerCall = new TriggerKey(CoreTrigger.EffectRemoved),

                SourceUnitId = key.SourceUnitId,
                TargetUnitId = effect.TargetUnitId,

                SourceCode = key.SourceCode,
                SourceId = key.SourceId,

                SourceRefCode = SourceType.Effect,
                SourceRefId = key.EffectId,

                EffectInstance = effect
            };

            foreach (var tg in effectData.Triggers)
            {
                if (tg.TriggerCall.Equals(effectCtx.triggerCall))
                    ActionHelper.ExecuteActions(tg.Actions, effectCtx);
            }

            // Remove stat modifiers
            foreach (var unit in effect.modifierTargets)
            {
                bs.StatManager.RemoveModifiersBySource(
                    unit,
                    key.SourceId,
                    key.SourceRefId
                );
            }

            effects.Remove(key);
        }

        public void RemoveBySource(SourceType sourceCode, int sourceId, BattleSystems bs)
        {
            // Snapshot to avoid modifying during iteration
            var toRemove = effects
                .Where(kvp =>
                    kvp.Key.SourceCode == sourceCode &&
                    kvp.Key.SourceId == sourceId
                )
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                RemoveEffect(key, bs);
            }
        }

        public void RemoveByUnit(int sourceUnitId, BattleSystems bs)
        {
            // Snapshot keys to avoid modifying during iteration
            var toRemove = effects
                .Where(kvp => kvp.Key.SourceUnitId == sourceUnitId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                RemoveEffect(key, bs);
            }
        }

        public void RemoveBySourceAndEffect(SourceType sourceCode,int sourceId,int effectId,BattleSystems bs)
        {
            var keys = effects.Keys
                .Where(k =>
                    k.SourceCode == sourceCode &&
                    k.SourceId == sourceId &&
                    k.EffectId == effectId)
                .ToList();

            foreach (var key in keys)
                RemoveEffect(key, bs);
        }

        public void Tick(BattleSystems bs)
        {
            var expired = effects.Values
                .Where(e => e.CurrentDuration == 0)
                .Select(e => e.Key)
                .ToList();

            foreach (var key in expired)
            {
                RemoveEffect(key, bs);
            }

            foreach (var effect in effects.Values)
            {
                if (effect.CurrentDuration > 0)
                    effect.CurrentDuration--;
            }
        }

        public List<EffectInstance> GetActiveEffects()
        {
            return effects.Values.ToList();
        }

        public IEnumerable<EffectInstance> AllEffects => effects.Values;
    }

    public class EffectListener : TriggerListener
    {
        private EffectManager effectManager;
        public EffectListener(EffectManager effectManager)
        {
            this.priority = 1;
            this.effectManager = effectManager;
        }

        public override void Trigger(TriggerContext context)
        {
            if (context is not BattleContext ctx)
                return;

            var triggerOwner = ctx.SourceUnitId;

            var activeEffectList = effectManager.GetAppliedEffects(triggerOwner);

            if (activeEffectList == null) return;

            foreach (var effect in activeEffectList)
            {
                CheckEffect(effect, ctx.triggerCall, ctx);
            }
        }

        private void CheckEffect(EffectInstance effectInstance, TriggerKey triggerCall, BattleContext baseCtx)
        {
            var effectData = effectManager.GetEffectData(effectInstance.Key.EffectId);
            foreach (var tg in effectData.Triggers)
            {
                if (!tg.TriggerCall.Equals(triggerCall))
                    continue;

                var effectCtx = new EffectContext
                {
                    battleSystems = baseCtx.battleSystems,
                    triggerCall = triggerCall,

                    SourceUnitId = effectInstance.Key.SourceUnitId,
                    TargetUnitId = effectInstance.TargetUnitId,

                    SourceCode = effectInstance.Key.SourceCode,
                    SourceId = effectInstance.Key.SourceId,

                    SourceRefCode = SourceType.Effect,
                    SourceRefId = effectData.EffectId,

                    EffectInstance = effectInstance
                };

                ActionHelper.ExecuteActions(tg.Actions, effectCtx);
            }
        }
    }

    public class EffectManager
    {
        public EffectDatabase efffectDb;
        private Dictionary<int, EffectList> effectRegistry = new();

        public void InitializeSystem(EffectDatabase effectDatabase, TriggerDispatcher dispatcher)
        {
            efffectDb = effectDatabase;
            dispatcher.Register(new EffectListener(this));
        }

        public void RegisterUnit(int unitId, EffectList unitEffectss)
        {
            effectRegistry[unitId] = unitEffectss;
        }

        public void AddEffect(int unitId,EffectInstance effect)
        {
            if (!effectRegistry.ContainsKey(unitId)) return;

            effectRegistry[unitId].AddEffect(effect);
        }

        public void RemoveEffect(int unitId, EffectKey key, BattleSystems bs)
        {
            if (!effectRegistry.TryGetValue(unitId, out var list))
                return;

            list.RemoveEffect(key, bs);
        }

        public void RemoveBySource(int unitId, SourceType code, int sourceId, BattleSystems bs)
        {
            if (!effectRegistry.TryGetValue(unitId, out var list))
                return;

            list.RemoveBySource(code, sourceId, bs);
        }

        public void RemoveByUnit(int unitId, int sourceUnitId, BattleSystems bs)
        {
            if (!effectRegistry.TryGetValue(unitId, out var list))
                return;

            list.RemoveByUnit(sourceUnitId, bs);
        }

        public EffectInstance GetEffect(int unitId,EffectKey effectKey)
        {
            if(!effectRegistry.ContainsKey(unitId)) return null;

            var list = effectRegistry[unitId];

            return list.GetEffect(effectKey);
        }

        public List<EffectInstance> GetAppliedEffects(int unitId)
        {
            if (!effectRegistry.TryGetValue(unitId, out var list))
                return null;

            return list.GetActiveEffects();
        }

        public EffectInstance CreateEffectInstance(EffectKey key, int targetUnitId)
        {
            return new EffectInstance(key, targetUnitId);
        }

        public void TickUnit(int unitId,BattleSystems bs)
        {
            if (!effectRegistry.TryGetValue(unitId, out var list))
                return;
            list.Tick(bs);
        }

        public EffectSO GetEffectData(int effectId)
        {
            return efffectDb.GetEffect(effectId);
        }

    }

    public class EffectContext : TriggerContext
    {
        public int SourceUnitId;
        public int TargetUnitId;

        public SourceType SourceCode;
        public int SourceId;

        public SourceType SourceRefCode; 
        public int SourceRefId; 

        public EffectInstance EffectInstance;
    }

    [Serializable]
    public class ApplyEffectActionData : ActionData
    {
        public int EffectId;
        public TargetType TargetType;
        public int MaxDuration;
        public bool CanRefresh;

        public override void Execute(TriggerContext context)
        {
            if (context is not EffectContext ctx) return;
            var em = ctx.battleSystems.EffectManager;
            int finalTarget = ctx.battleSystems.ResolveTarget(TargetType, ctx);
            var key = new EffectKey(
                EffectId,
                ctx.SourceUnitId,
                ctx.SourceCode,
                ctx.SourceId,
                ctx.SourceRefCode,
                ctx.SourceRefId);

            var effect = em.GetEffect(finalTarget, key);

            if (effect == null)
            {
                effect = em.CreateEffectInstance(key, finalTarget);
                effect.CurrentDuration = MaxDuration == 0 ? -1 : MaxDuration;

                em.AddEffect(finalTarget, effect);

                var effectData = effect.GetEffectData(em);

                var effectCtx = new EffectContext
                {
                    battleSystems = ctx.battleSystems,
                    triggerCall = new TriggerKey(CoreTrigger.EffectApplied),

                    SourceUnitId = effect.Key.SourceUnitId,
                    TargetUnitId = finalTarget,

                    SourceCode = effect.Key.SourceCode,
                    SourceId = effect.Key.SourceId,

                    SourceRefCode = SourceType.Effect,
                    SourceRefId = effect.Key.EffectId,

                    EffectInstance = effect
                };

                TriggerEffectApplied(effectData.Triggers, effectCtx);
                BattleManager.Instance.battleSystems.PrintApplyEffectLog(effectCtx);
            }
            else
            {
                if (CanRefresh) effect.CurrentDuration = MaxDuration;
            }
        }

        private void TriggerEffectApplied(List<TriggerGroup> effectTriggers, EffectContext ctxEffect)
        {
            foreach (var tg in effectTriggers)
            {
                if (tg.TriggerCall.Equals(new TriggerKey(CoreTrigger.EffectApplied)))
                    ActionHelper.ExecuteActions(tg.Actions, ctxEffect);
            }
        }
    }

    [Serializable]
    public class RemoveEffectActionData : ActionData
    {
        public int EffectId;
        public TargetType Target;

        // Optional filters
        public SourceType? SourceCode;
        public bool UseSourceId;
        public bool UseSourceRefId;

        public override void Execute(TriggerContext context)
        {
            if (context is not EffectContext ctx)
                return;

            int targetUnitId = Target == TargetType.Self
                ? ctx.SourceUnitId
                : ctx.TargetUnitId;

            var em = ctx.battleSystems.EffectManager;
            var effects = em.GetAppliedEffects(targetUnitId);
            if (effects == null || effects.Count == 0)
                return;

            // Snapshot matching effect keys
            var keysToRemove = effects
                .Where(e =>
                    e.Key.EffectId == EffectId &&
                    (!SourceCode.HasValue || e.Key.SourceCode == SourceCode.Value) &&
                    (!UseSourceId || e.Key.SourceId == ctx.SourceId) &&
                    (!UseSourceRefId || e.Key.SourceRefId == ctx.SourceRefId)
                )
                .Select(e => e.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                em.RemoveEffect(
                    targetUnitId,
                    key,
                    ctx.battleSystems
                );
            }
        }
    }
}

