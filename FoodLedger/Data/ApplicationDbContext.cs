using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // 定義設定的Table Class
    public DbSet<DailyRecord> DailyRecords { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DailyRecord>(entity =>
        {
            // 讓 created_at 在資料庫端自動生成 now()
            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.ModifiedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.RecordId)
                  .HasDefaultValueSql("gen_random_uuid()");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            ((BaseEntity)entityEntry.Entity).ModifiedAt = DateTimeOffset.UtcNow;

            if (entityEntry.State == EntityState.Added)
            {
                ((BaseEntity)entityEntry.Entity).CreatedAt = DateTimeOffset.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}