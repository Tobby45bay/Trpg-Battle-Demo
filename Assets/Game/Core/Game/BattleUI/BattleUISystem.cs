using Game.Systems.RPGInput;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Game.Core.Game.BattleUI
{
    public interface IMenu
    {
        void OnOpen();
        void OnClose();

        void OnNavigate(InputIntent intent);

        void OnConfirm();
        void OnCancel();
    }

    public class MenuInputController
    {
        private IMenu activeMenu;
        public bool HasActiveMenu => activeMenu != null;

        public void SetActiveMenu(IMenu menu)=> activeMenu = menu;

        public void CloseActiveMenu()
        {
            activeMenu?.OnClose();
            activeMenu = null;
        }

        public void HandleInput(InputIntent intent)
        {
            if (intent == InputIntent.Confirm)
                activeMenu?.OnConfirm();
            else if (intent == InputIntent.Cancel)
                activeMenu?.OnCancel();
            else
                activeMenu?.OnNavigate(intent);
        }
    }
}

