using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace debezium_poc
{
    public class BusinessDbContextFactory : IDesignTimeDbContextFactory<BusinessDbContext>
    {
        public const string CONN_STR = "Host=localhost;Port=5432;Username=testuser;Password=testpassword;Database=postgres";

        public BusinessDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BusinessDbContext>();
            optionsBuilder.UseNpgsql(CONN_STR);

            return new BusinessDbContext(optionsBuilder.Options);
        }
    }

    public class BusinessDbContext(DbContextOptions<BusinessDbContext> options)
        : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<BookLoan> BookLoans => Set<BookLoan>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<BookLoan>(entity =>
            {
                entity.HasKey(e => e.LoanId);
                entity.HasIndex(e => new { e.BookId, e.Mid });
            });
        }
    }
}
