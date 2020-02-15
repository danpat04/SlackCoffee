using Microsoft.Extensions.Options;
using SlackCoffee.SlackAuthentication;
using SlackCoffee.Utils.SlackApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SlackCoffee.Services
{

    public interface ISlackService
    {
        Task<Channel[]> GetChannelsAsync(string workspaceName);

        Task<Member[]> GetMembersAsync(string workspaceName);

        Task<Message> PostMessageAsync(string workspaceName, string channelId, string text);
    }

    public class SlackService : ISlackService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly SlackConfig _config;

        public SlackService(IHttpClientFactory clientFactory, IOptions<SlackConfig> config)
        {
            _clientFactory = clientFactory;
            _config = config.Value;
        }

        public HttpClient CreateClient(string workspaceName)
        {
            var workspace = _config.Workspaces.FirstOrDefault(w => w.Name == workspaceName);
            if (workspace == null)
                throw new KeyNotFoundException($"Cannot find worksapce named {workspaceName}");

            var client = _clientFactory.CreateClient();
            client.BaseAddress = new Uri("https://slack.com/");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {workspace.OAuthAccessToken}");
            return client;
        }

        public async Task<Channel[]> GetChannelsAsync(string workspaceName)
        {
            HttpClient client = CreateClient(workspaceName);
            var request = new ConversationsListRequest();
            var response = await request.SendAsync(client);

            return response.Channels;
        }

        public async Task<Member[]> GetMembersAsync(string workspaceName)
        {
            HttpClient client = CreateClient(workspaceName);
            var request = new UsersListRequest();
            var response = await request.SendAsync(client);

            return response.Members;
        }

        public async Task<Message> PostMessageAsync(string workspaceName, string channelId, string text)
        {
            HttpClient client = CreateClient(workspaceName);
            var request = new ChatPostMessage(channelId, text);
            var response = await request.SendAsync(client);

            return response.Msg;
        }
    }
}
