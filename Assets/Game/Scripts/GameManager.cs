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

                // setup listener for local client prefab
                if (obj.Connection.IsLocalClient)
                {
                    playerVehicle.AddComponent<AudioListener>();
                }

                // setup the 3d audio source for the players voice
                var playerAudioSource = playerVehicle.GetComponent<AudioSource>();
                var playerInfo = PlayerManager.Instance.GetPlayer(obj.Connection);

                if (playerAudioSource != null && playerInfo != null)
                {
                    // switch to 3d audio source
                    playerInfo.SwitchAudioSource(playerAudioSource);
                }

                _nextSpawnPointIndex++;
            }
        }
    }
}
