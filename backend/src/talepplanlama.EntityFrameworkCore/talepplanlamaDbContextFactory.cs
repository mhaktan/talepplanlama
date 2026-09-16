using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace talepplanlama.EntityFrameworkCore
{
    public class talepplanlamaDbContextFactory : IDesignTimeDbContextFactory<talepplanlamaDbContext>
    {
        public talepplanlamaDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../talepplanlama.Web.Host"))
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var connStr = configuration.GetConnectionString("Default");
            var builder = new DbContextOptionsBuilder();
            builder.UseNpgsql(connStr);
            return new talepplanlamaDbContext(builder.Options);
        }
    }
}
