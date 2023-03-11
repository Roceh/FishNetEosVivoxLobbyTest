using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using FishNet;
using FishNet.Managing;
using FishNet.Plugins.FishyEOS.Util;
using FishNet.Transporting.FishyEOSPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EOSLobbyTest
{
    public class UIPanelLobbies : UIPanel<UIPanelLobbies>, IUIPanel
    {
        [Tooltip("Controller for list of lobbies")]
        [SerializeField]
        private UIScrollViewLobbies lobbies;

        [SerializeField]
        private RectTransform busyRectComponent;

        [SerializeField]
        private GameObject panelBusy;

        [SerializeField]
        private float busyRotateSpeed = 200f;

        private void Update()
        {
            busyRectComponent.Rotate(0f, 0f, -busyRotateSpeed * Time.deltaTime);
        }

        public void Back()
        {
            UIPanelManager.Instance.HidePanel<UIPanelLobbies>(false);
        }

        private void SetLobbyBusy(bool status)
        {
            panelBusy.SetActive(status);
        }

        private void DoSearch()
        {
            lobbies.ClearLobbies();

            var search = new LobbySearch();
            var searchOptions = new CreateLobbySearchOptions { MaxResults = 50 };

            EOS.GetCachedLobbyInterface().CreateLobbySearch(ref searchOptions, out search);

            var lobbySearchSetParameterOptions = new LobbySearchSetParameterOptions { ComparisonOp = ComparisonOp.Equal, Parameter = new AttributeData { Key = "bucket", Value = EOSConsts.AllLobbiesBucketId } };

            search.SetParameter(ref lobbySearchSetParameterOptions);

            var lobbySearchFindOptions = new LobbySearchFindOptions { LocalUserId = EOS.LocalProductUserId };

            SetLobbyBusy(true);

            search.Find(ref lobbySearchFindOptions, null, delegate (ref LobbySearchFindCallbackInfo data)
            {
                SetLobbyBusy(false);

                if (data.ResultCode == Result.Success)
                {
                    Debug.Log("Lobbies search finished.");

                    var lobbySearchGetSearchResultCountOptions = new LobbySearchGetSearchResultCountOptions();
                    uint searchResultCount = search.GetSearchResultCount(ref lobbySearchGetSearchResultCountOptions);

                    Debug.LogFormat("Lobbies: searchResultCount = {0}", searchResultCount);

                    LobbySearchCopySearchResultByIndexOptions indexOptions = new LobbySearchCopySearchResultByIndexOptions();

                    for (uint i = 0; i < searchResultCount; i++)
                    {
                        indexOptions.LobbyIndex = i;

                        Result result = search.CopySearchResultByIndex(ref indexOptions, out LobbyDetails outLobbyDetails);

                        if (result == Result.Success && outLobbyDetails != null)
                        {
                            var lobbyDetailsCopyInfoOptions = new LobbyDetailsCopyInfoOptions();

                            Result infoResult = outLobbyDetails.CopyInfo(ref lobbyDetailsCopyInfoOptions, out LobbyDetailsInfo? outLobbyDetailsInfo);

                            if (infoResult != Result.Success)
                            {
                                Debug.LogErrorFormat("Lobbies: can't copy lobby info. Error code: {0}", infoResult);
                                return;
                            }

                            Debug.Log("Lobby = " + outLobbyDetailsInfo.Value.LobbyId);
                            Debug.Log("Owner = " + outLobbyDetailsInfo.Value.LobbyOwnerUserId);

                            // get attributes
                            var attributes = new List<AttributeData?>();
                            var lobbyDetailsGetAttributeCountOptions = new LobbyDetailsGetAttributeCountOptions();
                            uint attrCount = outLobbyDetails.GetAttributeCount(ref lobbyDetailsGetAttributeCountOptions);
                            for (uint j = 0; j < attrCount; j++)
                            {
                                LobbyDetailsCopyAttributeByIndexOptions attrOptions = new LobbyDetailsCopyAttributeByIndexOptions();
                                attrOptions.AttrIndex = j;
                                Result copyAttrResult = outLobbyDetails.CopyAttributeByIndex(ref attrOptions, out Epic.OnlineServices.Lobby.Attribute? outAttribute);
                                if (copyAttrResult == Result.Success && outAttribute != null && outAttribute?.Data != null)
                                {
                                    attributes.Add(outAttribute.Value.Data);
                                }
                            }

                            var lobbyNameAttribute = attributes.FirstOrDefault(x => x != null && x.Value.Key == EOSConsts.AttributeKeyLobbyName);

                            string lobbyName;

                            if (lobbyNameAttribute != null)
                            {
                                lobbyName = lobbyNameAttribute.Value.Value.AsUtf8;
                            }
                            else
                            {
                                lobbyName = outLobbyDetailsInfo.Value.LobbyId;
                            }

                            // there is a brief windows after the host leaves the lobby that it is left open
                            // we check for this state as the owner will be null and the AvailableSlots will be equal to max slots
                            if (outLobbyDetailsInfo.Value.LobbyOwnerUserId != null && outLobbyDetailsInfo.Value.AvailableSlots != outLobbyDetailsInfo.Value.MaxMembers)
                            {
                                var lobbyItem = lobbies.AddLobby(outLobbyDetails, outLobbyDetailsInfo.Value, lobbyName);
                                lobbyItem.JoinRequest += LobbyItem_JoinRequest;
                            }
                        }
                    }

                    Debug.Log("Lobbies  (OnLobbySearchCompleted)");
                }
            });
        }

        private void LobbyItem_JoinRequest(string lobbyName, LobbyDetails lobbyDetails, LobbyDetailsInfo lobbyDetailsInfo)
        {
            SetLobbyBusy(true);

            JoinLobbyOptions joinOptions = new JoinLobbyOptions();
            joinOptions.LobbyDetailsHandle = lobbyDetails;
            joinOptions.LocalUserId = EOS.LocalProductUserId;

            string lobbyOwnerId = lobbyDetailsInfo.LobbyOwnerUserId.ToString();

            EOS.GetCachedLobbyInterface().JoinLobby(ref joinOptions, null, delegate (ref JoinLobbyCallbackInfo data)
            {
                SetLobbyBusy(false);

                if (data.ResultCode != Result.Success)
                {
                    Debug.LogErrorFormat("JoinLobby error '{0}'", data.ResultCode);
                    return;
                }

                Debug.Log("Joined lobby." + data.LobbyId);
                Debug.Log("Lobby owner " + lobbyOwnerId);

                var fishy = InstanceFinder.NetworkManager.GetComponent<FishyEOS>();
                fishy.RemoteProductUserId = lobbyOwnerId;

                UIPanelManager.Instance.HidePanel<UIPanelLobbies>(true);

                UIPanelLobby.Instance.LobbyName = lobbyName;
                UIPanelLobby.Instance.LobbyId = data.LobbyId;
                UIPanelLobby.Instance.IsHost = false;

                UIPanelManager.Instance.ShowPanel<UIPanelLobby>();
            });
        }

        protected override void OnShowing()
        {
            SetLobbyBusy(false);
            DoSearch();
        }
    }
}
