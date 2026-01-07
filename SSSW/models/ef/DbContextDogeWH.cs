using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SSSW.models;

namespace SSSW.modelss
{
    public class DbContextDogeWH : DbContext
    {
        private readonly string _connectionString;

        [ActivatorUtilitiesConstructor]
        public  DbContextDogeWH(DbContextOptions<DbContextDogeWH> options) : base(options)
        {
        }

        protected DbContextDogeWH(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_connectionString);
            }
        }

        //Shot weight
        public DbSet<FT600> FT600s { get; set; }
        public DbSet<FT601> FT601s { get; set; }
        public DbSet<FT602> FT602s { get; set; }
        public DbSet<FT605> FT605s { get; set; }
        public DbSet<FT606_Label> FT606s { get; set; }
        public DbSet<FT608_Config> FT608s { get; set; }
        public DbSet<FT029_Operator_RFID> fT029_Operator_RFIDs { get; set; }
        public DbSet<FT031_Department> FT031s { get; set; }

        override protected void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DbContextDogeWH).Assembly);
            modelBuilder.Entity<FT600>();
            modelBuilder.Entity<FT601>();
            modelBuilder.Entity<FT602>();
            modelBuilder.Entity<FT605>();
            modelBuilder.Entity<FT606_Label>();
            modelBuilder.Entity<FT608_Config>();
            modelBuilder.Entity<FT029_Operator_RFID>();
            modelBuilder.Entity<FT031_Department>();

            //Add mandatory filter in  query clauses
            modelBuilder.Entity<FT600>().HasQueryFilter(p => p.Actived == true);
            modelBuilder.Entity<FT601>().HasQueryFilter(p => p.Actived == true);
            modelBuilder.Entity<FT602>().HasQueryFilter(p => p.Actived == true);
            modelBuilder.Entity<FT606_Label>().HasQueryFilter(p => p.Actived == true);
            modelBuilder.Entity<FT608_Config>().HasQueryFilter(p => p.Actived == true);
        }
    }
}
