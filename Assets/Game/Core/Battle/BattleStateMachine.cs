using Game.Systems.Trigger;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game.Core.Battle
{
    public class BattleStateMachine 
    {
        private readonly Dictionary<BattleState, IBattleState> states = new();
        private IBattleState currentState;
        private IBattleState nextState;

        public BattleState Current => currentState?.State ?? BattleState.None;

        public void RegisterState(IBattleState state)
        {
            states[state.State] = state;
        }

        public void ChangeState(BattleState state)
        {
            if (!states.TryGetValue(state, out var next))
            {
                Debug.LogError($"BattleState {state} not registered");
                return;
            }

            nextState = next;
        }

        public void Update()
        {
            // Deferred transition (safe mid-update)
            if (nextState != null)
            {
                currentState?.Exit();
                currentState = nextState;
                nextState = null;
                currentState.Enter();
            }

            currentState?.Update();
        }
    }

    public enum BattleState
    {
        None,
        StartBattle,

        TurnStart,
        PhaseStart,
        PhaseUpdate,
        PhaseEnd,

        TurnEnd,

        Interrupt,   // level up, dialogue, ambush, etc.
        BattleEnd
    }

    public interface IBattleState
    {
        BattleState State { get; }
        void Enter();
        void Update();
        void Exit();
    }
    public class PhaseStartState : IBattleState
    {
        private readonly TurnManager tm;

        public BattleState State => BattleState.PhaseStart;

        public PhaseStartState(TurnManager tm)
        {
            this.tm = tm;
        }

        public void Enter()
        {
            tm.StartPhase();
        }

        public void Update()
        {
            var handler = tm.GetCurrentHandler();
            handler.BeginPhase(tm.GetUnitsInPhase(tm.CurrentPhase));

            BattleManager.Instance.StateMachine
                .ChangeState(BattleState.PhaseUpdate);
        }

        public void Exit() { }
    }

    public class PhaseUpdateState : IBattleState
    {
        private readonly TurnManager tm;
        private readonly BattleStateMachine fsm;

        public BattleState State => BattleState.PhaseUpdate;

        public PhaseUpdateState(TurnManager tm, BattleStateMachine fsm)
        {
            this.tm = tm;
            this.fsm = fsm;
        }

        public void Update()
        {
            if (tm.GetCurrentHandler().PhaseComplete)
                fsm.ChangeState(BattleState.PhaseEnd);
        }

        public void Enter() { }
        public void Exit() { }
    }

    public class PhaseEndState : IBattleState
    {
        private readonly TurnManager tm;

        public BattleState State => BattleState.PhaseEnd;

        public PhaseEndState(TurnManager tm)
        {
            this.tm = tm;
        }

        public void Enter()
        {
            tm.EndPhase();
        }

        public void Update()
        {
            if (!tm.AdvancePhase())
            {
                BattleManager.Instance.StateMachine
                    .ChangeState(BattleState.TurnEnd);
            }
            else
            {
                BattleManager.Instance.StateMachine
                    .ChangeState(BattleState.PhaseStart);
            }
        }

        public void Exit() { }
    }

    public class TurnStartState : IBattleState
    {
        private readonly TurnManager tm;

        public BattleState State => BattleState.TurnStart;

        public TurnStartState(TurnManager tm)
        {
            this.tm = tm;
        }

        public void Enter()
        {
            tm.IncreacseTurnCounter();
            tm.StartTurn();
        }

        public void Update()
        {
            // Immediately advance
            BattleManager.Instance.StateMachine
                .ChangeState(BattleState.PhaseStart);
        }

        public void Exit() { }
    }

    public class StartBattleState : IBattleState
    {
        private readonly TurnManager tm;

        public BattleState State => BattleState.StartBattle;

        public StartBattleState(TurnManager tm)
        {
            this.tm = tm;
        }

        public void Enter()
        {
            tm.StartBattle();
        }

        public void Update()
        {
            BattleManager.Instance.StateMachine
                .ChangeState(BattleState.TurnStart);
        }

        public void Exit() { }
    }
}

