using System;
using System.Linq;
using System.Threading.Tasks;
using SlackCoffee.Models;
using SlackCoffee.Services;
using SlackCoffee.Utils;

namespace SlackCoffee.Controllers.CoffeeCommands
{
    public partial class CoffeeCommandHandlers
    {
        [CoffeeCommand("적립", "커피 요금을 적립합니다 (사용법: [금액])", false)]
        public async Task FillWallet(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            if (!int.TryParse(text, out var amount))
                throw new NotWellFormedException();

            var u = await coffee.FillWalletAsync(user.Id, amount, DateTime.Now);
            response.Ephemeral($"현재 잔액은 {u.Deposit}원 입니다.");
        }

        [CoffeeCommand("잔액", "현재 잔액을 확인합니다", false)]
        public async Task GetDeposit(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            var deposit = await coffee.GetDepositAsync(user.Id);
            response.Ephemeral($"현재 잔액은 {deposit}원 입니다.");
        }

        [CoffeeCommand("운영자지정", "운영자를 지정하거나 취소합니다 (사용법: [@사용자] [1: 운영자/ 2: 일반 사용자])", true)]
        public async Task SetManager(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            string[] splitted = text.Split(' ').Select(s => s.Trim()).ToArray();
            if (splitted.Length != 2 || !int.TryParse(splitted[1], out var isManagerInt))
                throw new NotWellFormedException();

            var userId = SlackTools.StringToUserId(splitted[0]);
            if (userId == null)
                throw new NotWellFormedException();

            var isManager = isManagerInt > 0;
            var changedUser = await coffee.UpdateUserAsync(userId, isManager);

            var subText = isManager ? "운영자로 지정 하였습니다." : "일반 사용자로 지정 하였습니다.";
            response.InChannel($"{user.Name} 님이 {SlackTools.StringToUserId(changedUser.Id)} 님을 {subText}");
        }
    }
}
