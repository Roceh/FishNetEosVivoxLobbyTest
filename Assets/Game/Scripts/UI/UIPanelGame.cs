
using FishNet;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EOSLobbyTest
{
    public class UIPanelGame : UIPanel<UIPanelGame>, IUIPanel
    {
        public void ShowSettings()
        {
            UIPanelManager.Instance.ShowPanel<UIPanelGameSettings>();
        }


        public void ExitGame()
        {
            // shutdown server if we are host
            if (InstanceFinder.NetworkManager.IsHost)
            {
                InstanceFinder.ServerManager.StopConnection(true);
            }

            // disconnect client
            InstanceFinder.ClientManager.StopConnection();

            // return to menu
            SceneManager.LoadScene(0);
        }
    }
}
