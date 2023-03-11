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

        // lobby id we are currently in
        public string ActiveLobbyId { get; set; }

        // triggered when ever any changes are done to the players
        public event Action PlayersChanged;

        public PlayerInfo ServerPlayer => _players.FirstOrDefault(x => x.IsServer);

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
