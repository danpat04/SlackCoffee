using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SlackCoffee.Utils
{
    public class SlackWorkspace
    {
        public string Name { get; set; }
        public string SigningSecret { get; set; }
        public string OAuthAccessToken { get; set; }
    }

    public class SlackConfig
    {
        public SlackWorkspace[] SlackWorkspaces { get; set; }
    }
}
