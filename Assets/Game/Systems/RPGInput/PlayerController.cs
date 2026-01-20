using Game.Core.Battle;
using Game.Core.Game.BattleUI;
using UnityEngine;

namespace Game.Systems.RPGInput
{
    public class PlayerController : MonoBehaviour
    {
        private IInputHandler currentHandler;
        private InputRouter input;
        private HandlerContext context;
        private BattleSystems battleSystems;
        private MenuInputController menuInput;

        public void Initialize(HandlerContext sharedContext, BattleSystems bs,MenuInputController menuInputController)
        {
            context = sharedContext;
            battleSystems = bs;
            menuInput = menuInputController;

            input = FindObjectOfType<InputRouter>();
            input.OnIntent += HandleIntent;
        }

        private void OnDisable()
        {
            if (input != null)
                input.OnIntent -= HandleIntent;
        }

        private void HandleIntent(InputIntent intent)
        {
            if (menuInput.HasActiveMenu)
            {
                menuInput.HandleInput(intent);
                return;
            }

            currentHandler?.HandleInput(intent);
        }

        public void SwitchState(HandlerState state)
        {
            context.SetState(state);

            currentHandler = HandlerFactory.Create(
                state,
                context,
                this,
                battleSystems
            );
            if (currentHandler == null)
            {
                Debug.LogError($"No input handler for state: {state}");
                SwitchState(HandlerState.Idle);
            }
        }
    }
}
