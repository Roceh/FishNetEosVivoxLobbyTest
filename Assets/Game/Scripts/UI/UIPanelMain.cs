using FishNet.Plugins.FishyEOS.Util;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using Epic.OnlineServices.Lobby;
using FishNet.Managing;
using System.Linq;
using FishNet.Transporting.FishyEOSPlugin;
using FishNet;

namespace EOSLobbyTest
{
    public class UIPanelMain : UIPanel<UIPanelMain>, IUIPanel
    {

        [Tooltip("Input for changing player name directly")]
        [SerializeField]
        private InputField inputFieldPlayerName;

        [Tooltip("Button for hosting game")]
        [SerializeField]
        private Button buttonHost;

        [Tooltip("Button for joining game")]
        [SerializeField]
        private Button buttonJoin;

        [Tooltip("Button for adjusting settings")]
        [SerializeField]
        private Button buttonSettings;

        private void Start()
        {
            inputFieldPlayerName.onValueChanged.AddListener(delegate
            {
                Settings.Instance.CurrentPlayerName = inputFieldPlayerName.text;
            });

            VivoxManager.Instance.OnInitialized += Vivox_OnAuthenticated;

            UpdateControlState();

            // do the connection to EOS
            StartCoroutine(ConnectToEOS());
        }

        private void Vivox_OnAuthenticated()
        {
            UpdateControlState();
        }

        private IEnumerator ConnectToEOS()
        {
            var authData = new AuthData();

            // do the EOS login
            authData.loginCredentialType = Epic.OnlineServices.Auth.LoginCredentialType.DeviceCode;
            yield return authData.Connect(out var authDataLogin);
            if (authDataLogin.loginCallbackInfo?.ResultCode != Result.Success)
            {
                // failed
                Debug.LogError($"[ServerPeer] Failed to authenticate with EOS Connect. {authDataLogin.loginCallbackInfo?.ResultCode}");
                UpdateControlState();
                yield break;
            }

            // force EOS relay always on - hides IP
            var relayControlOptions = new SetRelayControlOptions { RelayControl = RelayControl.ForceRelays };
            EOS.GetCachedP2PInterface().SetRelayControl(ref relayControlOptions);
            UpdateControlState();
        }

        private void UpdateControlState()
        {
            inputFieldPlayerName.text = Settings.Instance.CurrentPlayerName;

            // only allow host/join if all logged in ok
            buttonHost.interactable = EOS.LocalProductUserId != null;
            buttonJoin.interactable = EOS.LocalProductUserId != null;

            // as the voice device is not initialised until we logged in we have to wait on this - not ideal...
            buttonSettings.interactable = VivoxManager.Instance.Initialized;
        }

        protected override void OnShowing()
        {
            UpdateControlState();
        }

        private IEnumerator DoHostGame()
        {
            // is player name blank ?
            if (String.IsNullOrEmpty(Settings.Instance.CurrentPlayerName))
            {
                // show panel that gets player name
                yield return UIPanelManager.Instance.ShowPanelAndWaitTillHidden<UIPanelPlayerName>();

                // update player name on this panel
                UpdateControlState();
            }

            // only allow host if we have player name
            if (!String.IsNullOrEmpty(Settings.Instance.CurrentPlayerName))
            {
                yield return UIPanelManager.Instance.ShowPanelAndWaitTillHidden<UIPanelHostDetails>();

                // did we create a valid room name ?
                if (UIPanelHostDetails.Instance.UIResult)
                {
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
                            // hide busy panel 
                            UIPanelManager.Instance.HidePanel<UIPanelBusy>();

                            // created ok ?
                            if (data.ResultCode != Result.Success)
                            {
                                Debug.LogError("Failed to create EOS lobby");
                                return;
                            }

                            Debug.Log($"Created EOS lobby {data.LobbyId}");

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
                                    Debug.LogError($"Failed to update EOS lobby {updateData.LobbyId}: {updateData.ResultCode}");
                                }
                                else
                                {
                                    Debug.Log($"Updated EOS lobby {updateData.LobbyId}");

                                    // record the lobby id to the player manager - so that on disconnection of client
                                    // we can remove them from the EOS lobby
                                    PlayerManager.Instance.ActiveLobbyId = updateData.LobbyId;

                                    // setup ui room info
                                    UIPanelLobby.Instance.LobbyName = UIPanelHostDetails.Instance.LobbyName;
                                    UIPanelLobby.Instance.LobbyId = updateData.LobbyId;
                                    UIPanelLobby.Instance.IsHost = true;

                                    // show the room UI
                                    UIPanelManager.Instance.ShowPanel<UIPanelLobby>();
                                }
                            });
                        });
                }
            }
        }

        private IEnumerator DoJoinGame()
        {
            // is player name blank ?
            if (String.IsNullOrEmpty(Settings.Instance.CurrentPlayerName))
            {
                // show panel that gets player name
                yield return UIPanelManager.Instance.ShowPanelAndWaitTillHidden<UIPanelPlayerName>();

                // update player name on this panel
                UpdateControlState();
            }

            // only allow join if we have player name
            if (!String.IsNullOrEmpty(Settings.Instance.CurrentPlayerName))
            {
                // show panel showing list of lobbies
                UIPanelManager.Instance.ShowPanel<UIPanelLobbies>();
            }
        }

        public void HostGame()
        {
            StartCoroutine(DoHostGame());
        }

        public void JoinGame()
        {
            StartCoroutine(DoJoinGame());
        }

        public void ShowSettings()
        {
            UIPanelManager.Instance.ShowPanel<UIPanelSettings>();
        }

        public void Exit()
        {
            Application.Quit();
        }
    }
}
