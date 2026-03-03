
using System;
using System.Collections.Generic;

using System.Linq;
using TRPG.Core.Action;
using TRPG.Core.Event;
using TRPG.Game.Const;
using TRPG.Game.Data.GameActions;
using TRPG.Game.Systems.Rank.Actions;
using TRPG.Game.Unit;
using UnityEngine;

namespace TRPG.Game.TacticalBattle
{
    public sealed class BattleManager
    {
        
        private readonly EventBus _eventBus;
        private readonly ActionPipeline _actionPipeline;
        private readonly ActionDispatcher _actionDispatcher;
        private readonly BattleTurnSystem _turnSystem;
        public int CurrentTurn => _turnSystem.CurrentTurn;

        private BattleConfigData _config;
        private BattlePhase _currentPhase = BattlePhase.Setup;
        private BattleState _battleState = BattleState.NotStarted;

        private readonly Dictionary<int, BattleUnitState> _unitStates = new();
        private readonly List<BattleUnitState> _playerUnits = new();
        private readonly List<BattleUnitState> _allyUnits = new();
        private readonly List<BattleUnitState> _enemyUnits = new();
        private readonly List<BattleUnitState> _otherUnits = new();

        private int _playerDefeatedCount = 0;
        private int _enemyDefeatedCount = 0;
        public readonly GamesDatabeses GamesDatabeses;

        // Events
        public event Action<BattlePhase> OnPhaseChanged;
        public event Action<BattleState> OnBattleStateChanged;
        public event Action<BattleUnitState> OnUnitDefeated;

        public BattleManager(EventBus eventBus, ActionPipeline actionPipeline, ActionDispatcher actionDispatcher, GamesDatabeses gamesDatabeses)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _actionPipeline = actionPipeline ?? throw new ArgumentNullException(nameof(actionPipeline));
            _actionDispatcher = actionDispatcher ?? throw new ArgumentNullException(nameof(actionDispatcher));
            _turnSystem = new BattleTurnSystem();
            GamesDatabeses = gamesDatabeses;
            // Register event listeners
            _eventBus.Register(new UnitDefeatedListener(this));
            
        }

        // ================================
        // Initialization
        // ================================

        public void Initialize(BattleConfigData config, List<TRPGUnit> playerUnits,List<TRPGUnit> allyUnits, List<TRPGUnit> enemyUnits,List<TRPGUnit> otherUnits)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (playerUnits == null || playerUnits.Count == 0)
                throw new ArgumentException("Player units cannot be empty");
            if (enemyUnits == null || enemyUnits.Count == 0)
                throw new ArgumentException("Enemy units cannot be empty");

            Debug.Log($"[Battle] Initializing: {_config}");

            // Initialize unit states
            foreach (var unit in playerUnits)
            {
                var state = new BattleUnitState(unit, 0);
                _playerUnits.Add(state);
                _unitStates[unit.Id] = state;
            }

            foreach (var unit in allyUnits)
            {
                var state = new BattleUnitState(unit, 1);
                _allyUnits.Add(state);
                _unitStates[unit.Id] = state;
            }

            foreach (var unit in enemyUnits)
            {
                var state = new BattleUnitState(unit, 2);
                _enemyUnits.Add(state);
                _unitStates[unit.Id] = state;
            }
            foreach (var unit in otherUnits)
            {
                var state = new BattleUnitState(unit, 3);
                _otherUnits.Add(state);
                _unitStates[unit.Id] = state;
            }

            // Initialize turn order
            var allUnits = new List<BattleUnitState>();
            allUnits.AddRange(_playerUnits);
            allUnits.AddRange(_allyUnits);
            allUnits.AddRange(_enemyUnits);
            allUnits.AddRange(_otherUnits);
            _turnSystem.InitializeTurnOrder(allUnits);

            // Set state
            _battleState = BattleState.InProgress;
            SetPhase(BattlePhase.Setup);

            // Fire battle started event


            Debug.Log($"[Battle] Ready. Turn order: {string.Join(", ", _turnSystem.TurnOrder)}");
        }

        // ================================
        // Phase Management
        // ================================

        public void Update()
        {
            // Process all queued events first
            _eventBus.ProcessAll();

            // Check win conditions
            CheckVictoryConditions();
            if (_battleState != BattleState.InProgress)
                return;

            // Handle current phase
            switch (_currentPhase)
            {
                case BattlePhase.Setup:
                    HandleSetup();
                    break;

                case BattlePhase.TeamTurn:
                    HandleTeamTurn();
                    break;

                case BattlePhase.UnitAction:
                    HandleUnitAction();
                    break;

                case BattlePhase.Resolution:
                    HandleResolution();
                    break;

                case BattlePhase.TurnEnd:
                    HandleTurnEnd();
                    break;

                case BattlePhase.BattleEnd:
                    HandleBattleEnd();
                    break;
            }
        }

        private void HandleSetup()
        {
            Debug.Log("[Battle] Phase: Setup → TeamTurn");
            SetPhase(BattlePhase.TeamTurn);
        }

        private void HandleTeamTurn()
        {
            var actor = _turnSystem.CurrentActor;
            if (actor == null)
                return;

            Debug.Log($"[Battle] Team {actor.TeamId}'s turn (Unit: {actor.Unit.Name})");
            SetPhase(BattlePhase.UnitAction);
        }

        private void HandleUnitAction()
        {
            // Input handling happens here or in a separate input layer
            // For now, just advance to resolution
            SetPhase(BattlePhase.Resolution);
        }

        private void HandleResolution()
        {
            // Process any remaining events from actions
            _eventBus.ProcessAll();
            SetPhase(BattlePhase.TurnEnd);
        }

        private void HandleTurnEnd()
        {
            _turnSystem.EndTurn();
            SetPhase(BattlePhase.TeamTurn);
        }

        private void HandleBattleEnd()
        {
            Debug.Log($"[Battle] Ended with state: {_battleState}");
            _eventBus.Enqueue(new BattleEndedEvent
            {
                FinalState = _battleState,
                TurnsElapsed = _turnSystem.CurrentTurn,
                PlayerDefeated = _playerDefeatedCount,
                EnemyDefeated = _enemyDefeatedCount
            });
        }

        private void SetPhase(BattlePhase newPhase)
        {
            if (_currentPhase == newPhase)
                return;

            _currentPhase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);
        }

        // ================================
        // Victory Conditions
        // ================================

        private void CheckVictoryConditions()
        {
            if (_battleState != BattleState.InProgress)
                return;

            // Check if all enemies defeated
            if (_enemyDefeatedCount >= _enemyUnits.Count)
            {
                SetBattleState(BattleState.PlayerVictory);
                return;
            }

            // Check if all players defeated
            if (_playerDefeatedCount >= _playerUnits.Count)
            {
                SetBattleState(BattleState.EnemyVictory);
                return;
            }

            // Check turn limit
            if (_turnSystem.CurrentTurn > _config.TurnLimit)
            {
                SetBattleState(BattleState.Draw);
                return;
            }
        }

        private void SetBattleState(BattleState newState)
        {
            if (_battleState == newState)
                return;

            _battleState = newState;
            SetPhase(BattlePhase.BattleEnd);
            OnBattleStateChanged?.Invoke(newState);
        }

        // ================================
        // Unit Management
        // ================================

        public bool TryGetUnitState(int unitId, out BattleUnitState state)
        {
            return _unitStates.TryGetValue(unitId, out state);
        }

        public void DefeatUnit(int unitId)
        {
            if (!_unitStates.TryGetValue(unitId, out var state))
                return;

            Debug.Log($"[Battle] Unit defeated: {state.Unit.Name}");

            _turnSystem.MarkUnitDefeated(state);

            if (state.TeamId == 0)
                _playerDefeatedCount++;
            else
                _enemyDefeatedCount++;

            OnUnitDefeated?.Invoke(state);
            _eventBus.Enqueue(new CombatUnitDefeatedEvent
            {
                DefeatedUnitId = unitId,
                KillerUnitId = _turnSystem.CurrentActor?.Unit.Id ?? -1
            });
        }


        // ================================
        // State Access
        // ================================

        public BattleState GetBattleState() => _battleState;
        public BattlePhase GetCurrentPhase() => _currentPhase;
        public BattleUnitState GetCurrentActor() => _turnSystem.CurrentActor;
        public int GetCurrentTurn() => _turnSystem.CurrentTurn;
        public IReadOnlyList<BattleUnitState> GetTurnOrder() => _turnSystem.TurnOrder;

        // ================================
        // EVENT LISTENERS
        // ================================

        private class UnitDefeatedListener : EventListener<UnitDefeatedEvent>
        {
            private readonly BattleManager _battleManager;

            public UnitDefeatedListener(BattleManager battleManager)
            {
                _battleManager = battleManager;
            }

            public override int Priority => 0;

            protected override void HandleTyped(UnitDefeatedEvent ev)
            {
                _battleManager.DefeatUnit(ev.UnitId);
            }
        }
    }
}
