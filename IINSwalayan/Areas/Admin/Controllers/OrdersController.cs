using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IINSwalayan.Data;
using IINSwalayan.Models;

namespace IINSwalayan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(ApplicationDbContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Orders
        public async Task<IActionResult> Index(string status = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var ordersQuery = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .AsQueryable();

                // Filter by status
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var orderStatus))
                {
                    ordersQuery = ordersQuery.Where(o => o.Status == orderStatus);
                }

                // Filter by date range
                if (startDate.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.OrderDate.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.OrderDate.Date <= endDate.Value.Date);
                }

                var orders = await ordersQuery
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                ViewBag.Orders = orders;
                ViewBag.StatusFilter = status;
                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
                ViewBag.OrderStatuses = Enum.GetValues<OrderStatus>();

                ViewData["Title"] = "Kelola Pesanan";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                ViewBag.Error = "Terjadi kesalahan saat memuat data pesanan.";
                ViewBag.Orders = new List<Order>();
                ViewBag.OrderStatuses = Enum.GetValues<OrderStatus>();
                return View();
            }
        }

        // POST: Admin/Orders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status, string? notes)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Pesanan tidak ditemukan." });
                }

                var oldStatus = order.Status;
                order.Status = status;
                order.UpdatedAt = DateTime.Now;

                if (!string.IsNullOrEmpty(notes))
                {
                    order.Notes = notes;
                }

                _context.Update(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Order {order.OrderNumber} status updated from {oldStatus} to {status} by {User.Identity?.Name}");

                return Json(new
                {
                    success = true,
                    message = $"Status pesanan berhasil diubah menjadi {GetStatusDisplayName(status)}",
                    newStatus = status.ToString(),
                    newStatusDisplay = GetStatusDisplayName(status)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return Json(new { success = false, message = "Terjadi kesalahan saat mengupdate status pesanan." });
            }
        }

        // Helper method to get status display name
        private string GetStatusDisplayName(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Menunggu Konfirmasi",
                OrderStatus.Confirmed => "Dikonfirmasi",
                OrderStatus.Processing => "Diproses",
                OrderStatus.Shipped => "Dikirim",
                OrderStatus.Delivered => "Selesai",
                OrderStatus.Cancelled => "Dibatalkan",
                _ => status.ToString()
            };
        }

        // Helper method to get status badge class (untuk digunakan di view)
        public static string GetStatusBadgeClass(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "bg-warning",
                OrderStatus.Confirmed => "bg-info",
                OrderStatus.Processing => "bg-primary",
                OrderStatus.Shipped => "bg-secondary",
                OrderStatus.Delivered => "bg-success",
                OrderStatus.Cancelled => "bg-danger",
                _ => "bg-secondary"
            };
        }
    }

}