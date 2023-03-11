using FishNet;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EOSLobbyTest
{
    public class GameManager : MonoBehaviourSingleton<GameManager>
    {
        [Tooltip("Prefab object for player controlled object")]
        [SerializeField]
        private GameObject playerPrefab;

        [Tooltip("Transforms for each spawn location")]
        [SerializeField]
        private Transform[] spawnPoints;

        // which spawn point to use
        private int _nextSpawnPointIndex = 1;

        private void Start()
        {
            if (InstanceFinder.IsServer)
            {
                InstanceFinder.SceneManager.OnClientPresenceChangeEnd += SceneManager_OnClientPresenceChangeEnd;
            }

            VivoxManager.Instance.JoinChannel(UIPanelLobby.Instance.LobbyId + "_game", VivoxUnity.ChannelType.Positional, VivoxManager.ChatCapability.AudioOnly, true, null,
               () =>
               {
                   Debug.Log("Connected to vivox positional audio channel");
               });
        }

        private void SceneManager_OnClientPresenceChangeEnd(FishNet.Managing.Scened.ClientPresenceChangeEventArgs obj)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                var spawnPoint = spawnPoints[_nextSpawnPointIndex % spawnPoints.Length];

                InstanceFinder.SceneManager.AddConnectionToScene(obj.Connection, SceneManager.GetActiveScene());
                var playerVehicle = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
                InstanceFinder.ServerManager.Spawn(playerVehicle, obj.Connection);

                _nextSpawnPointIndex++;
            }
        }
    }
}
