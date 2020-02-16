using SlackCoffee.Services;
using SlackCoffee.Utils.SlackApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SlackCoffee.SlackAuthentication
{
    public class SlackWorkspace
    {
        public string Name { get; set; }
        public string SigningSecret { get; set; }
        public string OAuthAccessToken { get; set; }

        public Dictionary<string, string> Channels { get; set; }
    }

    public class SlackConfig
    {
        public SlackWorkspace[] Workspaces { get; set; }
    }
}
