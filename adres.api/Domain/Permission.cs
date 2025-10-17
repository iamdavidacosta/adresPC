namespace adres.api.Domain;

public class Permission
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;

    // Relaciones
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
