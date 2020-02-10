using System.Text.RegularExpressions;

namespace SlackCoffee.Utils
{
    public class SlackTools
    {
        public static string UserIdToString(string userId)
        {
            return $"<@{userId}>";
        }

        private static readonly Regex UserIdPattern = new Regex(@"^<@(?<userId>[A-Z0-9]+)\|[a-zA-Z0-9' \.]+>$");

        public static string StringToUserId(string text)
        {
            var matches = UserIdPattern.Matches(text);
            foreach (Match match in matches)
            {
                if (match.Groups.TryGetValue("userId", out var value))
                    return value.Value;
            }
            return null;
        }
    }
}
