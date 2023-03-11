using FishNet;
using FishNet.Managing.Scened;
using FishNet.Plugins.FishyEOS.Util;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EOSLobbyTest
{
    public class UIPanelLobby : UIPanel<UIPanelLobby>, IUIPanel
    {
        private bool _isFishnetConnected;
        private bool _isVivoxConnected;

        [Tooltip("Text item for lobby name")]
        [SerializeField]
        private Text textLobbyName;

        [Tooltip("Controller for list of players in lobby")]
        [SerializeField]
        private UIScrollViewPlayers players;

        [Tooltip("Button to back out ")]
        [SerializeField]
        private Button buttonBack;

        [Tooltip("Button to start game - host only")]
        [SerializeField]
        private Button buttonStartGame;

        // user given name of room
        public string LobbyName
        {
            get => textLobbyName.text;
            set => textLobbyName.text = value;
        }

        // EOS lobby info about room
        public string LobbyId { get; set; }

        // are we the host of this lobby
        public bool IsHost { get; set; }

        public void Back()
        {
            LeaveLobby();
        }

        public void StartGame()
        {
            InstanceFinder.SceneManager.LoadGlobalScenes(new SceneLoadData("Game") { ReplaceScenes = ReplaceOption.All });

            PlayerManager.Instance.ServerPlayer?.SendStartingGame();
        }

        protected override void OnShowing()
        {
            // add event callbacks
            PlayerManager.Instance.PlayersChanged += PlayerManager_PlayersChanged;
            InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
            VivoxManager.Instance.OnUserLoggedInEvent += VivoxManager_OnUserLoggedInEvent;
            VivoxManager.Instance.OnUserLoggedOutEvent += VivoxManager_OnUserLoggedOutEvent;

            if (IsHost)
            {
                // if we are host create the Fishnet server
                InstanceFinder.ServerManager.StartConnection();
            }

            // connect to the fishnet server - the details should be setup in the transport
            InstanceFinder.ClientManager.StartConnection();

            // connect to vivox
            VivoxManager.Instance.Login(Settings.Instance.CurrentPlayerName);

            // setup default state of ui
            players.ClearPlayers();
            UpdateControlState();
        }

        protected override void OnHidden()
        {
            // remove event callbacks
            VivoxManager.Instance.OnUserLoggedInEvent -= VivoxManager_OnUserLoggedInEvent;
            VivoxManager.Instance.OnUserLoggedOutEvent -= VivoxManager_OnUserLoggedOutEvent;
            PlayerManager.Instance.PlayersChanged -= PlayerManager_PlayersChanged;
            InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
        }

        private void UpdateControlState()
        {
            // only allow user to back out if we are fully setup
            buttonBack.interactable = _isFishnetConnected && _isVivoxConnected;
            // only show start game button for host
            SetShowStartGame(_isFishnetConnected && _isVivoxConnected && InstanceFinder.NetworkManager.IsServer);
        }

        private void PopulatePlayerList()
        {
            players.ClearPlayers();

            // extra check due to this triggering during app shutdown - EOS could be gone
            if (EOS.GetCachedConnectInterface() != null)
            {
                var playerInfos = PlayerManager.Instance.GetPlayers();

                foreach (var info in playerInfos)
                {
                    var playerItem = players.AddPlayer(info.UserId, info.PlayerName, info.UserId != EOS.LocalProductUserId.ToString() && InstanceFinder.NetworkManager.IsServer);
                    playerItem.KickRequest += PlayerItem_KickRequest;
                }
            }
        }

        private void PlayerItem_KickRequest(string playerId)
        {
            var player = PlayerManager.Instance.GetPlayers().FirstOrDefault(x => x.UserId == playerId);

            if (player != null)
            {
                InstanceFinder.NetworkManager.ServerManager.Kick(player.Owner.ClientId, FishNet.Managing.Server.KickReason.Unset);
            }
        }

        private void LeaveIfAllDisconnected()
        {
            // if we are not connected to fishnet or vivox kill the lobby ui
            if (!_isFishnetConnected && !_isVivoxConnected)
            {
                UIPanelManager.Instance.HidePanel<UIPanelLobby>();
            }
        }

        private void PlayerManager_PlayersChanged()
        {
            PopulatePlayerList();
        }

        private void ClientManager_OnClientConnectionState(FishNet.Transporting.ClientConnectionStateArgs obj)
        {
            if (obj.ConnectionState == FishNet.Transporting.LocalConnectionState.Started)
            {
                _isFishnetConnected = true;
            }
            else if (obj.ConnectionState == FishNet.Transporting.LocalConnectionState.Stopped)
            {
                _isFishnetConnected = false;
                VivoxManager.Instance.Logout();
                LeaveIfAllDisconnected();
            }

            UpdateControlState();
        }

        private void SetShowStartGame(bool status)
        {
            buttonStartGame.gameObject.SetActive(status);
        }

        private void LeaveLobby()
        {
            if (IsHost)
            {
                // if we are host shutdown the fishnet server
                InstanceFinder.ServerManager.StopConnection(true);
            }
            else
            {
                // if we are client just disconnect
                InstanceFinder.ClientManager.StopConnection();
            }

            // begin logout of vivox
            VivoxManager.Instance.Logout();
        }

        private void VivoxManager_OnUserLoggedOutEvent()
        {
            _isVivoxConnected = false;
            UpdateControlState();
            LeaveIfAllDisconnected();
        }

        private void VivoxManager_OnUserLoggedInEvent()
        {
            // logged into vivox - so now join the channel using the lobbyid as the vivox channel name
            VivoxManager.Instance.JoinChannel(LobbyId + "_lobby", VivoxUnity.ChannelType.NonPositional, VivoxManager.ChatCapability.AudioOnly, true, null,
                () =>
                {
                    Debug.Log("Connected to vivox lobby audio channel");
                    _isVivoxConnected = true;
                    UpdateControlState();
                });           
        }
    }
}