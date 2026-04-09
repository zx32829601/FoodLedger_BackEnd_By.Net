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
    public DbSet<UserAccount> UserAccounts { get; set; }
    public DbSet<SimpleFood> SimpleFoods { get; set; }
    public DbSet<SimpleFoodTranslation> SimpleFoodTranslations { get; set; }
    public DbSet<SimpleFoodCategory> SimpleFoodCategories { get; set; }
    public DbSet<FoodCategory> FoodCategories { get; set; }
    public DbSet<FoodCategoryTranslation> FoodCategoryTranslations { get; set; }

    public DbSet<FoodNutrient> FoodNutrients { get; set; }
    public DbSet<Nutrient> Nutrients { get; set; }
    public DbSet<NutrientTranslation> NutrientTranslations { get; set; }




    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DailyRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId);

            // 讓 created_at 在資料庫端自動生成 now()
            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.ModifiedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.RecordId)
                  .UseIdentityByDefaultColumn();
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasKey(e => e.UserId);
            
            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.ModifiedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UserId)
                  .UseIdentityByDefaultColumn();
        });

        modelBuilder.Entity<SimpleFoodTranslation>(entity =>
        {
            entity.HasKey(e => e.TranslationId);

            entity.Property(e => e.TranslationId)
                  .UseIdentityByDefaultColumn();

            //設定複合索引(FoodId, LangCode)，確保同一食物在同一語言下只有一筆翻譯
            entity.HasIndex(e => new { e.FoodId, e.LangCode })
              .IsUnique()
              .HasDatabaseName("ix_food_translation_food_id_lang_code");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ModifiedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<SimpleFood>(entity =>
        {
            entity.HasKey(e => e.FoodId);
            entity.Property(e => e.FoodId).UseIdentityByDefaultColumn();

            // 設定 FoodCode 為唯一索引，確保每個食物代碼只能有一筆資料
            entity.HasIndex(e => e.FoodCode).IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ModifiedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<SimpleFoodCategory>(entity =>
        {
            // 設定複合主鍵，確保同一食物和類別的組合只能有一筆資料
            entity.HasKey(fc => new { fc.FoodId, fc.CategoryId });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ModifiedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<FoodCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.CategoryId).UseIdentityByDefaultColumn();

            // 設定 CategoryCode 為唯一索引，確保每個類別只能有一筆資料
            entity.HasIndex(e => e.CategoryCode).IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ModifiedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<FoodCategoryTranslation>(entity =>
        {
            entity.HasKey(e => e.TranslationId);

            entity.Property(e => e.TranslationId)
                  .UseIdentityByDefaultColumn();

            //設定複合索引(CategoryId, LangCode)，確保同一類別在同一語言下只有一筆翻譯
            entity.HasIndex(e => new { e.CategoryId, e.LangCode })
              .IsUnique()
              .HasDatabaseName("idx_category_translation_category_id_lang_code");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ModifiedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<FoodNutrient>(entity =>
        {
            // 設定複合主鍵，確保同一食物和營養素的組合只能有一筆資料
            entity.HasKey(fc => new { fc.FoodId, fc.NutrientId });

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ModifiedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Nutrient>(entity =>
        {
            entity.HasKey(e => e.NutrientId);

            entity.Property(e => e.NutrientId)
                  .UseIdentityByDefaultColumn();

            // 設定 NutrientCode 為唯一索引，確保每個營養素代碼只能有一筆資料
            entity.HasIndex(e => e.NutrientCode).IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ModifiedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<NutrientTranslation>(entity =>
        {
            entity.HasKey(e => e.TranslationId);

            entity.Property(e => e.TranslationId)
                  .UseIdentityByDefaultColumn();

            //設定複合索引(NutrientId, LangCode)，確保同一營養素在同一語言下只有一筆翻譯
            entity.HasIndex(e => new { e.NutrientId, e.LangCode })
              .IsUnique()
              .HasDatabaseName("idx_category_translation_nutrient_id_lang_code");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ModifiedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
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