using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SlackCoffee.SlackAuthentication
{
    public class SlackAuthorizeAttribute : AuthorizeAttribute
    {
        public SlackAuthorizeAttribute() : base("Slack") { }
    }
}
