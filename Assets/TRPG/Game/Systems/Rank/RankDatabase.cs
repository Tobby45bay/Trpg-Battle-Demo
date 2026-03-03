
using System.Collections.Generic;
using TRPG.Game.Systems.Items;
using TRPG.Game.Systems.Magic;

namespace TRPG.Game.Systems.Rank
{
    public enum RankBonusType
    {
        PhysicalAttack,
        MagicAttack,
        PhysicalDefense,
        MagicDefense,
        Hit,
        Avoid,
        EffectChance,
        ElementResistance,
        ApCostReduction,
        MpCostReduction
    }

    public static class RankBonusDatabase
    {
        public static readonly Dictionary<WeaponType, Dictionary<RankTier, RankBonusDefinition>> WeaponBonuses;
        public static readonly Dictionary<ElementType, Dictionary<RankTier, RankBonusDefinition>> MagicBonuses;

        static RankBonusDatabase()
        {
            // Initialize weapon bonuses
            WeaponBonuses = new Dictionary<WeaponType, Dictionary<RankTier, RankBonusDefinition>>
            {
                {
                    WeaponType.Sword,
                    new Dictionary<RankTier, RankBonusDefinition>
                    {
                        { RankTier.E, new(RankTier.E, new() { new(RankBonusType.PhysicalAttack, 1) }) },
                        { RankTier.D, new(RankTier.D, new() { new(RankBonusType.PhysicalAttack, 2), new(RankBonusType.Hit, 3) }) },
                        { RankTier.C, new(RankTier.C, new() { new(RankBonusType.PhysicalAttack, 3), new(RankBonusType.Hit, 5) }) },
                        { RankTier.B, new(RankTier.B, new() { new(RankBonusType.PhysicalAttack, 4), new(RankBonusType.Hit, 7), new(RankBonusType.PhysicalDefense, 2) }) },
                        { RankTier.A, new(RankTier.A, new() { new(RankBonusType.PhysicalAttack, 5), new(RankBonusType.Hit, 10), new(RankBonusType.PhysicalDefense, 3) }) },
                        { RankTier.S, new(RankTier.S, new() { new(RankBonusType.PhysicalAttack, 6), new(RankBonusType.Hit, 12), new(RankBonusType.PhysicalDefense, 4) }) }
                    }
                },
                {
                    WeaponType.Spear,
                    new Dictionary<RankTier, RankBonusDefinition>
                    {
                        { RankTier.E, new(RankTier.E, new() { new(RankBonusType.PhysicalAttack, 1) }) },
                        { RankTier.D, new(RankTier.D, new() { new(RankBonusType.PhysicalAttack, 2), new(RankBonusType.Hit, 4) }) },
                        { RankTier.C, new(RankTier.C, new() { new(RankBonusType.PhysicalAttack, 3), new(RankBonusType.Hit, 6) }) },
                        { RankTier.B, new(RankTier.B, new() { new(RankBonusType.PhysicalAttack, 4), new(RankBonusType.Hit, 8), new(RankBonusType.Avoid, 2) }) },
                        { RankTier.A, new(RankTier.A, new() { new(RankBonusType.PhysicalAttack, 5), new(RankBonusType.Hit, 11), new(RankBonusType.Avoid, 3) }) },
                        { RankTier.S, new(RankTier.S, new() { new(RankBonusType.PhysicalAttack, 6), new(RankBonusType.Hit, 13), new(RankBonusType.Avoid, 4) }) }
                    }
                },
                {
                    WeaponType.Axe,
                    new Dictionary<RankTier, RankBonusDefinition>
                    {
                        { RankTier.E, new(RankTier.E, new() { new(RankBonusType.PhysicalAttack, 2) }) },
                        { RankTier.D, new(RankTier.D, new() { new(RankBonusType.PhysicalAttack, 3), new(RankBonusType.Hit, 1) }) },
                        { RankTier.C, new(RankTier.C, new() { new(RankBonusType.PhysicalAttack, 4), new(RankBonusType.Hit, 0), new(RankBonusType.PhysicalDefense, 1) }) },
                        { RankTier.B, new(RankTier.B, new() { new(RankBonusType.PhysicalAttack, 5), new(RankBonusType.Hit, -1), new(RankBonusType.PhysicalDefense, 2) }) },
                        { RankTier.A, new(RankTier.A, new() { new(RankBonusType.PhysicalAttack, 6), new(RankBonusType.Hit, -2), new(RankBonusType.PhysicalDefense, 3) }) },
                        { RankTier.S, new(RankTier.S, new() { new(RankBonusType.PhysicalAttack, 7), new(RankBonusType.Hit, -3), new(RankBonusType.PhysicalDefense, 4) }) }
                    }
                },
                {
                    WeaponType.Ranged,
                    new Dictionary<RankTier, RankBonusDefinition>
                    {
                        { RankTier.E, new(RankTier.E, new() { new(RankBonusType.Hit, 2) }) },
                        { RankTier.D, new(RankTier.D, new() { new(RankBonusType.Hit, 4), new(RankBonusType.Avoid, 1) }) },
                        { RankTier.C, new(RankTier.C, new() { new(RankBonusType.Hit, 6), new(RankBonusType.Avoid, 2), new(RankBonusType.PhysicalAttack, 1) }) },
                        { RankTier.B, new(RankTier.B, new() { new(RankBonusType.Hit, 8), new(RankBonusType.Avoid, 3), new(RankBonusType.PhysicalAttack, 2) }) },
                        { RankTier.A, new(RankTier.A, new() { new(RankBonusType.Hit, 10), new(RankBonusType.Avoid, 4), new(RankBonusType.PhysicalAttack, 3) }) },
                        { RankTier.S, new(RankTier.S, new() { new(RankBonusType.Hit, 12), new(RankBonusType.Avoid, 5), new(RankBonusType.PhysicalAttack, 4) }) }
                    }
                }
            };

            // Initialize magic bonuses
            MagicBonuses = new Dictionary<ElementType, Dictionary<RankTier, RankBonusDefinition>>
            {
                {
                    ElementType.Fire,
                    new Dictionary<RankTier, RankBonusDefinition>
                    {
                        { RankTier.E, new(RankTier.E, new() { new(RankBonusType.MagicAttack, 1) }) },
                        { RankTier.D, new(RankTier.D, new() { new(RankBonusType.MagicAttack, 2), new(RankBonusType.MpCostReduction, 2) }) },
                        { RankTier.C, new(RankTier.C, new() { new(RankBonusType.MagicAttack, 3), new(RankBonusType.MpCostReduction, 3) }) },
                        { RankTier.B, new(RankTier.B, new() { new(RankBonusType.MagicAttack, 4), new(RankBonusType.MpCostReduction, 4), new(RankBonusType.EffectChance, 5) }) },
                        { RankTier.A, new(RankTier.A, new() { new(RankBonusType.MagicAttack, 5), new(RankBonusType.MpCostReduction, 5), new(RankBonusType.EffectChance, 10) }) },
                        { RankTier.S, new(RankTier.S, new() { new(RankBonusType.MagicAttack, 6), new(RankBonusType.MpCostReduction, 6), new(RankBonusType.EffectChance, 15) }) }
                    }
                },
                {
                    ElementType.Water,
                    new Dictionary<RankTier, RankBonusDefinition>
                    {
                        { RankTier.E, new(RankTier.E, new() { new(RankBonusType.MagicAttack, 1) }) },
                        { RankTier.D, new(RankTier.D, new() { new(RankBonusType.MagicAttack, 2), new(RankBonusType.MpCostReduction, 2) }) },
                        { RankTier.C, new(RankTier.C, new() { new(RankBonusType.MagicAttack, 3), new(RankBonusType.MpCostReduction, 3) }) },
                        { RankTier.B, new(RankTier.B, new() { new(RankBonusType.MagicAttack, 4), new(RankBonusType.MpCostReduction, 4), new(RankBonusType.EffectChance, 5) }) },
                        { RankTier.A, new(RankTier.A, new() { new(RankBonusType.MagicAttack, 5), new(RankBonusType.MpCostReduction, 5), new(RankBonusType.EffectChance, 10) }) },
                        { RankTier.S, new(RankTier.S, new() { new(RankBonusType.MagicAttack, 6), new(RankBonusType.MpCostReduction, 6), new(RankBonusType.EffectChance, 15) }) }
                    }
                },
                {
                    ElementType.Wind,
                    new Dictionary<RankTier, RankBonusDefinition>
                    {
                        { RankTier.E, new(RankTier.E, new() { new(RankBonusType.MagicAttack, 1) }) },
                        { RankTier.D, new(RankTier.D, new() { new(RankBonusType.MagicAttack, 2), new(RankBonusType.Hit, 2) }) },
                        { RankTier.C, new(RankTier.C, new() { new(RankBonusType.MagicAttack, 3), new(RankBonusType.Hit, 4) }) },
                        { RankTier.B, new(RankTier.B, new() { new(RankBonusType.MagicAttack, 4), new(RankBonusType.Hit, 6), new(RankBonusType.Avoid, 2) }) },
                        { RankTier.A, new(RankTier.A, new() { new(RankBonusType.MagicAttack, 5), new(RankBonusType.Hit, 8), new(RankBonusType.Avoid, 3) }) },
                        { RankTier.S, new(RankTier.S, new() { new(RankBonusType.MagicAttack, 6), new(RankBonusType.Hit, 10), new(RankBonusType.Avoid, 4) }) }
                    }
                },
                {
                    ElementType.Earth,
                    new Dictionary<RankTier, RankBonusDefinition>
                    {
                        { RankTier.E, new(RankTier.E, new() { new(RankBonusType.MagicAttack, 1) }) },
                        { RankTier.D, new(RankTier.D, new() { new(RankBonusType.MagicAttack, 2), new(RankBonusType.PhysicalDefense, 1) }) },
                        { RankTier.C, new(RankTier.C, new() { new(RankBonusType.MagicAttack, 3), new(RankBonusType.PhysicalDefense, 2) }) },
                        { RankTier.B, new(RankTier.B, new() { new(RankBonusType.MagicAttack, 4), new(RankBonusType.PhysicalDefense, 3), new(RankBonusType.MagicDefense, 1) }) },
                        { RankTier.A, new(RankTier.A, new() { new(RankBonusType.MagicAttack, 5), new(RankBonusType.PhysicalDefense, 4), new(RankBonusType.MagicDefense, 2) }) },
                        { RankTier.S, new(RankTier.S, new() { new(RankBonusType.MagicAttack, 6), new(RankBonusType.PhysicalDefense, 5), new(RankBonusType.MagicDefense, 3) }) }
                    }
                },
                {
                    ElementType.Light,
                    new Dictionary<RankTier, RankBonusDefinition>
                    {
                        { RankTier.E, new(RankTier.E, new() { new(RankBonusType.MagicAttack, 1) }) },
                        { RankTier.D, new(RankTier.D, new() { new(RankBonusType.MagicAttack, 2), new(RankBonusType.EffectChance, 3) }) },
                        { RankTier.C, new(RankTier.C, new() { new(RankBonusType.MagicAttack, 3), new(RankBonusType.EffectChance, 6) }) },
                        { RankTier.B, new(RankTier.B, new() { new(RankBonusType.MagicAttack, 4), new(RankBonusType.EffectChance, 10), new(RankBonusType.MagicDefense, 2) }) },
                        { RankTier.A, new(RankTier.A, new() { new(RankBonusType.MagicAttack, 5), new(RankBonusType.EffectChance, 15), new(RankBonusType.MagicDefense, 3) }) },
                        { RankTier.S, new(RankTier.S, new() { new(RankBonusType.MagicAttack, 6), new(RankBonusType.EffectChance, 20), new(RankBonusType.MagicDefense, 4) }) }
                    }
                },
                {
                    ElementType.Dark,
                    new Dictionary<RankTier, RankBonusDefinition>
                    {
                        { RankTier.E, new(RankTier.E, new() { new(RankBonusType.MagicAttack, 1) }) },
                        { RankTier.D, new(RankTier.D, new() { new(RankBonusType.MagicAttack, 2), new(RankBonusType.EffectChance, 3) }) },
                        { RankTier.C, new(RankTier.C, new() { new(RankBonusType.MagicAttack, 3), new(RankBonusType.EffectChance, 6) }) },
                        { RankTier.B, new(RankTier.B, new() { new(RankBonusType.MagicAttack, 4), new(RankBonusType.EffectChance, 10), new(RankBonusType.Avoid, 2) }) },
                        { RankTier.A, new(RankTier.A, new() { new(RankBonusType.MagicAttack, 5), new(RankBonusType.EffectChance, 15), new(RankBonusType.Avoid, 3) }) },
                        { RankTier.S, new(RankTier.S, new() { new(RankBonusType.MagicAttack, 6), new(RankBonusType.EffectChance, 20), new(RankBonusType.Avoid, 4) }) }
                    }
                }
            };
        }
    }
}
