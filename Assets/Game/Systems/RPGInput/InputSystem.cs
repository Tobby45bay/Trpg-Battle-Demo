using Game.Core.Battle;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;

namespace Game.Systems.RPGInput
{
    public enum HandlerState
    {
        Idle,
        SeletingUnit,
        MovingUnit,
        SelectingItem,
        SelectingAttack,
        SelectingWeapon,
        SelectingTarget,
        ViewingInventory
    }

    public enum ActionType
    {
        Move,
        Attack,
        UseItem,
        Wait,
        Interact
    }

    public class ActionRequest
    {
        public int ActorId;

        public ActionType ActionType;

        // Targeting
        public int TargetUnitId = -1;
        public Vector2Int TargetTile;

        // Payload (only one usually used)
        public int AttackId = -1;
        public int ItemId = -1;
        public int WeaponId = -1;

        // Optional metadata
        public bool IsFreeAction;
        public bool IsForced;
    }

    public class HandlerContext
    {
        public HandlerState CurrentState { get; private set; }

        public int SelectedUnitId { get; private set; }
        public List<int> SelectedTargetUnits{ get; private set; } = new();

        public int SelectedItemId{ get; private set; }
        public int SelectedWeaponId { get; private set; }
        public int SelectedAttackId { get; private set; }

        public Vector2Int LastCommittedTile;

        public PhaseHandler CurrentPhase {  get; private set; }

        public void AddTarget(int unitId) => SelectedTargetUnits.Add(unitId);
        public void SelectUnit(int unitId)
        {
            SelectedUnitId = unitId;
            SelectedTargetUnits.Clear();
        }

        public void SetTargets(IEnumerable<int> targets)
        {
            SelectedTargetUnits.Clear();
            SelectedTargetUnits.AddRange(targets);
        }

        public void ResetSelectedUnitId() => SelectedUnitId = -1;
        public void ResetSelectedTargetUnits() => SelectedTargetUnits.Clear();

        public void ResetSelectedUnit()
        {
            SelectedUnitId = -1;
            SelectedTargetUnits.Clear();
        }

        public void SetSelectedItemId(int itemId) => SelectedItemId = itemId;
        public void ResetSelectedItemId() => SelectedItemId = -1;

        public void SetSelectedWeaponId(int itemId) => SelectedWeaponId = itemId;
        public void ResetSelectedWeaponId() => SelectedWeaponId = -1;

        public void SetSelectedAttackId(int attackId) => SelectedAttackId = attackId;
        public void ResetSelectedAttackId() => SelectedAttackId = -1;

        public void SetLastCommittedTile(Vector2Int lastCommittedTile) => LastCommittedTile = lastCommittedTile;
        public void RestLatCommittedTile() => LastCommittedTile = default;

        public void SetState(HandlerState state) => CurrentState = state;

        public void SetCurrentPhase(PhaseHandler phaseHandler) => CurrentPhase = phaseHandler;
        public void ResetCurrentPhase() => CurrentPhase = default;


        public void ResetSelection()
        {
            ResetSelectedUnit();
            ResetSelectedItemId();
            ResetSelectedWeaponId();
            ResetSelectedAttackId();
            RestLatCommittedTile();
            ResetCurrentPhase();
        }
    }

    public abstract class PhaseHandler
    {
        protected TurnManager turnManager;
        protected BattleSystems battleSystems;

        protected List<int> unitsInPhase;

        public HandlerContext Context { get; private set; } = new();

        public void Initialize(TurnManager tm, BattleSystems systems)
        {
            turnManager = tm;
            battleSystems = systems;
        }

        public void BeginPhase(List<int> units)
        {
            unitsInPhase = units;
            Context.ResetSelection();
            OnBeginPhase();
        }

        protected abstract void OnBeginPhase();
        public abstract void Update();
        protected abstract bool IsPhaseComplete();

        public bool PhaseComplete => IsPhaseComplete();

        public void TryEndPhase()
        {
            if (IsPhaseComplete())
                turnManager.EndPhase();
        }
    }

    public class PlayerHandler : PhaseHandler
    {
        private HashSet<int> availableUnits = new();
        private HashSet<int> actedUnits = new();

        private PlayerController controller;

        protected override void OnBeginPhase()
        {
            availableUnits.Clear();
            actedUnits.Clear();

            foreach (var unit in unitsInPhase)
                availableUnits.Add(unit);

            controller = GameObject.FindObjectOfType<PlayerController>();

            Context.SetCurrentPhase(this);

            controller.Initialize(Context);
            controller.SwitchState(HandlerState.Idle);
            controller.enabled = true;
        }

        public override void Update()
        {
            // Player input drives progress
            TryEndPhase();
        }

        public bool CanUnitAct(int unitId)
        {
            return availableUnits.Contains(unitId)
                && !actedUnits.Contains(unitId)
                && battleSystems.UnitRunTimeManager.CanAct(unitId);
        }

        public void CommitAction(ActionRequest request)
        {
            if (!CanUnitAct(request.ActorId))
                return;

            actedUnits.Add(request.ActorId);

            //battleSystems.CombatCoordinator.Resolve(request);

            Context.ResetSelection();
            controller.SwitchState(HandlerState.Idle);
        }

        protected override bool IsPhaseComplete()
        {
            foreach (var unit in availableUnits)
                if (!actedUnits.Contains(unit))
                    return false;

            return true;
        }
    }

    public static class HandlerFactory
    {
        public static IInputHandler Create(
            HandlerState state,
            HandlerContext context,
            PlayerController controller)
        {
            return state switch
            {
                HandlerState.Idle =>
                    new IdleHandler(context, controller),
                HandlerState.MovingUnit =>
                    new MovingUnitHandler(context, controller),
                HandlerState.SelectingTarget =>
                    new SelectingTargetHandler(context, controller),
                HandlerState.SelectingWeapon =>
                    new SelectingWeaponHandler(context, controller),
                HandlerState.SelectingAttack =>
                    new SelectingAttackHandler(context, controller),
                HandlerState.ViewingInventory =>
                    new ViewingInventoryHandler(context, controller),


                _ => null
            };
        }
    }

    public interface IInputHandler
    {
        void HandleInput(InputIntent intent);
    }

    public class IdleHandler : IInputHandler
    {
        private HandlerContext context;
        private PlayerController controller;
        private BattleSystems bs = BattleManager.Instance.battleSystems;

        public IdleHandler(HandlerContext context, PlayerController controller)
        {
            this.context = context;
            this.controller = controller;
        }

        public void HandleInput(InputIntent intent)
        {
            switch(intent)
            {
                case InputIntent.NavigateUp:
                    bs.TilemapManager.cursorController.Move(Vector2Int.up);
                    break;
                case InputIntent.NavigateDown:
                    bs.TilemapManager.cursorController.Move(Vector2Int.down);
                    break;
                case InputIntent.NavigateLeft:
                    bs.TilemapManager.cursorController.Move(Vector2Int.left);
                    break;
                case InputIntent.NavigateRight:
                    bs.TilemapManager.cursorController.Move(Vector2Int.right);
                    break;
                case InputIntent.Confirm:

                    break;
            }
        }
    }

    public class SelectingTargetHandler : IInputHandler
    {
        private HandlerContext context;
        private PlayerController controller;

        public SelectingTargetHandler(HandlerContext context, PlayerController controller)
        {
            this.context = context;
            this.controller = controller;
        }

        public void HandleInput(InputIntent intent)
        {
            throw new System.NotImplementedException();
        }
    }

    public class SelectingWeaponHandler : IInputHandler
    {
        private HandlerContext context;
        private PlayerController controller;

        public SelectingWeaponHandler(HandlerContext context, PlayerController controller)
        {
            this.context = context;
            this.controller = controller;
        }

        public void HandleInput()
        {

        }

        public void HandleInput(InputIntent intent)
        {
            throw new System.NotImplementedException();
        }
    }

    public class SelectingAttackHandler : IInputHandler
    {
        private HandlerContext context;
        private PlayerController controller;

        public SelectingAttackHandler(HandlerContext context, PlayerController controller)
        {
            this.context = context;
            this.controller = controller;
        }

        public void HandleInput(InputIntent intent)
        {
            throw new System.NotImplementedException();
        }
    }
    public class ViewingInventoryHandler : IInputHandler
    {
        private HandlerContext context;
        private PlayerController controller;

        public ViewingInventoryHandler(HandlerContext context, PlayerController controller)
        {
            this.context = context;
            this.controller = controller;
        }


        public void HandleInput(InputIntent intent)
        {
            throw new System.NotImplementedException();
        }
    }


    public class MovingUnitHandler : IInputHandler
    {
        private HandlerContext context;
        private PlayerController controller;

        public MovingUnitHandler(HandlerContext context, PlayerController controller)
        {
            this.context = context;
            this.controller = controller;
        }

        public void HandleInput(InputIntent intent)
        {
            throw new System.NotImplementedException();
        }
    }

}