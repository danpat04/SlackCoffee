using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SlackCoffee.Models;
using SlackCoffee.Services;
using SlackCoffee.Utils;

namespace SlackCoffee.Controllers
{
    public class CoffeeCommand : Command<User>
    {
        public readonly string Description;
        public readonly bool ForManager;

        public CoffeeCommand(string id, string description, bool forManager)
            : base(id)
        {
            Description = description;
            ForManager = forManager;
        }

        public override bool Authorized(User user)
        {
            return !ForManager || user.IsManager;
        }

        public override string MakeDescription()
        {
            if (ForManager)
                return $"[운영자 전용] {Description}";
            return Description;
        }

        public override int CompareTo(object other)
        {
            if (ForManager == ((CoffeeCommand)other).ForManager) return 0;
            return ForManager ? -1 : 1;
        }
    }

    public class CoffeeCommandAttribute: CommandAttribute
    {
        public CoffeeCommandAttribute(string command, string description, bool forManager)
            : base(new CoffeeCommand(command, description, forManager)) { }

        public CoffeeCommandAttribute(string command, string description)
            : base(new CoffeeCommand(command, description, false)) { }
    }

    [Route("coffee/[action]")]
    [Authorize(Policy = "Slack")]
    [ApiController]
    public class CoffeeController : ControllerBase
    {
        private readonly CoffeeContext _coffeeContext;
        private readonly ILogger _logger;

        private static CommandHandlers<CoffeeController, User> handlers = new CommandHandlers<CoffeeController, User>();

        public CoffeeController(CoffeeContext context, ILogger<CoffeeController> logger)
        {
            _coffeeContext = context;
            _logger = logger;
        }

        private IActionResult SlackOk(string message, bool inChannel=false)
        {
            return Ok(inChannel ? SimpleResponse.InChannel(message) : SimpleResponse.Ephemeral(message));
        }

        private IActionResult SlackBadRequest(string message)
        {
            // 모든 응답은 일단 Ok. 에러 메시지는 비공개로 전달해야 한다.
            return Ok(SimpleResponse.Ephemeral(message));
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

            var handler = handlers.GetHandler(this, user, command);
            if (handler == null)
                return SlackBadRequest("없는 명령어 입니다.");

            IActionResult result;
            try
            {
                result = await handler(option);
            }
            catch (BadRequestException e)
            {
                return SlackBadRequest(e.Message);
            }

            return result;
        }

        private Menu UnpackMenu(string text)
        {
            var splitted = text.Split(',', 4).Select(t => t.Trim()).ToArray();
            if (splitted.Length != 4)
                return null;

            if (!int.TryParse(splitted[2], out var price) ||
                !int.TryParse(splitted[3], out var order))
            {
                return null;
            }

            return new Menu {
                Id = splitted[0],
                Description = splitted[1],
                Price = price,
                Order = order,
                Enabled = true
            };
        }

        [CoffeeCommand("메뉴추가", "[이름], [설명], [가격(원)], [순서(숫자)]", true)]
        public async Task<IActionResult> AddMenu(User user, string text)
        {
            var menu = UnpackMenu(text);
            if (menu == null)
                return SlackBadRequest("잘못된 형식입니다.");

            var coffee = new CoffeeService(_coffeeContext);
            await coffee.AddMenuAsync(menu);

            await coffee.SaveAsync();
            return SlackOk($"{menu.Id}를 {menu.Price}원으로 추가하였습니다.");
        }

        [CoffeeCommand("메뉴수정", "[이름], [설명], [가격(원)], [순서(숫자)]", true)]
        public async Task<IActionResult> ChangeMenu(User user, string text)
        {
            var menu = UnpackMenu(text);
            if (menu == null)
                return SlackBadRequest("잘못된 형식입니다.");

            var coffee = new CoffeeService(_coffeeContext);
            await coffee.ChangeMenuAsync(menu);

            await coffee.SaveAsync();
            return SlackOk($"{menu.Id}를 {menu.Price}원으로 수정하였습니다.");
        }

        [CoffeeCommand("메뉴활성화", "[이름] [0: 비활성화/ 1: 활성화]", true)]
        public async Task<IActionResult> EnableMenu(User user, string text)
        {
            var splitted = text.Split(' ').Select(s => s.Trim()).ToArray();
            if (splitted.Length != 2 || !int.TryParse(splitted[1], out var enabledInt))
                return SlackBadRequest("잘못된 형식입니다.");

            var enabled = enabledInt > 0;

            var coffee = new CoffeeService(_coffeeContext);
            await coffee.EnableMenuAsync(splitted[0], enabled);

            await coffee.SaveAsync();
            return SlackOk($"{splitted[0]}을 {(enabled ? "활성화" : "비활성화")} 시켰습니다.");
        }

        [CoffeeCommand("메뉴", "", false)]
        public Task<IActionResult> GetMenu(User user, string text)
        {
            var enabledMenus = _coffeeContext.Menus.Where(m => m.Enabled).OrderBy(m => m.Order);
            var disabledMenus = _coffeeContext.Menus.Where(m => !m.Enabled).OrderBy(m => m.Order).ToArray();

            var sb = new StringBuilder("*메뉴*").AppendLine();
            foreach (var m in enabledMenus)
            {
                sb.AppendLine($"*{m.Id}* - {m.Description}: {m.Price}원");
            }

            if (disabledMenus.Length > 0)
            {
                sb.AppendLine().AppendLine("*비활성화된 메뉴*");
                foreach (var m in disabledMenus)
                {
                    sb.AppendLine($"{m.Description}");
                }
            }

            return Task.FromResult(SlackOk(sb.ToString()));
        }
    }
}