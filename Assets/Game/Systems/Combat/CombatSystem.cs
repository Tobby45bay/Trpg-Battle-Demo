using Game.Core.Battle;
using Game.Core.Game;
using Game.Systems.Effect;
using Game.Systems.Item;
using Game.Systems.Skill;
using Game.Systems.Stat;
using Game.Systems.Tags;
using Game.Systems.Trigger;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using static Game.Core.Game.GameManager;
using static Game.Systems.BattleMap.Pathfinder;
using static Game.Systems.Combat.CombatHelper;
using static UnityEngine.GraphicsBuffer;

namespace Game.Systems.Combat
{
    [Serializable]
    public enum DamageType
    {
        TruePhysical,
        Physical,
        Magical,
        TrueMagical,
        Healing
    }

    [Serializable]
    public class TargetData
    {
        public TargetShape shape;
        public int ShapeX;
        public int ShapeY;
        public bool UseRangeOverride;
        public int MinRangeOverride;
        public int MaxRangeOverride;
    }

    public class DamageResult
    {
        public float DamageAmount;
        public bool IsCritical;
        public bool IsHit;

        public DamageResult(float damageAmount, bool isCritical, bool isHit)
        {
            DamageAmount = damageAmount;
            IsCritical = isCritical;
            IsHit = isHit;
        }
    }

    public class CombatCoordinator
    {
        private GameManager gameManager;
        private BattleSystems battleSystems;

        public void InitializeSystem(BattleSystems battleSystems,GameManager gameManager)
        {
            this.battleSystems = battleSystems;
            this.gameManager = gameManager;
        }
        public void StartBattle(int attackingUnitId,int defendingUnitId)
        {
            var attackingUnitAttack = battleSystems.UnitRunTimeManager.GetSelectedAttack(attackingUnitId);
            var denfingAttack = battleSystems.UnitRunTimeManager.GetDefaultAttack(defendingUnitId);

            Trigger(CoreTrigger.CombatStart, attackingUnitId, defendingUnitId);
            Trigger(CoreTrigger.CombatStart, defendingUnitId, attackingUnitId);

            Trigger(CoreTrigger.Attack, attackingUnitId, defendingUnitId);
            Trigger(CoreTrigger.Defend, defendingUnitId, attackingUnitId);
            DoCombat(attackingUnitAttack, attackingUnitId, defendingUnitId);
            //canCounter
            Trigger(CoreTrigger.CombatEnd, attackingUnitId, defendingUnitId);
            Trigger(CoreTrigger.CombatEnd, defendingUnitId, attackingUnitId);
        }

        public void DoCombat(AttackData attackData,int attackingUnitId,int defendingUnitId)
        {
            DamageResult result = CombatHelper.ResolveDamage(attackData.PrimaryDamage,attackingUnitId,defendingUnitId,battleSystems,DamageResolutionType.Combat);

            if (result.IsHit)
            {
                Trigger(CoreTrigger.Hit, attackingUnitId, defendingUnitId);
                battleSystems.UnitRunTimeManager.TakeDamage(defendingUnitId, false, Mathf.Round(result.DamageAmount));
                if (result.IsCritical)
                    Trigger(CoreTrigger.Critical, attackingUnitId, defendingUnitId);

                Trigger(CoreTrigger.Attacked, defendingUnitId, attackingUnitId);
            }
            else
            {
                Trigger(CoreTrigger.Missed, attackingUnitId, defendingUnitId);
                Trigger(CoreTrigger.Avoid, defendingUnitId, attackingUnitId);
            }
        }


        public void Trigger(CoreTrigger coreTrigger,int triggerOwenerId,int otherId)
        {
            var context = new BattleContext
            {
                battleSystems = battleSystems,
                triggerCall = new TriggerKey(coreTrigger),
                SourceUnitId = triggerOwenerId,
                TargetUnitId = otherId
            };

            gameManager.triggerDispatcher.Trigger(context);
        }

        public void TriggerEffects(int attackOwnerId,int defendingUnitId,int attackId,List<ActionData> effects)
        {
            if (effects.Count == 0 || effects == null) return;
            var ctx = new EffectContext
            {
                battleSystems = battleSystems,
                triggerCall = new TriggerKey(CoreTrigger.Hit),
                SourceUnitId = attackOwnerId,
                TargetUnitId = defendingUnitId,
                SourceCode = SourceType.Unit,
                SourceId = attackOwnerId,
                SourceRefCode = SourceType.Attack,
                SourceRefId = attackId,
            };

            ActionHelper.ExecuteActions(effects, ctx);
        }
    }
    public static class CombatHelper
    {
        public static float CalculateHitRate(int attackingUnitId, int defendingUnitId,BattleSystems bs)
        {
            var baseHitRate = bs.UnitRunTimeManager.GetOtherStat(attackingUnitId, OtherStats.Hit_Rate,false);
            var baseAvoidRate = bs.UnitRunTimeManager.GetOtherStat(defendingUnitId, OtherStats.Avoid_Rate, false);

            var HitRate = baseHitRate + bs.UnitRunTimeManager.GetStat(attackingUnitId, CoreStats.Speed, false);
            var AvoidRate = baseAvoidRate + bs.UnitRunTimeManager.GetStat( defendingUnitId, CoreStats.Speed, false);
            return Mathf.Clamp(HitRate - AvoidRate, 0f, 100f);
        }

        public static float CalculateCriticalRate(int attackingUnitId, int defendingUnitId, BattleSystems bs)
        {
            var critRate = bs.UnitRunTimeManager.GetStat(attackingUnitId,CoreStats.Luck, false)/2f;
            var critAvoid = bs.UnitRunTimeManager.GetStat(defendingUnitId, CoreStats.Speed, false) / 3f;

            return Mathf.Clamp(critRate - critAvoid, 0f, 100f);
        }

        public static float CalculateDamageEffect(DamageEffectData damageEffectData,int atackingUnitId,int defendingUnitId,BattleSystems bs) 
        {
            var damageType = damageEffectData.damageType;
            bool isHealing = damageType == DamageType.Healing;
            float baseValue = 0f;

            if (isHealing)
            {
                baseValue = damageEffectData.BaseDamage > 0
                    ? damageEffectData.BaseDamage
                    : bs.StatManager.GetFinalStat(defendingUnitId, CoreStats.Health) * damageEffectData.DamageModifier;
            }
            else
            {
                baseValue = damageEffectData.BaseDamage > 0
                    ? damageEffectData.BaseDamage
                    : damageEffectData.UseOverrideStat
                        ? bs.StatManager.GetFinalStat(atackingUnitId, damageEffectData.OverrideStat)
                        : bs.UnitRunTimeManager.GetUnitDamage(atackingUnitId, damageEffectData.damageType);
            }

            float finalValue = baseValue * damageEffectData.DamageModifier;
            return finalValue;
        }

        public enum DamageResolutionType
        {
            Combat,   // hit / crit / avoid
            Effect    // guaranteed
        }

        public static DamageResult ResolveDamage(DamageEffectData dmg,int attackerId,int defenderId,BattleSystems bs,DamageResolutionType resolutionType)
        {
            bool isHit = true;
            bool isCritical = false;

            // ─────────────────────────────
            // HIT / CRIT (COMBAT ONLY)
            // ─────────────────────────────
            if (resolutionType == DamageResolutionType.Combat)
            {
                float hitRate = CalculateHitRate(attackerId, defenderId, bs);
                isHit = BattleRng.RollPercent(hitRate);

                if (!isHit)
                    return new DamageResult(0f, false, false);

                float critRate = CalculateCriticalRate(attackerId, defenderId, bs);
                isCritical = BattleRng.RollPercent(critRate);
            }

            // ─────────────────────────────
            // RAW DAMAGE
            // ─────────────────────────────
            float damage = CalculateDamageEffect(dmg, attackerId, defenderId, bs);

            // ─────────────────────────────
            // DEFENSE
            // ─────────────────────────────
            bool isTrueDamage =
                dmg.damageType == DamageType.TruePhysical ||
                dmg.damageType == DamageType.TrueMagical;

            if (!isTrueDamage && dmg.damageType != DamageType.Healing)
            {
                float defence = bs.UnitRunTimeManager
                    .GetUnitDefence(defenderId, dmg.damageType);

                damage = Mathf.Max(damage - defence, 0f);
            }

            // ─────────────────────────────
            // CRIT MULTIPLIER
            // ─────────────────────────────
            if (isCritical)
                damage *= GameConst.Combat.CritMultiplier;

            return new DamageResult(damage, isCritical, true);
        }

        public enum OverrideType
        {
            Attack,
            Defense,
        }
        public static CoreStats ResolveStat
            (int unitId,DamageType damageType,OverrideType type,CoreStats defaultPhysical,CoreStats defaultMagical,BattleSystems bs)
        {
            // 1. Determine default stat
            CoreStats statTag = damageType switch
            {
                DamageType.Physical or DamageType.TruePhysical => defaultPhysical,
                DamageType.Magical or DamageType.TrueMagical => defaultMagical,
                _ => defaultPhysical
            };

            // 2. Fetch override skill
            SkillData overrideSkill = type switch
            {
                OverrideType.Attack =>
                    bs.SkillManager.GetOverrideSkill("Attack", unitId),

                OverrideType.Defense =>
                    bs.SkillManager.GetOverrideSkill("Defense", unitId),
                _ => null
            };

            if (overrideSkill == null)
                return statTag;

            // 3. Resolve override container
            var overrideData = type switch
            {
                OverrideType.Attack => overrideSkill.attackOverride,
                OverrideType.Defense => overrideSkill.defenceOverride,
                _ => null
            };

            // 4. Validate override
            if (overrideData == null ||
                !overrideData.enabled ||
                overrideData.data.damageType != damageType)
                return statTag;

            // 5. True damage ignores modifiers, NOT stat selection
            return overrideData.data.statOverride;
        }
    }

    public static class BattleRng
    {
        private static System.Random rng = new System.Random();

        public static int Range(int min, int max) => rng.Next(min, max);
        public static float Range(float min, float max) =>
            (float)(rng.NextDouble() * (max - min) + min);

        public static bool RollPercent(float chance)
            => Range(0f, 100f) <= chance;
    }

    [Serializable]
    public class UnitAttackLoadout
    {
        public int DefaultAttackId;
        public int SelectedAttackId;
        public List<int> AdditionalAttackIds = new();

        public void AddAttack(int attackId) => AdditionalAttackIds.Add(attackId);
        public void RemoveAttack(int attackId) => AdditionalAttackIds.Remove(attackId);

    }

    public class AttackLoadoutManager
    {
        private Dictionary<int, UnitAttackLoadout> attackLoadoutRegistry = new();
        public AttackDatabase attackDb;

        public void InitializeSystem(AttackDatabase attackDatabase)
        {
            attackDb = attackDatabase;
            Debug.Log("System Up");
        }
        public void RegisterUnit(int unitId, List<int> knownAttacks)
        {
            if (!knownAttacks.Contains(unitId))
            {
                var attackLoadout = new UnitAttackLoadout();

                foreach(var attackId in knownAttacks)
                {
                    attackLoadout.AddAttack(attackId);
                    BattleManager.Instance.battleSystems.PrintAttackLoadoutLog(unitId, attackId,true);
                }
                attackLoadoutRegistry[unitId] = attackLoadout;
            }
        }

        public void AddAttack(int unitId, int attackId)
        {
            if (attackLoadoutRegistry.TryGetValue(unitId, out var attackLoadout)) return;

            if (attackLoadout.AdditionalAttackIds.Contains(attackId)) return;

            attackLoadout.AddAttack(attackId);
            BattleManager.Instance.battleSystems.PrintAttackLoadoutLog(unitId, attackId, true);
        }

        public void RemoveAttack(int unitId, int attackId)
        {
            if (attackLoadoutRegistry.TryGetValue(unitId, out var attackLoadout)) return;

            if (!attackLoadout.AdditionalAttackIds.Contains(attackId)) return;

            attackLoadout.RemoveAttack(attackId);
            BattleManager.Instance.battleSystems.PrintAttackLoadoutLog(unitId, attackId, false);
        }

        public List<AttackData> GetAttacksForWeapon(int unitId, WeaponType weaponType)
        {
            if (!attackLoadoutRegistry.ContainsKey(unitId)) return null;

            var attacks = attackLoadoutRegistry[unitId];
            List<AttackData> list = new();

            // Include default attack
            var defaultAttack = attackDb.GetAttack(attacks.DefaultAttackId);
            if (defaultAttack.RequiredWeapon == weaponType)
                list.Add(defaultAttack);

            // Include additional attacks
            foreach (var attack in attacks.AdditionalAttackIds)
            {
                var attackData = attackDb.GetAttack(attack);
                if (attackData.RequiredWeapon == weaponType)
                    list.Add(attackData);
            }

            return list;
        }

        public void SetDefaultAttack(int unitId, int attackId)
        {
            if (!attackLoadoutRegistry.ContainsKey(unitId)) return;
            attackLoadoutRegistry[unitId].DefaultAttackId = attackId;
            BattleManager.Instance.battleSystems.PrintAttackSelectedLog(unitId, attackId, true);
        }

        public AttackData GetDefaultAttack(int unitId)
        {
            return GetAttack(unitId, a => a.DefaultAttackId);
        }

        public AttackData GetSelectedAttack(int unitId)
        {
            return GetAttack(unitId, a => a.SelectedAttackId);
        }

        private AttackData GetAttack(int unitId, Func<UnitAttackLoadout, int> selector)
        {
            if (!attackLoadoutRegistry.TryGetValue(unitId, out var loadout))
                return null;

            int id = selector(loadout);
            return id >= 0 ? attackDb.GetAttack(id) : null;
        }
    }

    [Serializable]
    public class DamageEffectData
    {
        public DamageType damageType;
        public float BaseDamage;
        public float DamageModifier;
        public bool UseOverrideStat;
        public CoreStats OverrideStat;
    }

    [Serializable]
    public class AppyDamageActionData : ActionData
    {
        public DamageEffectData DamageEffectData;
        public TargetType TargetType;
        public override void Execute(TriggerContext context)
        {
            if (context is not EffectContext ctx) return;

            var bs = ctx.battleSystems;
            var dmg = DamageEffectData;

            int caster = ctx.SourceId;
            int target = ctx.battleSystems.ResolveTarget(TargetType, ctx);

            var result = CombatHelper.ResolveDamage(
                dmg,
                caster,
                target,
                bs,
                DamageResolutionType.Effect // guaranteed hit
            );

            bs.UnitRunTimeManager.TakeDamage(
                target,
                dmg.damageType == DamageType.Healing,
                Mathf.Round(result.DamageAmount)
            );
        }
    }
}