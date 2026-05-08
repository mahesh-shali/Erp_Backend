using Erp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppRole> Roles => Set<AppRole>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<SideNavItem> SideNavItems => Set<SideNavItem>();
    public DbSet<RoleSideNavPermission> RoleSideNavPermissions => Set<RoleSideNavPermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Department> Departments => Set<Department>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppRole>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(role => role.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(role => role.Description).HasColumnName("description").HasMaxLength(300).IsRequired();
            entity.Property(role => role.CreatedDate).HasColumnName("created_date").IsRequired();
            entity.Property(role => role.ModifiedDate).HasColumnName("modified_date");
            entity.HasIndex(role => role.Name).IsUnique();
        });

        builder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("id");
            entity.Property(user => user.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
            entity.Property(user => user.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
            entity.Property(user => user.Password).HasColumnName("password").IsRequired();
            entity.Property(user => user.RoleId).HasColumnName("roleid").IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasOne(user => user.Role)
                .WithMany(role => role.Users)
                .HasForeignKey(user => user.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(permission => permission.Id);
            entity.Property(permission => permission.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(permission => permission.RoleId).HasColumnName("roleid").IsRequired();
            entity.Property(permission => permission.Permission).HasColumnName("permission").HasMaxLength(120).IsRequired();
            entity.HasIndex(permission => new { permission.RoleId, permission.Permission }).IsUnique();
            entity.HasOne(permission => permission.Role)
                .WithMany(role => role.Permissions)
                .HasForeignKey(permission => permission.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SideNavItem>(entity =>
        {
            entity.ToTable("side_nav_items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(item => item.ParentId).HasColumnName("parent_id");
            entity.Property(item => item.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            entity.Property(item => item.Slug).HasColumnName("slug").HasMaxLength(120).IsRequired();
            entity.Property(item => item.Path).HasColumnName("path").HasMaxLength(240).IsRequired();
            entity.Property(item => item.Permission).HasColumnName("permission").HasMaxLength(120).IsRequired();
            entity.Property(item => item.Level).HasColumnName("level").IsRequired();
            entity.Property(item => item.DisplayOrder).HasColumnName("display_order").IsRequired();
            entity.Property(item => item.IsActive).HasColumnName("is_active").IsRequired();
            entity.Property(item => item.CreatedDate).HasColumnName("created_date").IsRequired();
            entity.Property(item => item.ModifiedDate).HasColumnName("modified_date");
            entity.HasIndex(item => item.Slug).IsUnique();
            entity.HasIndex(item => new { item.ParentId, item.DisplayOrder });
            entity.HasOne(item => item.Parent)
                .WithMany(item => item.Children)
                .HasForeignKey(item => item.ParentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RoleSideNavPermission>(entity =>
        {
            entity.ToTable("role_side_nav_permissions");
            entity.HasKey(permission => permission.Id);
            entity.Property(permission => permission.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(permission => permission.RoleId).HasColumnName("roleid").IsRequired();
            entity.Property(permission => permission.SideNavItemId).HasColumnName("side_nav_item_id").IsRequired();
            entity.Property(permission => permission.CanRead).HasColumnName("can_read").IsRequired();
            entity.Property(permission => permission.CanWrite).HasColumnName("can_write").IsRequired();
            entity.Property(permission => permission.CanUpdate).HasColumnName("can_update").IsRequired();
            entity.Property(permission => permission.CanDelete).HasColumnName("can_delete").IsRequired();
            entity.Property(permission => permission.IsVisible).HasColumnName("is_visible").IsRequired();
            entity.Property(permission => permission.CreatedDate).HasColumnName("created_date").IsRequired();
            entity.Property(permission => permission.ModifiedDate).HasColumnName("modified_date");
            entity.HasIndex(permission => new { permission.RoleId, permission.SideNavItemId }).IsUnique();
            entity.HasOne(permission => permission.Role)
                .WithMany(role => role.SideNavPermissions)
                .HasForeignKey(permission => permission.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(permission => permission.SideNavItem)
                .WithMany(item => item.RolePermissions)
                .HasForeignKey(permission => permission.SideNavItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.Id).HasColumnName("id");
            entity.Property(token => token.UserId).HasColumnName("userid").IsRequired();
            entity.Property(token => token.TokenHash).HasColumnName("token_hash").IsRequired();
            entity.Property(token => token.CreatedDate).HasColumnName("created_date").IsRequired();
            entity.Property(token => token.ExpiresDate).HasColumnName("expires_date").IsRequired();
            entity.Property(token => token.RevokedDate).HasColumnName("revoked_date");
            entity.Property(token => token.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash");
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Department>(entity =>
        {
            entity.HasKey(department => department.Id);
            entity.Property(department => department.Code).HasMaxLength(32).IsRequired();
            entity.Property(department => department.Name).HasMaxLength(160).IsRequired();
            entity.HasIndex(department => department.Code).IsUnique();
        });
    }
}
