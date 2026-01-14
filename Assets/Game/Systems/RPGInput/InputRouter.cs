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
            // Human input
            if (Input.GetKeyDown(KeyCode.W))
                Emit(InputIntent.NavigateUp);
            if (Input.GetKeyDown(KeyCode.S))
                Emit(InputIntent.NavigateDown);
            if (Input.GetKeyDown(KeyCode.A))
                Emit(InputIntent.NavigateLeft);
            if (Input.GetKeyDown(KeyCode.D))
                Emit(InputIntent.NavigateRight);
            if (Input.GetKeyDown(KeyCode.Space))
                Emit(InputIntent.Confirm);
            if (Input.GetKeyDown(KeyCode.Escape))
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

