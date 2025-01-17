﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineBookStoreMVC.DTOs;
using OnlineBookStoreMVC.Entities;
using OnlineBookStoreMVC.Enums;
using OnlineBookStoreMVC.Helper;
using OnlineBookStoreMVC.Implementation.Interface;

namespace OnlineBookStoreMVC.Implementation.Services
{
    [Authorize]
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<OrderService> _logger;

        public OrderService(ApplicationDbContext context, IShoppingCartService shoppingCartService,
                            IEmailService emailService, IHttpContextAccessor httpContextAccessor,
                            UserManager<User> userManager, ILogger<OrderService> logger)
        {
            _context = context;
            _shoppingCartService = shoppingCartService;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _logger = logger;
        }


        public async Task<OrderDto> CheckoutCompleteAsync(string userId)
        {
            var order = await _context.Orders.Include(o => o.OrderItems)
                                              .ThenInclude(oi => oi.Book)
                                              .Where(o => o.UserId == userId)
                                              .OrderByDescending(o => o.OrderDate)
                                              .FirstOrDefaultAsync();

            if (order == null) return null;

            return new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    BookId = oi.BookId,
                    BookTitle = oi.Book.Title,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList(),
                TotalAmount = order.TotalAmount
            };
        }
        public async Task<OrderSummaryDto> GetOrderSummaryAsync(string userId, Guid? selectedAddressId)
        {
            var cart = await _shoppingCartService.GetCartAsync(userId);

            // Check if the cart is null or empty
            if (cart == null || cart.ShoppingCartItems == null || !cart.ShoppingCartItems.Any())
            {
                throw new Exception("Shopping cart is empty.");
            }

            var address = selectedAddressId.HasValue
                ? await _context.Addresses.FirstOrDefaultAsync(a => a.Id == selectedAddressId.Value && a.UserId == userId)
                : await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

            if (address == null)
            {
                throw new Exception("No valid address found for the user.");
            }

            return new OrderSummaryDto
            {
                ShoppingCart = cart,  // This should have the cart details with items
                UserId = userId,
                Address = new AddressDto
                {
                    FullName = address.FullName,
                    PhoneNumber = address.PhoneNumber,
                    AddittionalPhoneNumber = address.AddittionalPhoneNumber,
                    City = address.City,
                    Region = address.Region,
                    DeliveryAddress = address.DeliveryAddress
                }
            };
        }
        public async Task<OrderDto> PlaceOrderAsync(OrderSummaryDto orderSummary)
        {
            if (orderSummary == null) throw new ArgumentNullException(nameof(orderSummary), "Order summary cannot be null.");
            if (orderSummary.ShoppingCart == null) throw new ArgumentNullException(nameof(orderSummary.ShoppingCart), "Shopping cart cannot be null.");
            if (orderSummary.Address == null) throw new ArgumentNullException(nameof(orderSummary.Address), "Address cannot be null.");

            try
            {
                // Validate stock availability
                foreach (var item in orderSummary.ShoppingCart.ShoppingCartItems)
                {
                    var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == item.BookId);
                    if (book == null) throw new InvalidOperationException($"The book '{item.BookTitle}' does not exist.");
                    if (item.Quantity > book.TotalQuantity) throw new InvalidOperationException($"Not enough stock for '{item.BookTitle}'. Only {book.TotalQuantity} available.");
                }

                // Fetch or validate existing address
                var existingAddress = await _context.Addresses.FirstOrDefaultAsync(a =>
                    a.UserId == orderSummary.UserId &&
                    a.FullName == orderSummary.Address.FullName &&
                    a.DeliveryAddress == orderSummary.Address.DeliveryAddress &&
                    a.City == orderSummary.Address.City &&
                    a.Region == orderSummary.Address.Region &&
                    a.PhoneNumber == orderSummary.Address.PhoneNumber);

                if (existingAddress == null) throw new InvalidOperationException("Selected address does not exist.");

                // Create new order
                var order = new Order
                {
                    UserId = orderSummary.UserId,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = orderSummary.OrderTotal,
                    OrderStatus = OrderStatus.Created,
                    PaymentStatus = PaymentStatus.Pending, // Default Payment Status
                    PaymentDetails = "Pending", // Default Payment Details
                    AddressId = existingAddress.Id,
                    OrderItems = orderSummary.ShoppingCart.ShoppingCartItems.Select(item => new OrderItem
                    {
                        BookId = item.BookId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price
                    }).ToList()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Update OrderItems with generated OrderId
                foreach (var orderItem in order.OrderItems)
                {
                    orderItem.OrderId = order.Id;
                }

                await _context.SaveChangesAsync();

                // Reduce stock
                foreach (var orderItem in order.OrderItems)
                {
                    var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == orderItem.BookId);
                    if (book != null)
                    {
                        book.TotalQuantity -= orderItem.Quantity;
                        _context.Books.Update(book);
                    }
                }

                await _context.SaveChangesAsync();

                // Send confirmation email
                var code = CodeGenerator.GenerateRandomCode(6);
                var deliveryDate = DateTime.UtcNow.AddDays(3);
                var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
                await _emailService.SendOrderConfirmationEmailAsync(currentUser.Email, orderSummary.Address.FullName, code, deliveryDate);

                // Return order details
                return new OrderDto
                {
                    Id = order.Id,
                    UserId = order.UserId,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    OrderStatus = order.OrderStatus,
                    PaymentStatus = order.PaymentStatus,
                    OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                    {
                        Id = oi.Id,
                        BookId = oi.BookId,
                        BookTitle = oi.Book?.Title,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                // Log and rethrow exception
                throw;
            }
        }
        public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(order => new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserName = order.User?.UserName ?? "Unknown User",
                OrderDate = order.OrderDate,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.OrderStatus,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    BookId = oi.BookId,
                    BookTitle = oi.Book.Title,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList(),
                TotalAmount = order.TotalAmount
            });
        }
        public async Task<OrderDto> GetOrderDetailsAsync(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book)
                .Include(o => o.User)
                .Include(i => i.Address)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return null;

            return new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserName = order.User.UserName ?? "Unknown User",
                OrderDate = order.OrderDate,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.OrderStatus,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    BookId = oi.BookId,
                    BookTitle = oi.Book?.Title ?? "Unknown Book",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList(),
                TotalAmount = order.TotalAmount,
                DeliveryAddress = order.Address.DeliveryAddress,
                DeliveryPhoneNumber = order.User.PhoneNumber,
                DeliveryEmail = order.User.Email
            };
        }
        public async Task<IEnumerable<OrderDto>> GetOrdersAsync(string? userId = null)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book)
                .Include(o => o.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(o => o.UserId == userId); // Filter by user if userId is provided
            }

            var orders = await query.ToListAsync();

            return orders.Select(order => new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserName = order.User?.UserName ?? "Unknown User",
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.OrderStatus,
                OrderDate = order.OrderDate,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    BookId = oi.BookId,
                    BookTitle = oi.Book.Title,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            });
        }
        public async Task<OrderDto> GetOrderByIdAsync(Guid orderId, string? userId = null)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                Console.WriteLine($"Order with ID {orderId} not found.");
                return null;
            }

            if (!string.IsNullOrEmpty(userId) && order.UserId != userId)
            {
                Console.WriteLine($"Unauthorized access to order {orderId} by user {userId}.");
                return null;
            }

            return new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserName = order.User?.UserName ?? "Unknown User",
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.OrderStatus,
                OrderDate = order.OrderDate,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    BookId = oi.BookId,
                    BookTitle = oi.Book.Title,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };
        }
        public async Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus? orderStatus, PaymentStatus? paymentStatus)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new KeyNotFoundException("Order not found.");

            if (orderStatus.HasValue)
                order.OrderStatus = orderStatus.Value;

            if (paymentStatus.HasValue)
                order.PaymentStatus = paymentStatus.Value;

            _context.Orders.Update(order);

            return await _context.SaveChangesAsync() > 0;
        }
        public List<Order> GetAllOrders()
        {
            return _context.Orders.ToList();
        }
        public async Task<IEnumerable<OrderDto>> GetOrdersByUserIdAsync(string userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(order => new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserName = order.User?.UserName ?? "Unknown User",
                OrderDate = order.OrderDate,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.OrderStatus,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    BookId = oi.BookId,
                    BookTitle = oi.Book?.Title ?? "Unknown Book",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList(),
                TotalAmount = order.TotalAmount
            });
        }
    }
   
       


        //public async Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(OrderStatus status, string userId = null)
        //{
        //    var query = _context.Orders
        //        .Include(o => o.OrderItems)
        //            .ThenInclude(oi => oi.Book)
        //        .Include(o => o.User)
        //        .AsQueryable();

        //    if (status != OrderStatus.Pending) // For 'All', return all orders
        //    {
        //        query = query.Where(o => o.OrderStatus == status);
        //    }

        //    if (!string.IsNullOrEmpty(userId))
        //    {
        //        query = query.Where(o => o.UserId == userId);
        //    }

        //    var orders = await query.ToListAsync();

        //    return orders.Select(order => new OrderDto
        //    {
        //        Id = order.Id,
        //        UserId = order.UserId,
        //        UserName = order.User?.UserName ?? "Unknown User",
        //        OrderDate = order.OrderDate,
        //        OrderItems = order.OrderItems.Select(oi => new OrderItemDto
        //        {
        //            Id = oi.Id,
        //            BookId = oi.BookId,
        //            BookTitle = oi.Book.Title,
        //            Quantity = oi.Quantity,
        //            UnitPrice = oi.UnitPrice
        //        }).ToList(),
        //        TotalAmount = order.TotalAmount,
        //        Status = order.OrderStatus.ToString()
        //    });
        //}
       
       
        //public async Task<IEnumerable<OrderDto>> GetAllPendingOrdersAsync(string userId)
        //{
        //    var orders = await _context.Orders
        //        .Where(o => o.OrderStatus == OrderStatus.Pending)
        //        .Include(o => o.OrderItems)
        //            .ThenInclude(oi => oi.Book)
        //        .Include(o => o.User)
        //        .ToListAsync();

        //    return orders.Select(order => new OrderDto
        //    {
        //        Id = order.Id,
        //        UserId = order.UserId,
        //        UserName = order.User?.UserName ?? "Unknown User",
        //        OrderDate = order.OrderDate,
        //        OrderItems = order.OrderItems.Select(oi => new OrderItemDto
        //        {
        //            Id = oi.Id,
        //            BookId = oi.BookId,
        //            BookTitle = oi.Book.Title,
        //            Quantity = oi.Quantity,
        //            UnitPrice = oi.UnitPrice
        //        }).ToList(),
        //        TotalAmount = order.TotalAmount
        //    });
        //}

        //public async Task<OrderDto> AssignDeliveryToOrderAsync(Guid orderId, Guid deliveryId)
        //{
        //    var order = await _context.Orders
        //        .Include(o => o.Delivery)
        //        .Include(o => o.OrderItems)
        //            .ThenInclude(oi => oi.Book)
        //        .FirstOrDefaultAsync(o => o.Id == orderId);

        //    if (order == null)
        //    {
        //        throw new Exception("Order not found");
        //    }

        //    var delivery = await _context.Deliveries.FirstOrDefaultAsync(d => d.Id == deliveryId);

        //    if (delivery == null)
        //    {
        //        throw new Exception("Delivery not found");
        //    }
        //    order.DeliveryId = delivery.Id;
        //    order.OrderStatus = OrderStatus.Shipping;


        //    _context.Orders.Update(order);
        //    await _context.SaveChangesAsync();
        //    return new OrderDto
        //    {
        //        Id = order.Id,
        //        UserId = order.UserId,
        //        UserName = order.User?.UserName ?? "Unknown User",
        //        OrderDate = order.OrderDate,
        //        TotalAmount = order.TotalAmount,
        //        OrderStatus = order.OrderStatus,
        //        DeliveryId = delivery.Id,
        //        DeliveryEmail = delivery.Email,
        //        DeliveryName = $"{delivery.FirstName} {delivery.LastName}",
        //        DeliveryPhoneNumber = delivery.PhoneNumber,
        //        OrderItems = order.OrderItems.Select(oi => new OrderItemDto
        //        {
        //            Id = oi.Id,
        //            BookId = oi.BookId,
        //            BookTitle = oi.Book?.Title ?? "Unknown Book",
        //            Quantity = oi.Quantity,
        //            UnitPrice = oi.UnitPrice
        //        }).ToList()
        //    };
        //}
   
}
