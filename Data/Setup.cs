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

        base.OnModelCreating(modelBuilder);
    }
}


