using SlackCoffee.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace SlackCoffee.Services
{
    public class CoffeeService: IDisposable, IAsyncDisposable
    {
        private static readonly TimeSpan OrderTolerance = new TimeSpan(6, 0, 0);

        private readonly CoffeeContext _context;

        private IDbContextTransaction _transaction;

        public CoffeeService(CoffeeContext context)
        {
            _context = context;
        }

        public void Dispose()
        {
            _transaction?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();
        }

        public async Task SaveAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _context.SaveChangesAsync();
                _transaction = null;
            }
        }

        private async Task BeginTransactionAsync()
        {
            if (_transaction == null)
                _transaction = await _context.Database.BeginTransactionAsync();
        }

        private async Task SetPriceAsync(Order order)
        {
            var menu = await _context.Menus.FindAsync(order.MenuId);
            var price = menu?.Price ?? 0;
            order.Price = price + (Math.Min(order.ShotCount - 1, 0) * 500);
        }

        public async Task<(Order Order, bool Additional)> MakeOrderAsync(string userId, string text, DateTime at, Order prevOrder = null)
        {
            await BeginTransactionAsync();

            var orders = await _context.Orders.ToArrayAsync();
            bool additionalOrder = _context.Orders.Count(o => o.PickedAt > DateTime.MinValue) > 0;

            text = text.Trim();

            Order order;
            if (string.IsNullOrEmpty(text))
            {
                var lastOrder = await _context.CompletedOrders
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderedAt)
                    .FirstOrDefaultAsync();

                if (lastOrder == null)
                    throw new BadRequestException("이전 주문이 없습니다.");

                var menu = await _context.Menus.FindAsync(lastOrder.MenuId);
                if (menu == null)
                    throw new MenuNotFoundException();
                if (!menu.Enabled)
                    throw new MenuDisabledException();

                order = Order.Reorder(lastOrder, at);
                await SetPriceAsync(order);
            }
            else
            {
                var index = text.IndexOf(' ');
                string menuName, options;
                if (index > 0)
                {
                    menuName = text.Substring(0, index);
                    options = text.Substring(index + 1);
                }
                else
                {
                    menuName = text;
                    options = "";
                }

                var menu = await _context.Menus.FindAsync(menuName);
                if (menu == null)
                    throw new MenuNotFoundException();
                if (!menu.Enabled)
                    throw new MenuDisabledException();

                order = new Order(userId, menu.Id, options, at);
                await SetPriceAsync(order);
            }

            if (prevOrder != null)
                order.PickedAt = prevOrder.PickedAt;

            _context.Orders.Add(order);
            return (order, additionalOrder);
        }

        public async Task<Order> CancelOrderAsync(string userId, DateTime at, DateTime after)
        {
            await BeginTransactionAsync();

            var prevOrders = await _context.Orders
                .Where(o => o.UserId == userId && o.OrderedAt <= at && o.OrderedAt >= after)
                .ToArrayAsync();

            if (prevOrders.Length > 0)
            {
                _context.Orders.RemoveRange(prevOrders);
                return prevOrders.FirstOrDefault(o => o.OrderedAt > (at - OrderTolerance));
            }

            return null;
        }
        public async Task<List<Order>> GetReservedOrdersAsync(DateTime at)
        {
            return await _context.Orders.Where(o => o.OrderedAt >= at).ToListAsync();
        }

        public async Task<List<Order>> GetOrdersAsync(DateTime at)
        {
            return await _context.Orders.Where(o => o.OrderedAt > (at - OrderTolerance) && o.OrderedAt < at).ToListAsync();
        }

        public async Task<List<Order>> PickMoreOrderAsync(int count, DateTime at)
        {
            await BeginTransactionAsync();

            var candidates = await _context.Orders
                .Where(o => o.OrderedAt > (at - OrderTolerance) && o.OrderedAt < at)
                .Where(o => o.PickedAt <= DateTime.MinValue)
                .ToListAsync();

            if (count < candidates.Count)
            {
                candidates.Shuffle();
                candidates = candidates.GetRange(0, count);
            }

            if (candidates.Count > 0)
            {
                candidates.ForEach(o => o.PickedAt = at);
                _context.Orders.UpdateRange(candidates);
            }

            return candidates;
        }

        public async Task<List<Order>> PickOrderAsync(int count, DateTime at)
        {
            await BeginTransactionAsync();

            var orders = await _context.Orders.ToArrayAsync();

            var pickedCount = _context.Orders.Count(o => o.PickedAt > DateTime.MinValue);
            if (pickedCount > 0)
                throw new BadRequestException("이미 추첨한 상태입니다.");

            var managerIds = await _context.Users
                .Where(u => u.IsManager)
                .Select(u => u.Id)
                .ToListAsync();

            var managers = await _context.Orders
                .Where(o => o.OrderedAt > at - OrderTolerance && o.OrderedAt < at)
                .Where(o => managerIds.Contains(o.UserId))
                .ToListAsync();

            var candidates = await _context.Orders
                .Where(o => o.OrderedAt > at - OrderTolerance && o.OrderedAt < at)
                .Where(o => !managerIds.Contains(o.UserId))
                .ToListAsync();

            var neededCount = Math.Max(count - managers.Count, 0);
            if (neededCount < candidates.Count)
            {
                candidates.Shuffle();
                candidates = candidates.GetRange(0, neededCount);
            }

            if (managers.Count > 0)
            {
                managers.ForEach(o => o.PickedAt = at);
                _context.UpdateRange(managers);
            }

            if (candidates.Count > 0)
            {
                candidates.ForEach(o => o.PickedAt = at);
                _context.UpdateRange(candidates);
            }

            managers.AddRange(candidates);
            return managers;
        }

        public async Task<List<Order>> CompleteOrderAsync(DateTime at)
        {
            await BeginTransactionAsync();

            var lastPickedAt = await _context.Orders
                .OrderByDescending(o => o.PickedAt)
                .Select(o => o.PickedAt)
                .FirstOrDefaultAsync();


            // 그럴리 없지만 lastPickedAt이 at 보다 미래라면 at을 대체한다  
            at = lastPickedAt > at ? lastPickedAt : at;

            var orders = await _context.Orders.Where(o => o.OrderedAt <= lastPickedAt).ToListAsync();
            var futureOrders = await _context.Orders.Where(o => o.OrderedAt > at).ToListAsync();
            var pickedOrders = orders.Where(o => o.PickedAt > DateTime.MinValue).ToList();

            if (pickedOrders.Count <= 0)
                throw new BadRequestException("추첨 된 주문이 없습니다.");

            var pickedUserIds = pickedOrders.Select(o => o.UserId).ToHashSet();

            var pickedUsers = await _context.Users
                .Where(u => pickedUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            foreach (var o in pickedOrders)
            {
                pickedUsers[o.UserId].Deposit -= o.Price;
            }

            foreach (var o in futureOrders)
            {
                o.OrderedAt = at;
            }

            _context.Orders.RemoveRange(orders);
            _context.CompletedOrders.AddRange(orders.Select(o => new CompletedOrder(o)));
            _context.Users.UpdateRange(pickedUsers.Values);

            if (futureOrders.Count > 0)
            {
                _context.Orders.UpdateRange(futureOrders);
            }

            return pickedOrders;
        }

        public async Task ResetOrdersAsync()
        {
            await BeginTransactionAsync();

            var orders = await _context.Orders.ToListAsync();
            _context.Orders.RemoveRange(orders);
        }

        public async Task<User> FillWalletAsync(string userId, int amount, DateTime at)
        {
            await BeginTransactionAsync();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new UserNotFoundException();

            user.Deposit += amount;
            _context.Users.Update(user);

            var history = new WalletHistory { Id = Guid.NewGuid().ToString(), UserId = userId, Amount = amount, At = at };
            _context.WalletHistory.Add(history);

            return user;
        }

        public async Task<int> GetDepositAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new UserNotFoundException();

            return user.Deposit;
        }

        public async Task<User> FindUserAsync(string userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User> CreateUserAsync(string userId, string userName, bool isManager)
        {
            await BeginTransactionAsync();

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
                throw new BadRequestException("이미 등록된 사용자입니다.");

            user = new User { Id = userId, Name = userName, IsManager = isManager, Deposit = 0 };
            await _context.Users.AddAsync(user);

            return user;
        }

        public async Task<List<User>> GetUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<Dictionary<string, User>> GetUsersAsync(IEnumerable<string> userIds)
        {
            var ids = userIds.ToHashSet();
            return await _context.Users.Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id);
        }

        public async Task<List<User>> GetUsersAsync(string name)
        {
            return await _context.Users.Where(u => u.Name == name).ToListAsync();
        }

        public async Task<User> UpdateUserAsync(string userId, bool isManager)
        {
            await BeginTransactionAsync();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new UserNotFoundException();

            user.IsManager = isManager;
            _context.Users.Update(user);

            return user;
        }
        
        public async Task<User> UpdateUserNameAsync(string userId, string name)
        {
            await BeginTransactionAsync();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new UserNotFoundException();

            user.Name = name;
            _context.Users.Update(user);

            return user;
        }

        public async Task MergeUserAsync(User user, params User[] targetUsers)
        {
            await BeginTransactionAsync();

            foreach (var u in targetUsers)
            {
                user.Merge(u);
                _context.Users.Remove(u);
            }
        }

        public async Task<(int, List<User>)> MergeUsersAsync(User user, bool dryRun)
        {
            await BeginTransactionAsync();

            int deposit = user.Deposit;
            List<User> mergedUsers = await _context.Users
                .Where(u => u.Name == user.Name && u.Id != user.Id)
                .ToListAsync();
            
            foreach (var u in mergedUsers)
            {
                deposit += u.Deposit;
                if (!dryRun)
                {
                    user.Merge(u);
                }
            }

            if (!dryRun)
            {
                _context.Users.RemoveRange(mergedUsers);
            }

            return (deposit, mergedUsers);
        }

        public async Task DeleteUserAsync(User user)
        {
            await BeginTransactionAsync();

            _context.Users.Remove(user);
        }

        public async Task AddMenuAsync(Menu menu)
        {
            await BeginTransactionAsync();

            var prevMenu = await _context.Menus.FindAsync(menu.Id);
            if (prevMenu != null)
                throw new BadRequestException("이미 존재하는 메뉴입니다.");

            _context.Menus.Add(menu);
        }

        public async Task ChangeMenuAsync(Menu menu)
        {
            await BeginTransactionAsync();

            var prevMenu = await _context.Menus.FindAsync(menu.Id);
            if (prevMenu == null)
                throw new MenuNotFoundException();

            prevMenu.Update(menu);
            _context.Menus.Update(prevMenu);
        }

        public async Task EnableMenuAsync(string menuId, bool enabled)
        {
            await BeginTransactionAsync();

            var menu = await _context.Menus.FindAsync(menuId);
            if (menu == null)
                throw new MenuNotFoundException();

            menu.Enabled = enabled;
            _context.Menus.Update(menu);
        }

        public async Task<Menu[]> GetMenusAsync()
        {
            return await _context.Menus.OrderBy(m => m.Order).ToArrayAsync();
        }

        public async Task<CompletedOrder[]> GetCompletedOrders(string userId, DateTime at, int days)
        {
            // days 전 아침 9시 기준
            DateTime since = at - new TimeSpan(days, 0, 0, 0);
            since = new DateTime(since.Year, since.Month, since.Day, 9, 0, 0);

            return await _context.CompletedOrders
                .Where(o => o.UserId == userId && o.OrderedAt > since)
                .ToArrayAsync();
        }

        public async Task<WalletHistory[]> GetFillHistories(string userId, DateTime at, int days)
        {
            // days 전 아침 9시 기준
            DateTime since = at - new TimeSpan(days, 0, 0, 0);
            since = new DateTime(since.Year, since.Month, since.Day, 9, 0, 0);

            return await _context.WalletHistory
                .Where(h => h.UserId == userId && h.At > since)
                .OrderBy(o => o.At)
                .ToArrayAsync();
        }
    }
}
