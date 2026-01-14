using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.BattleMap
{
    public enum HightlightState
    {
        None,
        Move,
        Attack
    }

    public class TileView : MonoBehaviour
    {
        [SerializeField] private GameObject highlightObject;
        [SerializeField] private SpriteRenderer highlightRenderer;
        private TileInstance tileInstance;

        private void Awake()
        {
            // If you didn’t assign it in inspector, try to get it automatically
            if (highlightRenderer == null && highlightObject != null)
                highlightRenderer = highlightObject.GetComponent<SpriteRenderer>();
        }

        public void SetHighlight(HightlightState highlight)
        {
            if (highlightObject == null || highlightRenderer == null) return;

            switch (highlight)
            {
                case HightlightState.None:
                    highlightObject.SetActive(false);
                    break;
                case HightlightState.Move:
                    highlightObject.SetActive(true);
                    highlightRenderer.color = new Color(0f, 0f, 1f, 0.3f); // transparent blue
                    break;
                case HightlightState.Attack:
                    highlightObject.SetActive(true);
                    highlightRenderer.color = new Color(1f, 0f, 0f, 0.3f); // transparent red
                    break;
            }
        }

        public void Bind(TileInstance tileInstance)
        {
            this.tileInstance = tileInstance;
        }
    }
}

