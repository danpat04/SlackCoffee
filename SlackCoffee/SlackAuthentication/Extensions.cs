using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SlackCoffee.SlackAuthentication
{
    public static class Extensions
    {
        public static void AddSlackAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
                options.AddPolicy("Slack",
                policy => policy.RequireClaim("SlackWorkspace")));

            // configure slack authentication 
            services.AddAuthentication("SlackAuthentication")
                .AddScheme<AuthenticationSchemeOptions, SlackAuthenticationHandler>("SlackAuthentication", null);
        }
    }
}
