using Erp.Api.Models;
using Erp.Api.Permissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Erp.Api.Data;

public static class DatabaseSeeder
{
    public static string SeedFingerprint
    {
        get
        {
            var builder = new StringBuilder();
            builder.AppendLine("seed:v2");
            foreach (var item in BuildSideNavSeeds())
            {
                builder.AppendLine($"{item.ParentSlug}|{item.Name}|{item.Slug}|{item.Path}|{item.Permission}|{item.Level}|{item.DisplayOrder}");
            }

            foreach (var permission in AppPermissions.All.Order(StringComparer.Ordinal))
            {
                builder.AppendLine($"permission|{permission}");
            }

            builder.AppendLine("role|SuperAdmin|all-nav");
            builder.AppendLine("role|Admin|all-nav");
            builder.AppendLine("role|Manager|dashboard-users-roles-departments");
            builder.AppendLine("role|Employee|dashboard-only");
            builder.AppendLine("admin|admin@erp.local|superadmin");

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
        }
    }

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<AppUser>>();

        await SeedSideNavItemsAsync(db);

        var navPermissions = await db.SideNavItems.Select(item => item.Permission).Distinct().ToListAsync();
        var superAdminRole = await EnsureRoleAsync(
            db,
            "SuperAdmin",
            "Full ERP system administrator with all sidebar permissions.",
            AppPermissions.All.Concat(navPermissions).Distinct());
        await EnsureSideNavPermissionsAsync(db, superAdminRole, canRead: true, canWrite: true, canUpdate: true, canDelete: true, isVisible: true);

        var adminRole = await EnsureRoleAsync(
            db,
            "Admin",
            "ERP administrator.",
            AppPermissions.All.Concat(navPermissions).Distinct());
        await EnsureSideNavPermissionsAsync(db, adminRole, canRead: true, canWrite: true, canUpdate: true, canDelete: true, isVisible: true);

        await EnsureRoleAsync(db, "Manager", "Can view users, roles, dashboard, and manage departments.",
        [
            AppPermissions.DashboardView,
            AppPermissions.UsersView,
            AppPermissions.RolesView,
            AppPermissions.DepartmentsView,
            AppPermissions.DepartmentsManage
        ]);
        var employeeRole = await EnsureRoleAsync(db, "Employee", "Default ERP portal user.",
        [
            AppPermissions.DashboardView
        ]);
        await EnsureDashboardOnlyAsync(db, employeeRole);

        const string adminEmail = "admin@erp.local";
        if (!await db.Users.AnyAsync(user => user.Email == adminEmail))
        {
            var admin = new AppUser
            {
                Id = Guid.NewGuid(),
                Name = "ERP Admin",
                Email = adminEmail,
                RoleId = superAdminRole.Id
            };
            admin.Password = passwordHasher.HashPassword(admin, "Admin@12345");
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
        else
        {
            var admin = await db.Users.FirstAsync(user => user.Email == adminEmail);
            if (admin.RoleId != superAdminRole.Id)
            {
                admin.RoleId = superAdminRole.Id;
                await db.SaveChangesAsync();
            }
        }
    }

    private static async Task SeedSideNavItemsAsync(AppDbContext db)
    {
        var mainItems = MainSideNavSeeds();

        foreach (var item in mainItems)
        {
            await EnsureSideNavItemAsync(db, item, parentId: null, level: 1);
        }

        foreach (var module in mainItems.Where(item => item.Slug is not "dashboard" and not "ai-chat"))
        {
            var parent = await db.SideNavItems.FirstAsync(item => item.Slug == module.Slug);
            var setup = new NavSeed(
                $"{module.Name} Setup",
                $"{module.Slug}-setup",
                $"{module.Path}/setup",
                PermissionFor($"{module.Slug}.setup"),
                1);
            var transactions = new NavSeed(
                $"{module.Name} Transactions",
                $"{module.Slug}-transactions",
                $"{module.Path}/transactions",
                PermissionFor($"{module.Slug}.transactions"),
                2);

            await EnsureSideNavItemAsync(db, setup, parent.Id, level: 2);
            await EnsureSideNavItemAsync(db, transactions, parent.Id, level: 2);

            var setupParent = await db.SideNavItems.FirstAsync(item => item.Slug == setup.Slug);
            var transactionParent = await db.SideNavItems.FirstAsync(item => item.Slug == transactions.Slug);

            await EnsureSideNavItemAsync(db, new NavSeed("Dummy List", $"{module.Slug}-setup-list", $"{setup.Path}/list", PermissionFor($"{module.Slug}.setup.list"), 1), setupParent.Id, level: 3);
            await EnsureSideNavItemAsync(db, new NavSeed("Dummy Create", $"{module.Slug}-setup-create", $"{setup.Path}/create", PermissionFor($"{module.Slug}.setup.create"), 2), setupParent.Id, level: 3);
            await EnsureSideNavItemAsync(db, new NavSeed("Dummy Entry", $"{module.Slug}-transactions-entry", $"{transactions.Path}/entry", PermissionFor($"{module.Slug}.transactions.entry"), 1), transactionParent.Id, level: 3);
            await EnsureSideNavItemAsync(db, new NavSeed("Dummy Report", $"{module.Slug}-transactions-report", $"{transactions.Path}/report", PermissionFor($"{module.Slug}.transactions.report"), 2), transactionParent.Id, level: 3);
        }
    }

    private static IReadOnlyList<NavSeed> MainSideNavSeeds()
    {
        return
        [
            new NavSeed("Dashboard", "dashboard", "/dashboard", AppPermissions.DashboardView, 1),
            new NavSeed("AI Chat", "ai-chat", "/ai-chat", AppPermissions.AiChatView, 2),
            new NavSeed("Master", "master", "/master", AppPermissions.MasterView, 3),
            new NavSeed("Sales", "sales", "/sales", AppPermissions.SalesView, 4),
            new NavSeed("Outsourcing", "outsourcing", "/outsourcing", AppPermissions.OutsourcingView, 5),
            new NavSeed("Production", "production", "/production", AppPermissions.ProductionView, 6),
            new NavSeed("Inventory", "inventory", "/inventory", AppPermissions.InventoryView, 7),
            new NavSeed("Planning", "planning", "/planning", AppPermissions.PlanningView, 8),
            new NavSeed("Cash Flow", "cash-flow", "/cash-flow", AppPermissions.CashFlowView, 9),
            new NavSeed("Inspection", "inspection", "/inspection", AppPermissions.InspectionView, 10),
            new NavSeed("Maintenance", "maintenance", "/maintenance", AppPermissions.MaintenanceView, 11),
            new NavSeed("Human Resource", "human-resource", "/human-resource", AppPermissions.HumanResourceView, 12)
        ];
    }

    private static IReadOnlyList<NavSeedFingerprint> BuildSideNavSeeds()
    {
        var seeds = new List<NavSeedFingerprint>();
        var mainItems = MainSideNavSeeds();
        seeds.AddRange(mainItems.Select(item => new NavSeedFingerprint(null, item.Name, item.Slug, item.Path, item.Permission, 1, item.DisplayOrder)));

        foreach (var module in mainItems.Where(item => item.Slug is not "dashboard" and not "ai-chat"))
        {
            var setup = new NavSeed(
                $"{module.Name} Setup",
                $"{module.Slug}-setup",
                $"{module.Path}/setup",
                PermissionFor($"{module.Slug}.setup"),
                1);
            var transactions = new NavSeed(
                $"{module.Name} Transactions",
                $"{module.Slug}-transactions",
                $"{module.Path}/transactions",
                PermissionFor($"{module.Slug}.transactions"),
                2);

            seeds.Add(new NavSeedFingerprint(module.Slug, setup.Name, setup.Slug, setup.Path, setup.Permission, 2, setup.DisplayOrder));
            seeds.Add(new NavSeedFingerprint(module.Slug, transactions.Name, transactions.Slug, transactions.Path, transactions.Permission, 2, transactions.DisplayOrder));
            seeds.Add(new NavSeedFingerprint(setup.Slug, "Dummy List", $"{module.Slug}-setup-list", $"{setup.Path}/list", PermissionFor($"{module.Slug}.setup.list"), 3, 1));
            seeds.Add(new NavSeedFingerprint(setup.Slug, "Dummy Create", $"{module.Slug}-setup-create", $"{setup.Path}/create", PermissionFor($"{module.Slug}.setup.create"), 3, 2));
            seeds.Add(new NavSeedFingerprint(transactions.Slug, "Dummy Entry", $"{module.Slug}-transactions-entry", $"{transactions.Path}/entry", PermissionFor($"{module.Slug}.transactions.entry"), 3, 1));
            seeds.Add(new NavSeedFingerprint(transactions.Slug, "Dummy Report", $"{module.Slug}-transactions-report", $"{transactions.Path}/report", PermissionFor($"{module.Slug}.transactions.report"), 3, 2));
        }

        return seeds;
    }

    private static string PermissionFor(string value)
    {
        return $"{value}.view";
    }

    private static async Task EnsureSideNavItemAsync(AppDbContext db, NavSeed seed, int? parentId, int level)
    {
        var item = await db.SideNavItems.FirstOrDefaultAsync(existing => existing.Slug == seed.Slug);
        if (item is null)
        {
            db.SideNavItems.Add(new SideNavItem
            {
                ParentId = parentId,
                Name = seed.Name,
                Slug = seed.Slug,
                Path = seed.Path,
                Permission = seed.Permission,
                Level = level,
                DisplayOrder = seed.DisplayOrder,
                IsActive = true,
                CreatedDate = DateTimeOffset.UtcNow
            });
        }
        else
        {
            var changed = false;
            changed |= SetIfChanged(item.ParentId, parentId, value => item.ParentId = value);
            changed |= SetIfChanged(item.Name, seed.Name, value => item.Name = value);
            changed |= SetIfChanged(item.Path, seed.Path, value => item.Path = value);
            changed |= SetIfChanged(item.Permission, seed.Permission, value => item.Permission = value);
            changed |= SetIfChanged(item.Level, level, value => item.Level = value);
            changed |= SetIfChanged(item.DisplayOrder, seed.DisplayOrder, value => item.DisplayOrder = value);
            changed |= SetIfChanged(item.IsActive, true, value => item.IsActive = value);

            if (changed)
            {
                item.ModifiedDate = DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task<AppRole> EnsureRoleAsync(
        AppDbContext db,
        string name,
        string description,
        IEnumerable<string> permissions)
    {
        var role = await db.Roles.Include(existing => existing.Permissions).FirstOrDefaultAsync(existing => existing.Name == name);
        if (role is null)
        {
            role = new AppRole
            {
                Name = name,
                Description = description,
                CreatedDate = DateTimeOffset.UtcNow
            };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }
        else
        {
            if (role.Description != description)
            {
                role.Description = description;
                role.ModifiedDate = DateTimeOffset.UtcNow;
            }
        }

        var changed = false;
        var existingPermissions = role.Permissions.Select(permission => permission.Permission).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var permission in permissions.Where(permission => !existingPermissions.Contains(permission)))
        {
            role.Permissions.Add(new RolePermission { Permission = permission });
            changed = true;
        }

        if (changed || db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync();
        }

        return role;
    }

    private static async Task EnsureSideNavPermissionsAsync(
        AppDbContext db,
        AppRole role,
        bool canRead,
        bool canWrite,
        bool canUpdate,
        bool canDelete,
        bool isVisible)
    {
        var sideNavItems = await db.SideNavItems.ToListAsync();
        foreach (var item in sideNavItems)
        {
            await EnsureRoleSideNavPermissionAsync(db, role.Id, item.Id, canRead, canWrite, canUpdate, canDelete, isVisible);
        }
    }

    private static async Task EnsureDashboardOnlyAsync(AppDbContext db, AppRole role)
    {
        var sideNavItems = await db.SideNavItems.ToListAsync();
        foreach (var item in sideNavItems)
        {
            var enabled = item.Slug == "dashboard";
            await EnsureRoleSideNavPermissionAsync(
                db,
                role.Id,
                item.Id,
                canRead: enabled,
                canWrite: false,
                canUpdate: false,
                canDelete: false,
                isVisible: enabled);
        }
    }

    private static async Task EnsureRoleSideNavPermissionAsync(
        AppDbContext db,
        int roleId,
        int sideNavItemId,
        bool canRead,
        bool canWrite,
        bool canUpdate,
        bool canDelete,
        bool isVisible)
    {
        var permission = await db.RoleSideNavPermissions
            .FirstOrDefaultAsync(existing => existing.RoleId == roleId && existing.SideNavItemId == sideNavItemId);

        if (permission is null)
        {
            db.RoleSideNavPermissions.Add(new RoleSideNavPermission
            {
                RoleId = roleId,
                SideNavItemId = sideNavItemId,
                CanRead = canRead,
                CanWrite = canWrite,
                CanUpdate = canUpdate,
                CanDelete = canDelete,
                IsVisible = isVisible,
                CreatedDate = DateTimeOffset.UtcNow
            });
        }
        else
        {
            var changed = false;
            changed |= SetIfChanged(permission.CanRead, canRead, value => permission.CanRead = value);
            changed |= SetIfChanged(permission.CanWrite, canWrite, value => permission.CanWrite = value);
            changed |= SetIfChanged(permission.CanUpdate, canUpdate, value => permission.CanUpdate = value);
            changed |= SetIfChanged(permission.CanDelete, canDelete, value => permission.CanDelete = value);
            changed |= SetIfChanged(permission.IsVisible, isVisible, value => permission.IsVisible = value);

            if (changed)
            {
                permission.ModifiedDate = DateTimeOffset.UtcNow;
            }
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync();
        }
    }

    private static bool SetIfChanged<T>(T currentValue, T newValue, Action<T> setValue)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
        {
            return false;
        }

        setValue(newValue);
        return true;
    }

    private sealed record NavSeed(string Name, string Slug, string Path, string Permission, int DisplayOrder);
    private sealed record NavSeedFingerprint(string? ParentSlug, string Name, string Slug, string Path, string Permission, int Level, int DisplayOrder);
}
