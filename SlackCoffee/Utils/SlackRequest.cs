using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using SlackCoffee.SlackAuthentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SlackCoffee.Utils
{
    public class FormKeyAttribute : Attribute
    {
        public readonly string Key;

        public FormKeyAttribute(string key)
        {
            Key = key;
        }
    }

    public class SlackRequest
    {
        [FormKey("token")]
        public string Token { get; private set; }

        [FormKey("command")]
        public string Command { get; private set; }

        [FormKey("text")]
        public string Text { get; private set; }

        [FormKey("response_url")]
        public string ResponseUrl { get; private set; }

        [FormKey("trigger_id")]
        public string TriggerId { get; private set; }

        [FormKey("user_id")]
        public string UserId { get; private set; }

        [FormKey("user_name")]
        public string UserName { get; private set; }
        
        [FormKey("team_id")]
        public string TeamId { get; private set; }

        [FormKey("channel_id")]
        public string ChannelId { get; private set; }

        [FormKey("enterprise_id")]
        public string EnterpriseId { get; private set; }

        public readonly SlackWorkspace Workspace;

        private static Dictionary<string, PropertyInfo> properties;

        private static Dictionary<string, PropertyInfo> GetProperties()
        {
            if (properties == null)
            {
                properties = new Dictionary<string, PropertyInfo>();
                var type = typeof(SlackRequest);
                foreach ((var p, var a) in type.GetProperties().Select(p => (p, p.GetCustomAttribute<FormKeyAttribute>())))
                {
                    if (a == null)
                        continue;
                    properties.Add(a.Key, p);
                }
            }

            return properties;
        }

        public SlackRequest(HttpContext context, SlackConfig config)
        {
            Workspace = config.Workspaces.First(w => w.Name == context.SlackWorkspaceName());
            foreach (var item in GetProperties())
            {
                if (!context.Request.Form.TryGetValue(item.Key, out var values) || values.Count <= 0)
                    continue;
                item.Value.SetValue(this, values[0]);
            }
        }
    }
}
