using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game.Systems.RPGInput
{
    public class InputRouter : MonoBehaviour
    {
        public event System.Action<InputIntent> OnIntent;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.W))
                Emit(InputIntent.NavigateUp);
            else if (Input.GetKeyDown(KeyCode.S))
                Emit(InputIntent.NavigateDown);
            else if (Input.GetKeyDown(KeyCode.A))
                Emit(InputIntent.NavigateLeft);
            else if (Input.GetKeyDown(KeyCode.D))
                Emit(InputIntent.NavigateRight);
            else if (Input.GetKeyDown(KeyCode.Space))
                Emit(InputIntent.Confirm);
            else if (Input.GetKeyDown(KeyCode.Escape))
                Emit(InputIntent.Cancel);
        }

        public void Emit(InputIntent intent)
        {
            OnIntent?.Invoke(intent);
        }
    }


    public enum InputIntent
    {
        NavigateUp,
        NavigateDown,
        NavigateLeft,
        NavigateRight,
        Confirm,
        Cancel
    }
}

