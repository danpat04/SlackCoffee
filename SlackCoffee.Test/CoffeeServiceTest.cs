using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SlackCoffee.Models;
using SlackCoffee.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
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
                new Menu {Id = "�޴�1", Description = "", Enabled = true, Order = 0, Price = 1000},
                new Menu {Id = "�޴�2", Description = "", Enabled = true, Order = 1, Price = 1500},
                new Menu {Id = "�޴�3", Description = "", Enabled = true, Order = 2, Price = 2000},
                new Menu {Id = "�޴�4", Description = "", Enabled = true, Order = 3, Price = 3000},
                new Menu {Id = "�޴�5", Description = "", Enabled = false, Order = 4, Price = 1000},
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

            // �������� ����
            var at = new DateTime(2023, 3, 17, 9, 0, 0);
            var noon = new DateTime(2023, 3, 17, 12, 0, 0);
            var service = new CoffeeService(context, NullLogger.Instance);

            // ���� �ֹ��� ��� ����
            await Assert.ThrowsAsync<BadRequestException>(() => service.MakeOrderAsync("id1", "", at));
            // ���� �޴��̱� ������ ����
            await Assert.ThrowsAsync<MenuNotFoundException>(() => service.MakeOrderAsync("id1", "���¸޴�", at));

            var order = (await service.MakeOrderAsync("id1", "�޴�1", at)).Order;
            var afternoonOrder = (await service.MakeOrderAsync("id1", "�޴�1", noon)).Order;
            await context.SaveChangesAsync();

            at = at.AddSeconds(10);

            // ���� ��÷�� ���߱� ������ �ϼ� ����
            await Assert.ThrowsAsync<BadRequestException>(() => service.CompleteOrderAsync(at, false));

            var pickedOrders = await service.PickOrderAsync(10, at.AddSeconds(10));
            await context.SaveChangesAsync();

            at = at.AddSeconds(10);

            // �̹� ��÷�� �߱� ������ ��÷ �Ұ�
            await Assert.ThrowsAsync<BadRequestException>(() => service.PickOrderAsync(10, at));

            Assert.Single(pickedOrders);
            Assert.Equal("id1", pickedOrders[0].UserId);
            Assert.Equal("�޴�1", pickedOrders[0].MenuId);

            // �߰� ��û
            var info = await service.MakeOrderAsync("id2", "�޴�2", at);
            Assert.True(info.Additional);
            await context.SaveChangesAsync();

            at = at.AddSeconds(10);

            // �߰� ��÷
            var pickedMore = await service.PickMoreOrderAsync(2, at);
            await context.SaveChangesAsync();

            Assert.Single(pickedMore);
            Assert.Equal("id2", pickedMore[0].UserId);

            at = at.AddSeconds(10);
            var completedOrders = await service.CompleteOrderAsync(at, false);
            await context.SaveChangesAsync();

            Assert.Equal(2, completedOrders.Count);
            Assert.Equal("id1", completedOrders[0].UserId);
            Assert.Equal("�޴�1", completedOrders[0].MenuId);

            var orders = await context.Orders.ToListAsync();
            var leftOrder = orders.FirstOrDefault();
            Assert.NotNull(leftOrder);
            // ���� ������ ���� �������� �ֹ� �ð��� ���� �ϼ� �ð��� ����
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
