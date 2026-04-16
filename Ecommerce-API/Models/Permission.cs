namespace Ecommerce_API.Models
{
    public class Permission
    {
        public Guid PermissionId { get; set; }
        public string PermissionKey { get; set; }
        public string Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; }
    }
}
