using Microsoft.EntityFrameworkCore;
using OnlineBookStoreMVC.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineBookStoreMVC.Entities
{
   public class Order : BaseEntity
    {
        public string UserId { get; set; }
        public User User { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderItem> OrderItems { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }
        public Guid AddressId { get; set; }
        public Address Address { get; set; }
        public Guid? DeliveryId { get; set; }
        public Delivery Delivery { get; set; }
        [Precision(16, 2)]
        public decimal ShippingFee { get; set; }
        public string? PaymentDetails { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public OrderStatus OrderStatus { get; set; }
    } 
}
