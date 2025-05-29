using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IINSwalayan.Data;
using IINSwalayan.Models;

namespace IINSwalayan.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ApplicationDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Categories
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Products)
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                ViewBag.Categories = categories;
                ViewData["Title"] = "Kelola Kategori";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
                ViewBag.Error = "Terjadi kesalahan saat memuat data kategori.";
                ViewBag.Categories = new List<Category>();
                return View();
            }
        }

        // GET: Admin/Categories/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Tambah Kategori";
            return View();
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, string description, string iconClass, bool isActive = true)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    TempData["Error"] = "Nama kategori harus diisi.";
                    ViewData["Title"] = "Tambah Kategori";
                    return View();
                }

                // Check if category name already exists
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower() && c.IsActive);

                if (existingCategory != null)
                {
                    TempData["Error"] = "Kategori dengan nama tersebut sudah ada.";
                    ViewData["Title"] = "Tambah Kategori";
                    return View();
                }

                var category = new Category
                {
                    Name = name,
                    Description = description,
                    IconClass = iconClass,
                    IsActive = isActive
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Kategori '{name}' berhasil ditambahkan!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                TempData["Error"] = $"Error: {ex.Message}";
                ViewData["Title"] = "Tambah Kategori";
                return View();
            }
        }

        // GET: Admin/Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "ID kategori tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    TempData["Error"] = "Kategori tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Category = category;
                ViewData["Title"] = "Edit Kategori";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category for edit");
                TempData["Error"] = "Terjadi kesalahan saat memuat data kategori.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name, string description, string iconClass, bool isActive = true)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    TempData["Error"] = "Kategori tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrEmpty(name))
                {
                    TempData["Error"] = "Nama kategori harus diisi.";
                    ViewBag.Category = category;
                    ViewData["Title"] = "Edit Kategori";
                    return View();
                }

                // Check if category name already exists (exclude current category)
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower() && c.IsActive && c.Id != id);

                if (existingCategory != null)
                {
                    TempData["Error"] = "Kategori dengan nama tersebut sudah ada.";
                    ViewBag.Category = category;
                    ViewData["Title"] = "Edit Kategori";
                    return View();
                }

                // Update category
                category.Name = name;
                category.Description = description;
                category.IconClass = iconClass;
                category.IsActive = isActive;

                _context.Update(category);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Kategori '{name}' berhasil diperbarui!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                TempData["Error"] = "Terjadi kesalahan saat memperbarui kategori.";

                var category = await _context.Categories.FindAsync(id);
                ViewBag.Category = category;
                ViewData["Title"] = "Edit Kategori";
                return View();
            }
        }

        // GET: Admin/Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "ID kategori tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    TempData["Error"] = "Kategori tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if category has active products
                var activeProductsCount = category.Products.Count(p => p.IsActive);
                if (activeProductsCount > 0)
                {
                    TempData["Error"] = $"Tidak dapat menghapus kategori '{category.Name}' karena masih memiliki {activeProductsCount} produk aktif.";
                    return RedirectToAction(nameof(Index));
                }

                // Soft delete
                category.IsActive = false;
                _context.Update(category);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Kategori '{category.Name}' berhasil dihapus!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                TempData["Error"] = "Terjadi kesalahan saat menghapus kategori.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}