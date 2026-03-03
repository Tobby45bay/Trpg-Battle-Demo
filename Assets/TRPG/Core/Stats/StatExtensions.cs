using System;
using System.Collections.Generic;
using TRPG.Core.Stats;

namespace TRPG.Core.Stats
{
    public static class StatCollectionExtensions
    {
        /// <summary>
        /// Safely adds a modifier to a stat.
        /// If the stat does not exist, it will be added with an optional base value.
        /// </summary>
        /// <param name="stats">The stat collection.</param>
        /// <param name="key">The stat key.</param>
        /// <param name="modifier">The modifier to add.</param>
        /// <param name="defaultBase">Base value to use if the stat does not exist yet.</param>
        public static void AddModifierSafe(this StatCollection stats, StatKey key, StatModifier modifier, float defaultBase = 0f)
        {
            if (!stats.Has(key))
            {
                stats.Add(key, defaultBase);
            }

            stats.Get(key).AddModifier(modifier);
        }

        /// <summary>
        /// Safely adds multiple modifiers to a stat.
        /// If the stat does not exist, it will be added with an optional base value.
        /// </summary>
        /// <param name="stats">The stat collection.</param>
        /// <param name="key">The stat key.</param>
        /// <param name="modifiers">The modifiers to add.</param>
        /// <param name="defaultBase">Base value to use if the stat does not exist yet.</param>
        public static void AddModifiersSafe(this StatCollection stats, StatKey key, IEnumerable<StatModifier> modifiers, float defaultBase = 0f)
        {
            if (!stats.Has(key))
            {
                stats.Add(key, defaultBase);
            }

            var stat = stats.Get(key);

            foreach (var modifier in modifiers)
            {
                if (modifier != null)
                    stat.AddModifier(modifier);
            }
        }

        /// <summary>
        /// Safely removes all modifiers from a stat by a given source.
        /// If the stat does not exist, nothing happens.
        /// </summary>
        public static void RemoveModifiersBySourceSafe(this StatCollection stats, StatKey key, object source)
        {
            if (stats.Has(key))
            {
                stats.Get(key).RemoveModifiersBySource(source);
            }
        }
    }
}
