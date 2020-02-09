using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
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

            Request.EnableBuffering();

            var bodyReader = new StreamReader(Request.Body);
            var body = await bodyReader.ReadToEndAsync();
            // body는 controller에서도 쓰기 때문에 읽었으면 reset 해 줘야 한다
            Request.Body.Seek(0, SeekOrigin.Begin);

            var baseString = Encoding.ASCII.GetBytes($"{SlackVerificationVersion}:{requestTimestamp[0]}:{body}");
            var key = Encoding.ASCII.GetBytes("c16aded04edf407c06e252191dc375c5");
            using var hmac = new HMACSHA256(key);

            byte[] hashValue = hmac.ComputeHash(baseString);
            StringBuilder hex = new StringBuilder("v0=", hashValue.Length * 2 + 3);
            foreach (byte b in hashValue)
                hex.AppendFormat("{0:x2}", b);

            var ourSignature = hex.ToString();
            var theirSignature = slackSignature[0];

            if (ourSignature != theirSignature)
                return AuthenticateResult.Fail("Signature check missed");

            var claims = new[] {
                new Claim("SlackSignature", ourSignature)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
