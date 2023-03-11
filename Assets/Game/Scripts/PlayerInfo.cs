using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Plugins.FishyEOS.Util;
using FishNet.Transporting.FishyEOSPlugin;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EOSLobbyTest
{
    public class PlayerInfo : NetworkBehaviour
    {
        [SyncVar(OnChange = nameof(OnPlayerName))]
        [HideInInspector]
        public string PlayerName;

        [SyncVar]
        [HideInInspector]
        public string UserId;

        private void OnPlayerName(string prev, string next, bool asServer)
        {
            PlayerManager.Instance.PlayerUpdated(UserId);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            var fishy = InstanceFinder.NetworkManager.GetComponent<FishyEOS>();
            UserId = fishy.GetRemoteConnectionAddress(Owner.ClientId);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (IsOwner)
            {
                SetPlayerName(Settings.Instance.CurrentPlayerName);
            }

            PlayerManager.Instance.AddPlayer(this);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            if (IsOwner && EOS.GetCachedLobbyInterface() != null)
            {
                var updateLobbyModificationOptions = new LeaveLobbyOptions { LocalUserId = ProductUserId.FromString(UserId), LobbyId = PlayerManager.Instance.ActiveLobbyId };

                // as we have a bit of a race condition depending whether the server kicks or we leave
                // just report the failure as a normal log
                EOS.GetCachedLobbyInterface().LeaveLobby(ref updateLobbyModificationOptions, null, delegate (ref LeaveLobbyCallbackInfo data)
                {
                    if (data.ResultCode != Result.Success)
                    {
                        Debug.Log($"User {UserId} failed to leave EOS lobby: {data.ResultCode}");
                    }
                    else
                    {
                        Debug.Log($"User {UserId} left EOS lobby");
                    }
                });
            }
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            // extra check here as this could occur when we are shutting down the game also
            if (EOS.GetCachedLobbyInterface() != null)
            {
                // fallback in case user has timed out or some such

                var kickMemberOptions = new KickMemberOptions { TargetUserId = ProductUserId.FromString(UserId), LocalUserId = EOS.LocalProductUserId, LobbyId = PlayerManager.Instance.ActiveLobbyId };

                EOS.GetCachedLobbyInterface().KickMember(ref kickMemberOptions, null, delegate (ref KickMemberCallbackInfo data)
                {
                    // as we have a bit of a race condition depending whether the server kicks or we leave
                    // just report the failure as a normal log
                    if (data.ResultCode != Result.Success)
                    {
                        Debug.Log($"User {UserId} failed to kick from EOS lobby: {data.ResultCode}");
                    }
                    else
                    {
                        Debug.Log($"User {UserId} kicked from EOS lobby");
                    }
                });
            }
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            PlayerManager.Instance.RemovePlayer(UserId);
        }

        [ServerRpc]
        private void SetPlayerName(string playerName)
        {
            PlayerName = playerName;
        }

        public void SendStartingGame()
        {
            if (IsServer)
            {
                DoStartingGame();
            }
        }

        [ObserversRpc]
        private void DoStartingGame()
        {
            UIPanelManager.Instance.HidePanel<UIPanelLobby>();
            UIPanelManager.Instance.HidePanel<UIPanelMain>();
        }
    }
}
