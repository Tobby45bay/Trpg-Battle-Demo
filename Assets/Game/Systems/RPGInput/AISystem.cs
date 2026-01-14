using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
namespace Game.Systems.RPGInput
{
    public class AIController
    {
        private AIInputDriver input;

        public AIController(InputRouter router)
        {
            input = new AIInputDriver(router);
        }

        public void BeginTurn()
        {
            // Decide WHAT to do
        }
    }
    public class AIInputDriver
    {
        private InputRouter input;

        public AIInputDriver(InputRouter input)
        {
            this.input = input;
        }

        public void Press(InputIntent intent)
        {
            input.Emit(intent);
        }
    }

    public class AIHandler : PhaseHandler
    {
        public bool IsPlanning;

        public override void Update()
        {
            throw new System.NotImplementedException();
        }

        protected override bool IsPhaseComplete()
        {
            throw new System.NotImplementedException();
        }

        protected override void OnBeginPhase()
        {
            throw new System.NotImplementedException();
        }
    }


}

