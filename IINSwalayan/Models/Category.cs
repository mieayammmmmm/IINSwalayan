using System.ComponentModel.DataAnnotations;

namespace IINSwalayan.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? IconClass { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}