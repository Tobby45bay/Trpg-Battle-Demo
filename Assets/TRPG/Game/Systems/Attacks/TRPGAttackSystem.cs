

namespace TRPG.Game.Systems.Attacks
{

    public sealed class AttackData
    {  
        public int CostGrowth;         // e.g., 15 per strike
        public int BaseAttackCost { get;} = 80; // e.g., 100
        public float AttackFactor { get;} = 1f; // optional multiplier (0.1f etc)

        // --- Strike Behavior ---
        public int MaxStrikes { get;} = 1;
        public bool CanCrit { get;} = true;

        // --- Combat Modifiers ---
        public float DamageMultiplier { get;} = 1f;
        public int HitBonus { get;} = 0;
        public int CritBonus { get;} = 0;

        // --- Optional: Chain Rules ---
        public bool StopChainOnMiss { get; } = false;
    }

}