using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SlackCoffee.Controllers.CoffeeCommands;
using SlackCoffee.Models;
using SlackCoffee.Services;
using SlackCoffee.Utils;

namespace SlackCoffee.Controllers
{

    [Route("coffee/[action]")]
    [Authorize(Policy = "Slack")]
    [ApiController]
    public class CoffeeController : ControllerBase
    {
        private readonly CoffeeContext _coffeeContext;
        private readonly ILogger _logger;

        private static CoffeeCommandHandlers commands = new CoffeeCommandHandlers();

        public CoffeeController(CoffeeContext context, ILogger<CoffeeController> logger)
        {
            _coffeeContext = context;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Check()
        {
            return Ok("CoffeeBot Working!");
        }

        [HttpPost]
        public async Task<IActionResult> Do()
        {
            using var coffee = new CoffeeService(_coffeeContext);
            var request = new SlackRequest(Request.Form);

            var user = await coffee.FindUserAsync(request.UserId);
            if (user == null)
            {
                user = await coffee.CreateUserAsync(request.UserId, false);
                await coffee.SaveAsync();
            }

            var splitted = request.Text.ToString().Trim().Split(' ', 2);

            var command = splitted[0];
            var option = splitted.Length > 1 ? splitted[1] : "";

            SlackResponse result;
            try
            {
                result = await commands.HandleCommandAsync(coffee, user, command, option, _logger);
            }
            catch (BadRequestException e)
            {
                return Ok(e.ResponseMsg);
            }

            await coffee.SaveAsync();
            return Ok(result);
        }
    }
}