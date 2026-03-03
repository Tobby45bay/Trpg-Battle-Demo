

using System;
using System.Collections.Generic;
using TRPG.Core.Event;
using TRPG.Core.Utility;
using TRPG.Game.Const;
using TRPG.Game.Systems.Attacks;
using TRPG.Game.Systems.TRPGEfect;
using TRPG.Game.Unit;
using UnityEditor.Experimental.GraphView;

namespace TRPG.Game.TacticalBattle
{
    public enum CombatStat
    {
        Hit,
        Crit,
        Damage,
        Defense,
        Avoid,
        Initiative,
        TempoRefresh,
        AttackCost
    }

    //This will be moved to Combat events script
    public sealed class CombatContext
    {
        public readonly TRPGUnit Attacker;
        public readonly TRPGUnit Defender;

        public readonly int CombatTimeLimit;

        public CombatContext(TRPGUnit attacker, TRPGUnit defender, int combatTimeLimit = 200)
        {
            Attacker = attacker;
            Defender = defender;
            CombatTimeLimit = combatTimeLimit;
        }
    }

    public sealed class StrikeContext
    {
        public TRPGUnit Attacker;
        public TRPGUnit Defender;

        public int StrikeIndex; // 1, 2, 3, 4

        public int Attack;
        public int Defense;
        public int Accuracy;
        public int CritChance;

        public bool IsPreview;
    }

    internal sealed class CombatState
    {
        private const int BaseAttackCost = 100;

        public int AttackerHP;
        public int DefenderHP;

        public int AttackerTempo;
        public int DefenderTempo;

        public int AttackerAttackCost;
        public int DefenderAttackCost;

        public int AttackerStrikeCount;
        public int DefenderStrikeCount;

        public int ElapsedTime;

        public CombatState(CombatContext context)
        {
            AttackerHP = context.Attacker.Health.CurrentHP;
            DefenderHP = context.Defender.Health.CurrentHP;

            AttackerTempo = 0;
            DefenderTempo = 0;

            AttackerAttackCost = BaseAttackCost + GetWeightPenalty(context.Attacker);
            DefenderAttackCost = BaseAttackCost + GetWeightPenalty(context.Defender);

            AttackerStrikeCount = 0;
            DefenderStrikeCount = 0;

            ElapsedTime = 0;
        }

        private int GetWeightPenalty(TRPGUnit unit)
        {
            /// place holder
            /// get weapon weight from equiped weapon
            /// when item and weapon system implemented

            return 0;
        }
    }

    public sealed class StrikeResult
    {
        public bool AttackerIsSource;

        public int HitRoll;
        public int CritRoll;

        public bool DidHit;
        public bool DidCrit;

        public int Damage;
        public int TargetRemainingHP;
    }

    public sealed class CombatResult
    {
        public List<StrikeResult> Strikes = new();
    }

    public static class CombatFormulas
    {
        public static int CalculateHit(int accuracy, int avoid)
            => Math.Clamp(accuracy - avoid, 0, 100);

        public static int CalculateCrit(int crit, int critResist)
            => Math.Clamp(crit - critResist, 0, 100);

        public static int CalculateDamage(int attack, int defense)
            => Math.Max(attack - defense, 0);
    }

    public sealed class StrikeData
    {
        public int Hit;
        public int Avoid;

        public int Attack;
        public int Defense;

        public int Crit;
        public int CritResist;

        public StrikeData(TRPGUnit source, TRPGUnit target)
        {
            //place holders
            Hit = (int)source.Stats.GetValue(GameConst.StatKeys.Hit);
            Avoid = (int)target.Stats.GetValue(GameConst.StatKeys.Avoid);

            Attack = 0;//from unit attackLoadoutComponet getSelectedAttackDamage
            Defense = 0; 

            Crit = 0;
            CritResist = 0;
        }
    }

    public interface IPreStrikeModifier
    {
        void Modify(StrikeData data, TRPGUnit source, TRPGUnit target);
    }

    public sealed class CombatResolver
    {
        private const int MaxStrikes = 4;
        private const int AttackCostIncrease = 25;
        private readonly EventBus _eventbus;

        public CombatResolver(EventBus eventbus)
        {
            _eventbus = eventbus;
        }

        public CombatResult Simulate(CombatContext context, int seed)
        {
            var random = new Random(seed);
            var state = new CombatState(context);
            var result = new CombatResult();

            // CombatDeclaredEvent


            while (state.ElapsedTime < context.CombatTimeLimit &&
                   state.AttackerHP > 0 &&
                   state.DefenderHP > 0)
            {
                state.ElapsedTime++;

                AdvanceTempo(context, state);

                TryStrike(context, state, result, random, true);
                TryStrike(context, state, result, random, false);
            }

            return result;
        }

        private void AdvanceTempo(CombatContext context, CombatState state)
        {
            state.AttackerTempo += (int)context.Attacker.Stats.GetValue(GameConst.StatKeys.Speed);
            state.DefenderTempo += (int)context.Defender.Stats.GetValue(GameConst.StatKeys.Speed);
        }

        private void TryStrike(
            CombatContext context,
            CombatState state,
            CombatResult result,
            Random random,
            bool attackerTurn)
        {
            if (attackerTurn)
            {
                if (state.AttackerStrikeCount >= MaxStrikes) return;
                if (state.AttackerTempo < state.AttackerAttackCost) return;

                state.AttackerTempo -= state.AttackerAttackCost;
                state.AttackerAttackCost += AttackCostIncrease;
                state.AttackerStrikeCount++;

                ResolveStrike(context.Attacker, context.Defender,
                    ref state.DefenderHP, result, random, true);
            }
            else
            {
                if (state.DefenderStrikeCount >= MaxStrikes) return;
                if (state.DefenderTempo < state.DefenderAttackCost) return;

                state.DefenderTempo -= state.DefenderAttackCost;
                state.DefenderAttackCost += AttackCostIncrease;
                state.DefenderStrikeCount++;

                ResolveStrike(context.Defender, context.Attacker,
                    ref state.AttackerHP, result, random, false);
            }
        }

        private void ResolveStrike(
            TRPGUnit source,
            TRPGUnit target,
            ref int targetHP,
            CombatResult result,
            Random random,
            bool attackerIsSource)
        {

            var strikeData = new StrikeData(source, target);

            // Apply source modifiers
            foreach (var modifier in source.PreStrikeModifiers)
                modifier.Modify(strikeData, source, target);

            // Apply target modifiers (defensive adjustments)
            foreach (var modifier in target.PreStrikeModifiers)
                modifier.Modify(strikeData, source, target);

            int hitChance = CombatFormulas.CalculateHit(strikeData.Hit, strikeData.Avoid);
            int hitRoll = random.Next(0, 100);
            bool didHit = hitRoll < hitChance;

            int critChance = CombatFormulas.CalculateCrit(strikeData.Crit, strikeData.CritResist);
            int critRoll = random.Next(0, 100);
            bool didCrit = didHit && critRoll < critChance;
            int damage = 0;

            if (didHit)
            {
                damage = CombatFormulas.CalculateDamage(strikeData.Attack, strikeData.Defense);
                if (didCrit) damage *= 3;

                targetHP -= damage;
                if (targetHP < 0) targetHP = 0;
            }

            result.Strikes.Add(new StrikeResult
            {
                AttackerIsSource = attackerIsSource,
                HitRoll = hitRoll,
                CritRoll = critRoll,
                DidHit = didHit,
                DidCrit = didCrit,
                Damage = damage,
                TargetRemainingHP = targetHP
            });
        }

        public void Execute(CombatContext context, CombatResult result)
        {
            foreach (var strike in result.Strikes)
            {
                if (strike.AttackerIsSource)
                {
                    context.Defender.Health.TakeDamage(strike.Damage);
                }
                else
                {
                    context.Attacker.Health.TakeDamage(strike.Damage);
                }
            }
        }

    }
}