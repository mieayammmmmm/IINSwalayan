using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IINSwalayan.Data;
using IINSwalayan.Models;
using IINSwalayan.Services;
using System.Security.Claims;

namespace IINSwalayan.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(ApplicationDbContext context, ICartService cartService, ILogger<OrderController> logger)
        {
            _context = context;
            _cartService = cartService;
            _logger = logger;
        }

        // GET: Order/Checkout
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var cart = await _cartService.GetCartAsync(userId);
                if (!cart.CartItems.Any())
                {
                    TempData["Error"] = "Keranjang belanja Anda kosong.";
                    return RedirectToAction("Index", "Cart");
                }

                // Get user info
                var user = await _context.Users.FindAsync(userId);
                ViewBag.User = user;
                ViewBag.Cart = cart;
                ViewData["Title"] = "Checkout";

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout page");
                TempData["Error"] = "Terjadi kesalahan saat memuat halaman checkout.";
                return RedirectToAction("Index", "Cart");
            }
        }

        // POST: Order/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string customerName, string customerEmail, string customerPhone, string shippingAddress, string notes)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Validation
                if (string.IsNullOrEmpty(customerName) || string.IsNullOrEmpty(customerEmail) ||
                    string.IsNullOrEmpty(customerPhone) || string.IsNullOrEmpty(shippingAddress))
                {
                    TempData["Error"] = "Harap lengkapi semua data yang diperlukan.";
                    return RedirectToAction("Checkout");
                }

                var cart = await _cartService.GetCartAsync(userId);
                if (!cart.CartItems.Any())
                {
                    TempData["Error"] = "Keranjang belanja Anda kosong.";
                    return RedirectToAction("Index", "Cart");
                }

                // Check stock availability
                foreach (var cartItem in cart.CartItems)
                {
                    var product = await _context.Products.FindAsync(cartItem.ProductId);
                    if (product == null || !product.IsActive || product.Stock < cartItem.Quantity)
                    {
                        TempData["Error"] = $"Stok produk {cartItem.Product.Name} tidak mencukupi.";
                        return RedirectToAction("Checkout");
                    }
                }

                // Generate order number
                var orderNumber = await GenerateOrderNumberAsync();

                // Create order
                var order = new Order
                {
                    OrderNumber = orderNumber,
                    UserId = userId,
                    OrderDate = DateTime.Now,
                    TotalAmount = cart.TotalAmount,
                    Status = OrderStatus.Pending,
                    CustomerName = customerName,
                    CustomerEmail = customerEmail,
                    CustomerPhone = customerPhone,
                    ShippingAddress = shippingAddress,
                    Notes = notes ?? string.Empty,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create order items and update stock
                foreach (var cartItem in cart.CartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.UnitPrice,
                        TotalPrice = cartItem.TotalPrice,
                        CreatedAt = DateTime.Now
                    };

                    _context.OrderItems.Add(orderItem);

                    // Update product stock
                    var product = await _context.Products.FindAsync(cartItem.ProductId);
                    if (product != null)
                    {
                        product.Stock -= cartItem.Quantity;
                        product.UpdatedAt = DateTime.Now;
                        _context.Update(product);
                    }
                }

                await _context.SaveChangesAsync();

                // Clear cart
                await _cartService.ClearCartAsync(userId);

                TempData["Success"] = $"Pesanan berhasil dibuat dengan nomor: {orderNumber}";
                return RedirectToAction("OrderSuccess", new { orderNumber = orderNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order");
                TempData["Error"] = "Terjadi kesalahan saat memproses pesanan. Silakan coba lagi.";
                return RedirectToAction("Checkout");
            }
        }

        // GET: Order/OrderSuccess
        public async Task<IActionResult> OrderSuccess(string orderNumber)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && o.UserId == userId);

                if (order == null)
                {
                    TempData["Error"] = "Pesanan tidak ditemukan.";
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.Order = order;
                ViewData["Title"] = "Pesanan Berhasil";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order success page");
                TempData["Error"] = "Terjadi kesalahan saat memuat halaman.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Order/MyOrders
        public async Task<IActionResult> MyOrders()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                ViewBag.Orders = orders;
                ViewData["Title"] = "Pesanan Saya";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user orders");
                TempData["Error"] = "Terjadi kesalahan saat memuat pesanan.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Order/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

                if (order == null)
                {
                    TempData["Error"] = "Pesanan tidak ditemukan.";
                    return RedirectToAction("MyOrders");
                }

                ViewBag.Order = order;
                ViewData["Title"] = $"Detail Pesanan #{order.OrderNumber}";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details");
                TempData["Error"] = "Terjadi kesalahan saat memuat detail pesanan.";
                return RedirectToAction("MyOrders");
            }
        }

        private async Task<string> GenerateOrderNumberAsync()
        {
            var prefix = "ORD";
            var dateString = DateTime.Now.ToString("yyyyMMdd");

            // Get last order number for today
            var lastOrder = await _context.Orders
                .Where(o => o.OrderNumber.StartsWith($"{prefix}-{dateString}"))
                .OrderByDescending(o => o.OrderNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastOrder != null)
            {
                var lastSequence = lastOrder.OrderNumber.Split('-').LastOrDefault();
                if (int.TryParse(lastSequence, out int lastSeq))
                {
                    sequence = lastSeq + 1;
                }
            }

            return $"{prefix}-{dateString}-{sequence:D4}";
        }
    }
}