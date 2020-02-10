using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SlackCoffee.Models;
using SlackCoffee.Services;
using SlackCoffee.Utils;

namespace SlackCoffee.Controllers.CoffeeCommands
{
    public partial class CoffeeCommandHandlers
    {
        [CoffeeCommand("적립", "[원]", false)]
        public async Task<SlackResponse> FillWallet(CoffeeService coffee, User user, string text)
        {
            if (!int.TryParse(text, out var amount))
                throw new BadRequestException("잘못된 형식입니다.");

            var u = await coffee.FillWalletAsync(user.Id, amount, DateTime.Now);
            return Ok($"현재 잔액은 {u.Deposit}원 입니다.");
        }

        [CoffeeCommand("잔액", "", false)]
        public async Task<SlackResponse> GetDeposit(CoffeeService coffee, User user, string text)
        {
            var deposit = coffee.GetDepositAsync(user.Id);
            return Ok($"현재 잔액은 {deposit}원 입니다.");
        }

        [CoffeeCommand("운영자지정", "[@사용자] [1: 운영자/ 2: 일반 사용자]", true)]
        public async Task<SlackResponse> SetManager(CoffeeService coffee, User user, string text)
        {
            string[] splitted = text.Split(' ').Select(s => s.Trim()).ToArray();
            if (splitted.Length != 2 || !int.TryParse(splitted[1], out var isManagerInt))
                throw new BadRequestException("잘못된 형식입니다.");

            var userId = SlackTools.StringToUserId(splitted[0]);
            if (userId == null)
                throw new BadRequestException("잘못된 형식입니다.");

            var isManager = isManagerInt > 0;
            await coffee.UpdateUserAsync(userId, isManager);

            return Ok(isManager ? "운영자로 지정 하였습니다." : "일반 사용자로 지정 하였습니다.");
        }
    }
}
