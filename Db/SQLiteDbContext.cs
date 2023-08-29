using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using spapp_backend.Core.Models;
using spapp_backend.Modules.Company.Models;
using spapp_backend.Modules.Crowdfunding.Models;
using spapp_backend.Modules.Market.Models;

namespace spapp_backend.Db
{
  public partial class SQLiteDbContext : IdentityDbContext<User, IdentityRole<uint>, uint>
  {
    public DbSet<LogEntry> Logs { get; set; }
    public DbSet<ForbiddenAction> Forbiddens { get; set; }
    public DbSet<FileMeta> Files { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<AccountTransaction> Transactions { get; set; }
    public DbSet<PaymentRequest> Payments { get; set; }

    // Marketplace
    public DbSet<Shop> Shops { get; set; }
    public DbSet<ShopSlot> ShopSlots { get; set; }
    public DbSet<ShopReview> ShopReviews { get; set; }
    public DbSet<ShopItem> ShopItems { get; set; }

    // Crowdfunding
    public DbSet<CrowdfundCompany> CrowdCompanies { get; set; }
    public DbSet<CrowdfundComment> CrowdComments { get; set; }

    // Company
    public DbSet<CompanyAutopayment> CompanyAutopayments { get; set; }
    public DbSet<CompanyEmployee> CompanyEmployees { get; set; }
    public DbSet<CompanyVacancy> CompanyVacancies { get; set; }
    public DbSet<UserCompany> Companies { get; set; }
    public DbSet<VacancyType> VacancyTypes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = "./Data/Main.db" };
      var connectionString = connectionStringBuilder.ToString();
      var connection = new SqliteConnection(connectionString);

      optionsBuilder.UseSqlite(connection);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<CrowdfundCompany>().HasMany(c => c.Images).WithMany();
      modelBuilder.Entity<ShopSlot>().HasMany(c => c.Images).WithMany();
      base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
      ModifyEntity();
      return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges()
    {
      ModifyEntity();
      return base.SaveChanges();
    }

    private void ModifyEntity()
    {
      foreach (var entry in ChangeTracker
       .Entries()
       .Where(entry => (entry.State == EntityState.Added || entry.State == EntityState.Modified) && entry.Entity is BaseModel)
     )
      {
        var entity = (BaseModel)entry.Entity;
        var now = DateTime.UtcNow;

        if (entry.State == EntityState.Added)
        {
          entity.CreatedAt = now;
        }
        else
        {
          entry.Property(nameof(entity.CreatedAt)).IsModified = false;
        }

        entity.UpdatedAt = now;
      }
    }

  }
}
