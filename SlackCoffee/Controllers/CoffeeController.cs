using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SlackBot;
using SlackBot.SlackAuthentication;
using SlackCoffee.Controllers.CoffeeCommands;
using SlackCoffee.Models;
using SlackCoffee.Services;
using Microsoft.EntityFrameworkCore;

namespace SlackCoffee.Controllers
{

    [Route("coffee/[action]")]
    [SlackAuthorize]
    [ApiController]
    public class CoffeeController : ControllerBase
    {
        private readonly DbContextOptions<CoffeeContext> _dbContextOptions;
        private readonly ISlackService _slackService;
        private readonly ILogger _logger;

        private static CoffeeCommandHandlers commands = new CoffeeCommandHandlers();

        public CoffeeController(DbContextOptions<CoffeeContext> options, ISlackService slackService, ILogger<CoffeeController> logger)
        {
            _dbContextOptions = options;
            _slackService = slackService;
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

            // 기다리지 않는다.
            Task.Run(() => ExecuteCommand(_dbContextOptions, request, response));
            return Ok();
        }

        private async void ExecuteCommand(DbContextOptions options, SlackRequest request, SlackResponse response)
        {
            using var context = new CoffeeContext(options);
            try
            {
                await ExecuteCommandAsync(context, request, response);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error occured while handling {request}");
            }
        }

        private async Task ExecuteCommandAsync(CoffeeContext context, SlackRequest request, SlackResponse response)
        {

            var splitted = request.Text.ToString().Trim().Split(' ', 2);

            var command = splitted[0];
            var option = splitted.Length > 1 ? splitted[1] : "";

            using var coffee = new CoffeeService(context);

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

            try
            {
                await commands.HandleCommandAsync(coffee, user, command, option, response, _logger);
                await coffee.SaveAsync();
            }
            catch (BadRequestException e)
            {
                response.Empty();
                response.Ephemeral(e.ResponseMsg);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error occured during handling command: {command}");
            }

            await response.SendAsync(_slackService);
        }
    }
}