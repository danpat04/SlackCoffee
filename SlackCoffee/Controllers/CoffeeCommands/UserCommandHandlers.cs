﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlackBot;
using SlackCoffee.Models;
using SlackCoffee.Services;

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

        // [CoffeeCommand("대리적립", "", true)]
        public async Task FillOtherWallet(CoffeeService coffee, User requestUser, string text, SlackResponse response)
        {
            var splitted = text.Split(' ');
            if (splitted.Length != 2 || !int.TryParse(splitted[1], out var amount))
                throw new NotWellFormedException();

            var userId = SlackBot.Utils.StringToUserId(splitted[0]);
            var user = await coffee.FindUserAsync(userId);
            if (user == null)
            {
                user = await coffee.CreateUserAsync(userId, null, false);
                await coffee.SaveAsync();
            }

            var u = await coffee.FillWalletAsync(userId, amount, DateTime.Now);
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

            var userId = SlackBot.Utils.StringToUserId(splitted[0]);
            if (userId == null)
                throw new NotWellFormedException();

            var isManager = isManagerInt > 0;
            var changedUser = await coffee.UpdateUserAsync(userId, isManager);

            var subText = isManager ? "운영자로 지정 하였습니다." : "일반 사용자로 지정 하였습니다.";
            response.InChannel($"{user.Name} 님이 {SlackBot.Utils.StringToUserId(changedUser.Id)} 님을 {subText}");
        }

        [CoffeeCommand("충전내역", "[일 수]", false)]
        public async Task GetHistory(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            if (!int.TryParse(text, out int days))
                days = 180;

            StringBuilder sb = new StringBuilder();
            var walletHistory = await coffee.GetFillHistories(user.Id, DateTime.Now, days);
            foreach (var fill in walletHistory)
                sb.AppendLine($"{fill.At.Month}월 {fill.At.Day}일 : *{fill.Amount}* 원");

            response.Ephemeral(sb.ToString());
        }

        [CoffeeCommand("통계", "[일 수]", false)]
        public async Task GetStatistic(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            if (!int.TryParse(text, out int days))
                days = 180;

            var orders = await coffee.GetCompletedOrders(user.Id, DateTime.Now, days);

            var totalCount = orders.Length;
            var pickedCount = orders.Count(o => o.IsPicked);
            var totalPaid = orders.Sum(o => o.IsPicked ? o.Price : 0);

            var orderedMenus = orders
                .GroupBy(o => o.MenuId)
                .Select(g => (g.Key, g.Count()))
                .OrderByDescending(i => i.Item2).Take(3).ToArray();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"지난 *{days}* 일 동안").AppendLine();
            sb.AppendLine($"주문한 잔 수:\t*{totalCount}*");
            sb.AppendLine($"마신 잔 수:\t\t*{pickedCount}*, {pickedCount / (float)totalCount:P1}");
            sb.AppendLine($"총 비용:\t\t\t *{totalPaid}* 원");
            if (orderedMenus.Length > 0)
            {
                sb.AppendLine("주로 주문한 메뉴:");
                for (var i = 0; i < orderedMenus.Length; i++)
                {
                    (var menuId, var count) = orderedMenus[i];
                    sb.AppendLine($"  {i + 1}. *{menuId}*, {count / (float)totalCount:P1}");
                }
            }

            response.Ephemeral(sb.ToString());
        }

        [CoffeeCommand("이름변경", "[변경할 이름]", false)]
        public async Task ChangeNameAsync(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            await coffee.UpdateUserNameAsync(user.Id, text);
            response.Ephemeral($"이름이 *{text}* 로 변경되었습니다.");
        }

        [CoffeeCommand("정보", "[대상자]", true)]
        public async Task UserInfoAsync(CoffeeService coffee, User _, string text, SlackResponse response)
        {
            var userId = SlackBot.Utils.StringToUserId(text);
            if (userId == null)
                throw new NotWellFormedException();

            var user = await coffee.FindUserAsync(userId);
            if (user == null)
            {
                response.Ephemeral("존재하지 않는 유저입니다.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            if (user.IsManager)
            {
                sb.AppendLine($"이름: {user.Name} (운영자)");
            }
            else
            {
                sb.AppendLine($"이름: {user.Name}");
            }

            sb.AppendLine($"아이디: {user.Id}");
            sb.AppendLine($"잔액: {user.Deposit}");
            
            response.Ephemeral(sb.ToString());
        }

        [CoffeeCommand("병합", "[사용자] [사라질 사용자]", true)]
        public async Task MergeUserAsync(CoffeeService coffee, User _, string text, SlackResponse response)
        {
            var args = text.Split(' ');
            if (args.Length != 2)
                throw new NotWellFormedException();

            var targetUserId = SlackBot.Utils.StringToUserId(args[1]);
            if (targetUserId == null)
            {
                targetUserId = args[1];
            }
            
            var targetUser = await coffee.FindUserAsync(targetUserId);

            if (targetUser == null)
            {
                response.Ephemeral($"병합하여 사라질 유저({targetUserId})가 없습니다.");
                return;
            }

            var userId = SlackBot.Utils.StringToUserId(args[0]);
            if (userId == null)
                throw new NotWellFormedException();

            if (userId == targetUserId)
            {
                response.Ephemeral("같은 아이디를 합칠 수는 없습니다.");
                return;
            }
            
            var user = await coffee.FindUserAsync(userId);
            if (user == null)
            {
                user = await coffee.CreateUserAsync(userId, "신규유저", false);
            }

            string targetUserName = targetUser.Name;
            await coffee.MergeUserAsync(user, targetUser);
            response.Ephemeral($"{targetUserName}이 {user.Name}으로 합쳐졌습니다.");
        }

        [CoffeeCommand("잔액목록", "잔액 확인", true)]
        public async Task UserListAsync(CoffeeService coffee, User _, string text, SlackResponse response)
        {
            var users = await coffee.GetUsersAsync();

            var sb = new StringBuilder();
            foreach (var user in users.OrderBy(u => u.Deposit))
            {
                sb.AppendLine($"{SlackBot.Utils.UserIdToString(user.Id)} ({user.Name}) : \t{user.Deposit}");
            }

            response.Ephemeral(sb.ToString());
        }
        
        
    }
}
