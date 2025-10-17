namespace adres.api.Domain;

public class User
{
    public Guid Id { get; set; }
    public string Sub { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EsRepresentanteLegal { get; set; }
    public DateTime CreatedAt { get; set; }

    // Relaciones
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
