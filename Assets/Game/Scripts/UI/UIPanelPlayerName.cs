using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EOSLobbyTest
{
    public class UIPanelPlayerName : UIPanel<UIPanelPlayerName>, IUIPanel
    {
        [SerializeField]
        private InputField inputFieldPlayerName;

        [SerializeField]
        private Button buttonSave;

        private void Start()
        {
            inputFieldPlayerName.onValueChanged.AddListener(delegate 
            {
                UpdateControlState();
            });            
        }

        private void UpdateControlState()
        {
            buttonSave.interactable = !String.IsNullOrEmpty(inputFieldPlayerName.text);
        }

        protected override void OnShowing()
        {
            UpdateControlState(); 
            
            inputFieldPlayerName.text = Settings.Instance.CurrentPlayerName;
        }

        protected override void OnShown()
        {
            inputFieldPlayerName.ActivateInputField();
        }

        public void Save()
        {
            Settings.Instance.CurrentPlayerName = inputFieldPlayerName.text;

            UIPanelManager.Instance.HidePanel<UIPanelPlayerName>(true);
        }

        public void Cancel()
        {
            UIPanelManager.Instance.HidePanel<UIPanelPlayerName>(false);
        }
    }
}
