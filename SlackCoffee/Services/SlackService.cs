using Microsoft.Extensions.Options;
using SlackCoffee.SlackAuthentication;
using SlackCoffee.Utils.SlackApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SlackCoffee.Services
{
    public class Workspace
    {
        public string Id { get; internal set; }

        public string Name { get; internal set; }

        public Dictionary<string, string> Channels { get; internal set; }
    }

    public interface ISlackService
    {
        Task<Workspace> GetWorkspaceAsync(string workspaceName);

        Task<List<Channel>> GetChannelsAsync(string workspaceName);

        Task<Member[]> GetMembersAsync(string workspaceName);

        Task<Message> PostMessageAsync(string workspaceName, string channelId, string text);

        Task PostEphemeralAsync(string workspaceName, string channelId, string userId, string text);
    }

    public class SlackService : ISlackService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly SlackConfig _config;

        private readonly Dictionary<string, Workspace> _workspaces = new Dictionary<string, Workspace>();

        public SlackService(IHttpClientFactory clientFactory, IOptions<SlackConfig> config)
        {
            _clientFactory = clientFactory;
            _config = config.Value;
        }

        public HttpClient CreateClient(string workspaceName)
        {
            var workspaceConfig = _config.Workspaces.First(w => w.Name == workspaceName);
            if (workspaceConfig == null)
                throw new KeyNotFoundException($"Cannot find worksapce named {workspaceName}");

            var client = _clientFactory.CreateClient();
            client.BaseAddress = new Uri("https://slack.com/");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {workspaceConfig.OAuthAccessToken}");
            return client;
        }

        public async Task<Workspace> GetWorkspaceAsync(string workspaceName)
        {
            var workspaceConfig = _config.Workspaces.First(w => w.Name == workspaceName);
            if (!_workspaces.TryGetValue(workspaceName, out var workspace))
            {
                var teamInfo = await GetTeamInfoAsync(workspaceName);

                workspace = new Workspace
                {
                    Id = teamInfo.Id,
                    Name = workspaceName,
                    Channels = workspaceConfig.Channels
                };

                _workspaces.Add(workspace.Name, workspace);
            }

            return workspace;
        }

        public async Task<Team> GetTeamInfoAsync(string workspaceName)
        {
            HttpClient client = CreateClient(workspaceName);
            var request = new TeamInfoRequest();
            var response = await request.SendAsync(client);

            return response.Team;
        }

        public async Task<List<Channel>> GetChannelsAsync(string workspaceName)
        {
            throw new NotImplementedException();

            HttpClient client = CreateClient(workspaceName);
            var request = new ConversationsListRequest();
            List<Channel> channels = new List<Channel>();

            ConversationsListRequest.Response response;
            do
            {
                response = await request.SendAsync(client);
                channels.AddRange(response.Channels);

                request = new ConversationsListRequest(response.ResponseMetadata.NextCursor);
            }
            while (response.Channels.Length > 0 && !string.IsNullOrEmpty(request.Cursor));

            return channels;
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
            var request = new ChatPostMessageRequest(channelId, text);
            var response = await request.SendAsync(client);

            return response.Msg;
        }

        public async Task PostEphemeralAsync(string workspaceName, string channelId, string userId, string text)
        {
            HttpClient client = CreateClient(workspaceName);
            var request = new ChatPostEphemeralMessageRequest(channelId, userId, text);
            await request.SendAsync(client);
        }
    }
}
