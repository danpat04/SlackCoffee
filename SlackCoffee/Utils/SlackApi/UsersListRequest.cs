using System.Net.Http;
using System.Text.Json.Serialization;

namespace SlackCoffee.Utils.SlackApi
{
    public class Member
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("real_name")]
        public string RealName { get; set; }
    }

    public class UsersListRequest : SlackApiRequest<UsersListRequest.Response>
    {
        public class Response : SlackApiResponse
        {
            [JsonPropertyName("members")]
            public Member[] Members { get; set; }
        }

        public UsersListRequest()
            : base(HttpMethod.Get, "api/users.list")
        {
        }
    }
}
