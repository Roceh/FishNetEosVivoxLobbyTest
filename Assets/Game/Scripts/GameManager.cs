using FishNet;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EOSLobbyTest
{
    public class GameManager : MonoBehaviourSingletonForScene<GameManager>
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
            if (InstanceFinder.NetworkManager != null && InstanceFinder.NetworkManager.IsHost)
            {
                InstanceFinder.SceneManager.OnClientPresenceChangeEnd += SceneManager_OnClientPresenceChangeEnd;
            }

            // if we are testing there will be no vivox instance - this is start in the lobby
            VivoxManager.Instance?.JoinChannel(PlayerManager.Instance?.ActiveLobbyId + "_game", VivoxUnity.ChannelType.Positional, VivoxManager.ChatCapability.AudioOnly, true, null,
               () =>
               {
                   Debug.Log("Connected to vivox positional audio channel");
               });
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (InstanceFinder.NetworkManager != null)
            {
                InstanceFinder.SceneManager.OnClientPresenceChangeEnd -= SceneManager_OnClientPresenceChangeEnd;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (UIPanelManager.Instance.PanelIsVisible<UIPanelGameSettings>())
                {
                    UIPanelManager.Instance.HidePanel<UIPanelGameSettings>();
                }
                else if (UIPanelManager.Instance.PanelIsVisible<UIPanelGame>())
                {
                    UIPanelManager.Instance.HidePanel<UIPanelGame>();
                }
                else
                {
                    UIPanelManager.Instance.ShowPanel<UIPanelGame>();
                }
            }
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
