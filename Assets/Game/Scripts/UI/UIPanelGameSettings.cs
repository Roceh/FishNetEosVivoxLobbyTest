namespace EOSLobbyTest
{
    public class UIPanelGameSettings : UIPanelSettings
    {
        public void Back()
        {
            UIPanelManager.Instance.HidePanel<UIPanelGameSettings>(false);
        }
    }
}