namespace SlackCoffee.SlackAuthentication
{
    public class SlackWorkspace
    {
        public string Name { get; set; }
        public string SigningSecret { get; set; }
        public string OAuthAccessToken { get; set; }
    }

    public class SlackConfig
    {
        public SlackWorkspace[] Workspaces { get; set; }
    }
}
