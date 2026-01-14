using Game.Core.Battle;
using Game.Systems.Tags;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Game.Core.Game.GameManager;

namespace Game.Systems.Trigger
{
    [Serializable]
    public enum CoreTrigger
    {
        // Battle flow
        BattleStart,
        PhaseStart,
        PhaseEnd,

        // Turn flow
        UnitTurnStart,
        UnitTurnEnd,
        TurnStart,
        TurnEnd,

        // Unit Actions
        UnitMoved,
        UnitWait,
        UnitItemUse,
        UnitDefeated,

        // Combat
        CombatStart,
        Attack,
        Defend,
        Hit,
        Attacked,
        Missed,
        Avoid,
        Critical,
        FollowUp,
        CombatEnd,

        //EffectInstance
        EffectApplied,
        EffectRemoved,

        //Tile
        UnitEnter,
        UnitExit,
        UnitInteract
    }

    [Serializable]
    public struct TriggerKey : IEquatable<TriggerKey>
    {
        public bool useCustom;      // Toggle: false = use CoreTrigger, true = use custom string
        public CoreTrigger core;
        public string custom;

        public TriggerKey(CoreTrigger core)
        {
            useCustom = false;
            this.core = core;
            custom = null;
        }

        public TriggerKey(string custom)
        {
            useCustom = true;
            core = default;
            this.custom = custom;
        }

        public override readonly string ToString() => useCustom ? custom : core.ToString();

        public readonly bool Equals(TriggerKey other) =>
            core == other.core && core == other.core;

        public override readonly int GetHashCode() =>
            HashCode.Combine(core, custom);
    }

    public abstract class TriggerContext
    {
        public TriggerKey triggerCall;
        public BattleSystems battleSystems;
    }

    [Serializable]
    public abstract class ActionData
    {
        public string ActionName;

        public abstract void Execute(TriggerContext context);
    }

    [Serializable]
    public class TriggerGroup
    {
        public TriggerKey TriggerCall;

        [SerializeReference]
        public List<ActionData> Actions = new();
    }

    public static class TriggerGroupExtensions
    {
        public static bool DoseTriggerGroupExist(List<TriggerGroup> triggerGroups,TriggerKey triggerCall)
        {
            foreach (TriggerGroup tg in triggerGroups) 
            {
                if(tg.TriggerCall.Equals(triggerCall)) return true;
            }
            return false;
        }

        public static TriggerGroup GetTriggerGroup(List<TriggerGroup> triggerGroups, TriggerKey triggerCall)
        {
            foreach (TriggerGroup tg in triggerGroups)
            {
                if( tg.TriggerCall.Equals(triggerCall)) return tg;
            }
            return null;
        }

        public static TriggerGroup GetOrCreate(List<TriggerGroup> triggerGroups, TriggerKey triggerCall)
        {
            foreach (TriggerGroup tg in triggerGroups)
            {
                if (tg.TriggerCall.Equals(triggerCall)) return tg;
            }
            return new TriggerGroup() { TriggerCall = triggerCall };
        }
    }

    public static class ActionHelper
    {
        public static void ExecuteActions(List<ActionData> actions, TriggerContext context)
        {
            foreach (ActionData actionData in actions)
            {
                if (actionData == null) continue;
                actionData.Execute(context);
            }
        }

    }

    public abstract class TriggerListener
    {
        public string system { get; protected set; }
        public int priority { get; protected set; }

        public abstract void Trigger(TriggerContext context);
    }

    public class TriggerDispatcher
    {
        private List<TriggerListener> listeners = new();
        public void Register(TriggerListener listener)
        {
            listeners.Add(listener);
            listeners = listeners.OrderByDescending(l => l.priority).ToList();
        }

        public void Trigger(TriggerContext context)
        {
            foreach (var listener in listeners)
            {
                listener.Trigger(context);
            }
        }
    }
}
