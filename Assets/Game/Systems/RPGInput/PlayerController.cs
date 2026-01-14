using UnityEngine;

namespace Game.Systems.RPGInput
{
    public class PlayerController : MonoBehaviour
    {
        private IInputHandler currentHandler;
        private InputRouter input;
        private HandlerContext context;

        public void Initialize(HandlerContext sharedContext)
        {
            context = sharedContext;

            input = FindObjectOfType<InputRouter>();
            input.OnIntent += HandleIntent;
        }

        private void HandleIntent(InputIntent intent)
        {
            currentHandler?.HandleInput(intent);
        }

        public void SwitchState(HandlerState state)
        {
            context.SetState(state);
            currentHandler = HandlerFactory.Create(state, context, this);
        }
    }
}
