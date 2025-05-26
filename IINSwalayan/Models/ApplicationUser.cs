using Microsoft.AspNetCore.Identity;

namespace IINSwalayan.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}