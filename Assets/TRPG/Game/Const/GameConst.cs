using Game.Systems.Tags;
using TRPG.Core.Effects;
using TRPG.Core.Event;
using TRPG.Core.Stats;
using TRPG.Game.TacticalBattle;
using TRPG.Game.Unit;


namespace TRPG.Game.Const 
{
    public static class GameConst
    {
        public static class Id
        {
            public const int UnitStart = 100;
            public const int ItemStart = 300;
        }

        public static class Other
        {
            public const int NullUnitId = -1;
        }

        public static class Skills
        {
            public const int MaxSkillEquipped = 5;
        }

        public static class Combat
        {
            public const float SpeedFactor = 0.5f; // Each speed point adds 0.5% to hit or avoid
            public const float LuckFactor = 0.3f;  // Each luck point adds 0.3% to hit
            public const float CritMultiplier = 1.5f;
            public const float EffectiveMultiplier = 1.3f;

        }

        public static class Magic
        {
            public const float LowElementAffinityFactor = 1.3f;
            public const float HighElementAffinityFactor = 1.5f;
        }

        public static class EffectConts
        {
            public const int InfiniteDuration = -1;
            public const int ListenerPriority = 50;
        }

        public static class Inventory
        {
            public const int MaxItemsPerUnit = 5;
        }

        public static class StatKeys
        {
            // Core level-up stats
            public static readonly StatKey Vitality = new(0, "Vitality");
            public static readonly StatKey Might = new(1, "Might");
            public static readonly StatKey Finesse = new(2, "Finesse");
            public static readonly StatKey Guard = new(3, "Guard");
            public static readonly StatKey Focus = new(4, "Focus");
            public static readonly StatKey Spirit = new(5, "Spirit");

            // Derived stats
            public static readonly StatKey HP = new(10, "HP");
            public static readonly StatKey PhysicalAttack = new(11, "PhysicalAttack");
            public static readonly StatKey MagicAttack = new(12, "MagicAttack");
            public static readonly StatKey PhysicalDefense = new(13, "PhysicalDefense");
            public static readonly StatKey MagicDefense = new(14, "MagicDefense");
            public static readonly StatKey Speed = new(15, "Speed");
            public static readonly StatKey Hit = new(16, "Hit");
            public static readonly StatKey Avoid = new(17, "Avoid");
            public static readonly StatKey PhysicalCapacity = new(18, "PhysicalCapacity");
            public static readonly StatKey MagicCapacity = new(19, "MagicCapacity");
            public static readonly StatKey CritChance = new(20, "CritChance");
            public static readonly StatKey CritDamage = new(21, "CritDamage");
        }

        public static class Stats
        {
            public const int MaxLevel = 99;
            public const int LuckCap = 45;
            public const int NULLStat = -1;
        }

        public static class GameTags
        {
            public static readonly TagKey Fire = new(0, "Fire");
            public static readonly TagKey Water = new(1, "Water");
            public static readonly TagKey Wind = new(2, "Wind");
            public static readonly TagKey Earth = new(3, "Earth");

            public static readonly TagKey Light = new(10, "Light");
            public static readonly TagKey Dark = new(11, "Dark");
            public static readonly TagKey Corrupt = new(12, "Corrupt");

            public static readonly TagKey Physical = new(20, "Physical");
            public static readonly TagKey Magical = new(21, "Magical");

        }
    }
}

namespace TRPG.Game.Const
{

    public abstract class GameEvent : IEventContext
    {
        public abstract EventKey EventKey { get; }
        public TRPGUnit SourceUnit { get; }
        public TRPGUnit TargetUnit { get; }
        public BattleManager BattleManager { get; }

        protected GameEvent(TRPGUnit source, TRPGUnit target)
        {
            SourceUnit = source;
            TargetUnit = target;
        }
    }
}
