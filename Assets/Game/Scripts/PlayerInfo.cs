using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Plugins.FishyEOS.Util;
using FishNet.Transporting.FishyEOSPlugin;
using System.Collections.Concurrent;
using UnityEngine;

namespace EOSLobbyTest
{
    public class PlayerInfo : NetworkBehaviour
    {
        [SyncVar(OnChange = nameof(OnPlayerName))]
        [HideInInspector]
        public string PlayerName;

        [SyncVar]
        [HideInInspector]
        public string UserId;

        private const int SampleRate = 48000;

        private ConcurrentQueue<short[]> _audioFrameQueue = new ConcurrentQueue<short[]>();
        private bool _catchUp;
        private short[] _currentVoiceFrame;
        private AudioSource _voiceAudioSource;
        private int _voiceFrameIndex;

        public void EnqueueAudioFrame(short[] frames)
        {
            if (_audioFrameQueue?.Count > SampleRate / 500) // Clear frames if it's way over the queue
            {
                _audioFrameQueue = new ConcurrentQueue<short[]>();
            }

            _audioFrameQueue?.Enqueue(frames);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (IsOwner)
            {
                SetPlayerName(Settings.Instance.CurrentPlayerName);
            }

            PlayerManager.Instance.AddPlayer(this);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            var fishy = InstanceFinder.NetworkManager.GetComponent<FishyEOS>();
            UserId = fishy.GetRemoteConnectionAddress(Owner.ClientId);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            // we have dropped a connection - try to leave the EOS lobby we were in - we might have already left it
            // in which case this will just warn it cannot leave
            CleanUpEOSandVivox();
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            PlayerManager.Instance?.RemovePlayer(UserId);
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            // extra check here as this could occur when we are shutting down the game also
            if (EOS.GetCachedLobbyInterface() != null)
            {
                // fallback in case user has timed out or some such

                var kickMemberOptions = new KickMemberOptions { TargetUserId = ProductUserId.FromString(UserId), LocalUserId = EOS.LocalProductUserId, LobbyId = PlayerManager.Instance.ActiveLobbyId };

                EOS.GetCachedLobbyInterface().KickMember(ref kickMemberOptions, null, delegate (ref KickMemberCallbackInfo data)
                {
                    // as we have a bit of a race condition depending whether the server kicks or we leave
                    // just report the failure as a normal log
                    if (data.ResultCode != Result.Success)
                    {
                        Debug.Log($"User {UserId} failed to kick from EOS lobby: {data.ResultCode}");
                    }
                    else
                    {
                        Debug.Log($"User {UserId} kicked from EOS lobby");
                    }
                });
            }
        }

        public void SendStartingGame()
        {
            if (IsServer)
            {
                DoStartingGame();
            }
        }

        public void SwitchAudioSource(AudioSource source)
        {
            if (_voiceAudioSource != null)
            {
                _voiceAudioSource.Stop();
            }

            _voiceAudioSource = source;
            _voiceAudioSource.clip = AudioClip.Create("voice", SampleRate, 1, SampleRate, true, OnAudioRead);
        }

        private void CleanUpEOSandVivox()
        {
            if (IsOwner)
            {
                if (EOS.GetCachedLobbyInterface() != null)
                {
                    var updateLobbyModificationOptions = new LeaveLobbyOptions { LocalUserId = ProductUserId.FromString(UserId), LobbyId = PlayerManager.Instance.ActiveLobbyId };

                    // as we have a bit of a race condition depending whether the server kicks or we leave
                    // just report the failure as a normal log
                    EOS.GetCachedLobbyInterface().LeaveLobby(ref updateLobbyModificationOptions, null, delegate (ref LeaveLobbyCallbackInfo data)
                    {
                        if (data.ResultCode != Result.Success)
                        {
                            Debug.Log($"User {UserId} failed to leave EOS lobby: {data.ResultCode}");
                        }
                        else
                        {
                            Debug.Log($"User {UserId} left EOS lobby");
                        }
                    });
                }
            }
        }

        [ObserversRpc]
        private void DoStartingGame()
        {
            // ...
        }

        private void OnAudioRead(float[] data)
        {
            if (_audioFrameQueue?.Count > SampleRate / 1000 || _catchUp)
            {
                _catchUp = true;
                _audioFrameQueue?.TryDequeue(out short[] _);
                _catchUp = _audioFrameQueue?.Count <= 20;
            }

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = 0;

                if (_audioFrameQueue is null)
                    continue;

                if (_currentVoiceFrame is null || _voiceFrameIndex >= _currentVoiceFrame.Length)
                {
                    if (!_audioFrameQueue.TryDequeue(out short[] frame))
                        continue;

                    _voiceFrameIndex = 0;
                    _currentVoiceFrame = frame;
                }

                data[i] = _currentVoiceFrame[_voiceFrameIndex++] / (float)short.MaxValue;
            }
        }

        private void OnPlayerName(string prev, string next, bool asServer)
        {
            PlayerManager.Instance.PlayerUpdated(UserId);
        }

        [ServerRpc]
        private void SetPlayerName(string playerName)
        {
            PlayerName = playerName;
        }

        private void Start()
        {
            _voiceAudioSource = GetComponent<AudioSource>();
            _voiceAudioSource.clip = AudioClip.Create("voice", SampleRate, 1, SampleRate, true, OnAudioRead);
        }

        private void Update()
        {
            if (_voiceAudioSource != null && _audioFrameQueue.Count > 0 && !_voiceAudioSource.isPlaying)
            {
                _voiceAudioSource.Play();
            }
        }
    }
}