using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SlackCoffee.Models;
using SlackCoffee.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SlackCoffee.Test
{
    public class TestCoffeeContext: CoffeeContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase("TestCoffeeDatabase");
                optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            }
        }

        public static TestCoffeeContext CreateContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<TestCoffeeContext>();
            optionsBuilder.UseInMemoryDatabase("TestCoffeeDatabase");
            optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));

            return new TestCoffeeContext(optionsBuilder.Options);
        }

        public TestCoffeeContext(DbContextOptions options) : base(options)
        {
        }

        public void SetUsers()
        {
            var users = new List<User>
            {
                new User{Id = "id1", Deposit = 1500, Name = "", IsManager = true},
                new User{Id = "id2", Deposit = 1000, Name = "", IsManager = true},
                new User{Id = "id3", Deposit = 1000, Name = "", IsManager = false},
                new User{Id = "id4", Deposit = 2000, Name = "", IsManager = false},
                new User{Id = "id5", Deposit = 2000, Name = "", IsManager = false},
                new User{Id = "id6", Deposit = 2000, Name = "", IsManager = false},
                new User{Id = "id7", Deposit = 2000, Name = "", IsManager = false},
                new User{Id = "id8", Deposit = 0, Name = "", IsManager = false},
                new User{Id = "id9", Deposit = 0, Name = "", IsManager = false},
            };
            Users.AddRange(users);
        }

        public void SetMenus()
        {
            var menus = new List<Menu>
            {
                new Menu {Id = "메뉴1", Description = "", Enabled = true, Order = 0, Price = 1000},
                new Menu {Id = "메뉴2", Description = "", Enabled = true, Order = 1, Price = 1500},
                new Menu {Id = "메뉴3", Description = "", Enabled = true, Order = 2, Price = 2000},
                new Menu {Id = "메뉴4", Description = "", Enabled = true, Order = 3, Price = 3000},
                new Menu {Id = "메뉴5", Description = "", Enabled = false, Order = 4, Price = 1000},
            };
            Menus.AddRange(menus);
        }
    }

    public class CoffeeServiceTest
    {
        [Fact]
        public async void CoffeeService()
        {
            using var context = TestCoffeeContext.CreateContext();
            context.SetUsers();
            context.SetMenus();
            await context.SaveChangesAsync();

            // 오전으로 고정
            var at = new DateTime(2023, 3, 17, 9, 0, 0);
            var noon = new DateTime(2023, 3, 17, 12, 0, 0);
            var service = new CoffeeService(context);

            // 이전 주문이 없어서 실패
            await Assert.ThrowsAsync<BadRequestException>(() => service.MakeOrderAsync("id1", "", at));
            // 없는 메뉴이기 때문에 실패
            await Assert.ThrowsAsync<MenuNotFoundException>(() => service.MakeOrderAsync("id1", "없는메뉴", at));

            var order = (await service.MakeOrderAsync("id1", "메뉴1", at)).Order;
            var afternoonOrder = (await service.MakeOrderAsync("id1", "메뉴1", noon)).Order;
            await context.SaveChangesAsync();

            at = at.AddSeconds(10);

            // 아직 추첨을 안했기 때문에 완성 실패
            await Assert.ThrowsAsync<BadRequestException>(() => service.CompleteOrderAsync(at));

            var pickedOrders = await service.PickOrderAsync(10, at.AddSeconds(10));
            await context.SaveChangesAsync();

            at = at.AddSeconds(10);

            // 이미 추첨을 했기 때문에 추첨 불가
            await Assert.ThrowsAsync<BadRequestException>(() => service.PickOrderAsync(10, at));

            Assert.Single(pickedOrders);
            Assert.Equal("id1", pickedOrders[0].UserId);
            Assert.Equal("메뉴1", pickedOrders[0].MenuId);

            // 추가 신청
            var info = await service.MakeOrderAsync("id2", "메뉴2", at);
            Assert.True(info.Additional);
            await context.SaveChangesAsync();

            at = at.AddSeconds(10);

            // 추가 추첨
            var pickedMore = await service.PickMoreOrderAsync(2, at);
            await context.SaveChangesAsync();

            Assert.Single(pickedMore);
            Assert.Equal("id2", pickedMore[0].UserId);

            at = at.AddSeconds(10);
            var completedOrders = await service.CompleteOrderAsync(at);
            await context.SaveChangesAsync();

            Assert.Equal(2, completedOrders.Count);
            Assert.Equal("id1", completedOrders[0].UserId);
            Assert.Equal("메뉴1", completedOrders[0].MenuId);

            var orders = await context.Orders.ToListAsync();
            var leftOrder = orders.FirstOrDefault();
            Assert.NotNull(leftOrder);
            // 남은 예약은 오후 예약으로 주문 시각이 이전 완성 시각과 같다
            Assert.Equal(at, leftOrder.OrderedAt);
            Assert.Equal(afternoonOrder.Id, leftOrder.Id);

            var completeds = await context.CompletedOrders.ToListAsync();
            Assert.Equal(2, completeds.Count);

            var user = await context.Users.FindAsync("id1");
            Assert.Equal(500, (int)user.Deposit);

            user = await context.Users.FindAsync("id2");
            Assert.Equal(-500, (int)user.Deposit);
        }
    }
}
