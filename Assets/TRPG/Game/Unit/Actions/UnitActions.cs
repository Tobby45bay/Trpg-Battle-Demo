using System;
using TRPG.Core.Action;
using TRPG.Core.Conditions;
using TRPG.Game.Data.GameActions;
using TRPG.Game.TacticalBattle;

namespace TRPG.Game.Unit.Actions
{
    /// <summary>
    /// Base class for resource-related actions.
    /// </summary>
    public abstract class ResourceActionData : TargetedActionData
    {
        public ResourceType ResourceType { get; }
        public int Amount { get; }

        protected ResourceActionData(
            string name,
            int targetUnitId,
            ResourceType resourceType,
            int amount,
            ICondition? condition = null)
            : base(name, targetUnitId, condition)
        {
            ResourceType = resourceType;
            Amount = Math.Max(0, amount);
        }
    }

    // =========================================================
    // SPEND RESOURCE ACTION
    // =========================================================

    /// <summary>
    /// Spends AP or MP from the target unit.
    /// 
    /// Example: After performing an action that costs 2 AP, enqueue this.
    /// Fails if unit doesn't have enough resources.
    /// </summary>
    public sealed class SpendResourceAction : ResourceActionData
    {
        public SpendResourceAction(
            int targetUnitId,
            ResourceType resourceType,
            int amount,
            ICondition? condition = null)
            : base("SpendResource", targetUnitId, resourceType, amount, condition)
        {
        }

        public override string ToString() =>
            $"SpendResource[{ResourceType}: {Amount}] -> TRPGUnit {TargetUnitId}";
    }

    // =========================================================
    // RESTORE RESOURCE ACTION
    // =========================================================

    /// <summary>
    /// Restores AP or MP to the target unit.
    /// 
    /// Example: After using a restoration item or ability.
    /// Clamps to max automatically.
    /// </summary>
    public sealed class RestoreResourceAction : ResourceActionData
    {
        public RestoreResourceAction(
            int targetUnitId,
            ResourceType resourceType,
            int amount,
            ICondition? condition = null)
            : base("RestoreResource", targetUnitId, resourceType, amount, condition)
        {
        }

        public override string ToString() =>
            $"RestoreResource[{ResourceType}: +{Amount}] -> TRPGUnit {TargetUnitId}";
    }

    // =========================================================
    // RESTORE FULL ACTION
    // =========================================================

    /// <summary>
    /// Completely refills the specified resource.
    /// 
    /// Example: Battle start, restoration ritual, etc.
    /// </summary>
    public sealed class RestoreResourceFullAction : TargetedActionData
    {
        public ResourceType ResourceType { get; }

        public RestoreResourceFullAction(
            int targetUnitId,
            ResourceType resourceType,
            ICondition? condition = null)
            : base("RestoreResourceFull", targetUnitId, condition)
        {
            ResourceType = resourceType;
        }

        public override string ToString() =>
            $"RestoreResourceFull[{ResourceType}] -> TRPGUnit {TargetUnitId}";
    }

    // =========================================================
    // DRAIN RESOURCE ACTION
    // =========================================================

    /// <summary>
    /// Completely depletes the specified resource.
    /// 
    /// Example: Curse effect, punishment ability, etc.
    /// </summary>
    public sealed class DrainResourceAction : TargetedActionData
    {
        public ResourceType ResourceType { get; }

        public DrainResourceAction(
            int targetUnitId,
            ResourceType resourceType,
            ICondition? condition = null)
            : base("DrainResource", targetUnitId, condition)
        {
            ResourceType = resourceType;
        }

        public override string ToString() =>
            $"DrainResource[{ResourceType}] -> TRPGUnit {TargetUnitId}";
    }
}

// =========================================================
// RESOURCE ACTION EXECUTORS
// =========================================================

namespace TRPG.Game.Unit.Actions
{
    /// <summary>
    /// Executes SpendResourceAction.
    /// Fails if the unit doesn't have enough resources.
    /// </summary>
    public sealed class SpendResourceExecutor : ActionExecutor<SpendResourceAction>
    {
        protected override ActionResult ExecuteTyped(SpendResourceAction data, IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;

            var unit = battleContext.TargetUnit;
            if (unit == null)
                return ActionResult.Failed;

            var resourceComponent = unit.GetComponent<UnitResourceComponent>();
            if (resourceComponent == null)
                return ActionResult.Failed;

            // Check if unit has enough resources
            if (data.ResourceType == ResourceType.AP)
            {
                if (!resourceComponent.CanSpendAP(data.Amount))
                    return ActionResult.Failed;

                resourceComponent.SpendAP(data.Amount);
            }
            else if (data.ResourceType == ResourceType.MP)
            {
                if (!resourceComponent.CanSpendMP(data.Amount))
                    return ActionResult.Failed;

                resourceComponent.SpendMP(data.Amount);
            }

            return ActionResult.Completed;
        }
    }

    /// <summary>
    /// Executes RestoreResourceAction.
    /// Always succeeds; restoration is clamped to max automatically.
    /// </summary>
    public sealed class RestoreResourceExecutor : ActionExecutor<RestoreResourceAction>
    {
        protected override ActionResult ExecuteTyped(RestoreResourceAction data, IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;

            var unit = battleContext.TargetUnit;
            if (unit == null)
                return ActionResult.Failed;

            var resourceComponent = unit.GetComponent<UnitResourceComponent>();
            if (resourceComponent == null)
                return ActionResult.Failed;

            if (data.ResourceType == ResourceType.AP)
                resourceComponent.RestoreAP(data.Amount);
            else if (data.ResourceType == ResourceType.MP)
                resourceComponent.RestoreMP(data.Amount);

            return ActionResult.Completed;
        }
    }

    /// <summary>
    /// Executes RestoreResourceFullAction.
    /// Completely refills the specified resource to maximum.
    /// </summary>
    public sealed class RestoreResourceFullExecutor : ActionExecutor<RestoreResourceFullAction>
    {
        protected override ActionResult ExecuteTyped(RestoreResourceFullAction data, IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;

            var unit = battleContext.TargetUnit;
            if (unit == null)
                return ActionResult.Failed;

            var resourceComponent = unit.GetComponent<UnitResourceComponent>();
            if (resourceComponent == null)
                return ActionResult.Failed;

            if (data.ResourceType == ResourceType.AP)
                resourceComponent.RestoreAPFull();
            else if (data.ResourceType == ResourceType.MP)
                resourceComponent.RestoreMPFull();

            return ActionResult.Completed;
        }
    }

    /// <summary>
    /// Executes DrainResourceAction.
    /// Completely depletes the specified resource.
    /// </summary>
    public sealed class DrainResourceExecutor : ActionExecutor<DrainResourceAction>
    {
        protected override ActionResult ExecuteTyped(DrainResourceAction data, IActionContext context)
        {
            if (context is not BattleActionContext battleContext)
                return ActionResult.Failed;

            var unit = battleContext.TargetUnit;
            if (unit == null)
                return ActionResult.Failed;

            var resourceComponent = unit.GetComponent<UnitResourceComponent>();
            if (resourceComponent == null)
                return ActionResult.Failed;

            if (data.ResourceType == ResourceType.AP)
                resourceComponent.SpendAP(resourceComponent.CurrentAP);
            else if (data.ResourceType == ResourceType.MP)
                resourceComponent.SpendMP(resourceComponent.CurrentMP);

            return ActionResult.Completed;
        }
    }
}
