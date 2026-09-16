using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using talepplanlama.EntityFrameworkCore;
using talepplanlama.EntityFrameworkCore.Seed;
using talepplanlama.Entities;

namespace talepplanlama.Web.Host
{
    /// <summary>
    /// Background service that runs migration + seed once at startup
    /// without blocking the HTTP pipeline.
    /// </summary>
    public class MigrationHostedService : IHostedService
    {
        private readonly IConfiguration _config;

        public MigrationHostedService(IConfiguration config)
        {
            _config = config;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var connStr = _config.GetConnectionString("Default") ?? "";
            if (string.IsNullOrEmpty(connStr)) return;

            // Run in background to avoid blocking host startup (prevents EF tooling timeout)
            _ = Task.Run(async () =>
            {
                // Small delay to ensure host is fully started before DB operations
                await Task.Delay(1000, cancellationToken);
                try
                {
                    var optionsBuilder = new DbContextOptionsBuilder();
                    optionsBuilder.UseNpgsql(connStr);

                    using (var db = new talepplanlamaDbContext(optionsBuilder.Options))
                    {
                        // EnsureCreated bos veritabaninda en guncel semayi kurar ve true doner.
                        // Dolu veritabaninda hicbir sey yapmaz — o durumda bekleyen migration'lar
                        // SchemaMigrations.Apply icinde sirayla calistirilir.
                        var freshlyCreated = db.Database.EnsureCreated();
                        Console.WriteLine("[Migration] Migration tanimi yok — sema EnsureCreated ile yonetiliyor.");
                        Console.WriteLine("[Migration] Database is up to date.");

                        // Seed sample data — wrapped in its own try so a failure here doesn't block RBAC seed below.
                        try
                        {
                    if (!db.RequestTypes.Any())
                    {
                        db.RequestTypes.AddRange(
                    new RequestType { Id = 1, Name = "Talep Tipi 1", Description = "Lorem ipsum dolor sit amet" },
                    new RequestType { Id = 2, Name = "Talep Tipi 2", Description = "Consectetur adipiscing elit" }
                        );
                    }
                    if (!db.ChangeRequests.Any())
                    {
                        db.ChangeRequests.AddRange(
                    new ChangeRequest { Id = 3, Title = "Introduction to Physics", RequestNumber = "ABC-001", Description = "Lorem ipsum dolor sit amet", Justification = "Sample Item 1", EffectiveDate = new DateTime(2024, 3, 15), Status = (ChangeRequestStatus)0, FirstApproverRole = "Sample Item 1", SecondApproverRole = "Sample Item 1", RevisionNote = "Lorem ipsum dolor sit amet", RequestTypeId = 1 },
                    new ChangeRequest { Id = 4, Title = "Advanced Mathematics", RequestNumber = "XYZ-002", Description = "Consectetur adipiscing elit", Justification = "Sample Item 2", EffectiveDate = new DateTime(2024, 6, 20), Status = (ChangeRequestStatus)0, FirstApproverRole = "Sample Item 2", SecondApproverRole = "Sample Item 2", RevisionNote = "Consectetur adipiscing elit", RequestTypeId = 2 }
                        );
                    }
                    if (!db.ImplementationLogs.Any())
                    {
                        db.ImplementationLogs.AddRange(
                    new ImplementationLog { Id = 5, Phase = (ImplementationLogPhase)0, Notes = "Lorem ipsum dolor sit amet", CompletedAt = new DateTime(2024, 3, 15), ChangeRequestId = 3 },
                    new ImplementationLog { Id = 6, Phase = (ImplementationLogPhase)1, Notes = "Consectetur adipiscing elit", CompletedAt = new DateTime(2024, 6, 20), ChangeRequestId = 4 }
                        );
                    }
                            db.SaveChanges();
                            Console.WriteLine("[Seed] Sample data created.");
                        }
                        catch (Exception sampleEx)
                        {
                            Console.WriteLine($"[Seed] Sample data skipped: {sampleEx.GetType().Name}: {sampleEx.Message}");
                            // Carry on — RBAC seed must still run so admin/123qwe is usable.
                        }
                        // Sync identity sequences to MAX(Id). Seeded rows carry explicit Ids which do NOT
                        // advance Postgres identity sequences → nextval collides with a seed row and the
                        // first few inserts fail with a duplicate-key 500. Runs every startup; idempotent.
                        try
                        {
                            db.Database.ExecuteSqlRaw("SELECT setval(pg_get_serial_sequence('\"RequestTypes\"', 'Id'), (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"RequestTypes\") + 1, false);");
                            db.Database.ExecuteSqlRaw("SELECT setval(pg_get_serial_sequence('\"ChangeRequests\"', 'Id'), (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"ChangeRequests\") + 1, false);");
                            db.Database.ExecuteSqlRaw("SELECT setval(pg_get_serial_sequence('\"ImplementationLogs\"', 'Id'), (SELECT COALESCE(MAX(\"Id\"), 0) FROM \"ImplementationLogs\") + 1, false);");
                            Console.WriteLine("[Seed] Identity sequences synced.");
                        }
                        catch (Exception seqEx)
                        {
                            Console.WriteLine($"[Seed] Sequence sync skipped: {seqEx.GetType().Name}: {seqEx.Message}");
                        }
                    }
                    // RBAC seed (Admin/User roles + permissions + admin user) runs through ABP DI
                    // so PermissionRegistry can be injected. SeedHelper is idempotent.
                    SeedHelper.SeedHostDb(Abp.Dependency.IocManager.Instance);
                    Console.WriteLine("[Seed] RBAC seed complete (Admin role + admin user).");
                }
                catch (Exception ex)
                {
                    // Full diagnostic — surface the real cause so silent seed failures are debuggable.
                    Console.WriteLine($"[Migration] FAILED: {ex.GetType().Name}: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"[Migration] InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    Console.WriteLine("[Migration] StackTrace:");
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine("[Migration] App continues without migration — admin user will not exist.");
                }
            }, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class Program
    {
        // Runtime entry: WebHost is required because ABP Startup returns IServiceProvider.
        public static void Main(string[] args)
        {
            // Npgsql 7+ requires UTC DateTimes — enable legacy behavior for ABP compatibility
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build()
                .Run();
        }

        // Design-time entry for EF Core tools (dotnet ef migrations).
        // Without this, EF tools wait 5 minutes for IHost build (resolver default timeout)
        // and then SIGTERM any running dotnet process — killing live dev servers.
        // We expose a minimal IHost that EF tools resolve in milliseconds; the actual
        // DbContext is built by IDesignTimeDbContextFactory in the EntityFrameworkCore project.
        public static IHostBuilder CreateHostBuilder(string[] args)
            => Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args);
    }
}
