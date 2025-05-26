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

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel
            {
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive),
                TotalCategories = await _context.Categories.CountAsync(c => c.IsActive),
                LowStockProducts = await _context.Products.CountAsync(p => p.Stock <= 10 && p.IsActive),
                RecentProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }
    }


}