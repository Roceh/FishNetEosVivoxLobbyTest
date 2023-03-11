using Epic.OnlineServices.Lobby;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EOSLobbyTest
{
    public class UIScrollViewLobbies : MonoBehaviour
    {
        [Tooltip("Prefab for lobby item")]
        [SerializeField]
        private GameObject lobbyItemPrefab;

        [Tooltip("Container for the lobby items")]
        [SerializeField]
        private Transform container;

        public UILobbyItem[] GetLobbies()
        {
            return container.GetComponentsInChildren<UILobbyItem>();
        }

        public void ClearLobbies()
        {
            for (var i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        public UILobbyItem AddLobby(LobbyDetails lobbyDetails, LobbyDetailsInfo lobbyDetailsInfo, string lobbyName)
        {
            var lobbyGameObject = GameObject.Instantiate(lobbyItemPrefab, container);
            var lobbyItem = lobbyGameObject.GetComponent<UILobbyItem>();
            lobbyItem.LobbyDetails = lobbyDetails;
            lobbyItem.LobbyDetailsInfo = lobbyDetailsInfo;
            lobbyItem.LobbyName = lobbyName;
            return lobbyItem;
        }
    }
}
