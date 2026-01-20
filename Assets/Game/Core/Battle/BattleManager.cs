using Game.Core.Game;
using Game.Core.Game.BattleUI;
using Game.Systems.BattleMap;
using Game.Systems.Combat;
using Game.Systems.Effect;
using Game.Systems.Faction;
using Game.Systems.Item;
using Game.Systems.Job;
using Game.Systems.Logging;
using Game.Systems.RPGInput;
using Game.Systems.Skill;
using Game.Systems.Stat;
using Game.Systems.Trigger;
using Game.Systems.Units;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Game.Core.Game.GameManager;

namespace Game.Core.Battle
{
    public class BattleManager : MonoBehaviour
    {
        public BattleLogger _Logger { get; private set; }
        public BattleStateMachine StateMachine { get; private set; }
        public static void LogEvent(BattleEvent evt)
        {
            Instance._Logger.Log(evt);
        }

        public CommandMenu CommandMenu;

        public static BattleManager Instance { get; private set; }
        public GameDatabases gameDatabases { get; set; }
        public BattleSystems battleSystems;
        public TilemapManager tilemapManager;
        public BattleConfigSO config;

        private bool battleActive = false;
        private int testFactionId = 3;

        public void Awake()
        {
            Instance = this;
            _Logger = new BattleLogger();
            battleSystems = new BattleSystems();
            battleSystems.InitializeSystems(GameManager.Instance, tilemapManager);
            InitializeBattleStateMachine();

            CommandMenu = FindObjectOfType<CommandMenu>();
            var battleData = new BattleConfigData()
            {
                mapData = config.MapData,
                StartingTiles = config.StatingTiles,
                UnitsInMap = config.UnitsOnMap,
            };
            battleSystems.TilemapManager.LoadMap(config.MapData);
            LoadUnitsOnMap(battleData);

            StartBattle();
        }

        public void InitializeBattleStateMachine()
        {
            StateMachine = new BattleStateMachine();

            StateMachine.RegisterState(new StartBattleState(battleSystems.TurnManager));
            StateMachine.RegisterState(new TurnStartState(battleSystems.TurnManager));
            StateMachine.RegisterState(new PhaseStartState(battleSystems.TurnManager));
            StateMachine.RegisterState(new PhaseUpdateState(battleSystems.TurnManager,StateMachine));
            StateMachine.RegisterState(new PhaseEndState(battleSystems.TurnManager));
            //StateMachine.RegisterState(new TurnEndState(battleSystems.TurnManager));
            //StateMachine.RegisterState(new InterruptState());
        }

        public void LoadUnitsOnMap(BattleConfigData configData)
        {
            foreach (var unitSave in configData.UnitsInMap)
            {
                var unit = battleSystems.UnitFactory.CreateUnit(unitSave);

                battleSystems.UnitRunTimeManager.RegisterUnit(unit.instanceId, unit);
                var log = new CreateUnitLog(unit);
                var evt = new BattleEvent(battleSystems.TurnManager.GetCurentTurn(), log);
                LogEvent(evt);
                battleSystems.UnitRunTimeManager.PrintUnit(unit.instanceId);

                battleSystems.TurnManager.AddUnit(unit, testFactionId);
                battleSystems.TilemapManager.RegisterUnit(unit.instanceId, unit.templateId, unitSave.OverrideTilePostion);
            }
        }

        public void StartBattle()
        {
            battleActive = true;

            StateMachine.ChangeState(BattleState.StartBattle);
        }

        public void Update()
        {
            StateMachine.Update();
        }
    }

    public class BattleSystems
    {
        public GlobalSystems GlobalSystems;
        public GameDatabases GameDatabases;

        public StatManager StatManager { get; private set; }
        public SkillManager SkillManager { get; private set; }
        public EffectManager EffectManager { get; private set; }
        public UnitRunTimeManager UnitRunTimeManager { get; private set; }
        public UnitFactory UnitFactory { get; private set; }
        public InventoryManager InventoryManager { get; private set; }
        public AttackLoadoutManager AttackLoadOutManager { get; private set; }
        public JobManager JobManager { get; private set; }
        public BattleLoader BattleLoader { get; private set; }
        public TilemapManager TilemapManager { get; private set; }
        public TurnManager TurnManager { get; private set; }
        public CombatCoordinator CombatCoordinator { get; private set; }
        public MenuInputController MenuInputController { get; private set; }

        public void InitializeSystems(GameManager gm, TilemapManager tilemapManager)
        {

            StatManager = new StatManager();
            StatManager.InitializeSystem(gm.tagDb);

            SkillManager = new SkillManager();
            SkillManager.InitializeSystem(gm, this);

            EffectManager = new EffectManager();
            EffectManager.InitializeSystem(gm.EffectDb, gm.triggerDispatcher);

            UnitRunTimeManager = new UnitRunTimeManager();
            UnitRunTimeManager.Initialize(this);

            UnitFactory = new UnitFactory();
            UnitFactory.Initialize(gm.UnitDb, this);

            InventoryManager = new InventoryManager();
            InventoryManager.InitializeSystem(gm);

            AttackLoadOutManager = new AttackLoadoutManager();
            AttackLoadOutManager.InitializeSystem(gm.AttackDb);

            JobManager = new JobManager();
            JobManager.InitializeSystem(gm, this);

            TilemapManager = tilemapManager;
            TilemapManager.Initialize(gm, this);

            TurnManager = new TurnManager();
            TurnManager.InitializeSystem(gm, this);

            CombatCoordinator = new CombatCoordinator();
            CombatCoordinator.InitializeSystem(this, gm);

            MenuInputController = new MenuInputController();
        }

        public int ResolveTarget(TargetType type, EffectContext ctx)
        {
            return type switch
            {
                TargetType.Self => ctx.SourceId,
                TargetType.Other => ctx.TargetUnitId,
                _ => ctx.TargetUnitId
            };
        }

        public void PrintApplyEffectLog(EffectContext ctx)
        {
            var sourceUnit =
                UnitRunTimeManager.GetUnit(ctx.SourceUnitId).unitName;

            var targetUnit =
                UnitRunTimeManager.GetUnit(ctx.TargetUnitId).unitName;

            var effectName =
                GameManager.Instance.EffectDb
                    .GetEffect(ctx.EffectInstance.Key.EffectId).EffectName;

            string sourceName = ctx.SourceRefCode switch
            {
                SourceType.Skill =>
                    GameManager.Instance.SkillDb.GetSkill(ctx.SourceRefId).skillName,

                SourceType.Item =>
                    GameManager.Instance.ItemDb.GetItem(ctx.SourceRefId).itemName,

                SourceType.Effect =>
                    GameManager.Instance.EffectDb.GetEffect(ctx.SourceRefId).EffectName,

                _ => "Unknown"
            };

            var log =
                $"{sourceUnit} applied {effectName} to {targetUnit} from {sourceName}";

            Debug.Log(log);
        }

        public void PrintApplyModifierLog(EffectContext ctx,ModifierEffectData modifierData)
        {
            var targetName = UnitRunTimeManager.GetUnit(ctx.TargetUnitId).unitName;

            string sourceInstanceName = "";
            string sourceName = "";

            switch (ctx.SourceRefCode)
            {
                case SourceType.Unit:
                    sourceInstanceName =
                        UnitRunTimeManager.GetUnit(ctx.SourceId).unitName;

                    sourceName =
                        GameManager.Instance.SkillDb
                            .GetSkill(ctx.SourceRefId).skillName;
                    break;

                case SourceType.Effect:
                    sourceInstanceName =
                        UnitRunTimeManager.GetUnit(ctx.SourceId).unitName;

                    sourceName =
                        GameManager.Instance.EffectDb
                            .GetEffect(ctx.SourceRefId).EffectName;
                    break;

                case SourceType.Weapon:
                    sourceInstanceName =
                        GameManager.Instance.ItemDb
                            .GetItem(ctx.SourceId).name;

                    sourceName =
                        GameManager.Instance.ItemDb
                            .GetItem(ctx.SourceRefId).name;
                    break;
            }

            var modLog =
                $"{modifierData.Type} {modifierData.Value} " +
                $"to {modifierData.GetStatKey()}";

            var log =
                $"{targetName} received {modLog} from {sourceName} ({sourceInstanceName})";

            Debug.Log(log);
        }

        public void PrintAttackLoadoutLog(int unitId,int attackId,bool added)
        {
            var unitName = UnitRunTimeManager.GetUnit(unitId).unitName;
            var attackName =GameManager.Instance.AttackDb.GetAttack(attackId).name;
            var action = added ? "added" : "removed";
            Debug.Log($"{attackName} was {action} to {unitName}");
        }

        public void PrintAttackSelectedLog(int unitId, int attackId, bool isDefualt)
        {
            var unitName = UnitRunTimeManager.GetUnit(unitId).unitName;
            var attackName = GameManager.Instance.AttackDb.GetAttack(attackId).name;
            var action =  isDefualt ? "default" : "selelected";
            Debug.Log($"{attackName} was  set as {action} attack : on {unitName}");
        }

        public void ResloveRequest(ActionRequest request)
        {
            switch (request.ActionType)
            {
                case ActionType.Move:
                    TilemapManager.TryMoveUnit(request.ActorId, request.TargetTile);
                    break;
                case ActionType.Wait:
                    break;
                case ActionType.Attack:
                    break;
            }
        }

    }

    public class BattleContext : TriggerContext
    {
        public int SourceUnitId;
        public int TargetUnitId;
    }

    public enum TurnPhase
    {
        Player,
        Ally,
        Enemy,
        Neutral
    }

    public class TurnManager
    {
        public int TurnCount { get; private set; }
        public TurnPhase CurrentPhase { get; private set; }

        private GameManager gm;
        private BattleSystems bs;

        private readonly Dictionary<TurnPhase, List<int>> unitsByPhase = new();
        private Dictionary<TurnPhase, PhaseHandler> phaseHandlers;
        private int phaseIndex;

        public List<TurnPhase> PhaseOrder = new()
        {
            TurnPhase.Player,
            TurnPhase.Ally,
            TurnPhase.Enemy,
            TurnPhase.Neutral
        };

        public PhaseHandler GetCurrentHandler() => phaseHandlers[CurrentPhase];

        public void InitializeSystem(GameManager gameManager, BattleSystems battleSystems)
        {
            TurnCount = 0;
            gm = gameManager;
            bs = battleSystems;

            foreach (TurnPhase phase in Enum.GetValues(typeof(TurnPhase)))
                unitsByPhase[phase] = new List<int>();

            phaseHandlers = new Dictionary<TurnPhase, PhaseHandler>
            {
                { TurnPhase.Player, new PlayerHandler() },
                { TurnPhase.Enemy, new AIHandler() },
                { TurnPhase.Ally, new AIHandler() },
                { TurnPhase.Neutral, new AIHandler() }
            };

            foreach (var handler in phaseHandlers.Values)
                handler.Initialize(this, battleSystems);
        }

        

        public void AddUnit(UnitInstance unit, int playerFactionId)
        {
            var unitFactionId = unit.factionId;
            var unitId = unit.instanceId;
            if (unitFactionId == playerFactionId) { unitsByPhase[TurnPhase.Player].Add(unitId); return; }

            var factionRel = gm.FactionDb.GetRelationshipTag(unitFactionId, playerFactionId);
            switch (factionRel)
            {
                case FactionRelationshipTag.isAlly: unitsByPhase[TurnPhase.Ally].Add(unitId); break;
                case FactionRelationshipTag.isEnemy: unitsByPhase[TurnPhase.Enemy].Add(unitId); break;
                case FactionRelationshipTag.isNeutral: unitsByPhase[TurnPhase.Neutral].Add(unitId); break;
            }

        }

        public void Trigger(int unitId, TriggerKey triggerCall)
        {
            var context = new BattleContext
            {
                battleSystems = bs,
                triggerCall = triggerCall,
                SourceUnitId = unitId,
                TargetUnitId = unitId,
            };
            gm.triggerDispatcher.Trigger(context);
        }

        public void StartBattle()
        {
            var log = new LogEventData($"Battle has started");
            var evt = new BattleEvent(TurnCount, log);
            BattleManager.LogEvent(evt);

            foreach (var phase in unitsByPhase)
            {
                LoopThoughPhase(phase.Key, CoreTrigger.BattleStart);
            }
        }

        public void StartTurn()
        {
            var log = new LogEventData($"Turn has started");
            var evt = new BattleEvent(TurnCount, log);
            BattleManager.LogEvent(evt);
            phaseIndex = 0;
            CurrentPhase = PhaseOrder[0];

            foreach (var phase in unitsByPhase)
            {
                LoopThoughPhase(phase.Key,CoreTrigger.TurnStart);
            }
        }

        public void EndTurn()
        {
            var log = new LogEventData($"Turn has Ended");
            var evt = new BattleEvent(TurnCount, log);
            BattleManager.LogEvent(evt);

            foreach (var phase in unitsByPhase)
            {
                LoopThoughPhase(phase.Key, CoreTrigger.TurnEnd);
            }
        }

        private void LoopThoughPhase(TurnPhase turnPhase,CoreTrigger trigger)
        {
            foreach (var unitId in unitsByPhase[turnPhase])
            {
                Trigger(unitId, new TriggerKey(trigger));
                bs.UnitRunTimeManager.PrintUnit(unitId);
                if(trigger == CoreTrigger.TurnEnd) bs.UnitRunTimeManager.TickUnit(unitId);
            }
        }

        public void StartPhase()
        {
            var log = new LogEventData($"{CurrentPhase} has started");
            var evt = new BattleEvent(TurnCount, log);
            BattleManager.LogEvent(evt);
            var units = unitsByPhase[CurrentPhase];
            foreach (var unit in units)
            {
                Trigger(unit, new TriggerKey(CoreTrigger.PhaseStart));
                bs.UnitRunTimeManager.PrintUnit(unit);
            }
        }

        public void EndPhase()
        {
            var log = new LogEventData($"{CurrentPhase} has Ended");
            var evt = new BattleEvent(TurnCount, log);
            BattleManager.LogEvent(evt);
            var units = unitsByPhase[CurrentPhase];

            foreach (var unit in units)
            {
                Trigger(unit, new TriggerKey(CoreTrigger.PhaseEnd));
                bs.UnitRunTimeManager.PrintUnit(unit);
            }
        }


        public List<int> GetUnitsInPhase(TurnPhase phase)
        {
            return unitsByPhase[phase];
        }

        public int GetCurentTurn()
        {
            return TurnCount;
        }

        public void IncreacseTurnCounter()
        {
            TurnCount++;
            CurrentPhase = TurnPhase.Player;
        }

        public bool AdvancePhase()
        {
            phaseIndex++;

            if (phaseIndex >= PhaseOrder.Count)
            {
                return false; // no more phases this turn
            }

            CurrentPhase = PhaseOrder[phaseIndex];
            Debug.Log($"Advancing to phase {CurrentPhase}");
            return true;
        }

        public override string ToString()
        {
            return $"Turn: {TurnCount} phase{CurrentPhase}";
        }
    }
}