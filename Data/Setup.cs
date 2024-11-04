using Microsoft.EntityFrameworkCore;
using UserAuthentication_ASPNET.Models.Entities;

namespace UserAuthentication_ASPNET.Data;

public partial class DataContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => new
            {
                u.Email
            })
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasMany(u => u.Tokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId);

        modelBuilder.Entity<Token>()
            .HasIndex(t => new
            {
                t.Refresh
            });

        base.OnModelCreating(modelBuilder);
    }
}


