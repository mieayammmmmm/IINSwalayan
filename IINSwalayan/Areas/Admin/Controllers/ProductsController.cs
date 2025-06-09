using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IINSwalayan.Data;
using IINSwalayan.Models;

namespace IINSwalayan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Products
       
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive) // Only show active products
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                ViewBag.Products = products;
                ViewData["Title"] = "Kelola Produk";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                ViewBag.Error = "Terjadi kesalahan saat memuat data produk.";
                ViewBag.Products = new List<Product>();
                return View();
            }
        }

        // GET: Admin/Products/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .ToListAsync();

                ViewData["Title"] = "Tambah Produk";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create product form");
                ViewBag.Error = "Terjadi kesalahan saat memuat form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, string description, decimal price, int stock, int categoryId, string imageUrl, IFormFile imageFile, bool isActive = true)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrEmpty(name) || price <= 0 || stock < 0 || categoryId <= 0)
                {
                    TempData["Error"] = "Harap isi semua field yang diperlukan dengan benar.";
                    ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                    ViewData["Title"] = "Tambah Produk";
                    return View();
                }

                // Handle image upload
                string finalImageUrl = imageUrl ?? string.Empty;
                if (imageFile != null && imageFile.Length > 0)
                {
                    finalImageUrl = await SaveImageFile(imageFile);
                }

                var product = new Product
                {
                    Name = name,
                    Description = description ?? string.Empty,
                    Price = price,
                    Stock = stock,
                    CategoryId = categoryId,
                    ImageUrl = finalImageUrl,
                    IsActive = isActive,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Produk '{name}' berhasil ditambahkan!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                TempData["Error"] = $"Error: {ex.Message}";

                ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                ViewData["Title"] = "Tambah Produk";
                return View();
            }
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "ID produk tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Produk tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Product = product;
                ViewBag.Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .ToListAsync();

                ViewData["Title"] = "Edit Produk";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product for edit");
                TempData["Error"] = "Terjadi kesalahan saat memuat data produk.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name, string description, decimal price, int stock, int categoryId, string imageUrl, IFormFile imageFile, bool isActive = true)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Produk tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrEmpty(name) || price <= 0 || stock < 0 || categoryId <= 0)
                {
                    TempData["Error"] = "Harap isi semua field yang diperlukan dengan benar.";
                    ViewBag.Product = product;
                    ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                    ViewData["Title"] = "Edit Produk";
                    return View();
                }

                // Handle image upload - prioritize file upload over URL
                string finalImageUrl = product.ImageUrl ?? string.Empty; // Keep existing image as default

                if (imageFile != null && imageFile.Length > 0)
                {
                    // File upload has priority
                    finalImageUrl = await SaveImageFile(imageFile);
                    _logger.LogInformation($"New image uploaded during edit: {finalImageUrl}");
                }
                else if (!string.IsNullOrEmpty(imageUrl) && imageUrl != product.ImageUrl)
                {
                    // Use URL if provided and different from current
                    finalImageUrl = imageUrl;
                    _logger.LogInformation($"Image URL updated during edit: {finalImageUrl}");
                }

                // Update product
                product.Name = name;
                product.Description = description ?? string.Empty;
                product.Price = price;
                product.Stock = stock;
                product.CategoryId = categoryId;
                product.ImageUrl = finalImageUrl;
                product.IsActive = isActive;
                product.UpdatedAt = DateTime.Now;

                _context.Update(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Produk '{name}' berhasil diperbarui!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                TempData["Error"] = $"Terjadi kesalahan saat memperbarui produk: {ex.Message}";

                var product = await _context.Products.FindAsync(id);
                ViewBag.Product = product;
                ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                ViewData["Title"] = "Edit Produk";
                return View();
            }
        }

        // GET: Admin/Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "ID produk tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Produk tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                // Soft delete
                product.IsActive = false;
                product.UpdatedAt = DateTime.Now;

                _context.Update(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Produk '{product.Name}' berhasil dihapus!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                TempData["Error"] = "Terjadi kesalahan saat menghapus produk.";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<string> SaveImageFile(IFormFile imageFile)
        {
            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(imageFile.FileName)?.ToLowerInvariant();

                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                {
                    throw new InvalidOperationException("Format file tidak didukung. Gunakan JPG, PNG, GIF, atau WebP.");
                }

                // Validate file size (max 5MB)
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    throw new InvalidOperationException("Ukuran file terlalu besar. Maksimal 5MB.");
                }

                // Create uploads directory
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                return $"/uploads/products/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving image file");
                throw;
            }
        }
    }
}