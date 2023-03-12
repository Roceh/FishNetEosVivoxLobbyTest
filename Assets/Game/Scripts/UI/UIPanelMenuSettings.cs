using FishNet;
using FishNet.Managing.Scened;
using FishNet.Plugins.FishyEOS.Util;
using System;
namespace EOSLobbyTest
{
    public class UIPanelMenuSettings : UIPanelSettings
    {
        public void Back()
        {
            UIPanelManager.Instance.HidePanel<UIPanelMenuSettings>(false);
        }
    }
}