using Epic.OnlineServices.Lobby;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EOSLobbyTest
{
    public class UILobbyItem : MonoBehaviour
    {
        [Tooltip("UI text item for name of lobby")]
        [SerializeField]
        private Text textLobbyName;

        [Tooltip("UI text item for player count status")]
        [SerializeField]
        private Text textPlayerCount;

        [Tooltip("UI button to allow joining of lobby")]
        [SerializeField]
        private Button buttonJoin;

        // Display name for lobby
        public string LobbyName
        {
            get => textLobbyName.text;
            set => textLobbyName.text = value;
        }

        // EOS Id for lobby
        public LobbyDetails LobbyDetails { get; set; }

        // EOS extra info about lobby 
        private LobbyDetailsInfo _lobbyDetailsInfo;
        public LobbyDetailsInfo LobbyDetailsInfo
        {
            get => _lobbyDetailsInfo;
            set
            {
                _lobbyDetailsInfo = value;
                UpdateControlState();
            }
        }

        public event Action<string, LobbyDetails, LobbyDetailsInfo> JoinRequest;

        private void UpdateControlState()
        {
            buttonJoin.interactable = _lobbyDetailsInfo.AvailableSlots > 0;
            textPlayerCount.text = $"{_lobbyDetailsInfo.MaxMembers - _lobbyDetailsInfo.AvailableSlots} / {_lobbyDetailsInfo.MaxMembers}";
        }

        public void Join()
        {
            JoinRequest?.Invoke(LobbyName, LobbyDetails, LobbyDetailsInfo);
        }
    }
}
