using Ecommerce_API.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce_API.Models
{
    // Use IdentityUser<Guid> so ASP.NET Core Identity can manage authentication data (password hash, email, phone, etc.)
    public class User : IdentityUser<Guid>
    {
        // Rename enum-based role to avoid collision with the `Role` navigation property
        public UserRole RoleType { get; set; } = UserRole.Guest;
        public string? Address { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public Cart? Cart { get; set; }
        public Guid RoleId { get; set; }
        public Role Role { get; set; }



    }
}
