using System.Net.Http;
using System.Text.Json.Serialization;

namespace SlackCoffee.Utils.SlackApi
{
    public class Channel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class ConversationsListRequest : SlackApiRequest<ConversationsListRequest.Response>
    {
        public class Response : SlackApiResponse
        {
            [JsonPropertyName("channels")]
            public Channel[] Channels { get; set; }
        }

        public ConversationsListRequest()
            : base(HttpMethod.Get, "api/conversations.list")
        {
        }
    }
}
