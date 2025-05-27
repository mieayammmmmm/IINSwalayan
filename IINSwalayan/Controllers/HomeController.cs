using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IINSwalayan.Data;
using IINSwalayan.Models;

namespace IINSwalayan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get featured products (latest 8 products)
                var featuredProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToListAsync();

                // Get categories with products
                var categories = await _context.Categories
                    .Include(c => c.Products)
                    .Where(c => c.IsActive && c.Products.Any(p => p.IsActive))
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                ViewBag.FeaturedProducts = featuredProducts;
                ViewBag.Categories = categories;
                ViewData["Title"] = "IIN Swalayan - Belanja Online Mudah & Terpercaya";

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page");
                ViewBag.FeaturedProducts = new List<Product>();
                ViewBag.Categories = new List<Category>();
                return View();
            }
        }

        public async Task<IActionResult> Products(int? categoryId, string search, int page = 1)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive);

                // Filter by category
                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                    var category = await _context.Categories.FindAsync(categoryId.Value);
                    ViewBag.CurrentCategory = category;
                }

                // Filter by search
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
                    ViewBag.SearchTerm = search;
                }

                // Pagination
                int pageSize = 12;
                var totalProducts = await query.CountAsync();
                var products = await query
                    .OrderBy(p => p.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get all categories for filter
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                ViewBag.Products = products;
                ViewBag.Categories = categories;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
                ViewBag.TotalProducts = totalProducts;

                ViewData["Title"] = "Semua Produk - IIN Swalayan";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products page");
                ViewBag.Products = new List<Product>();
                ViewBag.Categories = new List<Category>();
                return View();
            }
        }

        public async Task<IActionResult> ProductDetail(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (product == null)
                {
                    return NotFound();
                }

                // Get related products (same category)
                var relatedProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                    .Take(4)
                    .ToListAsync();

                ViewBag.Product = product;
                ViewBag.RelatedProducts = relatedProducts;
                ViewData["Title"] = $"{product.Name} - IIN Swalayan";

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product detail");
                return NotFound();
            }
        }

        public IActionResult About()
        {
            ViewData["Title"] = "Tentang Kami - IIN Swalayan";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Title"] = "Kontak - IIN Swalayan";
            return View();
        }

        public IActionResult Privacy()
        {
            ViewData["Title"] = "Kebijakan Privasi - IIN Swalayan";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}