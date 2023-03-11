using System;
using System.ComponentModel;
using System.Linq;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;
using VivoxUnity;
using Unity.Services.Authentication;
using System.Threading.Tasks;

// Based on vivox chat sample scene

namespace EOSLobbyTest
{
    public class VivoxManager : MonoBehaviourSingletonPersistent<VivoxManager>
    {
        /// <summary>
        /// Defines properties that can change.  Used by the functions that subscribe to the OnAfterTYPEValueUpdated functions.
        /// </summary>
        public enum ChangedProperty
        {
            None,
            Speaking,
            Typing,
            Muted
        }

        public enum ChatCapability
        {
            TextOnly,
            AudioOnly,
            TextAndAudio
        };

        public delegate void ParticipantValueChangedHandler(string username, ChannelId channel, bool value);

        public event ParticipantValueChangedHandler OnSpeechDetectedEvent;

        public delegate void ParticipantValueUpdatedHandler(string username, ChannelId channel, double value);

        public event ParticipantValueUpdatedHandler OnAudioEnergyChangedEvent;

        public delegate void ParticipantStatusChangedHandler(string username, ChannelId channel, IParticipant participant);

        public event ParticipantStatusChangedHandler OnParticipantAddedEvent;

        public event ParticipantStatusChangedHandler OnParticipantRemovedEvent;

        public delegate void ChannelTextMessageChangedHandler(string sender, IChannelTextMessage channelTextMessage);

        public event ChannelTextMessageChangedHandler OnTextMessageLogReceivedEvent;

        public delegate void LoginStatusChangedHandler();

        public event LoginStatusChangedHandler OnUserLoggedInEvent;

        public event LoginStatusChangedHandler OnUserLoggedOutEvent;

        public delegate void RecoveryStateChangedHandler(ConnectionRecoveryState recoveryState);

        public event RecoveryStateChangedHandler OnRecoveryStateChangedEvent;

        public event Action OnInitialized;

        private Account m_Account;

        private Client _client => VivoxService.Instance.Client;

        public bool Initialized {get; private set;}

        public LoginState LoginState { get; private set; }

        public ILoginSession LoginSession;

        public IReadOnlyDictionary<ChannelId, IChannelSession> ActiveChannels => LoginSession?.ChannelSessions;

        public IAudioDevices AudioInputDevices => _client.AudioInputDevices;

        public IAudioDevices AudioOutputDevices => _client.AudioOutputDevices;

        /// <summary>
        /// Retrieves the first instance of a session that is transmitting.
        /// </summary>
        public IChannelSession TransmittingSession
        {
            get
            {
                if (_client == null)
                    throw new NullReferenceException("client");
                return _client.GetLoginSession(m_Account).ChannelSessions.FirstOrDefault(x => x.IsTransmitting);
            }
            set
            {
                if (value != null)
                {
                    _client.GetLoginSession(m_Account).SetTransmissionMode(TransmissionMode.Single, value.Channel);
                }
            }
        }


        private async void Start()
        {
            var options = new InitializationOptions();

            await UnityServices.InitializeAsync(options);

            AuthenticationService.Instance.SignedIn += AuthenticationService_SignedIn;

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            VivoxService.Instance.Initialize();

            VivoxManager.Instance.AudioInputDevices.BeginRefresh((x) =>
            {
                if (x.IsCompleted)
                {
                    Initialized = true;

                    var existingDevice = VivoxManager.Instance.AudioInputDevices.AvailableDevices.FirstOrDefault(x => x.Name == Settings.Instance.CurrentVoiceDeviceName);

                    if (existingDevice != null && existingDevice != VivoxManager.Instance.AudioInputDevices.ActiveDevice)
                    {
                        VivoxManager.Instance.AudioInputDevices.BeginSetActiveDevice(existingDevice, (ar) =>
                        {
                            if (ar.IsCompleted)
                            {
                                VivoxManager.Instance.AudioInputDevices.EndSetActiveDevice(ar);
                                OnInitialized?.Invoke();
                            }
                        });
                    }
                    else
                    {
                        OnInitialized?.Invoke();
                    }
                }
            });
        }

        private void AuthenticationService_SignedIn()
        {
            Debug.Log($"Unity authentication PlayerId: " + AuthenticationService.Instance.PlayerId);
        }

        private void OnApplicationQuit()
        {
            // Needed to add this to prevent some unsuccessful uninit, we can revisit to do better -carlo
            Client.Cleanup();
            if (_client != null)
            {
                VivoxLog("Uninitializing client.");
                _client.Uninitialize();
            }
        }

        public void Login(string displayName = null)
        {
            m_Account = new Account(displayName);

            LoginSession = _client.GetLoginSession(m_Account);
            LoginSession.PropertyChanged += OnLoginSessionPropertyChanged;
            LoginSession.BeginLogin(LoginSession.GetLoginToken(), SubscriptionMode.Accept, null, null, null, ar =>
            {
                try
                {
                    LoginSession.EndLogin(ar);
                }
                catch (Exception e)
                {
                    // Handle error
                    VivoxLogError(nameof(e));
                    // Unbind if we failed to login.
                    LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
                    return;
                }
            });
        }

        public void Logout()
        {
            if (LoginSession != null && LoginState != LoginState.LoggedOut && LoginState != LoginState.LoggingOut)
            {
                LoginSession.Logout();
            }
        }

        public void JoinChannel(string channelName, ChannelType channelType, ChatCapability chatCapability, bool transmissionSwitch, Channel3DProperties properties, Action connected)
        {
            if (LoginState == LoginState.LoggedIn)
            {
                Channel channel = new Channel(channelName, channelType, properties);

                IChannelSession channelSession = LoginSession.GetChannelSession(channel);
                channelSession.PropertyChanged += OnChannelPropertyChanged;
                channelSession.Participants.AfterKeyAdded += OnParticipantAdded;
                channelSession.Participants.BeforeKeyRemoved += OnParticipantRemoved;
                channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
                channelSession.MessageLog.AfterItemAdded += OnMessageLogRecieved;
                channelSession.BeginConnect(chatCapability != ChatCapability.TextOnly, chatCapability != ChatCapability.AudioOnly, transmissionSwitch, channelSession.GetConnectToken(), ar =>
                {
                    try
                    {
                        channelSession.EndConnect(ar);
                        connected?.Invoke();
                    }
                    catch (Exception e)
                    {
                        // Handle error
                        VivoxLogError($"Could not connect to voice channel: {e.Message}");
                        return;
                    }
                });
            }
            else
            {
                VivoxLogError("Cannot join a channel when not logged in.");
            }
        }

        public void SendTextMessage(string messageToSend, ChannelId channel, string applicationStanzaNamespace = null, string applicationStanzaBody = null)
        {
            if (ChannelId.IsNullOrEmpty(channel))
            {
                throw new ArgumentException("Must provide a valid ChannelId");
            }
            if (string.IsNullOrEmpty(messageToSend))
            {
                throw new ArgumentException("Must provide a message to send");
            }
            var channelSession = LoginSession.GetChannelSession(channel);
            channelSession.BeginSendText(null, messageToSend, applicationStanzaNamespace, applicationStanzaBody, ar =>
            {
                try
                {
                    channelSession.EndSendText(ar);
                }
                catch (Exception e)
                {
                    VivoxLog($"SendTextMessage failed with exception {e.Message}");
                }
            });
        }

        public void DisconnectAllChannels()
        {
            if (ActiveChannels?.Count > 0)
            {
                foreach (var channelSession in ActiveChannels)
                {
                    channelSession?.Disconnect();
                }
            }
        }

        private void OnMessageLogRecieved(object sender, QueueItemAddedEventArgs<IChannelTextMessage> textMessage)
        {
            ValidateArgs(new object[] { sender, textMessage });

            IChannelTextMessage channelTextMessage = textMessage.Value;
            VivoxLog(channelTextMessage.Message);
            OnTextMessageLogReceivedEvent?.Invoke(channelTextMessage.Sender.DisplayName, channelTextMessage);
        }

        private void OnLoginSessionPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "RecoveryState")
            {
                OnRecoveryStateChangedEvent?.Invoke(LoginSession.RecoveryState);
                return;
            }
            if (propertyChangedEventArgs.PropertyName != "State")
            {
                return;
            }
            var loginSession = (ILoginSession)sender;
            LoginState = loginSession.State;
            VivoxLog("Detecting login session change");
            switch (LoginState)
            {
                case LoginState.LoggingIn:
                    {
                        VivoxLog("Logging in");
                        break;
                    }
                case LoginState.LoggedIn:
                    {
                        VivoxLog("Connected to voice server and logged in.");
                        OnUserLoggedInEvent?.Invoke();
                        break;
                    }
                case LoginState.LoggingOut:
                    {
                        VivoxLog("Logging out");
                        break;
                    }
                case LoginState.LoggedOut:
                    {
                        VivoxLog("Logged out");
                        OnUserLoggedOutEvent?.Invoke();
                        LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
                        break;
                    }
                default:
                    break;
            }
        }

        private void OnParticipantAdded(object sender, KeyEventArg<string> keyEventArg)
        {
            ValidateArgs(new object[] { sender, keyEventArg });

            // INFO: sender is the dictionary that changed and trigger the event.  Need to cast it back to access it.
            var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
            // Look up the participant via the key.
            var participant = source[keyEventArg.Key];
            var username = participant.Account.Name;
            var channel = participant.ParentChannelSession.Key;
            var channelSession = participant.ParentChannelSession;

            // Trigger callback
            OnParticipantAddedEvent?.Invoke(username, channel, participant);
        }

        private void OnParticipantRemoved(object sender, KeyEventArg<string> keyEventArg)
        {
            ValidateArgs(new object[] { sender, keyEventArg });

            // INFO: sender is the dictionary that changed and trigger the event.  Need to cast it back to access it.
            var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
            // Look up the participant via the key.
            var participant = source[keyEventArg.Key];
            var username = participant.Account.Name;
            var channel = participant.ParentChannelSession.Key;
            var channelSession = participant.ParentChannelSession;

            if (participant.IsSelf)
            {
                VivoxLog($"Unsubscribing from: {channelSession.Key.Name}");
                // Now that we are disconnected, unsubscribe.
                channelSession.PropertyChanged -= OnChannelPropertyChanged;
                channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
                channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
                channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
                channelSession.MessageLog.AfterItemAdded -= OnMessageLogRecieved;

                // Remove session.
                var user = _client.GetLoginSession(m_Account);
                user.DeleteChannelSession(channelSession.Channel);
            }

            // Trigger callback
            OnParticipantRemovedEvent?.Invoke(username, channel, participant);
        }

        private static void ValidateArgs(object[] objs)
        {
            foreach (var obj in objs)
            {
                if (obj == null)
                    throw new ArgumentNullException(obj.GetType().ToString(), "Specify a non-null/non-empty argument.");
            }
        }

        private void OnParticipantValueUpdated(object sender, ValueEventArg<string, IParticipant> valueEventArg)
        {
            ValidateArgs(new object[] { sender, valueEventArg });

            var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
            // Look up the participant via the key.
            var participant = source[valueEventArg.Key];

            string username = valueEventArg.Value.Account.Name;
            ChannelId channel = valueEventArg.Value.ParentChannelSession.Key;
            string property = valueEventArg.PropertyName;

            switch (property)
            {
                case "SpeechDetected":
                    {
                        VivoxLog($"OnSpeechDetectedEvent: {username} in {channel}.");
                        OnSpeechDetectedEvent?.Invoke(username, channel, valueEventArg.Value.SpeechDetected);
                        break;
                    }
                case "AudioEnergy":
                    {
                        OnAudioEnergyChangedEvent?.Invoke(username, channel, valueEventArg.Value.AudioEnergy);
                        break;
                    }
                default:
                    break;
            }
        }

        private void OnChannelPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            ValidateArgs(new object[] { sender, propertyChangedEventArgs });

            var channelSession = (IChannelSession)sender;

            // IF the channel has removed audio, make sure all the VAD indicators aren't showing speaking.
            if (propertyChangedEventArgs.PropertyName == "AudioState" && channelSession.AudioState == ConnectionState.Disconnected)
            {
                VivoxLog($"Audio disconnected from: {channelSession.Key.Name}");

                foreach (var participant in channelSession.Participants)
                {
                    OnSpeechDetectedEvent?.Invoke(participant.Account.Name, channelSession.Channel, false);
                }
            }

            // IF the channel has fully disconnected, unsubscribe and remove.
            if ((propertyChangedEventArgs.PropertyName == "AudioState" || propertyChangedEventArgs.PropertyName == "TextState") &&
                channelSession.AudioState == ConnectionState.Disconnected &&
                channelSession.TextState == ConnectionState.Disconnected)
            {
                VivoxLog($"Unsubscribing from: {channelSession.Key.Name}");
                // Now that we are disconnected, unsubscribe.
                channelSession.PropertyChanged -= OnChannelPropertyChanged;
                channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
                channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
                channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
                channelSession.MessageLog.AfterItemAdded -= OnMessageLogRecieved;

                // Remove session.
                var user = _client.GetLoginSession(m_Account);
                user.DeleteChannelSession(channelSession.Channel);
            }
        }

        private void VivoxLog(string msg)
        {
            Debug.Log("<color=green>VivoxVoice: </color>: " + msg);
        }

        private void VivoxLogError(string msg)
        {
            Debug.LogError("<color=green>VivoxVoice: </color>: " + msg);
        }
    }
}