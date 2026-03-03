
using System;
using System.Collections.Generic;
using System.Linq;
using TRPG.Game.Unit;

namespace TRPG.Game.TacticalBattle
{
    public struct CombatActorState
    {
        public TRPGUnit Unit;
        public int NextActionTime;
        public int StrikeCount;
        public int MaxStrikes;
        public int AttackCost;

        public bool CanAct => Unit.Health.IsAlive;
    }


    public sealed class TimelineActor
    {
        public TRPGUnit Unit { get; }
        public int NextActionTime { get; set; }
        public int ActionCount { get; private set; }

        private readonly Func<TRPGUnit, TRPGUnit, int> _attackCostProvider;

        public TimelineActor(
            TRPGUnit unit,
            int initiativeSeed,
            Func<TRPGUnit, TRPGUnit, int> attackCostProvider)
        {
            Unit = unit;
            NextActionTime = initiativeSeed;
            _attackCostProvider = attackCostProvider;
            ActionCount = 0;
        }

        public int GetAttackCost(TRPGUnit opponent)
            => _attackCostProvider(Unit, opponent);

        public void Commit(TRPGUnit opponent)
        {
            NextActionTime += GetAttackCost(opponent);
            ActionCount++;
        }
    }

    public sealed class CombatTimeline
    {
        private readonly List<TimelineActor> _actors;
        public IReadOnlyList<TimelineActor> Actors => _actors;

        public CombatTimeline(IEnumerable<TimelineActor> actors)
        {
            _actors = actors.ToList();
        }

        public bool HasNextAction()
        {
            return _actors.Any(a => a.Unit.Health.IsAlive);
        }

        public TimelineActor GetNextActor()
        {
            return _actors
                .Where(a => a.Unit.Health.IsAlive)
                .OrderBy(a => a.NextActionTime)
                .First();
        }

        public TimelineActor GetOpponentOf(TimelineActor actor)
        {
            return _actors.First(a => a != actor && a.Unit.Health.IsAlive);
        }
    }
}




