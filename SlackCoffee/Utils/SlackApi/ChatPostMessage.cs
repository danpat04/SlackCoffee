using System.Net.Http;
using System.Text.Json.Serialization;

namespace SlackCoffee.Utils.SlackApi
{
    public class Message
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("user")]
        public string UserId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("subtype")]
        public string Subtype { get; set; }

        [JsonPropertyName("ts")]
        public string Timestamp { get; set; }
    }

    public class ChatPostMessage: SlackApiRequest<ChatPostMessage.Response>
    {
        public class Response : SlackApiResponse
        {
            [JsonPropertyName("channel")]
            public string Channel { get; set; }

            [JsonPropertyName("ts")]
            public string Timestamp { get; set; }

            [JsonPropertyName("message")]
            public Message Msg { get; set; }
        }

        public class ChatContent
        {
            [JsonPropertyName("channel")]
            public string Channel { get; set; }

            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        public ChatPostMessage(string channelId, string text)
            : base(HttpMethod.Post, "api/chat.postMessage")
        {
            AddJsonContent(new ChatContent { Channel = channelId, Text = text });
        }
    }
}
