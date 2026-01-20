using Game.Core.Battle;
using Game.Systems.Job;
using Game.Systems.RPGInput;
using Game.Systems.Units;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Game.Core.Game.BattleUI.CommandMenu;
namespace Game.Core.Game.BattleUI
{
    public class CommandMenu : MonoBehaviour, IMenu
    {
        [Header("Prefab")]
        [SerializeField] private CommandButton buttonPrefab;

        [Header("Layout")]
        [SerializeField] private Transform buttonRoot;

        [SerializeField] private Sprite buttonSprite;

        private readonly List<CommandButton> buttons = new();
        private readonly List<CommandSelection> commands = new();

        private int currentIndex;

        private UnitInstance unit;
        private BattleSystems bs;
        private PlayerController playerController;
        private HandlerContext context;

        private void Awake()
        {
            CreateButtons();
        }

        private void CreateButtons()
        {
            foreach (Transform child in buttonRoot)
                Destroy(child.gameObject);

            buttons.Clear();

            foreach (var command in System.Enum.GetValues(typeof(CommandSelection)))
            {
                var btn = Instantiate(buttonPrefab, buttonRoot);
                btn.Initialize((CommandSelection)command, buttonSprite);
                btn.gameObject.SetActive(false); // hidden by default
                buttons.Add(btn);
            }
        }

        public void Open(BattleSystems bs,int unitId,PlayerController playerController,HandlerContext context)
        {
            unit = bs.UnitRunTimeManager.GetUnit(unitId);
            this.bs = bs;
            this.playerController = playerController;
            this.context = context;

            BuildCommandList(unit);

            currentIndex = 0;
            RefreshSelection();

            OnOpen();
        }
        private void BuildCommandList(UnitInstance unit)
        {
            commands.Clear();

            if (!context.HasIntendedTile)
                commands.Add(CommandSelection.Move);

            commands.Add(CommandSelection.Attack);
            commands.Add(CommandSelection.View);
            commands.Add(CommandSelection.Wait);

            for (int i = 0; i < buttons.Count; i++)
            {
                bool enabled = commands.Contains(buttons[i].Command);
                buttons[i].gameObject.SetActive(enabled);
            }
        }

        public void OnConfirm()
        {
            ExecuteCommand(commands[currentIndex]);
        }

        public void OnCancel()
        {
            OnClose();
            if (context.HasIntendedTile)
            {
                bs.TilemapManager.GetUnitView(unit.instanceId, out var unitView);
                unitView.MoveToTile(bs.TilemapManager.GetWorldPosition(context.LastCommittedTile));
                bs.TilemapManager.cursorController.SnapTo(context.LastCommittedTile);
                context.ClearIntendedTile();
                playerController.SwitchState(HandlerState.MovingUnit); 
            }
            else
            {
                playerController.SwitchState(HandlerState.Idle);
            }

        }

        public void OnOpen()
        {
            gameObject.SetActive(true);
            Debug.Log("Opening menu");
        }

        public void OnClose()
        {
            gameObject.SetActive(false);
            bs.MenuInputController.SetActiveMenu(null);
            Debug.Log("Closing menu");
        }

        public void OnNavigate(InputIntent intent)
        {
            switch (intent)
            {
                case InputIntent.NavigateUp:
                    currentIndex = (currentIndex - 1 + commands.Count) % commands.Count;
                    RefreshSelection();
                    break;

                case InputIntent.NavigateDown:
                    currentIndex = (currentIndex + 1) % commands.Count;
                    RefreshSelection();
                    break;
            }
            Debug.Log($"{buttons[currentIndex].Command} is current selection");
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].SetSelected(i == currentIndex);
            }
        }

        private void ExecuteCommand(CommandSelection command)
        {
            switch (command)
            {
                case CommandSelection.Move:
                    playerController.SwitchState(HandlerState.MovingUnit);
                    OnClose();
                    break;

                case CommandSelection.Attack:
                    playerController.SwitchState(HandlerState.SelectingAttack);
                    OnClose();
                    break;

                case CommandSelection.View:
                    playerController.SwitchState(HandlerState.ViewingInventory);
                    OnClose();
                    break;

                case CommandSelection.Wait:

                    OnClose();
                    break;
            }
            Debug.Log($"{buttons[currentIndex].Command} is selected");
        }
    }

    public enum CommandSelection
    {
        Move,
        Attack,
        View,
        Wait
    }

}