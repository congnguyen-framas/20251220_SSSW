using Microsoft.EntityFrameworkCore;
using SSSW.Model;

namespace SSSW.models
{
    public class DbContextDogeWH : DbContext
    {
        public DbContextDogeWH(DbContextOptions<DbContextDogeWH> options) : base(options)
        {
        }

        protected DbContextDogeWH()
        {
        }

        //Shot weight
        public DbSet<FT600> FT600Db { get; set; }
        public DbSet<FT601> FT601Db { get; set; }
        public DbSet<FT602> FT602Db { get; set; }
        public DbSet<FT605> FT605Db { get; set; }
        public DbSet<FT606_Label> FT606Db { get; set; }

        override protected void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DbContextDogeWH).Assembly);
            modelBuilder.Entity<FT600>();
            modelBuilder.Entity<FT601>();
            modelBuilder.Entity<FT602>();
            modelBuilder.Entity<FT605>();
            modelBuilder.Entity<FT606_Label>();

            //Add mandatory filter in  query clauses
            modelBuilder.Entity<FT600>().HasQueryFilter(p => p.Actived == true);
            modelBuilder.Entity<FT601>().HasQueryFilter(p => p.Actived == true);
            modelBuilder.Entity<FT602>().HasQueryFilter(p => p.Actived == true);
            modelBuilder.Entity<FT606_Label>().HasQueryFilter(p => p.Actived == true);
        }
    }
}
