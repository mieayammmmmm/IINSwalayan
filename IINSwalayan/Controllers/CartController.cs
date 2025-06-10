using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IINSwalayan.Services;
using System.Security.Claims;

namespace IINSwalayan.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService cartService, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var cart = await _cartService.GetCartAsync(userId);
                ViewData["Title"] = "Keranjang Belanja";
                return View(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart");
                TempData["Error"] = "Terjadi kesalahan saat memuat keranjang belanja.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Anda harus login terlebih dahulu." });
                }

                var success = await _cartService.AddToCartAsync(userId, productId, quantity);

                if (success)
                {
                    var cartCount = await _cartService.GetCartItemCountAsync(userId);
                    return Json(new { success = true, message = "Produk berhasil ditambahkan ke keranjang!", cartCount = cartCount });
                }
                else
                {
                    return Json(new { success = false, message = "Gagal menambahkan produk ke keranjang. Stok mungkin tidak mencukupi." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to cart");
                return Json(new { success = false, message = "Terjadi kesalahan saat menambahkan produk ke keranjang." });
            }
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Anda harus login terlebih dahulu." });
                }

                var success = await _cartService.UpdateCartItemAsync(userId, productId, quantity);

                if (success)
                {
                    var cartCount = await _cartService.GetCartItemCountAsync(userId);
                    var cartTotal = await _cartService.GetCartTotalAsync(userId);
                    return Json(new
                    {
                        success = true,
                        message = "Keranjang berhasil diperbarui!",
                        cartCount = cartCount,
                        cartTotal = cartTotal.ToString("N0")
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Gagal memperbarui keranjang." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity");
                return Json(new { success = false, message = "Terjadi kesalahan saat memperbarui keranjang." });
            }
        }

        // POST: Cart/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Anda harus login terlebih dahulu." });
                }

                var success = await _cartService.RemoveFromCartAsync(userId, productId);

                if (success)
                {
                    var cartCount = await _cartService.GetCartItemCountAsync(userId);
                    var cartTotal = await _cartService.GetCartTotalAsync(userId);
                    return Json(new
                    {
                        success = true,
                        message = "Produk berhasil dihapus dari keranjang!",
                        cartCount = cartCount,
                        cartTotal = cartTotal.ToString("N0")
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Gagal menghapus produk dari keranjang." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                return Json(new { success = false, message = "Terjadi kesalahan saat menghapus produk dari keranjang." });
            }
        }

        // POST: Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                await _cartService.ClearCartAsync(userId);
                TempData["Success"] = "Keranjang berhasil dikosongkan!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                TempData["Error"] = "Terjadi kesalahan saat mengosongkan keranjang.";
                return RedirectToAction("Index");
            }
        }

        // GET: Cart/GetCartCount (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { cartCount = 0 });
                }

                var cartCount = await _cartService.GetCartItemCountAsync(userId);
                return Json(new { cartCount = cartCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart count");
                return Json(new { cartCount = 0 });
            }
        }
    }
}