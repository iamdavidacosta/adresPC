namespace adres.api.Domain;

public class User
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Subject (UUID) del proveedor OAuth - Autentic Sign
    /// </summary>
    public string Sub { get; set; } = string.Empty;
    
    /// <summary>
    /// Nombre de usuario preferido
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Correo electrónico del usuario
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Nombre completo del usuario
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Primer nombre
    /// </summary>
    public string? FirstName { get; set; }
    
    /// <summary>
    /// Apellidos
    /// </summary>
    public string? LastName { get; set; }
    
    /// <summary>
    /// Número de documento de identidad
    /// </summary>
    public string? DocumentNumber { get; set; }
    
    /// <summary>
    /// Tipo de documento (CC, CE, NIT, etc.)
    /// </summary>
    public string? DocumentType { get; set; }
    
    /// <summary>
    /// Teléfono de contacto
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Indica si el usuario es Representante Legal
    /// </summary>
    public bool EsRepresentanteLegal { get; set; }
    
    /// <summary>
    /// Cargo o posición del usuario
    /// </summary>
    public string? Position { get; set; }
    
    /// <summary>
    /// Departamento o área
    /// </summary>
    public string? Department { get; set; }
    
    /// <summary>
    /// Indica si el usuario está activo
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Relaciones
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
