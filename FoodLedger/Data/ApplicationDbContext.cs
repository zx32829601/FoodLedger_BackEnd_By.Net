using FoodLedger.Data.Entities;
using FoodLedger.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// FoodLedger 的 EF Core 資料存取入口，整合 Identity、領域實體設定與稽核欄位維護。
/// </summary>
/// <remarks>
/// 寫入 <see cref="BaseEntity" /> 時會統一設定建立與修改時間，並以目前登入者作為異動者；
/// 無 HTTP 使用者的 migration、seed 或測試流程則使用 <c>System</c>。
/// </remarks>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<long>, long>
{
    private const string SystemActor = "System";

    private readonly ICurrentUserService? _currentUserService;

    /// <summary>
    /// 初始化 FoodLedger 資料庫內容。
    /// </summary>
    /// <param name="options">EF Core 資料庫連線與 provider 設定。</param>
    /// <param name="currentUserService">目前登入者資訊；非 HTTP 流程可省略。</param>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<DailyRecord> DailyRecords => Set<DailyRecord>();
    public DbSet<BodyProfile> BodyProfiles => Set<BodyProfile>();
    public DbSet<DefinedCode> DefinedCodes => Set<DefinedCode>();
    public DbSet<DefinedCodeTranslation> DefinedCodeTranslations =>
        Set<DefinedCodeTranslation>();
    public DbSet<SimpleFood> SimpleFoods => Set<SimpleFood>();
    public DbSet<SimpleFoodTranslation> SimpleFoodTranslations => Set<SimpleFoodTranslation>();
    public DbSet<SimpleFoodCategory> SimpleFoodCategories => Set<SimpleFoodCategory>();
    public DbSet<FoodCategory> FoodCategories => Set<FoodCategory>();
    public DbSet<FoodCategoryTranslation> FoodCategoryTranslations => Set<FoodCategoryTranslation>();
    public DbSet<FoodNutrient> FoodNutrients => Set<FoodNutrient>();
    public DbSet<Nutrient> Nutrients => Set<Nutrient>();
    public DbSet<NutrientTranslation> NutrientTranslations => Set<NutrientTranslation>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("application_user");

            entity.Property(e => e.Id)
                .HasColumnName("user_id");

            entity.Property(e => e.DisplayName)
                .HasMaxLength(50)
                .HasColumnName("display_name");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.Property(e => e.ModifiedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("modified_at");

            entity.Property(e => e.UserName).HasColumnName("user_name");
            entity.Property(e => e.NormalizedUserName).HasColumnName("normalized_user_name");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.NormalizedEmail).HasColumnName("normalized_email");
            entity.Property(e => e.EmailConfirmed).HasColumnName("email_confirmed");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.SecurityStamp).HasColumnName("security_stamp");
            entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp");
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");
            entity.Property(e => e.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
            entity.Property(e => e.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            entity.Property(e => e.LockoutEnd).HasColumnName("lockout_end");
            entity.Property(e => e.LockoutEnabled).HasColumnName("lockout_enabled");
            entity.Property(e => e.AccessFailedCount).HasColumnName("access_failed_count");

            entity.HasIndex(e => e.NormalizedEmail)
                .HasDatabaseName("ix_application_user_normalized_email");

            entity.HasIndex(e => e.NormalizedUserName)
                .IsUnique()
                .HasDatabaseName("ix_application_user_normalized_user_name");
        });

        modelBuilder.Entity<IdentityRole<long>>(entity =>
        {
            entity.ToTable("application_role");
            entity.Property(e => e.Id).HasColumnName("role_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.NormalizedName).HasColumnName("normalized_name");
            entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp");

            entity.HasIndex(e => e.NormalizedName)
                .IsUnique()
                .HasDatabaseName("ix_application_role_normalized_name");
        });

        modelBuilder.Entity<IdentityUserRole<long>>(entity =>
        {
            entity.ToTable("application_user_role");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
        });

        modelBuilder.Entity<IdentityUserClaim<long>>(entity =>
        {
            entity.ToTable("application_user_claim");
            entity.Property(e => e.Id).HasColumnName("user_claim_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ClaimType).HasColumnName("claim_type");
            entity.Property(e => e.ClaimValue).HasColumnName("claim_value");
        });

        modelBuilder.Entity<IdentityUserLogin<long>>(entity =>
        {
            entity.ToTable("application_user_login");
            entity.Property(e => e.LoginProvider)
                .HasMaxLength(128)
                .HasColumnName("login_provider");
            entity.Property(e => e.ProviderKey)
                .HasMaxLength(128)
                .HasColumnName("provider_key");
            entity.Property(e => e.ProviderDisplayName).HasColumnName("provider_display_name");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<IdentityRoleClaim<long>>(entity =>
        {
            entity.ToTable("application_role_claim");
            entity.Property(e => e.Id).HasColumnName("role_claim_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.ClaimType).HasColumnName("claim_type");
            entity.Property(e => e.ClaimValue).HasColumnName("claim_value");
        });

        modelBuilder.Entity<IdentityUserToken<long>>(entity =>
        {
            entity.ToTable("application_user_token");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.LoginProvider)
                .HasMaxLength(128)
                .HasColumnName("login_provider");
            entity.Property(e => e.Name)
                .HasMaxLength(128)
                .HasColumnName("name");
            entity.Property(e => e.Value).HasColumnName("value");
        });
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditValues();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditValues();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditValues()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = string.IsNullOrWhiteSpace(_currentUserService?.UserName)
            ? SystemActor
            : _currentUserService!.UserName!;
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Added)
            {
                entityEntry.Entity.CreatedAt = now;
                entityEntry.Entity.CreatedBy = actor;
                entityEntry.Entity.ModifiedAt = now;
                continue;
            }

            entityEntry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
            entityEntry.Property(nameof(BaseEntity.CreatedBy)).IsModified = false;
            entityEntry.Entity.ModifiedAt = now;
            entityEntry.Entity.ModifiedBy = actor;
        }

        var userEntries = ChangeTracker.Entries<ApplicationUser>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var userEntry in userEntries)
        {
            if (userEntry.State == EntityState.Added)
            {
                userEntry.Entity.CreatedAt = now;
                userEntry.Entity.ModifiedAt = now;
                continue;
            }

            userEntry.Property(nameof(ApplicationUser.CreatedAt)).IsModified = false;
            userEntry.Entity.ModifiedAt = now;
        }
    }
}
