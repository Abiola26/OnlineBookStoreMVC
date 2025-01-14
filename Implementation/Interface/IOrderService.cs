using OnlineBookStoreMVC.DTOs;
using OnlineBookStoreMVC.Entities;
using OnlineBookStoreMVC.Enums;

namespace OnlineBookStoreMVC.Implementation.Interface
{
    public interface IOrderService
    {
       // Task<List<OrderSummaryDto>> GetAllOrderSummariesAsync(string userId);
        Task<OrderDto> CheckoutCompleteAsync(string userId);
        Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
        Task<OrderDto> GetOrderDetailsAsync(Guid id);
        Task<OrderDto> PlaceOrderAsync(OrderSummaryDto orderSummary);
        //Task<IEnumerable<OrderDto>> GetAllPendingOrdersAsync(string userId);
        List<Order> GetAllOrders();
        //Task<OrderDto> AssignDeliveryToOrderAsync(Guid orderId, Guid deliveryId);
        Task<OrderSummaryDto> GetOrderSummaryAsync(string userId, Guid? selectedAddressId);
        Task<IEnumerable<OrderDto>> GetOrdersAsync(string? userId = null);
        Task<OrderDto> GetOrderByIdAsync(Guid orderId, string? userId = null);
        Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus? orderStatus, PaymentStatus? paymentStatus);
        Task<IEnumerable<OrderDto>> GetOrdersByUserIdAsync(string userId);
    }
}
