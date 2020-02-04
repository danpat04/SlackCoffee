using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SlackCoffee.Utils
{
    public class SlackAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private const string SlackVerificationVersion = "v0";

        public SlackAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Slack-Request-Timestamp", out var requestTimestamp))
                return AuthenticateResult.Fail("X-Slack-Request-Timestamp missing");
            // check request timestamp

            if (!Request.Headers.TryGetValue("X-Slack-Signature", out var slackSignature))
                return AuthenticateResult.Fail("X-Slack-Signature missing");

            var bodyReader = new StreamReader(Request.Body);

            var baseString = $"{SlackVerificationVersion}:{requestTimestamp[0]}:{await bodyReader.ReadToEndAsync()}";
            using var hmac = new HMACSHA256(Encoding.ASCII.GetBytes("c16aded04edf407c06e252191dc375c5"));

            byte[] hashValue = hmac.ComputeHash(Encoding.ASCII.GetBytes(baseString));
            byte[] signature = Encoding.ASCII.GetBytes(slackSignature[0]);

            if (hashValue != signature)
                return AuthenticateResult.Fail("Signature check missed");

            var claims = new[] {
                new Claim("SlackSignatureBaseString", baseString)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
