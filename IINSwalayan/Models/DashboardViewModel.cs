using IINSwalayan.Models;

namespace IINSwalayan.Models
{
    public class DashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int LowStockProducts { get; set; }
        public List<Product> RecentProducts { get; set; } = new();
    }
}