using Microsoft.EntityFrameworkCore;
using TaskAssistant.Models;

namespace TaskAssistant.Data
{
    /// <summary>
    /// ?用程序?据?上下文
    /// ??管理所有?据??体和?据??接
    /// </summary>
    public class AppDbContext : DbContext
    {
        #region 构造函?

        /// <summary>
        /// 初始化?据?上下文
        /// </summary>
        /// <param name="options">?据?上下文??</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        #endregion

        #region DbSet ?性

        /// <summary>
        /// ?本信息?据集
        /// </summary>
        public DbSet<ScriptInfo> Scripts { get; set; }

        /// <summary>
        /// 任?信息?据集
        /// </summary>
        public DbSet<TaskInfo> Tasks { get; set; }

        #endregion

        #region 模型配置

        /// <summary>
        /// 配置?据?模型
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置?本信息?体
            ConfigureScriptInfo(modelBuilder);

            // 配置任?信息?体
            ConfigureTaskInfo(modelBuilder);
        }

        /// <summary>
        /// 配置?本信息?体
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        private static void ConfigureScriptInfo(ModelBuilder modelBuilder)
        {
            var scriptEntity = modelBuilder.Entity<ScriptInfo>();

            // 配置索引
            scriptEntity.HasIndex(s => s.Name)
                        .IsUnique()
                        .HasDatabaseName("IX_Scripts_Name");

            scriptEntity.HasIndex(s => s.Category)
                        .HasDatabaseName("IX_Scripts_Category");

            scriptEntity.HasIndex(s => s.CreatedAt)
                        .HasDatabaseName("IX_Scripts_CreatedAt");

            scriptEntity.HasIndex(s => s.IsEnabled)
                        .HasDatabaseName("IX_Scripts_IsEnabled");

            // 配置默?值
            scriptEntity.Property(s => s.CreatedAt)
                        .HasDefaultValueSql("datetime('now')");

            scriptEntity.Property(s => s.LastModified)
                        .HasDefaultValueSql("datetime('now')");

            scriptEntity.Property(s => s.IsEnabled)
                        .HasDefaultValue(true);

            scriptEntity.Property(s => s.ExecutionCount)
                        .HasDefaultValue(0);

            scriptEntity.Property(s => s.Category)
                        .HasDefaultValue("默?");

            scriptEntity.Property(s => s.Tags)
                        .HasDefaultValue("[]");

            scriptEntity.Property(s => s.Version)
                        .HasDefaultValue("1.0.0");
        }

        /// <summary>
        /// 配置任?信息?体
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        private static void ConfigureTaskInfo(ModelBuilder modelBuilder)
        {
            var taskEntity = modelBuilder.Entity<TaskInfo>();

            // 配置外??系
            taskEntity.HasOne(t => t.Script)
                      .WithMany()
                      .HasForeignKey(t => t.ScriptId)
                      .OnDelete(DeleteBehavior.SetNull);

            // 配置索引
            taskEntity.HasIndex(t => t.Name)
                      .HasDatabaseName("IX_Tasks_Name");

            taskEntity.HasIndex(t => t.Status)
                      .HasDatabaseName("IX_Tasks_Status");

            taskEntity.HasIndex(t => t.TaskType)
                      .HasDatabaseName("IX_Tasks_TaskType");

            taskEntity.HasIndex(t => t.Priority)
                      .HasDatabaseName("IX_Tasks_Priority");

            taskEntity.HasIndex(t => t.CreatedAt)
                      .HasDatabaseName("IX_Tasks_CreatedAt");

            taskEntity.HasIndex(t => t.ScheduledTime)
                      .HasDatabaseName("IX_Tasks_ScheduledTime");

            taskEntity.HasIndex(t => t.IsEnabled)
                      .HasDatabaseName("IX_Tasks_IsEnabled");

            // 配置默?值
            taskEntity.Property(t => t.CreatedAt)
                      .HasDefaultValueSql("datetime('now')");

            taskEntity.Property(t => t.LastModified)
                      .HasDefaultValueSql("datetime('now')");

            taskEntity.Property(t => t.Status)
                      .HasDefaultValue("待?行");

            taskEntity.Property(t => t.Priority)
                      .HasDefaultValue(5);

            taskEntity.Property(t => t.IsEnabled)
                      .HasDefaultValue(true);

            taskEntity.Property(t => t.Configuration)
                      .HasDefaultValue("{}");

            taskEntity.Property(t => t.ExecutionCount)
                      .HasDefaultValue(0);

            taskEntity.Property(t => t.SuccessCount)
                      .HasDefaultValue(0);

            taskEntity.Property(t => t.FailureCount)
                      .HasDefaultValue(0);
        }

        #endregion

        #region ?据?生命周期

        /// <summary>
        /// 保存更改前的?理
        /// 自?更新??戳等
        /// </summary>
        /// <returns>受影?的行?</returns>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// 异步保存更改前的?理
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>受影?的行?</returns>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// 更新??戳
        /// </summary>
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is ScriptInfo scriptInfo)
                {
                    if (entry.State == EntityState.Added)
                    {
                        scriptInfo.CreatedAt = DateTime.Now;
                    }
                    scriptInfo.LastModified = DateTime.Now;
                }

                if (entry.Entity is TaskInfo taskInfo)
                {
                    if (entry.State == EntityState.Added)
                    {
                        taskInfo.CreatedAt = DateTime.Now;
                    }
                    taskInfo.LastModified = DateTime.Now;
                }
            }
        }

        #endregion
    }
}