using Game.Core.Battle;
using Game.Core.Game;
using Game.Systems.BattleMap;
using Game.Systems.Item;
using Game.Systems.Job;
using Game.Systems.Stat;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using static UnityEditor.PlayerSettings;
using static UnityEngine.UI.CanvasScaler;

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

        public Vector2Int LastCommittedTile { get; private set; }
        public Vector2Int IntendedTile { get; private set; }
        public bool HasIntendedTile { get; private set; }
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
        public void SetIntendedTile(Vector2Int tile)
        {
            IntendedTile = tile;
            HasIntendedTile = true;
        }
        public void ClearIntendedTile()
        {
            IntendedTile = default;
            HasIntendedTile = false;
        }

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
            ClearIntendedTile();
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

            controller.Initialize(Context,battleSystems, battleSystems.MenuInputController);
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

        public bool IsUnitInPhase(int unitId)
        {
            return unitsInPhase.Contains(unitId);
        }
    }

    public static class HandlerFactory
    {
        public static IInputHandler Create
            (HandlerState state,
            HandlerContext context,
            PlayerController controller,
            BattleSystems bs)
        {
            return state switch
            {
                HandlerState.Idle =>
                    new IdleHandler(context, controller,bs),
                HandlerState.MovingUnit =>
                    new MovingUnitHandler(context, controller,bs),
                HandlerState.SelectingTarget =>
                    new SelectingTargetHandler(context, controller),
                HandlerState.SelectingWeapon =>
                    new SelectingWeaponHandler(context, controller),
                HandlerState.SelectingAttack =>
                    new SelectingAttackHandler(context, controller),
                HandlerState.ViewingInventory =>
                    new ViewingInventoryHandler(context, controller),
                _ => throw new System.NotImplementedException($"Handler not implemented: {state}")
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

        public IdleHandler(HandlerContext context, PlayerController controller, BattleSystems bs)
        {
            this.context = context;
            this.controller = controller;
            this.bs = bs;
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
                    if (TrySelectUnitUnderCursor(out int unitId))
                    {
                        bs.TilemapManager.TryGetUnitPos(unitId, out Vector2Int pos);
                        context.SetLastCommittedTile(pos);
                        bs.MenuInputController.SetActiveMenu(BattleManager.Instance.CommandMenu);
                        BattleManager.Instance.CommandMenu.Open(bs, unitId, controller,context);
                    }
                    break;
                case InputIntent.Cancel:
                    context.ResetSelectedUnitId();
                    context.RestLatCommittedTile();
                    break;
            }
        }

        private bool TrySelectUnitUnderCursor(out int unitId)
        {

            // 1. Ask the tilemap / cursor what unit is under it
            if (!bs.TilemapManager.cursorController.TryGetHoveredUnit(out unitId))
                return false;

            // 2. Validate against current phase ownership
            if (context.CurrentPhase is not PlayerHandler phase ||
                !phase.IsUnitInPhase(unitId))
                return false;

            // 3. Commit selection
            context.SelectUnit(unitId);
            return true;
        }

    }

    public class MovingUnitHandler : IInputHandler
    {
        private HandlerContext context;
        private PlayerController controller;
        private BattleSystems bs;
        private List<TileInstance> reachableTiles = new();
        private HashSet<Vector2Int> reachablePositions = new();
        private List<TileInstance> attackableTiles = new();
        private Vector2Int previewTile;

        public MovingUnitHandler(HandlerContext context, PlayerController controller, BattleSystems bs)
        {
            this.context = context;
            this.controller = controller;
            this.bs = bs;
            if (context.LastCommittedTile == default)
            {
                bs.TilemapManager.TryGetUnitPos(context.SelectedUnitId, out var pos);
                context.SetLastCommittedTile(pos);
            }
            previewTile = context.LastCommittedTile;
            UpdateUnitView(previewTile);
            ShowUnitRange();
        }

        public void HandleInput(InputIntent intent)
        {
            switch (intent)
            {
                case InputIntent.NavigateUp:
                    bs.TilemapManager.cursorController.Move(Vector2Int.up);
                    TryUpdatePreview();
                    break;
                case InputIntent.NavigateDown:
                    bs.TilemapManager.cursorController.Move(Vector2Int.down);
                    TryUpdatePreview();
                    break;
                case InputIntent.NavigateLeft:
                    bs.TilemapManager.cursorController.Move(Vector2Int.left);
                    TryUpdatePreview();
                    break;
                case InputIntent.NavigateRight:
                    bs.TilemapManager.cursorController.Move(Vector2Int.right);
                    TryUpdatePreview();
                    break;
                case InputIntent.Confirm:
                    if (!bs.TilemapManager.cursorController.TryGetHoveredTile(out TileInstance tile)) return;

                    if (!reachablePositions.Contains(tile.MapPosition)) return;

                    if (tile.IsOccupied) return;

                    context.SetIntendedTile(tile.MapPosition);

                    UpdateUnitView(context.IntendedTile);

                    bs.MenuInputController.SetActiveMenu(BattleManager.Instance.CommandMenu);
                    BattleManager.Instance.CommandMenu.Open(bs,context.SelectedUnitId,controller,context);
                    ClearTiles();
                    break;
                case InputIntent.Cancel:
                    SnapBackToOrginal();
                    context.ClearIntendedTile();
                    controller.SwitchState(HandlerState.Idle);
                    ClearTiles();
                    break;
            }
        }

        private void SnapBackToOrginal()
        {
            bs.TilemapManager.cursorController.SnapTo(context.LastCommittedTile);
            UpdateUnitView(context.LastCommittedTile);
        }

        private void UpdateUnitView(Vector2Int position)
        {
            bs.TilemapManager.GetUnitView(context.SelectedUnitId, out var unitView);
            unitView.MoveToTile(bs.TilemapManager.GetWorldPosition(position));
        }

        private void TryUpdatePreview()
        {
            var cursorPos = bs.TilemapManager.cursorController.GetCursorPostion();

            if (!reachablePositions.Contains(cursorPos)) return;

            if (!bs.TilemapManager.TryGetTile(cursorPos, out var tile)) return;
            if (tile.IsOccupied) return;

            previewTile = cursorPos;
            UpdateUnitView(previewTile);
        }

        private void ShowUnitRange()
        {
            int unitId = context.SelectedUnitId;

            var moveRange =
                bs.UnitRunTimeManager.GetOtherStat(unitId, OtherStats.Move, false);

            var moveProfile =
                bs.UnitRunTimeManager.GetUnitCurrentMoveProfile(unitId);

            if (!bs.UnitRunTimeManager.TryGetEquippedWeapon(in unitId, out var equippedWeapon))
                return;

            var weaponData = (WeaponSO)equippedWeapon.Data;

            if (!bs.TilemapManager.TryGetUnitPos(unitId, out Vector2Int pos))
                return;

            if (!TryGenearteReachableRange(pos, (int)moveRange, moveProfile, out var reachablePositions))
                return;

            var attackOrigins = new List<Vector2Int>(reachablePositions){pos};

            GenearteAttckRange(attackOrigins,weaponData.minRange,weaponData.maxRange);

            if (reachableTiles.Count > 0)
                TilemapHelper.SetHightlightOfTiles(reachableTiles, HightlightState.Move);

            if (attackableTiles.Count > 0)
                TilemapHelper.SetHightlightOfTiles(attackableTiles, HightlightState.Attack);
        }


        private void GenearteAttckRange(List<Vector2Int> reachablePostion, int minAttack, int maxAttack)
        {
            attackableTiles.Clear();

            var attackablePositions =
                bs.TilemapManager.Pathfinder.GetAttackableTiles(reachablePostion, minAttack, maxAttack);

            foreach (var pos in attackablePositions)
            {
                if (!bs.TilemapManager.TryGetNode(pos, out var node) || node == null)
                    continue;

                // Prevent overlap with move range
                if (reachableTiles.Contains(node))
                    continue;

                attackableTiles.Add(node);
            }
        }

        private bool TryGenearteReachableRange(in Vector2Int origin,int moveRange, MovementProfileData movementProfile, out List<Vector2Int> reachablePositions)
        {
            reachableTiles.Clear();

            var vaildPostions = bs.TilemapManager.Pathfinder.GetReachableTiles(origin, moveRange, movementProfile);

            reachablePositions = new List<Vector2Int>(vaildPostions.Keys);

            if (reachablePositions == null || reachablePositions.Count == 0)
                return false;

            foreach (var pos in reachablePositions)
            {
                if (!bs.TilemapManager.TryGetNode(pos, out var node) || node == null)
                    continue;

                reachableTiles.Add(node);
            }
            return true;
        }


        private void ClearTiles()
        {
            TilemapHelper.SetHightlightOfTiles(reachableTiles, HightlightState.None);
            reachableTiles.Clear();
            reachablePositions.Clear();
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
}