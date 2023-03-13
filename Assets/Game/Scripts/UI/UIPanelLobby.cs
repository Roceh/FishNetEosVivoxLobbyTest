using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.RTCAudio;
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
            joinOptions.LocalRTCOptions = new LocalRTCOptions { UseManualAudioOutput = true };

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

                SetupCustomRTC();

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
                EnableRTCRoom = true,
                EnableJoinById = false,
                RejoinAfterKickRequiresInvite = false,
                LocalRTCOptions = new LocalRTCOptions { UseManualAudioOutput = true }
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

                            SetupCustomRTC();

                            // if we are host create the FishNet server & local client
                            InstanceFinder.ServerManager.StartConnection();
                            InstanceFinder.ClientManager.StartConnection();

                            Debug.Log($"UIPanelLobby: Started local server and client.");
                        }
                    });
                });
        }

        private void SetupCustomRTC()
        {
            GetRTCRoomNameOptions options = new GetRTCRoomNameOptions()
            {
                LobbyId = LobbyId,
                LocalUserId = EOS.LocalProductUserId,
            };

            Result result = EOS.GetCachedLobbyInterface().GetRTCRoomName(ref options, out Utf8String roomName);

            if (result != Result.Success)
            {
                Debug.Log($"UIPanelLobby: Could not get RTC Room Name. Error Code: {result}");
                return;
            }

            Debug.Log($"UIPanelLobby: In RTC room {roomName}");

            AddNotifyAudioBeforeRenderOptions addNotifyAudioBeforeRenderOptions = new AddNotifyAudioBeforeRenderOptions();

            addNotifyAudioBeforeRenderOptions.LocalUserId = EOS.LocalProductUserId;
            addNotifyAudioBeforeRenderOptions.RoomName = roomName;
            addNotifyAudioBeforeRenderOptions.UnmixedAudio = true;

            EOS.GetCachedRTCInterface().GetAudioInterface().AddNotifyAudioBeforeRender(ref addNotifyAudioBeforeRenderOptions, null, delegate (ref AudioBeforeRenderCallbackInfo data)
            {
                var player = PlayerManager.Instance.GetPlayer(data.ParticipantId.ToString());

                if (player != null && data.Buffer.Value.Frames != null)
                {
                    player.EnqueueAudioFrame(data.Buffer.Value.Frames);
                }
            });
        }

        protected override void OnShowing()
        {
            // add event callbacks
            PlayerManager.Instance.PlayersChanged += PlayerManager_PlayersChanged;
            InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;

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
            PlayerManager.Instance.PlayersChanged -= PlayerManager_PlayersChanged;
            InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
        }

        private void UpdateControlState()
        {
            // only allow user to back out if we are fully setup
            buttonBack.interactable = _isFishnetConnected;
            // only show start game button for host
            SetShowStartGame(_isFishnetConnected && InstanceFinder.NetworkManager.IsServer);
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
            if (_isFishnetConnected)
            {
                UIPanelManager.Instance.HidePanel<UIPanelBusy>();
            }
        }

        private void LeaveIfAllDisconnected()
        {
            // if we are not connected to fishnet kill the lobby ui
            if (!_isFishnetConnected)
            {
                // disconnect from lobby - something has gone wrong with the connection to fishnet server
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
            }
        }
    }
}