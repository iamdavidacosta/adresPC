namespace adres.api.Domain;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Relaciones
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
