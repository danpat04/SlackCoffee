using SlackCoffee.Models;
using SlackCoffee.Services;
using SlackCoffee.Utils;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackCoffee.Controllers.CoffeeCommands
{
    public partial class CoffeeCommandHandlers
    {
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
        public async Task<SlackResponse> AddMenu(CoffeeService coffee, User user, string text)
        {
            var menu = UnpackMenu(text);
            if (menu == null)
                throw new BadRequestException("잘못된 형식입니다.");

            await coffee.AddMenuAsync(menu);

            return Ok($"{menu.Id}를 {menu.Price}원으로 추가하였습니다.");
        }

        [CoffeeCommand("메뉴수정", "[이름], [설명], [가격(원)], [순서(숫자)]", true)]
        public async Task<SlackResponse> ChangeMenu(CoffeeService coffee, User user, string text)
        {
            var menu = UnpackMenu(text);
            if (menu == null)
                throw new BadRequestException("잘못된 형식입니다.");

            await coffee.ChangeMenuAsync(menu);

            return Ok($"{menu.Id}를 {menu.Price}원으로 수정하였습니다.");
        }

        [CoffeeCommand("메뉴활성화", "[이름] [0: 비활성화/ 1: 활성화]", true)]
        public async Task<SlackResponse> EnableMenu(CoffeeService coffee, User user, string text)
        {
            var splitted = text.Split(' ').Select(s => s.Trim()).ToArray();
            if (splitted.Length != 2 || !int.TryParse(splitted[1], out var enabledInt))
                throw new BadRequestException("잘못된 형식입니다.");

            var enabled = enabledInt > 0;
            await coffee.EnableMenuAsync(splitted[0], enabled);

            return Ok($"{splitted[0]}을 {(enabled ? "활성화" : "비활성화")} 시켰습니다.");
        }

        [CoffeeCommand("메뉴", "", false)]
        public async Task<SlackResponse> GetMenu(CoffeeService coffee, User user, string text)
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

            return Ok(sb.ToString());
        }
    }
}
