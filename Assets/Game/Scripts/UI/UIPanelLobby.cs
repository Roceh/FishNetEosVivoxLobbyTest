using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using FishNet;
using FishNet.Managing.Scened;
using FishNet.Plugins.FishyEOS.Util;
using FishNet.Transporting.FishyEOSPlugin;
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

        // EOS host id 
        public string OwnerId { get; set; }

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

        private void JoinLobby()
        {
            PlayerManager.Instance.ActiveLobbyId = null;

            JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions();
            joinOptions.LocalUserId = EOS.LocalProductUserId;
            joinOptions.LobbyId = LobbyId;

            // show busy panel while we join the lobby
            UIPanelManager.Instance.ShowPanel<UIPanelBusy>();

            EOS.GetCachedLobbyInterface().JoinLobbyById(ref joinOptions, null, delegate (ref JoinLobbyByIdCallbackInfo data)
            {
                if (data.ResultCode != Result.Success)
                {
                    Debug.LogErrorFormat("UIPanelLobby: JoinLobby error '{0}'", data.ResultCode);

                    UIPanelManager.Instance.HidePanel<UIPanelBusy>();
                    UIPanelManager.Instance.HidePanel<UIPanelLobby>();
                    return;
                }

                Debug.Log("UIPanelLobby: Joined lobby." + data.LobbyId);
                Debug.Log("UIPanelLobby: Lobby owner " + OwnerId);

                var fishy = InstanceFinder.NetworkManager.GetComponent<FishyEOS>();
                fishy.RemoteProductUserId = OwnerId;

                // store which lobby have joined
                PlayerManager.Instance.ActiveLobbyId = data.LobbyId;

                Debug.Log($"UIPanelLobby: Connected to EOS lobby successfully.");

                InstanceFinder.ClientManager.StartConnection();

                Debug.Log($"UIPanelLobby: Started client connection.");
            });
        }

        private void CreateLobby()
        {
            PlayerManager.Instance.ActiveLobbyId = null;

            LobbyId = null;

            var options = new CreateLobbyOptions
            {
                LocalUserId = EOS.LocalProductUserId,
                MaxLobbyMembers = 4,
                PermissionLevel = LobbyPermissionLevel.Publicadvertised,
                PresenceEnabled = false,
                AllowInvites = false,
                BucketId = EOSConsts.AllLobbiesBucketId,
                DisableHostMigration = true,
                EnableRTCRoom = false,
                EnableJoinById = false,
                RejoinAfterKickRequiresInvite = false
            };

            // show busy panel while we create the lobby
            UIPanelManager.Instance.ShowPanel<UIPanelBusy>();

            EOS.GetCachedLobbyInterface().CreateLobby(ref options, null,
                delegate (ref CreateLobbyCallbackInfo data)
                {
                    // created ok ?
                    if (data.ResultCode != Result.Success)
                    {
                        Debug.LogError("UIPanelLobby: Failed to create EOS lobby");

                        UIPanelManager.Instance.HidePanel<UIPanelBusy>();
                        UIPanelManager.Instance.HidePanel<UIPanelLobby>();

                        return;
                    }

                    Debug.Log($"UIPanelLobby: Created EOS lobby {data.LobbyId}");

                    LobbyModification lobbyModification = new LobbyModification();
                    AttributeData lobbyNameAttributeData = new AttributeData { Key = "lobby_name", Value = UIPanelHostDetails.Instance.LobbyName };

                    var updateLobbyModificationOptions = new UpdateLobbyModificationOptions { LobbyId = data.LobbyId, LocalUserId = EOS.LocalProductUserId };

                    EOS.GetCachedLobbyInterface().UpdateLobbyModification(ref updateLobbyModificationOptions, out lobbyModification);

                    var attributeLobbyName = new LobbyModificationAddAttributeOptions { Attribute = lobbyNameAttributeData, Visibility = LobbyAttributeVisibility.Public };
                    lobbyModification.AddAttribute(ref attributeLobbyName);

                    var updateLobbyOptions = new UpdateLobbyOptions { LobbyModificationHandle = lobbyModification };

                    EOS.GetCachedLobbyInterface().UpdateLobby(ref updateLobbyOptions, null, delegate (ref UpdateLobbyCallbackInfo updateData)
                    {
                        if (updateData.ResultCode != Result.Success)
                        {
                            Debug.LogError($"UIPanelLobby: Failed to update EOS lobby {updateData.LobbyId}: {updateData.ResultCode}");

                            UIPanelManager.Instance.HidePanel<UIPanelBusy>();
                            UIPanelManager.Instance.HidePanel<UIPanelLobby>();
                        }
                        else
                        {
                            // record the lobby id to the player manager - so that on disconnection of client
                            // we can remove them from the EOS lobby
                            PlayerManager.Instance.ActiveLobbyId = updateData.LobbyId;

                            // used to leave the lobby
                            LobbyId = updateData.LobbyId;
                            OwnerId = EOS.LocalProductUserId.ToString();

                            Debug.Log($"UIPanelLobby: Updated EOS lobby {updateData.LobbyId} successfully. Lobby has now been created.");

                            // if we are host create the FishNet server & local client
                            InstanceFinder.ServerManager.StartConnection();
                            InstanceFinder.ClientManager.StartConnection();

                            Debug.Log($"UIPanelLobby: Started local server and client.");
                        }
                    });
                });
        }

        protected override void OnShowing()
        {
            // add event callbacks
            PlayerManager.Instance.PlayersChanged += PlayerManager_PlayersChanged;
            InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
            VivoxManager.Instance.OnUserLoggedInEvent += VivoxManager_OnUserLoggedInEvent;
            VivoxManager.Instance.OnUserLoggedOutEvent += VivoxManager_OnUserLoggedOutEvent;

            // connect to vivox
            VivoxManager.Instance.Login(Settings.Instance.CurrentPlayerName);

            // setup default state of ui
            players.ClearPlayers();
            UpdateControlState();

            if (IsHost)
            {
                // we are the host - so create the EOS lobby - and create the server once lobby is up
                CreateLobby();
            }
            else
            {
                // we are the client - connect to given parameters
                JoinLobby();
            }
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

        private void HideBusyIfAllConnected()
        {
            if (_isVivoxConnected && _isFishnetConnected)
            {
                UIPanelManager.Instance.HidePanel<UIPanelBusy>();
            }
        }

        private void LeaveIfAllDisconnected()
        {
            // if we are not connected to fishnet or vivox kill the lobby ui
            if (!_isFishnetConnected && !_isVivoxConnected)
            {
                // disconnect from lobby - something has gone wrong with the connection to vivox or fishnet server
                LeaveLobby();

                // hide the lobby
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

                Debug.Log("UIPanelLobby: FishNet connection has started.");

                HideBusyIfAllConnected();
            }
            else if (obj.ConnectionState == FishNet.Transporting.LocalConnectionState.Stopped)
            {
                _isFishnetConnected = false;

                Debug.Log("UIPanelLobby: FishNet connection has stopped.");

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
            // could be in middle of joining
            UIPanelManager.Instance.HidePanel<UIPanelBusy>();

            if (!string.IsNullOrEmpty(LobbyId))
            {
                var updateLobbyModificationOptions = new LeaveLobbyOptions { LocalUserId = EOS.LocalProductUserId, LobbyId = LobbyId };

                // specifically leave lobby instead of letting the FishNet stop connection to handle it - its possible we have joined the lobby
                // but have not connected to the server - so we want to make sure we leave the lobby regardless of that state
                EOS.GetCachedLobbyInterface().LeaveLobby(ref updateLobbyModificationOptions, null, delegate (ref LeaveLobbyCallbackInfo data)
                {
                    if (data.ResultCode != Result.Success)
                    {
                        Debug.Log($"Failed to leave EOS lobby: {data.ResultCode}");
                    }
                    else
                    {
                        Debug.Log($"Left EOS lobby successfully");
                    }
                });

                if (IsHost)
                {
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
        }

        private void VivoxManager_OnUserLoggedOutEvent()
        {
            _isVivoxConnected = false;

            Debug.Log("UIPanelLobby: Vivox user has logged out.");

            UpdateControlState();
            LeaveIfAllDisconnected();
        }

        private void VivoxManager_OnUserLoggedInEvent()
        {
            // logged into vivox - so now join the channel using the lobbyid as the vivox channel name
            VivoxManager.Instance.JoinChannel(LobbyId + "_lobby", VivoxUnity.ChannelType.NonPositional, VivoxManager.ChatCapability.AudioOnly, true, null,
                () =>
                {
                    _isVivoxConnected = true;

                    Debug.Log("UIPanelLobby: Vivox user has logged in and joined lobby.");

                    HideBusyIfAllConnected();
                    UpdateControlState();
                });           
        }
    }
}