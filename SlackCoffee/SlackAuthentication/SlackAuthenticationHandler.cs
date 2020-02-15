using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SlackCoffee.SlackAuthentication
{
    public class SlackAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string WorkspaceClaimType = "SlackWorkspace";
        private const string SlackVerificationVersion = "v0";
        private readonly SlackConfig _slackConfig;

        public SlackAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IOptions<SlackConfig> slackConfig)
            : base(options, logger, encoder, clock)
        {
            _slackConfig = slackConfig.Value;
        }

        private Dictionary<string, HMACSHA256> _hashes;

        private string GetWorkspace(byte[] baseString, string signature)
        {
            if (_hashes == null)
            {
                _hashes = new Dictionary<string, HMACSHA256>();
                foreach (var workspace in _slackConfig.Workspaces)
                {
                    var key = Encoding.ASCII.GetBytes(workspace.SigningSecret);
                    _hashes.Add(workspace.Name, new HMACSHA256(key));
                }
            }

            foreach (var info in _hashes)
            {
                if (ComputeSignature(info.Value, baseString) == signature)
                    return info.Key;
            }
            return null;
        }

        private string ComputeSignature(HMACSHA256 hash, byte[] baseString)
        {
            byte[] hashedValue = hash.ComputeHash(baseString);
            StringBuilder hex = new StringBuilder("v0=", hashedValue.Length * 2 + 3);
            foreach (byte b in hashedValue)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
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
            var workspace = GetWorkspace(baseString, slackSignature);
            if (workspace == null)
                return AuthenticateResult.Fail("Signature check missed");

            var claims = new[] {
                new Claim(WorkspaceClaimType, workspace)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
