using System;
using System.Collections;
using System.Collections.Generic;
using TRPG.Core.Effects;
using UnityEngine;

namespace TRPG.Core.Stats.Rules
{
    public static class DefaultModifierType
    {
        public static readonly ModifierTypeKey Flat = new(0, "Flat", 0);
        public static readonly ModifierTypeKey Percent = new(1, "Percent", 1);
        public static readonly ModifierTypeKey PercentAdd = new(2, "PercentAdd", 2);
        public static readonly ModifierTypeKey Cap = new(3, "Cap", 3);

        public static readonly ModifierTypeKey[] Default = { Flat, Percent, PercentAdd, Cap, };
    }


    /// <summary>
    /// Adds a flat value directly to the stat.
    /// Example: +5 Attack.
    /// Applied last.
    /// </summary>
    public sealed class FlatRule : IModifierRule
    {
        public void Apply(ref float value, ref float percentAdd, float m)
        {
            value += m;
        }
    }

    /// <summary>
    /// Multiplies the current stat value immediately.
    /// Example: +20% => value *= 1.2.
    /// Applied before additive percentages.
    /// </summary>
    public sealed class PercentRule : IModifierRule
    {
        public void Apply(ref float value, ref float percentAdd, float m)
        {
            value *= 1f + m;
        }
    }

    /// <summary>
    /// Adds to a shared percentage pool that is applied once after all modifiers.
    /// Example: +10% and +20% become a single +30% multiplier.
    /// </summary>
    public sealed class PercentAddRule : IModifierRule
    {
        public void Apply(ref float value, ref float percentAdd, float m)
        {
            percentAdd += m;
        }
    }

    /// <summary>
    /// Caps the stat to a maximum value.
    /// Example: Speed cannot exceed 30.
    /// Applied first.
    /// </summary>
    public sealed class CapRule : IModifierRule
    {
        public void Apply(ref float value, ref float percentAdd, float m)
        {
            value = Math.Min(value, m);
        }
    }
}
