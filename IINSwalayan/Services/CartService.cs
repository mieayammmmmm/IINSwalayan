using Microsoft.EntityFrameworkCore;
using IINSwalayan.Data;
using IINSwalayan.Models;

namespace IINSwalayan.Services
{
    public interface ICartService
    {
        Task<Cart> GetCartAsync(string userId);
        Task<bool> AddToCartAsync(string userId, int productId, int quantity = 1);
        Task<bool> UpdateCartItemAsync(string userId, int productId, int quantity);
        Task<bool> RemoveFromCartAsync(string userId, int productId);
        Task<bool> ClearCartAsync(string userId);
        Task<int> GetCartItemCountAsync(string userId);
        Task<decimal> GetCartTotalAsync(string userId);
    }

    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartService> _logger;

        public CartService(ApplicationDbContext context, ILogger<CartService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Cart> GetCartAsync(string userId)
        {
            try
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .ThenInclude(p => p.Category)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                return cart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> AddToCartAsync(string userId, int productId, int quantity = 1)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null || !product.IsActive || product.Stock < quantity)
                {
                    return false;
                }

                var cart = await GetCartAsync(userId);
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

                if (existingItem != null)
                {
                    var newQuantity = existingItem.Quantity + quantity;
                    if (newQuantity > product.Stock)
                    {
                        return false;
                    }
                    existingItem.Quantity = newQuantity;
                    existingItem.UpdatedAt = DateTime.Now;
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = product.Price
                    };
                    _context.CartItems.Add(cartItem);
                }

                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cart for user {UserId}", productId, userId);
                return false;
            }
        }

        public async Task<bool> UpdateCartItemAsync(string userId, int productId, int quantity)
        {
            try
            {
                if (quantity <= 0)
                {
                    return await RemoveFromCartAsync(userId, productId);
                }

                var cart = await GetCartAsync(userId);
                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

                if (cartItem == null)
                {
                    return false;
                }

                var product = await _context.Products.FindAsync(productId);
                if (product == null || quantity > product.Stock)
                {
                    return false;
                }

                cartItem.Quantity = quantity;
                cartItem.UpdatedAt = DateTime.Now;
                cart.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> RemoveFromCartAsync(string userId, int productId)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

                if (cartItem == null)
                {
                    return false;
                }

                _context.CartItems.Remove(cartItem);
                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ClearCartAsync(string userId)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                if (cart.CartItems.Any())
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                    cart.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
                return false;
            }
        }

        public async Task<int> GetCartItemCountAsync(string userId)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                return cart.TotalItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item count for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<decimal> GetCartTotalAsync(string userId)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                return cart.TotalAmount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart total for user {UserId}", userId);
                return 0;
            }
        }
    }
}