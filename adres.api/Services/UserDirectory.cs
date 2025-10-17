using adres.api.Data;
using Microsoft.EntityFrameworkCore;

namespace adres.api.Services;

public class UserDirectory : IUserDirectory
{
    private readonly AdresAuthDbContext _context;

    public UserDirectory(AdresAuthDbContext context)
    {
        _context = context;
    }

    public async Task<UserDirectoryResult?> FindBySubOrEmailAsync(string? sub, string? email)
    {
        if (string.IsNullOrWhiteSpace(sub) && string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(sub))
        {
            query = query.Where(u => u.Sub == sub);
        }
        else if (!string.IsNullOrWhiteSpace(email))
        {
            query = query.Where(u => u.Email == email);
        }

        var user = await query.FirstOrDefaultAsync();

        if (user == null)
        {
            return null;
        }

        var roles = user.UserRoles
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Key)
            .Distinct()
            .ToList();

        return new UserDirectoryResult
        {
            Sub = user.Sub,
            Username = user.Username,
            Email = user.Email,
            EsRepresentanteLegal = user.EsRepresentanteLegal,
            Roles = roles,
            Permissions = permissions
        };
    }
}
