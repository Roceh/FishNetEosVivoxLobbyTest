using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EOSLobbyTest
{
    public class UIPlayerItem : MonoBehaviour
    {
        [Tooltip("UI text item for name of player")]
        [SerializeField]
        private Text textPlayerName;

        [Tooltip("UI button to allow kick of user")]
        [SerializeField]
        private Button buttonKick;

        // Display name for player
        public string PlayerName
        {
            get => textPlayerName.text;
            set => textPlayerName.text = value;
        }

        public event Action<string> KickRequest;

        // Disable/Enable kick button
        public bool CanKick
        {
            get => buttonKick.gameObject.activeSelf;
            set => buttonKick.gameObject.SetActive(value);
        }

        // EOS Id for player
        public string PlayerId { get; set; }

        public void Kick()
        {
            KickRequest?.Invoke(PlayerId);
        }
    }
}
