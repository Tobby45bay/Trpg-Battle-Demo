using System.Collections;
using TRPG.Core.Event;
namespace TRPG.Core.PhaseSystem
{
    public interface ITurnSystem
    {
        int CurrentTurn { get; }

        int CurrentTeam { get; }

        void Start();
        void EndTurn();
    }

    public interface IPhase { }

    public interface IPhaseManager
    {
        IPhase Current { get; }
        void Advance();
    }

    public interface IInitiativeSystem
    {
        void Advance();
    }

    public struct TurnStartedEvent : IEventContext
    {
        public int TeamId;
    }

    public struct TurnEndedEvent : IEventContext
    {
        public int TeamId;
    }
}