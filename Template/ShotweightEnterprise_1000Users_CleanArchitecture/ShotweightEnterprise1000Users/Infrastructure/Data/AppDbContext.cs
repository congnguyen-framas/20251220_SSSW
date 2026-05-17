
using Microsoft.EntityFrameworkCore;
using ShotweightEnterprise1000Users.Domain.Entities;

namespace ShotweightEnterprise1000Users.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Step> Steps => Set<Step>();

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }
    }
}
