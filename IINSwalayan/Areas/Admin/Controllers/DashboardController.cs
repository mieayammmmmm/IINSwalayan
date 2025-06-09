using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IINSwalayan.Data;
using IINSwalayan.Models;

namespace IINSwalayan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation($"Admin dashboard accessed by {User.Identity?.Name}");

                // Initialize default values
                ViewBag.TotalProducts = 0;
                ViewBag.TotalCategories = 0;
                ViewBag.LowStockProducts = 0;
                ViewBag.TotalOrders = 0;
                ViewBag.PendingOrders = 0;
                ViewBag.TodayOrders = 0;
                ViewBag.MonthlyRevenue = 0;

                // Safely get counts if tables exist
                try
                {
                    if (await _context.Products.AnyAsync())
                    {
                        ViewBag.TotalProducts = await _context.Products.CountAsync(p => p.IsActive);
                        ViewBag.LowStockProducts = await _context.Products.CountAsync(p => p.Stock <= 10 && p.IsActive);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error accessing Products table");
                }

                try
                {
                    if (await _context.Categories.AnyAsync())
                    {
                        ViewBag.TotalCategories = await _context.Categories.CountAsync(c => c.IsActive);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error accessing Categories table");
                }

                // Get Order statistics
                try
                {
                    if (await _context.Orders.AnyAsync())
                    {
                        ViewBag.TotalOrders = await _context.Orders.CountAsync();
                        ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);

                        var today = DateTime.Today;
                        ViewBag.TodayOrders = await _context.Orders.CountAsync(o => o.OrderDate.Date == today);

                        var currentMonth = DateTime.Now.Month;
                        var currentYear = DateTime.Now.Year;
                        ViewBag.MonthlyRevenue = await _context.Orders
                            .Where(o => o.OrderDate.Month == currentMonth &&
                                       o.OrderDate.Year == currentYear &&
                                       o.Status != OrderStatus.Cancelled)
                            .SumAsync(o => o.TotalAmount);

                        // Get recent orders for dashboard
                        ViewBag.RecentOrders = await _context.Orders
                            .Include(o => o.User)
                            .OrderByDescending(o => o.OrderDate)
                            .Take(5)
                            .ToListAsync();
                    }
                    else
                    {
                        ViewBag.RecentOrders = new List<Order>();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error accessing Orders table");
                    ViewBag.RecentOrders = new List<Order>();
                }

                ViewData["Title"] = "Dashboard Admin";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");

                // Set safe default values
                ViewBag.TotalProducts = 0;
                ViewBag.TotalCategories = 0;
                ViewBag.LowStockProducts = 0;
                ViewBag.TotalOrders = 0;
                ViewBag.PendingOrders = 0;
                ViewBag.TodayOrders = 0;
                ViewBag.MonthlyRevenue = 0;
                ViewBag.RecentOrders = new List<Order>();
                ViewBag.Error = "Terjadi kesalahan saat memuat dashboard.";

                return View();
            }
        }
    }
}