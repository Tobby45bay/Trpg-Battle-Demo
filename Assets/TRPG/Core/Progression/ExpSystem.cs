using System;
using TRPG.Core.Event;

namespace TRPG.Core.Progression
{
    public sealed class LevelState
    {
        private readonly ILevelCurve _curve;

        public int Level { get; private set; }
        public int CurrentExp { get; private set; }

        public LevelState(int startingLevel, ILevelCurve curve)
        {
            Level = startingLevel;
            _curve = curve ?? throw new ArgumentNullException(nameof(curve));
        }

        public int AddExp(int amount)
        {
            if (amount <= 0)
                return 0;

            if (Level >= _curve.MaxLevel)
                return 0;

            CurrentExp += amount;
            int levelsGained = 0;

            while (Level < _curve.MaxLevel)
            {
                int required = _curve.GetExpRequired(Level);

                if (required <= 0)
                    throw new InvalidOperationException(
                        $"Invalid EXP requirement for level {Level}.");

                if (CurrentExp < required)
                    break;

                CurrentExp -= required;
                Level++;
                levelsGained++;

                if (Level >= _curve.MaxLevel)
                {
                    CurrentExp = 0;
                    break;
                }
            }

            return levelsGained;
        }

        public int ExpToNext => Level < _curve.MaxLevel
        ? _curve.GetExpRequired(Level) : 0;
    }
    public interface ILevelCurve
    {
        int GetExpRequired(int level);
        int MaxLevel { get; }
    }
    public struct ExperienceGainedEvent : IEventContext
    {
        public int UnitId;
        public int Amount;

        public ExperienceGainedEvent(int unitId, int amount)
        {
            if (amount < 0)
                throw new ArgumentException("EXP amount cannot be negative.");

            UnitId = unitId;
            Amount = amount;
        }
    }

    public struct LevelUpEvent : IEventContext
    {
        public int UnitId;
        public int NewLevel;
    }

    public sealed class ExperienceSystem : EventListener<ExperienceGainedEvent>
    {
        private readonly ILevelRepository _repository;
        private readonly EventBus _bus;

        public override int Priority => 0;

        public ExperienceSystem(ILevelRepository repository, EventBus bus)
        {
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));

            _bus = bus
                ?? throw new ArgumentNullException(nameof(bus));
        }

        protected override void HandleTyped(ExperienceGainedEvent ev)
        {
            if (!_repository.TryGet(ev.UnitId, out var levelState))
                return;

            int previousLevel = levelState.Level;
            int gained = levelState.AddExp(ev.Amount);

            for (int i = 1; i <= gained; i++)
            {
                _bus.Enqueue(new LevelUpEvent
                {
                    UnitId = ev.UnitId,
                    NewLevel = previousLevel + i
                });
            }
        }
    }

    public interface ILevelRepository
    {
        bool TryGet(int unitId, out LevelState state);
    }
}
