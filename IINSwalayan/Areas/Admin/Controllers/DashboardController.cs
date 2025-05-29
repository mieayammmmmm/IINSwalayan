using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IINSwalayan.Data;

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
                ViewBag.Error = "Terjadi kesalahan saat memuat dashboard.";

                return View();
            }
        }
    }
}