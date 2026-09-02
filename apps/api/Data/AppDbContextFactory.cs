using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PlanVest.Api.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PLANVEST_MIGRATIONS_CONNECTION")
            ?? "Host=127.0.0.1;Port=1;Database=planvest_design;Username=planvest_design;Timeout=1";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AppDbContext(options);
    }
}
