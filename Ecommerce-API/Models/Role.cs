namespace Ecommerce_API.Models
{
    public class Role
    {

        public Guid RoleId { get; set; }
        public string RoleName { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; }
        public ICollection<User> Users { get; set; }
    }
}
