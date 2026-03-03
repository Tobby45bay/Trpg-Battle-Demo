using System;
using System.Collections;
using System.Collections.Generic;
using TRPG.Core.Effects;
using TRPG.Core.Stats;
using UnityEngine;

namespace TRPG.Game.Systems.Stats.Rules
{
    public sealed class DynamicCapRule : IModifierRule
    {
        private readonly Func<IEffectContext, float> capFunc;
        public DynamicCapRule(Func<IEffectContext, float> capFunc) => this.capFunc = capFunc;

        public void Apply(ref float value, ref float percentAdd, float m)
        {
            value = Math.Min(value, capFunc.Invoke(null)); // pass context if available
        }
    }

}
