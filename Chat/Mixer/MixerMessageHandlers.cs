using StreamCore.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StreamCore.Mixer
{
    public class MixerMessageHandlers : GenericMessageHandler<MixerMessage>
    {
        #region Message Handler Dictionaries
        private static Dictionary<string, Action<MixerMessage>> _Message_CALLBACKS = new Dictionary<string, Action<MixerMessage>>();
        #endregion
        /// <summary>
        /// Twitch PRIVMSG event handler. *Note* The callback is NOT on the Unity thread!
        /// </summary>
        public static Action<MixerMessage> Message
        {
            set { lock (_Message_CALLBACKS) { _Message_CALLBACKS[Assembly.GetCallingAssembly().GetHashCode().ToString()] = value; } }
            get { return _Message_CALLBACKS.TryGetValue(Assembly.GetCallingAssembly().GetHashCode().ToString(), out var callback) ? callback : null; }
        }

        public MixerMessageHandlers()
        {
        }


        internal static void Initialize()
        {
            if (Initialized)
                return;

            // Initialize our message handlers
            _messageHandlers.Add("Message", Message_Handler);

            Initialized = true;
        }

        private static void Message_Handler(MixerMessage twitchMsg, string invokerHash)
        {
            SafeInvoke(_Message_CALLBACKS, twitchMsg, invokerHash);
        }
    }
}
