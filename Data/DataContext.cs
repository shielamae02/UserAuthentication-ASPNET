using Microsoft.EntityFrameworkCore;
using UserAuthentication_ASPNET.Models.Entities;

namespace UserAuthentication_ASPNET.Data;

public partial class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; init; }
}
