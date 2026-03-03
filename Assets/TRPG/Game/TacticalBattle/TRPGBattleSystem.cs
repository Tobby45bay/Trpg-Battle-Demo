
using System;
using System.Collections.Generic;
using System.Linq;
using TRPG.Core.Action;
using TRPG.Core.Event;
using TRPG.Core.Logging;
using TRPG.Game.Const;
using TRPG.Game.Systems.Rank.Actions;
using TRPG.Game.Systems.TRPGEfect;
using TRPG.Game.Unit;
using UnityEngine;
using GameEvent = TRPG.Game.Const.GameEvent;

namespace TRPG.Game.TacticalBattle
{
    public enum BattlePhase
    {
        Setup = 0,
        TeamTurn = 1,
        UnitAction = 2,
        Resolution = 3,
        TurnEnd = 4,
        BattleEnd = 5
    }

    public enum BattleState
    {
        NotStarted,
        InProgress,
        PlayerVictory,
        EnemyVictory,
        Draw
    }

    [Serializable]
    public class BattleConfigData
    {
        // Participants
        public List<int> PlayerUnitIds = new();
        public List<UnitConfigData> AllyUnitIds = new();
        public List<UnitConfigData> EnemyUnitIds = new();
        public List<UnitConfigData> OtherUnitIds = new();

        // Environment
        public int MapId;
        public int EnvironmentDifficulty = 1; // 1-5, affects enemy stats

        // Rules
        public int TurnLimit = 50;
        public bool AllowFleeing = true;
        public bool IsTraining = false;

        // Rewards
        public int ExpReward = 100;
        public int GoldReward = 500;
        public List<int> ItemRewards = new();

        // AI
        public int EnemyAILevel = 1; // 1-5, affects decision-making

        public override string ToString() =>
            $"Battle(Map:{MapId}, Diff:{EnvironmentDifficulty}, Players:{PlayerUnitIds.Count}, Enemies:{EnemyUnitIds.Count})";
    }

    public class UnitConfigData
    {
        //unit config
        public int UnitTemplateId;
        public int FactionId;
        //map postin
        public Vector2Int StartingPostion;
        public int TunrIndex;
    }

    public sealed class BattleUnitState
    {
        public TRPGUnit Unit { get; }
        public int TeamId { get; }
        public int InitiativeValue { get; }
        public int TurnOrder { get; internal set; }
        public bool IsDefeated { get; internal set; }
        public bool CanAct { get; internal set; } = true;

        // Temporary battle state
        public int CurrentActionPointsUsed { get; internal set; }
        public int CurrentSpeedModifier { get; internal set; }

        public BattleUnitState(TRPGUnit unit, int teamId)
        {
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            TeamId = teamId;
            InitiativeValue = (int)(unit.Stats.Stats.Get(GameConst.StatKeys.Speed).Value * 1.2f +
                                    unit.Stats.Stats.Get(GameConst.StatKeys.Finesse).Value * 0.5f);
        }

        public override string ToString() => $"{Unit.Name} (Team {TeamId}, Init: {InitiativeValue})";
    }

    public sealed class BattleTurnSystem
    {
        private int _currentTurn = 1;
        private int _currentTeamId = 0; 
        private BattleUnitState _currentActor;
        private readonly List<BattleUnitState> _turnOrder = new();
        private int _currentActorIndex = 0;

        public int CurrentTurn => _currentTurn;
        public int CurrentTeamId => _currentTeamId;
        public BattleUnitState CurrentActor => _currentActor;
        public IReadOnlyList<BattleUnitState> TurnOrder => _turnOrder.AsReadOnly();

        public void InitializeTurnOrder(List<BattleUnitState> allUnits)
        {
            _turnOrder.Clear();
            _turnOrder.AddRange(allUnits.Where(u => !u.IsDefeated));
            _turnOrder.Sort((a, b) => b.InitiativeValue.CompareTo(a.InitiativeValue));

            for (int i = 0; i < _turnOrder.Count; i++)
                _turnOrder[i].TurnOrder = i;

            _currentActorIndex = 0;
            _currentActor = _turnOrder.Count > 0 ? _turnOrder[0] : null;
            _currentTeamId = _currentActor?.TeamId ?? 0;
        }

        public void AdvanceToNextUnit()
        {
            if (_turnOrder.Count == 0)
                return;

            // Skip defeated units
            do
            {
                _currentActorIndex = (_currentActorIndex + 1) % _turnOrder.Count;
                _currentActor = _turnOrder[_currentActorIndex];
            } while (_currentActor.IsDefeated && _currentActorIndex < _turnOrder.Count);

            _currentTeamId = _currentActor.TeamId;
        }

        public void EndTurn()
        {
            _currentTurn++;
            AdvanceToNextUnit();
        }

        public void MarkUnitDefeated(BattleUnitState unit)
        {
            unit.IsDefeated = true;
            if (_currentActor == unit)
                AdvanceToNextUnit();
        }
    }

    public sealed class BattleLauncher
    {
        private BattleManager _battleManager;
        private BattleConfigData _currentConfig;

        public void Start(
            BattleConfigData config,
            BattleManager battleManager,
            List<TRPGUnit> playerUnits,
            List<TRPGUnit>allyUnits,
            List<TRPGUnit> enemyUnits,
            List<TRPGUnit>otherUnits)
        {
            _battleManager = battleManager ?? throw new ArgumentNullException(nameof(battleManager));
            _currentConfig = config ?? throw new ArgumentNullException(nameof(config));

            Debug.Log($"[BattleLauncher] Starting battle: {_currentConfig}");
            _battleManager.Initialize(_currentConfig, playerUnits,allyUnits, enemyUnits,otherUnits);
        }

        public BattleManager GetBattleManager() => _battleManager;
    }

    // =========================================================
    // BATTLE LOADER
    // =========================================================

    public sealed class BattleLoader
    {
        public BattleConfigData LoadConfig(int configId)
        {
            // Later: load from Resources or database
            Debug.Log($"[BattleLoader] Loading config: {configId}");
            return new BattleConfigData();
        }

        public List<TRPGUnit> LoadPlayerUnits(List<int> unitIds)
        {
            // Later: load from roster system
            Debug.Log($"[BattleLoader] Loading {unitIds.Count} player units");
            return new List<TRPGUnit>();
        }

        public List<TRPGUnit> LoadEnemyUnits(List<int> unitIds)
        {
            // Later: load from enemy database, apply difficulty scaling
            Debug.Log($"[BattleLoader] Loading {unitIds.Count} enemy units");
            return new List<TRPGUnit>();
        }

        public ActionPipeline RegisterActionExecutors(ActionDispatcher actionDispatcher, GamesDatabeses databeses, ICoreLogger logger)
        {
            //effect
            actionDispatcher.RegisterExecutor(new AddStatModifierFromEffectExecutor());
            actionDispatcher.RegisterExecutor(new ApplyEffectActionExecutor(databeses.EffectDatabase));
            actionDispatcher.RegisterExecutor(new RemoveEffectActionExecutor());
            actionDispatcher.RegisterExecutor(new IncreaseEffectStackActionExecutor());
            actionDispatcher.RegisterExecutor(new ReplaceEffectActionExecutor(actionDispatcher));

            return new ActionPipeline(actionDispatcher,logger);
        }
    }

    public sealed class BattleStartedEvent : GameEvent
    {
        public int PlayerUnits;
        public int AllyUnits;
        public int EnemyUnits;
        public int OtherUnits;
        public int TurnLimit;

        public BattleStartedEvent(TRPGUnit source, TRPGUnit target) : base(source, target)
        {
        }

        public override EventKey EventKey => throw new NotImplementedException();

        public override  string ToString() => $"BattleStarted(P:{PlayerUnits} vs E:{EnemyUnits}, TurnLimit:{TurnLimit})";
    }

    public struct BattleEndedEvent : IEventContext
    {
        public BattleState FinalState;
        public int TurnsElapsed;
        public int PlayerDefeated;
        public int EnemyDefeated;

        public override string ToString() => $"BattleEnded({FinalState}, Turns:{TurnsElapsed})";
    }


}
