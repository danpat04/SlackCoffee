using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SlackCoffee.Utils.SlackApi
{
    public class SlackApiRequest<TResponse> : HttpRequestMessage
        where TResponse : SlackApiResponse
    {
        public SlackApiRequest(HttpMethod method, string requestUri) : base(method, requestUri) { }

        public async Task<TResponse> SendAsync(HttpClient client)
        {
            var response = await client.SendAsync(this);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"{response.StatusCode}: {response.ReasonPhrase}");

            using var responseStream = await response.Content.ReadAsStreamAsync();
            var test = await response.Content.ReadAsStringAsync();
            return await JsonSerializer.DeserializeAsync<TResponse>(responseStream);
        }

        public void AddJsonContent<T>(T content)
        {
            var contentData = JsonSerializer.Serialize<T>(content);
            Content = new StringContent(contentData, Encoding.UTF8, "application/json");
        }
    }

    public class SlackApiResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }
    }
}
