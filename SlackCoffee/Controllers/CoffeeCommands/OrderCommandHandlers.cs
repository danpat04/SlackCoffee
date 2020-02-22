using SlackCoffee.Models;
using SlackCoffee.Services;
using SlackCoffee.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackCoffee.Controllers.CoffeeCommands
{
    public static class Extensions
    {
        public static async Task AppendOrders(this StringBuilder sb, CoffeeService coffee, IEnumerable<Order> orders)
        {
            var users = await coffee.GetUsersAsync(orders.Select(o => o.UserId));
            foreach (var order in orders)
            {
                if (order.IsPicked)
                    sb.Append(":_v: ");
                 sb.AppendLine($"{users[order.UserId].Name}: {order.MenuId} {order.Options}");
            }
        }

        public static async Task AppendOrderInfo(this StringBuilder sb, CoffeeService coffee, List<Order> orders)
        {
            var pickedCount = orders.Count(o => o.IsPicked);
            if (pickedCount > 0)
            {
                sb.AppendLine($"총 {orders.Count}명 중 {pickedCount}명 당첨");
                var pickedOrders = orders
                    .Where(o => o.IsPicked)
                    .OrderBy(o => o.MenuId);
                var restOrders = orders
                    .Where(o => !o.IsPicked)
                    .OrderBy(o => o.OrderedAt);

                await sb.AppendOrders(coffee, pickedOrders);

                var menus = (await coffee.GetMenusAsync()).ToDictionary(m => m.Id);
                var menuCounts = pickedOrders.GroupBy(o => o.MenuId).Select(grouping => (grouping.Key, grouping.Count()));
                var steamCount = menuCounts.Select((info) => menus[info.Key].SteamMilkNeeded ? info.Item2 : 0).Sum();

                if (steamCount > 0)
                    sb.AppendLine($"*스팀밀크* : {steamCount} 잔");
            }
            else
            {
                sb.AppendLine($"총 {orders.Count}명");
                await sb.AppendOrders(coffee, orders.OrderBy(o => o.OrderedAt));
            }
        }

        public static DateTime Noon(this DateTime at)
        {
            var now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, 12, 0, 0); // same day 12:00 pm
        }
    }

    public partial class CoffeeCommandHandlers
    {
        [CoffeeCommand("주문", "커피를 주문합니다 (사용법: [메뉴] [옵션])", false)]
        public async Task MakeOrder(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            var canceled = await coffee.CancelOrderAsync(user.Id, DateTime.Now, DateTime.MinValue);
            var order = await coffee.MakeOrderAsync(user.Id, text, DateTime.Now);
            var deposit = await coffee.GetDepositAsync(user.Id);
            response
                .Ephemeral($"{order.Price}원, 현재 잔액 {deposit}원")
                .InChannel($"{user.Name} 님이 {order.MenuId}{(canceled ? "로 변경" : "를 주문")} 하였습니다.");
        }

        [CoffeeCommand("오후예약", "커피를 주문합니다 (사용법: [메뉴] [옵션])", false)]
        public async Task MakeReservationForAfternoon(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            var at = DateTime.Now.Noon();
            if (DateTime.Now > at)
                throw new BadRequestException("오전에만 사용 가능한 메뉴입니다.");
            var canceled = await coffee.CancelOrderAsync(user.Id, at, at);
            var order = await coffee.MakeOrderAsync(user.Id, text, at);
            var deposit = await coffee.GetDepositAsync(user.Id);
            response
                .Ephemeral($"{order.Price}원, 현재 잔액 {deposit}원")
                .InChannel($"{user.Name} 님이 오후 커피를 {order.MenuId}로 {(canceled ? "변경" : "예약")} 하였습니다.");
        }

        [CoffeeCommand("주문취소", "주문한 커피를 취소합니다", false)]
        public async Task CancelOrder(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            var canceled = await coffee.CancelOrderAsync(user.Id, DateTime.Now, DateTime.MinValue);
            response.Ephemeral(canceled ? "취소하였습니다." : "예약이 없습니다.");
            if (canceled)
                response.InChannel($"{user.Name} 님이 주문을 취소하였습니다.");
        }

        [CoffeeCommand("오후예약취소", "주문한 커피를 취소합니다", false)]
        public async Task CancelAfternoonReservationOrder(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            var at = DateTime.Now.Noon();
            if (DateTime.Now > at)
                throw new BadRequestException("오전에만 사용 가능한 메뉴입니다.");
            var canceled = await coffee.CancelOrderAsync(user.Id, at, at);
            response.Ephemeral(canceled ? "취소하였습니다." : "예약이 없습니다.");
            if (canceled)
                response.InChannel($"{user.Name} 님이 오후 커피를 취소하였습니다.");
        }


        [CoffeeCommand("명단", "현재 신청자 목록을 표시합니다", false)]
        public async Task GetOrders(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            var at = DateTime.Now;
            var orders = await coffee.GetOrdersAsync(at);

            if (orders.Count <= 0)
            {
                response.Ephemeral("주문자가 없습니다.");
                return;
            }

            var sb = new StringBuilder();
            await sb.AppendOrderInfo(coffee, orders);

            response.Ephemeral(sb.ToString());
        }

        [CoffeeCommand("오후명단", "오후 커피 신청자 목록을 표시합니다", false)]
        public async Task GetAfternoonOrders(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            var at = DateTime.Now.Noon();
            if (DateTime.Now >= at)
                throw new BadRequestException("오전에만 사용 가능한 메뉴입니다.");

            var orders = await coffee.GetReservedOrdersAsync(at);
            if (orders.Count <= 0)
            {
                response.Ephemeral("오후 커피 예약자가 없습니다.");
                return;
            }

            var sb = new StringBuilder();
            await sb.AppendOrderInfo(coffee, orders);

            response.Ephemeral(sb.ToString());
        }



        [CoffeeCommand("추첨", "인원수 만큼 랜덤하게 추첨합니다 (사용법: [인원수])", true)]
        public async Task PickOrders(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            if (!int.TryParse(text, out var count))
                throw new NotWellFormedException();
            if (count <= 0)
                throw new NotWellFormedException();

            var at = DateTime.Now;

            await coffee.PickOrderAsync(count, at);
            var orders = await coffee.GetOrdersAsync(at);

            var picked = orders.Where(o => o.PickedAt > DateTime.MinValue).ToList();
            var sb = new StringBuilder($"<당첨자 명단> {orders.Count}명 중에 {picked.Count}명").AppendLine();
            await sb.AppendOrders(coffee, picked);

            var responseText = sb.ToString();

            response
                .Ephemeral("추첨하였습니다")
                .InChannel(responseText, "user")
                .InChannel(responseText, "manager");
        }

        [CoffeeCommand("추가추첨", "인원수 만큼 선착순으로 추첨합니다 (사용법: [인원수])", true)]
        public async Task PickMoreOrders(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            if (!int.TryParse(text, out var count))
                throw new NotWellFormedException();
            if (count <= 0)
                throw new NotWellFormedException();

            var at = DateTime.Now;

            var picked = await coffee.PickMoreOrderAsync(count, at);
            if (picked.Count <= 0)
                throw new NoOneToPickException();

            var orders = await coffee.GetOrdersAsync(at);
            var candidatesCount = picked.Count + orders.Count(o => o.PickedAt <= DateTime.MinValue);
            var sb = new StringBuilder($"<추가 당첨자 명단> {candidatesCount}명 중에 {picked.Count}명").AppendLine();
            await sb.AppendOrders(coffee, picked);

            var responseText = sb.ToString();

            response
                .Ephemeral("추첨하였습니다")
                .InChannel(responseText, "manager")
                .InChannel(responseText, "user");
        }

        [CoffeeCommand("완성", "주문자들에게 완성을 알리고 요금을 계산합니다", true)]
        public async Task CompleteOrders(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            var orders = await coffee.CompleteOrderAsync();
            var sb = new StringBuilder();
            foreach (var o in orders)
            {
                sb.Append(SlackTools.UserIdToString(o.UserId)).Append(' ');
            }
            sb.Append("님 커피 가져가세요~");
            // TODO: 왓카페 채널에 완성 명단을 알리기
            response
                .Ephemeral("공지하였습니다.")
                .InChannel(sb.ToString(), "user");
        }

        [CoffeeCommand("리셋", "모든 주문을 리셋합니다", true)]
        public async Task ResetOrders(CoffeeService coffee, User user, string text, SlackResponse response)
        {
            await coffee.ResetOrdersAsync();
            response.InChannel($"{user.Name} 님이 모든 주문을 리셋하였습니다.");
        }
    }
}
