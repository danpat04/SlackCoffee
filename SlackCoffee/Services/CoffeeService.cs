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

        private IDbContextTransaction __transaction = null;
        private IDbContextTransaction _transaction
        {
            get { return __transaction; }
            set
            {
                if (value == null)
                {
                    value = null;
                }
                __transaction = value;
            }
        }

        public CoffeeService(CoffeeContext context)
        {
            _context = context;
        }

        public void Dispose()
        {
            if (_transaction != null)
                _transaction.Dispose();
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
            var price = menu == null ? 0 : menu.Price;
            order.Price = price + (Math.Min(order.ShotCount - 1, 0) * 500);
        }

        public async Task<Order> MakeOrderAsync(string userId, string text, DateTime at)
        {
            await BeginTransactionAsync();

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

                order = Order.Reorder(lastOrder, at);
                await SetPriceAsync(order);
            }
            else
            {
                var index = text.IndexOf(' ');
                string menuName, options;
                if (index > 0)
                {
                    menuName = text.Substring(0, index + 1);
                    options = text.Substring(index + 1);
                }
                else
                {
                    menuName = text;
                    options = "";
                }

                var menu = await _context.Menus
                    .Where(m => m.Id == menuName)
                    .FirstOrDefaultAsync();
                if (menu == null)
                    throw new BadRequestException("없는 메뉴 입니다.");

                order = new Order(userId, menu.Id, options, at);
                await SetPriceAsync(order);
            }

            await CancelOrderAsync(userId);

            _context.Orders.Add(order);
            return order;
        }

        public async Task<bool> CancelOrderAsync(string id)
        {
            await BeginTransactionAsync();

            var prevOrders = await _context.Orders
                .Where(o => o.UserId == id).ToArrayAsync();

            if (prevOrders.Length > 0)
            {
                _context.Orders.RemoveRange(prevOrders);
                return true;
            }

            return false;
        }

        public async Task<List<Order>> PickMoreOrderAsync(uint count, DateTime at)
        {
            await BeginTransactionAsync();

            var candidates = await _context.Orders
                .Where(o => o.OrderedAt > (at - OrderTolerance))
                .Where(o => o.PickedAt <= DateTime.MinValue)
                .ToListAsync();

            if (count < candidates.Count)
            {
                candidates.Shuffle();
                candidates = candidates.GetRange(0, (int)count);
            }

            if (candidates.Count > 0)
            {
                candidates.ForEach(o => o.PickedAt = at);
                _context.Orders.UpdateRange(candidates);
            }

            return candidates;
        }

        public async Task<List<Order>> PickOrderAsync(uint count, DateTime at)
        {
            await BeginTransactionAsync();

            var pickedCount = _context.Orders.Where(o => o.PickedAt > DateTime.MinValue).Count();
            if (pickedCount > 0)
                throw new BadRequestException("이미 추첨한 상태입니다.");

            var managerIds = await _context.Users
                .Where(u => u.IsManager)
                .Select(u => u.Id)
                .ToListAsync();

            var managers = await _context.Orders
                .Where(o => o.OrderedAt > at - OrderTolerance)
                .Where(o => managerIds.Contains(o.UserId))
                .ToListAsync();

            var candidates = await _context.Orders
                .Where(o => o.OrderedAt > at - OrderTolerance)
                .Where(o => !managerIds.Contains(o.UserId))
                .ToListAsync();

            var neededCount = Math.Max((int)count - managers.Count, 0);
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

        public async Task<List<Order>> CompleteOrderAsync()
        {
            await BeginTransactionAsync();

            var orders = await _context.Orders.ToListAsync();
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

            _context.Orders.RemoveRange(orders);
            _context.CompletedOrders.AddRange(orders.Select(o => new CompletedOrder(o)));
            _context.Users.UpdateRange(pickedUsers.Values);

            return pickedOrders;
        }

        public async Task<User> FillWalletAsync(string userId, int amount, DateTime at)
        {
            await BeginTransactionAsync();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new BadRequestException("존재하지 않는 사용자입니다.");

            user.Deposit += amount;
            _context.Users.Update(user);

            var history = new WalletHistory { UserId = userId, Amount = amount, At = at };
            await _context.WalletHistory.AddAsync(history);

            return user;
        }

        public async Task<User> FindUserAsync(string userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User> CreateUserAsync(string userId, bool isManager)
        {
            await BeginTransactionAsync();

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
                throw new BadRequestException("이미 등록된 사용자입니다.");

            user = new User { Id = userId, IsManager = isManager, Deposit = 0 };
            await _context.Users.AddAsync(user);

            return user;
        }

        public async Task<User> UpdateUserAsync(string userId, bool isManager)
        {
            await BeginTransactionAsync();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new BadRequestException("존재하지 않는 사용자입니다.");

            user.IsManager = isManager;
            _context.Users.Update(user);

            return user;
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
                throw new BadRequestException("없는 메뉴입니다.");

            prevMenu.Update(menu);
            _context.Menus.Update(prevMenu);
        }

        public async Task EnableMenuAsync(string menuId, bool enabled)
        {
            await BeginTransactionAsync();

            var menu = await _context.Menus.FindAsync(menuId);
            if (menu == null)
                throw new BadRequestException("없는 메뉴입니다.");

            menu.Enabled = enabled;
            _context.Menus.Update(menu);
        }

        public async Task<Menu[]> GetMenusAsync()
        {
            return await _context.Menus.OrderBy(m => m.Order).ToArrayAsync();
        }
    }
}
