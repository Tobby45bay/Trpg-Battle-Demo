using Game.Systems.Stat;
using Game.Systems.Trigger;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Game.Core.Game.GameManager;

namespace Game.Systems.Effect
{
    [CreateAssetMenu(menuName = "Effect Templates/Buff")]
    public class BuffTemplate : EffectTemplate
    {
        public List<ModifierEffectData> modifiers;

        public override void Build(EffectSO effect)
        {
            var tg = TriggerGroupExtensions.GetOrCreate(effect.Triggers, new TriggerKey(CoreTrigger.EffectApplied));

            int modIndex = 0;

            foreach (var mod in modifiers)
            {
                tg.Actions.Add(new AddStatModifierActionData
                {
                    Effect = mod,
                    ModId = modIndex++,
                    TargetType = TargetType.Target
                });
            }
        }
    }
}
