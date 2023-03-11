using FishNet;
using FishNet.Managing.Scened;
using FishNet.Plugins.FishyEOS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VivoxUnity;

namespace EOSLobbyTest
{
    public class UIPanelSettings : UIPanel<UIPanelSettings>, IUIPanel
    {
        [Tooltip("Drop down for screen resolution")]
        [SerializeField]
        private Dropdown resolutionDropDown;

        [Tooltip("Drop down for display mode")]
        [SerializeField]
        private Dropdown displayModeDropDown;

        [Tooltip("Drop down for vivox voice device")]
        [SerializeField]
        private Dropdown voiceDeviceDropDown;

        private string[] _audioDeviceNames;

        public void Back()
        {
            UIPanelManager.Instance.HidePanel<UIPanelSettings>(false);
        }

        protected override void OnShowing()
        {
            resolutionDropDown.options.Clear();

            foreach (var setting in Screen.resolutions)
            {
                resolutionDropDown.options.Add(new Dropdown.OptionData { text = $"{setting.width} x {setting.height} ({setting.refreshRate} hz)" });
            }

            resolutionDropDown.value = Settings.Instance.GetBestMatchIndexToAvailableResolutions();
            resolutionDropDown.RefreshShownValue();

            resolutionDropDown.onValueChanged.AddListener(HandleResolutionDropDown);

            displayModeDropDown.options.Clear();

            foreach (FullScreenMode fullScreenOption in Enum.GetValues(typeof(FullScreenMode)))
            {
                string fullScreenOptionText;

                switch (fullScreenOption)
                {
                    case FullScreenMode.ExclusiveFullScreen:
                        fullScreenOptionText = "Exclusive Full Screen";
                        break;
                    case FullScreenMode.FullScreenWindow:
                        fullScreenOptionText = "Full Screen Windowed";
                        break;
                    case FullScreenMode.MaximizedWindow:
                        fullScreenOptionText = "Maximized Windowed";
                        break;
                    case FullScreenMode.Windowed:
                        fullScreenOptionText = "Windowed";
                        break;
                    default:
                        throw new Exception("Unknown full screen option");
                }

                displayModeDropDown.options.Add(new Dropdown.OptionData { text = fullScreenOptionText });
            }

            displayModeDropDown.value = (int)Settings.Instance.CurrentFullScreenMode;
            displayModeDropDown.RefreshShownValue();

            displayModeDropDown.onValueChanged.AddListener(HandleDisplayModeDropDown);

            voiceDeviceDropDown.options.Clear();

            _audioDeviceNames = VivoxManager.Instance.AudioInputDevices.AvailableDevices.Select(x=> x.Name).ToArray();

            foreach (var deviceName in _audioDeviceNames)
            {
                voiceDeviceDropDown.options.Add(new Dropdown.OptionData { text = deviceName });
            }

            voiceDeviceDropDown.value = Array.FindIndex(_audioDeviceNames, x => x == VivoxManager.Instance.AudioInputDevices.ActiveDevice.Name);
            voiceDeviceDropDown.RefreshShownValue();

            voiceDeviceDropDown.onValueChanged.AddListener(HandleVoiceDeviceDropDown);
        }

        private void HandleResolutionDropDown(int selected)
        {
            var currentResolution = Settings.Instance.CurrentResolution;

            if (Screen.resolutions[selected].width != currentResolution.width || Screen.resolutions[selected].height != currentResolution.height || Screen.resolutions[selected].refreshRate != currentResolution.refreshRate)
            {
                Screen.SetResolution(Screen.resolutions[selected].width, Screen.resolutions[selected].height, Settings.Instance.CurrentFullScreenMode, Screen.resolutions[selected].refreshRate);
                Settings.Instance.CurrentResolution = Screen.resolutions[selected];
            }
        }

        private void HandleDisplayModeDropDown(int selected)
        {
            if ((FullScreenMode)selected != Settings.Instance.CurrentFullScreenMode)
            {
                var resolution = Settings.Instance.CurrentResolution;
                Screen.SetResolution(resolution.width, resolution.height, (FullScreenMode)selected, resolution.refreshRate);
                Settings.Instance.CurrentFullScreenMode = (FullScreenMode)selected;
            }
        }

        private void HandleVoiceDeviceDropDown(int selected)
        {
            var existingDevice = VivoxManager.Instance.AudioInputDevices.AvailableDevices.FirstOrDefault(x => x.Name == _audioDeviceNames[selected]);

            if (existingDevice != null && existingDevice != VivoxManager.Instance.AudioInputDevices.ActiveDevice)
            {
                VivoxManager.Instance.AudioInputDevices.BeginSetActiveDevice(existingDevice, (ar) =>
                {
                    if (ar.IsCompleted)
                    {
                        VivoxManager.Instance.AudioInputDevices.EndSetActiveDevice(ar);

                        Settings.Instance.CurrentVoiceDeviceName = VivoxManager.Instance.AudioInputDevices.ActiveDevice.Name;
                    }
                });
            }            
        }
    }
}