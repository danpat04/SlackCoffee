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
    public static class StringBuilderExtensions
    {
        public static StringBuilder AppendOrders(this StringBuilder sb, List<Order> orders, bool withAt)
        {
            foreach (var order in orders)
            {
                if (withAt)
                    sb.AppendLine($"{SlackTools.UserIdToString(order.UserId)}: {order.MenuId} {order.Options} ({order.OrderedAt.ToString("h시 m분")}에 주문)");
                else
                    sb.AppendLine($"{SlackTools.UserIdToString(order.UserId)}: {order.MenuId} {order.Options}");
            }
            return sb;
        }
    }

    public partial class CoffeeCommandHandlers
    {
        [CoffeeCommand("주문", "[메뉴] [옵션]", false)]
        public async Task<SlackResponse> MakeOrder(CoffeeService coffee, User user, string text)
        {
            var order  = await coffee.MakeOrderAsync(user.Id, text, DateTime.Now);
            var deposit = await coffee.GetDepositAsync(user.Id);
            // TODO: 채널에도 예약했음을 알리기
            return Ok($"{order.MenuId}를 예약하였습니다.\n {order.Price}원 - 현재 잔액 {deposit}원");
        }

        [CoffeeCommand("주문취소", "", false)]
        public async Task<SlackResponse> CancelOrder(CoffeeService coffee, User user, string text)
        {
            var canceled = await coffee.CancelOrderAsync(user.Id);
            // TODO: 채널에도 예약 취소되었음을 알리기
            return Ok(canceled ? "취소하였습니다." : "예약이 없습니다.");
        }

        [CoffeeCommand("대기자", "", true)]
        public async Task<SlackResponse> GetOrders(CoffeeService coffee, User user, string text)
        {
            var at = DateTime.Now;
            var orders = await coffee.GetOrdersAsync(at);

            if (orders.Count > 0)
                return Ok("주문자가 없습니다.");

            var sb = new StringBuilder($"총 {orders.Count}명").AppendLine();
            sb.AppendOrders(orders, true);

            return Ok(sb.ToString());
        }

        [CoffeeCommand("추첨", "[인원수]", true)]
        public async Task<SlackResponse> PickOrders(CoffeeService coffee, User user, string text)
        {
            if (int.TryParse(text, out var count))
                throw new NotWellFormedException();

            var at = DateTime.Now;

            await coffee.PickOrderAsync(count, at);
            var orders = await coffee.GetOrdersAsync(at);

            var picked = orders.Where(o => o.PickedAt > DateTime.MinValue).ToList();
            var sb = new StringBuilder($"<당첨자 명단> {orders.Count}명 중에 {picked.Count}명").AppendLine();
            sb.AppendOrders(picked, false);

            // TODO: 왓카페 채널에 추첨 명단을 알리기
            return Ok(sb.ToString(), true);
        }

        [CoffeeCommand("추가추첨", "[인원수]", true)]
        public async Task<SlackResponse> PickMoreOrders(CoffeeService coffee, User user, string text)
        {
            if (int.TryParse(text, out var count))
                throw new NotWellFormedException();

            var at = DateTime.Now;

            var picked = await coffee.PickMoreOrderAsync(count, at);
            if (picked.Count <= 0)
                throw new NoOneToPickException();

            var orders = await coffee.GetOrdersAsync(at);
            var candidatesCount = picked.Count + orders.Count(o => o.PickedAt <= DateTime.MinValue);
            var sb = new StringBuilder($"<추가 당첨자 명단> {candidatesCount}명 중에 {picked.Count}명").AppendLine();
            sb.AppendOrders(picked, false);

            // TODO: 왓카페 채널에 추첨 명단을 알리기
            return Ok(sb.ToString(), true);
        }

        [CoffeeCommand("완성", "", true)]
        public async Task<SlackResponse> CompleteOrders(CoffeeService coffee, User user, string text)
        {
            var orders = await coffee.CompleteOrderAsync();
            var sb = new StringBuilder();
            foreach (var o in orders)
            {
                sb.Append(SlackTools.UserIdToString(o.UserId)).Append(' ');
            }
            sb.Append("님 커피 가져가세요~");
            return Ok(sb.ToString(), true);
        }

        [CoffeeCommand("리셋", "모든 주문을 리셋", true)]
        public async Task<SlackResponse> ResetOrders(CoffeeService coffee, User user, string text)
        {
            await coffee.ResetOrdersAsync();
            return Ok("모든 주문을 리셋하였습니다.", true);
        }
    }
}
