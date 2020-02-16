using SlackCoffee.Services;
using SlackCoffee.Utils.SlackApi;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SlackCoffee.Utils
{

    public class SlackResponse
    {
        private readonly SlackRequest _request;
        private readonly List<(string, string, bool)> _messages = new List<(string, string, bool)>();

        public SlackResponse(SlackRequest request)
        {
            _request = request;
        }

        public void Empty()
        {
            _messages.Clear();
        }

        public SlackResponse InChannel(string text, string channelName = null)
        {
            string channelId;
            if (string.IsNullOrEmpty(channelName))
                channelId = _request.ChannelId;
            else
                channelId = _request.Workspace.Channels[channelName];

            _messages.Add((channelId, text, true));
            return this;
        }
        public SlackResponse Ephemeral(string text, string channelName = null)
        {
            string channelId;
            if (string.IsNullOrEmpty(channelName))
                channelId = _request.ChannelId;
            else
                channelId = _request.Workspace.Channels[channelName];

            _messages.Add((channelId, text, false));
            return this;
        }

        public async Task SendAsync(ISlackService service)
        {
            var tasks = _messages.Select(t => SendMessageAsync(t, service));
            await Task.WhenAll(tasks);
        }

        private async Task SendMessageAsync((string channel, string text, bool inChannel) messageInfo, ISlackService service)
        {
            if (messageInfo.inChannel)
                await service.PostMessageAsync(_request.Workspace.Name, messageInfo.channel, messageInfo.text);
            else
                await service.PostEphemeralAsync(_request.Workspace.Name, messageInfo.channel, _request.UserId, messageInfo.text);
        }
    }
}
