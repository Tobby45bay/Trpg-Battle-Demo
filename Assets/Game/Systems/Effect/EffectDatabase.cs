using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Systems.Effect
{
    [CreateAssetMenu(menuName = "EffectInstance System/Database/EffectInstance Database")]
    public class EffectDatabase : ScriptableObject
    {
        public List<EffectSO> effects;

        private Dictionary<int, EffectSO> lookup;

        public void Initialize()
        {
            lookup = new Dictionary<int, EffectSO>();

            foreach (var effect in effects)
            {
                if (lookup.ContainsKey(effect.EffectId))
                {
                    Debug.LogError($"Duplicate EffectId {effect.EffectId}");
                    continue;
                }

                BuildEffect(effect);
                lookup.Add(effect.EffectId, effect);
            }
        }

        private void BuildEffect(EffectSO effect)
        {
            effect.Triggers.Clear();

            foreach (var template in effect.Templates)
            {
                if (template == null) continue;
                template.Build(effect);
            }
        }

        public EffectSO GetEffect(int effectId)
        {
            if (lookup == null)
                Initialize();

            lookup.TryGetValue(effectId, out var effect);
            return effect;
        }
    }
}

