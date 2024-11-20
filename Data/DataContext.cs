using Microsoft.EntityFrameworkCore;
using UserAuthentication_ASPNET.Models.Entities;

namespace UserAuthentication_ASPNET.Data;

public partial class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; init; }
    public DbSet<Token> Tokens { get; init; }


    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimeStamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimeStamps()
    {
        var entries = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }

            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
