namespace adres.api.Services;

public class UserDirectoryResult
{
    public string Sub { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EsRepresentanteLegal { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

public interface IUserDirectory
{
    Task<UserDirectoryResult?> FindBySubOrEmailAsync(string? sub, string? email);
}
