using Microsoft.EntityFrameworkCore;
using Abp.EntityFrameworkCore;
using talepplanlama.Entities;

namespace talepplanlama.EntityFrameworkCore
{
    public class talepplanlamaDbContext : AbpDbContext
    {
        public DbSet<RequestType> RequestTypes { get; set; }
        public DbSet<ChangeRequest> ChangeRequests { get; set; }
        public DbSet<ImplementationLog> ImplementationLogs { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<AppRole> AppRoles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<ApprovalRecord> ApprovalRecords { get; set; }
        public DbSet<StatusChangeLog> StatusChangeLogs { get; set; }


        public talepplanlamaDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // RequestType 1:N ChangeRequest
            modelBuilder.Entity<ChangeRequest>()
                .HasOne(x => x.RequestType)
                .WithMany(x => x.ChangeRequests)
                .HasForeignKey(x => x.RequestTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ChangeRequest 1:N ImplementationLog
            modelBuilder.Entity<ImplementationLog>()
                .HasOne(x => x.ChangeRequest)
                .WithMany(x => x.ImplementationLogs)
                .HasForeignKey(x => x.ChangeRequestId)
                .OnDelete(DeleteBehavior.Cascade);


            // RBAC: AppUser N:N AppRole via UserRole junction
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => new { ur.UserId, ur.RoleId })
                .IsUnique();

            // RolePermission: AppRole 1:N RolePermission
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RolePermission>()
                .HasIndex(rp => new { rp.RoleId, rp.PermissionName })
                .IsUnique();

            // AppRole.Name unique
            modelBuilder.Entity<AppRole>()
                .HasIndex(r => r.Name)
                .IsUnique();

        }
    }
}
