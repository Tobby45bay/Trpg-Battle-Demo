using Game.Core.Battle;
using Game.Systems.Effect;
using Game.Systems.Stat;
using Game.Systems.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Logging
{
    public interface ILog
    {
        void Log(string message);
    }

    public abstract class EventData
    {
        public string EventName { get; set; }
        public abstract string CreateLog(int turnCount);
    }

    public class AddStatModifierEventData : EventData
    {
        public AddStatModifierActionData modifierActionData { get; set; }
        public EffectContext effectContext { get; set; }

        public AddStatModifierEventData(AddStatModifierActionData modifierActionData, EffectContext effectContext)
        {
            this.modifierActionData = modifierActionData;
            this.effectContext = effectContext;
        }

        public override string CreateLog(int turnCount)
        {
            return 
                $"-Turn:{turnCount}";
        }

        
    }

    public class ApplyEffectEventData : EventData
    {
        public ApplyEffectActionData actionData { get; set; }
        public EffectContext effectContext;

        public ApplyEffectEventData(ApplyEffectActionData data, EffectContext context)
        {
            actionData = data;
            effectContext = context;
        }

        public override string CreateLog(int turnCount)
        {
            return "";
        }
    }

    public class LogEventData : EventData
    {
        public string Log { get; set; }
        public LogEventData(string log)
        {
            EventName = "log";
            Log = log;
        }

        public override string CreateLog(int turnCount)
        {
            return $"-Turn:{turnCount} {Log}";
        }
    }

    public class CreateUnitLog : EventData
    {
        public UnitInstance UnitInstance { get; set; }

        public CreateUnitLog(UnitInstance unitInstance)
        {
            EventName = "AddEffect unit";
            UnitInstance = unitInstance;
        }
        public override string CreateLog(int turnCount)
        {
            return $"-Turn:{turnCount} Unit: {UnitInstance.unitName} was Created \n" +
                UnitInstance.statsData.ToString();
        }
    }

    public class BattleEvent
    {
        public float Timestamp { get; private set; }
        public int TurnCount { get; private set; }
        public EventData Event { get; private set; }
        public string Log { get; private set; }

        public BattleEvent(int turnCount, EventData eventData)
        {
            Timestamp = Time.time;
            TurnCount = turnCount;
            Event = eventData;
            Log = Event.CreateLog(turnCount);
        }

        public void PrintLog()
        {
            Debug.Log(Log);
        }
    }

    public class BattleLogger : ILog
    {
        private readonly List<BattleEvent> events = new();

        public void Log(BattleEvent battleEvent)
        {
            events.Add(battleEvent);
            battleEvent.PrintLog();
        }

        public void PrintLog()
        {
            foreach(var e in events)
                e.PrintLog();
        }

        void ILog.Log(string message)
        {
            throw new NotImplementedException();
        }
    }

}
