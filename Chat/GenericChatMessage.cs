using StreamCore.Mixer;
using StreamCore.Twitch;
using StreamCore.YouTube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamCore.Chat
{
    public class GenericChatUser
    {
        public string id { get; set; } = "";
        public string displayName { get; set; } = "";
        public string color { get; set; } = "";

        public TwitchUser Twitch { get => (this is TwitchUser) ? this as TwitchUser : new TwitchUser(); }
        public YouTubeUser YouTube { get => (this is YouTubeUser) ? this as YouTubeUser : new YouTubeUser(); }
        public MixerUser Mixer { get => (this is MixerUser) ? this as MixerUser : new MixerUser(); }
    }

    public class GenericChatMessage
    {
        public string id { get; set; } = "";
        public string message { get; set; } = "";
        public string messageType { get; set; } = "";
        public GenericChatUser user { get; set; } 

        public TwitchMessage Twitch { get => (this is TwitchMessage) ? this as TwitchMessage : new TwitchMessage(); }
        public YouTubeMessage YouTube { get => (this is YouTubeMessage) ? this as YouTubeMessage : new YouTubeMessage(); }
        public MixerMessage Mixer { get => (this is MixerMessage) ? this as MixerMessage : new MixerMessage(); }
    }
}
