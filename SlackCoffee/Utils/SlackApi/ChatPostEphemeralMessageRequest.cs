using System.Net.Http;
using System.Text.Json.Serialization;

namespace SlackCoffee.Utils.SlackApi
{
    public class ChatPostEphemeralMessageRequest : SlackApiRequest<ChatPostEphemeralMessageRequest.Response>
    {
        public class MessageAttachments
        {
            [JsonPropertyName("pretext")]
            public string Pretext { get; set; }

            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        public class Response : SlackApiResponse
        {
            [JsonPropertyName("message_ts")]
            public string Timestamp { get; set; }
        }

        public class ChatContent
        {
            [JsonPropertyName("channel")]
            public string ChannelId { get; set; }

            [JsonPropertyName("user")]
            public string UserId { get; set; }

            [JsonPropertyName("text")]
            public string Text { get; set; }

            [JsonPropertyName("attachments")]
            public MessageAttachments Attachements { get; set; }
        }

        public ChatPostEphemeralMessageRequest(string channelId, string userId, string text)
            : base(HttpMethod.Post, "api/chat.postEphemeral")
        {
            var content = new ChatContent
            {
                ChannelId = channelId,
                UserId = userId,
                Text = text,
                Attachements = new MessageAttachments { Pretext = "", Text = text }
            };
            SetJsonContent(content);
        }
    }
}
