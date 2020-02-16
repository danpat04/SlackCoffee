using System.Collections.Generic;
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
        [JsonPropertyName("exclude_archived")]
        public bool ExcludeArchived { get; set; }

        [JsonPropertyName("cursor")]
        public string Cursor { get; set; }

        public class Response : SlackApiResponse
        {
            public class Metadata
            {
                [JsonPropertyName("next_cursor")]
                public string NextCursor { get; set; }
            }

            [JsonPropertyName("channels")]
            public Channel[] Channels { get; set; }

            [JsonPropertyName("response_metadata")]
            public Metadata ResponseMetadata { get; set; }
        }

        public ConversationsListRequest()
            : base(HttpMethod.Get, "api/conversations.list")
        {
            var content = new Dictionary<string, string>();
            content.Add("exclude_archived", "true");
            SetUrlEncodedContent(content);
        }

        public ConversationsListRequest(string cursor)
            : base(HttpMethod.Get, "api/conversations.list")
        {
            var content = new Dictionary<string, string>();
            content.Add("exclude_archived", "true");
            content.Add("cursor", cursor);
            SetUrlEncodedContent(content);
        }
    }
}
