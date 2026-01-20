using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Game.Core.Game.BattleUI.CommandMenu;

namespace Game.Core.Game.BattleUI
{
    public class CommandButton : MonoBehaviour
    {
        public CommandSelection Command { get; private set; }

        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI label;

        public void Initialize(
            CommandSelection command,Sprite iconSprite)
        {
            Command = command;
            label.text = command.ToString();
            icon.sprite = iconSprite;
            gameObject.SetActive(true);
        }

        public void SetSelected(bool selected)
        {
            label.color = selected ? Color.yellow : Color.white;
        }
    }
}
