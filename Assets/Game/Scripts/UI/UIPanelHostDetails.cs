using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EOSLobbyTest
{
    public class UIPanelHostDetails : UIPanel<UIPanelHostDetails>, IUIPanel
    {
        [SerializeField]
        private InputField inputFieldLobbyName;

        [SerializeField]
        private Button buttonSave;

        // room that has been entered by the user
        public string LobbyName { get; set; }

        private void Start()
        {
            inputFieldLobbyName.onValueChanged.AddListener(delegate 
            {
                UpdateControlState();
            });            
        }

        private void UpdateControlState()
        {
            buttonSave.interactable = !String.IsNullOrEmpty(inputFieldLobbyName.text);
        }

        protected override void OnShowing()
        {
            UpdateControlState();

            inputFieldLobbyName.text = "";
        }

        protected override void OnShown()
        {
            inputFieldLobbyName.ActivateInputField();
        }

        public void Save()
        {
            LobbyName = inputFieldLobbyName.text;

            UIPanelManager.Instance.HidePanel<UIPanelHostDetails>(true);
        }

        public void Cancel()
        {
            UIPanelManager.Instance.HidePanel<UIPanelHostDetails>(false);
        }
    }
}
