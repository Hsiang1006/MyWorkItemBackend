using Microsoft.EntityFrameworkCore;
using MyWorkItemBackend.Entities;

namespace MyWorkItemBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<UserWorkItemStatus> UserWorkItemStatuses => Set<UserWorkItemStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 設定 UserRole 複合主鍵
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        // 設定個人化狀態表複合主鍵 
        modelBuilder.Entity<UserWorkItemStatus>()
            .HasKey(s => new { s.UserId, s.WorkItemId });

        // 設定 WorkItem 標題必填與長度限制 [cite: 451, 472]
        modelBuilder.Entity<WorkItem>()
            .Property(w => w.Title).IsRequired().HasMaxLength(200);

        // 加入 CreatedAt 索引以加速排序查詢 
        modelBuilder.Entity<WorkItem>()
            .HasIndex(w => w.CreatedAt);

        // 1. 設定資料庫自動產生 UUID
        modelBuilder.Entity<User>().Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
        modelBuilder.Entity<Role>().Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");
        modelBuilder.Entity<WorkItem>().Property(w => w.WorkItemId).HasDefaultValueSql("gen_random_uuid()");

        // 2. 設定資料庫預設時間 (當新增資料時自動產生目前 UTC 時間)
        modelBuilder.Entity<User>().Property(u => u.CreatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        modelBuilder.Entity<User>().Property(u => u.UpdatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        
        modelBuilder.Entity<Role>().Property(r => r.CreatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        modelBuilder.Entity<Role>().Property(r => r.UpdatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        
        modelBuilder.Entity<WorkItem>().Property(w => w.CreatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        modelBuilder.Entity<WorkItem>().Property(w => w.UpdatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        modelBuilder.Entity<UserWorkItemStatus>().Property(s => s.CreatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        modelBuilder.Entity<UserWorkItemStatus>().Property(s => s.UpdatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        // 3. 針對 WorkItem 設定全域軟刪除過濾器
        modelBuilder.Entity<WorkItem>().HasQueryFilter(w => !w.IsDeleted);
    }

    public override int SaveChanges()
    {
        ProcessSave();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ProcessSave();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ProcessSave()
    {
        var entries = ChangeTracker.Entries();
        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // 設定建立時間與更新時間
                var createdAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                if (createdAtProp != null) createdAtProp.CurrentValue = utcNow;

                var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProp != null) updatedAtProp.CurrentValue = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                // 僅設定更新時間
                var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProp != null) updatedAtProp.CurrentValue = utcNow;
            }
            else if (entry.State == EntityState.Deleted)
            {
                // 針對有 IsDeleted 屬性的實體進行軟刪除 (目前僅 WorkItem)
                if (entry.Entity is WorkItem workItem)
                {
                    entry.State = EntityState.Modified;
                    workItem.IsDeleted = true;
                    workItem.DeletedAt = utcNow;
                    workItem.UpdatedAt = utcNow;
                }
            }
        }
    }
}