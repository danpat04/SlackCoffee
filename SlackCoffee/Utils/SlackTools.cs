using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SlackCoffee.Utils
{
    public class SlackTools
    {
        public static string UserIdToString(string userId)
        {
            return $"<@{userId}>";
        }
    }
}
