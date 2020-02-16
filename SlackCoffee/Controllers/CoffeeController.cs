using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SlackCoffee.Controllers.CoffeeCommands;
using SlackCoffee.Models;
using SlackCoffee.Services;
using SlackCoffee.SlackAuthentication;
using SlackCoffee.Utils;

namespace SlackCoffee.Controllers
{

    [Route("coffee/[action]")]
    [SlackAuthorize]
    [ApiController]
    public class CoffeeController : ControllerBase
    {
        private readonly ISlackService _slackService;
        private readonly CoffeeContext _coffeeContext;
        private readonly SlackConfig _config;
        private readonly ILogger _logger;

        private static CoffeeCommandHandlers commands = new CoffeeCommandHandlers();

        public CoffeeController(ISlackService slackService, CoffeeContext context, IOptions<SlackConfig> config, ILogger<CoffeeController> logger)
        {
            _slackService = slackService;
            _coffeeContext = context;
            _config = config.Value;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Check()
        {
            return Ok("CoffeeBot Working!");
        }

        private async Task<string> GetUserNameAsync(string workspaceName, string userId)
        {
            var members = await _slackService.GetMembersAsync(workspaceName);
            var member = members.FirstOrDefault(m => m.Id == userId);
            if (member == null)
                return null;

            return member.RealName.Split(' ').FirstOrDefault();
        }

        [HttpPost]
        public async Task<IActionResult> Do()
        {
            try
            {
                return await DoAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occured during handling slash command");
                throw;
            }
        }

        private async Task<IActionResult> DoAsync()
        {
            var workspaceName = HttpContext.SlackWorkspaceName();
            var request = new SlackRequest(HttpContext, await _slackService.GetWorkspaceAsync(workspaceName));
            var response = new SlackResponse(request);

            using var coffee = new CoffeeService(_coffeeContext);

            var user = await coffee.FindUserAsync(request.UserId);
            if (user == null)
            {
                var userName = await GetUserNameAsync(request.Workspace.Name, request.UserId);
                user = await coffee.CreateUserAsync(request.UserId, userName, false);
                await coffee.SaveAsync();
            }
            else if(string.IsNullOrEmpty(user.Name))
            {
                var userName = await GetUserNameAsync(request.Workspace.Name, request.UserId);
                user = await coffee.UpdateUserNameAsync(request.UserId, userName);
                await coffee.SaveAsync();
            }

            var splitted = request.Text.ToString().Trim().Split(' ', 2);

            var command = splitted[0];
            var option = splitted.Length > 1 ? splitted[1] : "";

            try
            {
                await commands.HandleCommandAsync(coffee, user, command, option, response, _logger);
            }
            catch (BadRequestException e)
            {
                return Ok(e.ResponseMsg);
            }

            await coffee.SaveAsync();
            await response.SendAsync(_slackService);

            return Ok();
        }
    }
}