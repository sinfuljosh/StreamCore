using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mixer.Base;
using Mixer.Base.Clients;
using Mixer.Base.Model.Channel;
using Mixer.Base.Model.Chat;
using Mixer.Base.Model.User;
using Mixer.Base.Util;

namespace StreamCore.Mixer
{
    public class MixerClient
    {
        const string ClientID = "32f704195ca2ce5ec81f7bc796282d3877da9b18cf3e1cda";

        /// <summary>
        /// True if the client has been initialized already.
        /// </summary>
        public static bool Initialized { get; private set; } = false;

        /// <summary>
        /// True if the client is connected to Twitch.
        /// </summary>
        public static bool Connected => client?.Connected ?? false;

        /// <summary>
        /// The last time the client established a connection to the Twitch servers.
        /// </summary>
        public static DateTime ConnectionTime { get; private set; } = DateTime.MinValue;

        /// <summary>
        /// A reference to the currently logged in user
        /// </summary>
        public static UserModel MixerUser { get; set; } = null;

        /// <summary>
        /// Callback that occurs when a connection to the Twitch servers is successfully established. *NOT THREAD SAFE, USE CAUTION!*
        /// </summary>
        public static Action OnConnected;

        /// <summary>
        /// Callback that occurs when we get disconnected from the Twitch servers. *NOT THREAD SAFE, USE CAUTION!*
        /// </summary>
        public static Action OnDisconnected;

        static ChatClient client;
        private static ConcurrentQueue<KeyValuePair<int, string>> _sendQueue = new ConcurrentQueue<KeyValuePair<int, string>>();

        internal static void Initialize_Internal()
        {
            if (Initialized)
                return;

            MixerMessageHandlers.Initialize();
            Task.Factory.StartNew(Loop);

            Initialized = true;
            Task.Run(() =>
            {
                Thread.Sleep(1000);
                Connect();
            });
        }

        static void Loop()
        {
            while (!Globals.IsApplicationExiting)
            {
                try
                {
                    if (Connected)
                    {
                        if (_sendQueue.Count > 0)
                        {
                            if (_sendQueue.TryDequeue(out var fullMsg))
                            {
                                // Split off the assembly hash, we'll use this in the callback we invoke to filter out calls to the assembly that created the callback.
                                string assembly = fullMsg.Key.ToString();
                                string msg = fullMsg.Value;

                                // Send the message, then invoke the received callback for all the other assemblies
                                client.SendMessage(msg);
                                //OnMessageReceived(msg, assembly);
                            }
                        }
                    }
                    Thread.Sleep(250);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Plugin.Log(ex.ToString());
                }
            }
        }

        /// <summary>
        /// Shuts down the websocket client, called internally. There is no need to call this function.
        /// </summary>
        public static void Shutdown()
        {
            Plugin.Log("Shutdown Mixer");
            if (Connected)
            {
                DisConnect();
            }
        }

        private static void DisConnect()
        {
            if (client != null && client.Connected)
            {
                client.OnDisconnectOccurred -= ChatClient_DoReconnect;
                client.Disconnect();
                OnDisconnected?.Invoke();
                client = null;
            }
        }

        private async static void Connect()
        {
            try
            {
                Plugin.Log("Connecting to Mixer");
                DisConnect();

                List<OAuthClientScopeEnum> scopes = new List<OAuthClientScopeEnum>()
            {
                OAuthClientScopeEnum.chat__bypass_links,
                OAuthClientScopeEnum.chat__bypass_slowchat,
                OAuthClientScopeEnum.chat__chat,
                OAuthClientScopeEnum.chat__connect,
                OAuthClientScopeEnum.channel__details__self,
            };

                MixerConnection connection = await MixerConnection.ConnectViaLocalhostOAuthBrowser(ClientID, scopes).ConfigureAwait(false);
                Plugin.Log($"OAuth Mixerconnection successful? => {connection != null}");

                if (connection != null)
                {
                    Plugin.Log("Getting info");
                    MixerUser = await connection.Users.GetCurrentUser().ConfigureAwait(false);
                    ExpandedChannelModel channel = await connection.Channels.GetChannel(MixerUser.username).ConfigureAwait(false);

                    client = await ChatClient.CreateFromChannel(connection, channel).ConfigureAwait(false);

                    client.OnDisconnectOccurred += ChatClient_OnDisconnectOccurred;
                    client.OnDisconnectOccurred += ChatClient_DoReconnect;
                    client.OnMessageOccurred += ChatClient_OnMessageOccurred;
                    //client.OnUserJoinOccurred += ChatClient_OnUserJoinOccurred;
                    //client.OnUserLeaveOccurred += ChatClient_OnUserLeaveOccurred;
                    Plugin.Log("Connecting to Mixer chat");
                    if (await client.Connect().ConfigureAwait(false) && await client.Authenticate().ConfigureAwait(false))
                    {
                        DoOnConnect();
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log($"{e.GetType().Name}: {e.Message}");
            }
        }

        private static async Task Reconnect()
        {
            do
            {
                await Task.Delay(2500);
            }
            while (!await client.Connect() && !await client.Authenticate());
            DoOnConnect();
        }

        static void DoOnConnect()
        {
            Plugin.Log("Connected to Mixer chat");
            ConnectionTime = DateTime.Now;
            OnConnected?.Invoke();
        }

        private static void ChatClient_OnDisconnectOccurred(object sender, System.Net.WebSockets.WebSocketCloseStatus e)
        {
            Plugin.Log("Disconnected Mixer");
            OnDisconnected?.Invoke();
        }

        private static async void ChatClient_DoReconnect(object sender, System.Net.WebSockets.WebSocketCloseStatus e)
        {
            Plugin.Log("Reconnecting Mixer");
            await Task.Delay(2000);
            await Reconnect();
        }

        private static void ChatClient_OnMessageOccurred(object sender, ChatMessageEventModel e)
        {
            string message = "";
            foreach (ChatMessageDataModel m in e.message.message)
            {
                message += m.text;
            }
            //Plugin.Log("Got message: " + message);
            //OnMessageReceived(message);
            MixerMessageHandlers.InvokeHandler(new MixerMessage()
            {
                id = e.id.ToString(),
                message = message,
                messageType = "Message",
                user = new MixerUser()
                {
                    id = e.user_id.ToString(),
                    color = ColorFromRoles(e.user_roles),
                    displayName = e.user_name,
                    Avatar = e.user_avatar
                },
                MessageModel = e
            }, "");
        }

        static string ColorFromRoles(string[] roles)
        {
            //Todo
            return "";
        }

        //private static void ChatClient_OnUserJoinOccurred(object sender, ChatUserEventModel e)
        //{

        //}

        //private static void ChatClient_OnUserLeaveOccurred(object sender, ChatUserEventModel e)
        //{

        //}

        // Prepend the assembly hash code before adding it to the send queue, to be used in identifying the assembly for our callback
        private static void SendRawInternal(Assembly assembly, string msg)
        {
            if (Connected && !string.IsNullOrEmpty(msg))
                _sendQueue.Enqueue(new KeyValuePair<int, string>(assembly.GetHashCode(), msg));
        }

        /// <summary>
        /// Sends an escaped chat message to the channel defined in TwitchLoginInfo.ini.
        /// </summary>
        /// <param name="msg">The chat message to be sent.</param>
        public static void SendMessage(string msg)
        {
            SendRawInternal(Assembly.GetCallingAssembly(), msg);
        }
    }
}
