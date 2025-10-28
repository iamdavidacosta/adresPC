using adres.api.Domain;
using Microsoft.EntityFrameworkCore;

namespace adres.api.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AdresAuthDbContext context)
    {
        // Verificar si ya existen datos
        if (await context.Users.AnyAsync())
        {
            return; // Ya hay datos, no hacer nada
        }

        // Crear roles (sin especificar ID, SQL Server lo auto-genera)
        var roleAdmin = new Role { Name = "Admin" };
        var roleAnalista = new Role { Name = "Analista" };
        var roleConsulta = new Role { Name = "Consulta" };

        context.Roles.AddRange(roleAdmin, roleAnalista, roleConsulta);
        await context.SaveChangesAsync();

        // Crear permisos (sin especificar ID)
        var permConsultarPagos = new Permission { Key = "CONSULTAR_PAGOS" };
        var permCrearSolicitud = new Permission { Key = "CREAR_SOLICITUD" };

        context.Permissions.AddRange(permConsultarPagos, permCrearSolicitud);
        await context.SaveChangesAsync();

        // Crear usuarios
        
        // Usuario Jorge Hernández - Mapeado con el UUID real de Autentic Sign
        var userJorge = new User
        {
            Id = Guid.NewGuid(),
            Sub = "d8213788-117a-4a1d-877b-32d47bdb2b1e", // UUID real de Autentic Sign
            Username = "j.hernandez",
            Email = "jorge.hernandez@adres.gov.co",
            FullName = "Jorge Hernández",
            FirstName = "Jorge",
            LastName = "Hernández",
            DocumentType = "CC",
            DocumentNumber = "1023456789",
            PhoneNumber = "+57 310 5551234",
            EsRepresentanteLegal = true,
            Position = "Representante Legal",
            Department = "Dirección General",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        var userJuan = new User
        {
            Id = Guid.NewGuid(),
            Sub = "u-12345",
            Username = "j.perez",
            Email = "juan@adres.gov.co",
            FullName = "Juan Pérez",
            FirstName = "Juan",
            LastName = "Pérez",
            DocumentType = "CC",
            DocumentNumber = "1012345678",
            PhoneNumber = "+57 300 1234567",
            EsRepresentanteLegal = true,
            Position = "Director Administrativo",
            Department = "Administración",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var userMaria = new User
        {
            Id = Guid.NewGuid(),
            Sub = "u-67890",
            Username = "m.gomez",
            Email = "maria@adres.gov.co",
            FullName = "María Gómez",
            FirstName = "María",
            LastName = "Gómez",
            DocumentType = "CC",
            DocumentNumber = "1098765432",
            PhoneNumber = "+57 315 9876543",
            EsRepresentanteLegal = false,
            Position = "Analista de Consulta",
            Department = "Información",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(userJorge, userJuan, userMaria);
        await context.SaveChangesAsync();

        // Asignar rol Admin a Jorge (Representante Legal)
        context.UserRoles.Add(
            new UserRole { UserId = userJorge.Id, RoleId = roleAdmin.Id }
        );
        
        // Asignar roles a Juan (Admin y Analista)
        context.UserRoles.AddRange(
            new UserRole { UserId = userJuan.Id, RoleId = roleAdmin.Id },
            new UserRole { UserId = userJuan.Id, RoleId = roleAnalista.Id }
        );

        // Asignar rol a María (Consulta)
        context.UserRoles.Add(
            new UserRole { UserId = userMaria.Id, RoleId = roleConsulta.Id }
        );

        await context.SaveChangesAsync();

        // Asignar permisos a roles
        // Admin: CONSULTAR_PAGOS, CREAR_SOLICITUD
        context.RolePermissions.AddRange(
            new RolePermission { RoleId = roleAdmin.Id, PermissionId = permConsultarPagos.Id },
            new RolePermission { RoleId = roleAdmin.Id, PermissionId = permCrearSolicitud.Id }
        );

        // Analista: CONSULTAR_PAGOS, CREAR_SOLICITUD
        context.RolePermissions.AddRange(
            new RolePermission { RoleId = roleAnalista.Id, PermissionId = permConsultarPagos.Id },
            new RolePermission { RoleId = roleAnalista.Id, PermissionId = permCrearSolicitud.Id }
        );

        // Consulta: CONSULTAR_PAGOS
        context.RolePermissions.Add(
            new RolePermission { RoleId = roleConsulta.Id, PermissionId = permConsultarPagos.Id }
        );

        await context.SaveChangesAsync();
    }
}
