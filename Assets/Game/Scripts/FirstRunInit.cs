using FishNet.Plugins.FishyEOS.Util;
using PlayEveryWare.EpicOnlineServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EOSLobbyTest
{
    // do some stuff when app is started
    public class FirstRunInit : MonoBehaviour
    {
        private void Awake()
        {
            // get the settings
            Settings.Instance.Load();

            // this inits as well as gets
            if (EOS.GetManager() == null)
            {
                Debug.LogError("Failed to find EOSManager.");
            }
        }

        private void Start()
        {
            // make sure resolution is set to the settings resolution
            var resolutionIndex = Settings.Instance.GetBestMatchIndexToAvailableResolutions();

            if (resolutionIndex != -1)
            {
                Screen.SetResolution(Screen.resolutions[resolutionIndex].width, Screen.resolutions[resolutionIndex].height, Settings.Instance.CurrentFullScreenMode, Screen.resolutions[resolutionIndex].refreshRate);
            }
        }
    }
}
