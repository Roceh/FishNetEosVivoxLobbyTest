using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EOSLobbyTest
{
    public class PlayerTank : NetworkBehaviour
    {
        public override void OnStartClient()
        {
            base.OnStartClient();

            if (IsOwner)
            {
                this.gameObject.AddComponent<AudioListener>();
            }

            // setup the 3d audio source for the players voice
            var playerAudioSource = GetComponent<AudioSource>();
            var playerInfo = PlayerManager.Instance.GetPlayer(Owner);

            if (playerAudioSource != null && playerInfo != null)
            {
                // switch to 3d audio source
                playerInfo.SwitchAudioSource(playerAudioSource);
            }
        }
    }
}
