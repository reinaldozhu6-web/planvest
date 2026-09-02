using Microsoft.EntityFrameworkCore;
using PlanVest.Api.Models;

namespace PlanVest.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<ApplicationUser>();
        user.HasKey(value => value.Id);
        user.HasIndex(value => value.NormalizedEmail).IsUnique();
        user.Property(value => value.DisplayName).HasMaxLength(80);
        user.Property(value => value.Email).HasMaxLength(254);
        user.Property(value => value.NormalizedEmail).HasMaxLength(254);
        user.Property(value => value.PasswordHash).HasMaxLength(1000);
    }
}
