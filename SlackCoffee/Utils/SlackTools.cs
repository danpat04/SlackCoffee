using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace SlackCoffee.Utils
{
    public class SlackTools
    {
        public static string UserIdToString(string userId)
        {
            return $"<@{userId}>";
        }

        private static readonly Regex UserIdPattern = new Regex(@"^\<\@(<userId>)\>$");

        public static string StringToUserId(string text)
        {
            var matches = UserIdPattern.Matches(text);
            return matches.Count != 1 ? null : matches[0].Value;
        }
    }
}
