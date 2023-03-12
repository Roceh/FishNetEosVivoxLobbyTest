using FishNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EOSLobbyTest
{
    public class PlayerManager : MonoBehaviourSingletonPersistent<PlayerManager>
    {
        private List<PlayerInfo> _players = new List<PlayerInfo>();

        // EOS lobby id we are currently in
        public string ActiveLobbyId { get; set; } = "testing";

        // triggered when ever any changes are done to the players
        public event Action PlayersChanged;

        // the player info for the server
        public PlayerInfo ServerPlayer => _players.FirstOrDefault(x => x.IsServer);

        // the player info for the active client
        public PlayerInfo ActivePlayer => _players.FirstOrDefault(x => x.IsOwner);

        public List<PlayerInfo> GetPlayers()
        {
            return _players;
        }

        public void PlayerUpdated(string userId)
        {
            PlayersChanged?.Invoke();
        }

        public void AddPlayer(PlayerInfo info)
        {
            _players.Add(info);

            PlayersChanged?.Invoke();
        }

        public void RemovePlayer(string userId)
        {
            _players.RemoveAll(x => x.UserId == userId);
            PlayersChanged?.Invoke();
        }
    }
}
