using Game.Systems.Stat;
using Game.Systems.Trigger;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Game.Core.Game.GameManager;

namespace Game.Systems.Effect
{
    [CreateAssetMenu(menuName = "Effect Templates/Status/Stat Per Stack")]
    public class StatusStatPerStackTemplate : EffectTemplate
    {
        public ModifierEffectData modifier;
        public TargetType TargetType;

        public override void Build(EffectSO effect)
        {
            var tg = TriggerGroupExtensions.GetOrCreate(effect.Triggers, new TriggerKey(CoreTrigger.EffectApplied));

            tg.Actions.Add(new AddStatModifierFromStacksActionData
            {
                Effect = modifier,
                ModId = 0,
                TargetType = TargetType
            });
        }
    }
}

