﻿using SlackBot;
using SlackCoffee.Models;
using SlackCoffee.Services;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackCoffee.Controllers.CoffeeCommands
{
    public partial class CoffeeCommandHandlers
    {
        private Menu UnpackMenu(string text)
        {
            var splitted = text.Split(',').Select(t => t.Trim()).ToArray();
            if (splitted.Length < 4)
                return null;

            if (!int.TryParse(splitted[2], out var price) ||
                !int.TryParse(splitted[3], out var order))
            {
                return null;
            }

            var steamMilkNeeded = false;
            if (splitted.Length > 4)
            {
                if (!int.TryParse(splitted[4], out var steamMilkInt))
                    return null;
                steamMilkNeeded = steamMilkInt > 0;
            }

            return new Menu {
                Id = splitted[0],
                Description = splitted[1],
                Price = price,
                Order = order,
                Enabled = true,
                SteamMilkNeeded = steamMilkNeeded
            };
        }

        [CoffeeCommand("메뉴추가", "[이름], [설명], [가격(원)], [순서(숫자)] [스팀밀크 필요(옵션)]", true)]
        public async Task AddMenu(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            var menu = UnpackMenu(text);
            if (menu == null)
                throw new NotWellFormedException();

            await coffee.AddMenuAsync(menu);

            response.Ephemeral($"{menu.Id}를 {menu.Price}원으로 추가하였습니다.");
        }

        [CoffeeCommand("메뉴수정", "[이름], [설명], [가격(원)], [순서(숫자)] [스팀밀크 필요(옵션)]", true)]
        public async Task ChangeMenu(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            var menu = UnpackMenu(text);
            if (menu == null)
                throw new NotWellFormedException();

            await coffee.ChangeMenuAsync(menu);

            response.Ephemeral($"{menu.Id}를 {menu.Price}원으로 수정하였습니다.");
        }

        [CoffeeCommand("메뉴활성화", "[이름] [0: 비활성화/ 1: 활성화]", true)]
        public async Task EnableMenu(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            var splitted = text.Split(' ').Select(s => s.Trim()).ToArray();
            if (splitted.Length != 2 || !int.TryParse(splitted[1], out var enabledInt))
                throw new NotWellFormedException();

            var enabled = enabledInt > 0;
            await coffee.EnableMenuAsync(splitted[0], enabled);

            response.Ephemeral($"{splitted[0]}를 {(enabled ? "활성화" : "비활성화")} 시켰습니다.");
        }

        [CoffeeCommand("메뉴", "메뉴 목록을 표시합니다", false)]
        public async Task GetMenu(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            var menus = await coffee.GetMenusAsync();
            var enabledMenus = menus.Where(m => m.Enabled);
            var disabledMenus = menus.Where(m => !m.Enabled).ToArray();

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

            response.Ephemeral(sb.ToString());
        }
    }
}
