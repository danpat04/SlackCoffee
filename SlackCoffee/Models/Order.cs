﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SlackCoffee.Models
{
    public abstract class OrderBase
    {
        [Key]
        public string Id { get; set; }

        [ForeignKey("users")]
        [MaxLength(15)]
        public string UserId { get; set; }

        [ForeignKey("menus")]
        [MaxLength(40)]
        public string MenuId { get; set; }

        public string Options { get; set; }
        public DateTime OrderedAt { get; set; }
        public int ShotCount { get; set; }
        public int Price { get; set; }
        public DateTime PickedAt { get; set; }

        public OrderBase()
        {
            Id = Guid.NewGuid().ToString();
            UserId = "";
            MenuId = "";
            Options = "";
            OrderedAt = DateTime.MinValue;
            Price = 0;
            ShotCount = 1;
            PickedAt = DateTime.MinValue;
        }

        public OrderBase(string userId, string menuId, string options, DateTime at)
        {
            Id = Guid.NewGuid().ToString();
            UserId = userId;
            MenuId = menuId;
            Options = options;
            OrderedAt = at;
            Price = 0;

            // ShotCount = 1 + (uint)options.Split('+').Select(s => s.Contains("샷") ? 1 : 0).Sum();
            ShotCount = 1;  // 일단 더블샷을 허용하지 않는다

            PickedAt = DateTime.MinValue;
        }

        public bool IsPicked => PickedAt > DateTime.MinValue;
    }

    public class Order : OrderBase
    {
        public Order() : base() { }

        public Order(string userId, string menuId, string options, DateTime at) : base(userId, menuId, options, at) { }

        public static Order Reorder(CompletedOrder lastOrder, DateTime at)
        {
            return new Order(lastOrder.UserId, lastOrder.MenuId, lastOrder.Options, at);
        }
    }

    public class CompletedOrder : OrderBase
    {
        public CompletedOrder() : base() { }

        public CompletedOrder(Order order)
        {
            Id = order.Id;
            UserId = order.UserId;
            MenuId = order.MenuId;
            Options = order.Options;
            OrderedAt = order.OrderedAt;
            Price = order.Price;
            ShotCount = order.ShotCount;
            PickedAt = order.PickedAt;
        }
    }
}
