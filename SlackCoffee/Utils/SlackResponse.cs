using SlackCoffee.Services;
using SlackCoffee.SlackAuthentication;
using SlackCoffee.Utils.SlackApi;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SlackCoffee.Utils
{
    public abstract class SlackResponse
    {
    }

    public class SimpleResponse : SlackResponse
    {
        [JsonPropertyName("response_type")]
        public string ResponseType { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        public SimpleResponse(string text, bool inChannel)
        {
            ResponseType = inChannel ? "in_channel" : "ephemeral";
            Text = text;
        }
    }

    public enum ResponseChannelType
    {
        SenderChannel = 0,
        UserChannel = 1,
        ManagerChannel = 2
    }

    public class MultipleResponse : SimpleResponse
    {
        private readonly List<(string, ResponseChannelType)> _responses = new List<(string, ResponseChannelType)>();

        public bool IsMultiple => _responses.Count > 0;

        public MultipleResponse(string text)
            : base(text, false)
        {
        }

        public MultipleResponse(string text, bool inChannel)
            : base(text, inChannel)
        {
        }

        public MultipleResponse AddResponse(string text, ResponseChannelType channel)
        {
            _responses.Add((text, channel));
            return this;
        }

        public async Task SendChannelResponse(ISlackService service, SlackRequest request)
        {
            Channel[] channels = null;

            foreach ((var text, var channelType) in _responses)
            {
                string channelId;
                if (channelType == ResponseChannelType.SenderChannel)
                    channelId = request.ChannelId;
                else
                {
                    if (channels == null)
                        channels = await service.GetChannelsAsync(request.Workspace.Name);

                    var channelName = channelType == ResponseChannelType.ManagerChannel ?
                        request.Workspace.ManagerChannelName : request.Workspace.UserChannelName;
                    var channel = channels.FirstOrDefault(c => c.Name == channelName);
                    channelId = channel.Id;
                }

                await service.PostMessageAsync(request.Workspace.Name, channelId, text);
            }
        }
    }
}
