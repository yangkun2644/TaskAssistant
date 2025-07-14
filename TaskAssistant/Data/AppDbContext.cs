using Microsoft.EntityFrameworkCore;
using TaskAssistant.Models;

namespace TaskAssistant.Data
{
    /// <summary>
    /// 应用程序数据库上下文
    /// 负责管理所有数据库实体和数据库连接
    /// </summary>
    public class AppDbContext : DbContext
    {
        #region 构造函数

        /// <summary>
        /// 初始化数据库上下文
        /// </summary>
        /// <param name="options">数据库上下文选项</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        #endregion

        #region DbSet 属性

        /// <summary>
        /// 脚本信息数据集
        /// </summary>
        public DbSet<ScriptInfo> Scripts { get; set; }

        /// <summary>
        /// 任务信息数据集
        /// </summary>
        public DbSet<TaskInfo> Tasks { get; set; }

        /// <summary>
        /// 脚本执行日志数据集
        /// </summary>
        public DbSet<ScriptExecutionLog> ScriptExecutionLogs { get; set; }

        /// <summary>
        /// 应用程序设置数据集
        /// </summary>
        public DbSet<AppSettings> AppSettings { get; set; }

        #endregion

        #region 模型配置

        /// <summary>
        /// 配置数据库模型
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置脚本信息实体
            ConfigureScriptInfo(modelBuilder);

            // 配置任务信息实体
            ConfigureTaskInfo(modelBuilder);

            // 配置脚本执行日志实体
            ConfigureScriptExecutionLog(modelBuilder);

            // 配置应用程序设置实体
            ConfigureAppSettings(modelBuilder);
        }

        /// <summary>
        /// 配置脚本信息实体
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

            // 配置默认值
            scriptEntity.Property(s => s.CreatedAt)
                        .HasDefaultValueSql("datetime('now')");

            scriptEntity.Property(s => s.LastModified)
                        .HasDefaultValueSql("datetime('now')");

            scriptEntity.Property(s => s.IsEnabled)
                        .HasDefaultValue(true);

            scriptEntity.Property(s => s.ExecutionCount)
                        .HasDefaultValue(0);

            scriptEntity.Property(s => s.Category)
                        .HasDefaultValue("默认");

            scriptEntity.Property(s => s.Tags)
                        .HasDefaultValue("[]");

            scriptEntity.Property(s => s.Version)
                        .HasDefaultValue("1.0.0");
        }

        /// <summary>
        /// 配置任务信息实体
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        private static void ConfigureTaskInfo(ModelBuilder modelBuilder)
        {
            var taskEntity = modelBuilder.Entity<TaskInfo>();

            // 配置外键关系
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

            // 配置默认值
            taskEntity.Property(t => t.CreatedAt)
                      .HasDefaultValueSql("datetime('now')");

            taskEntity.Property(t => t.LastModified)
                      .HasDefaultValueSql("datetime('now')");

            taskEntity.Property(t => t.Status)
                      .HasDefaultValue("待执行");

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

        /// <summary>
        /// 配置脚本执行日志实体
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        private static void ConfigureScriptExecutionLog(ModelBuilder modelBuilder)
        {
            var logEntity = modelBuilder.Entity<ScriptExecutionLog>();

            // 配置外键关系
            logEntity.HasOne(l => l.Script)
                     .WithMany()
                     .HasForeignKey(l => l.ScriptId)
                     .OnDelete(DeleteBehavior.SetNull);

            logEntity.HasOne(l => l.Task)
                     .WithMany()
                     .HasForeignKey(l => l.TaskId)
                     .OnDelete(DeleteBehavior.SetNull);

            // 配置索引
            logEntity.HasIndex(l => l.ScriptId)
                     .HasDatabaseName("IX_ScriptExecutionLogs_ScriptId");

            logEntity.HasIndex(l => l.TaskId)
                     .HasDatabaseName("IX_ScriptExecutionLogs_TaskId");

            logEntity.HasIndex(l => l.Status)
                     .HasDatabaseName("IX_ScriptExecutionLogs_Status");

            logEntity.HasIndex(l => l.StartTime)
                     .HasDatabaseName("IX_ScriptExecutionLogs_StartTime");

            logEntity.HasIndex(l => l.EndTime)
                     .HasDatabaseName("IX_ScriptExecutionLogs_EndTime");

            // 配置默认值
            logEntity.Property(l => l.StartTime)
                     .HasDefaultValueSql("datetime('now')");

            logEntity.Property(l => l.Status)
                     .HasDefaultValue("Running");

            logEntity.Property(l => l.Duration)
                     .HasDefaultValue(0);

            logEntity.Property(l => l.Output)
                     .HasDefaultValue("");

            logEntity.Property(l => l.ErrorOutput)
                     .HasDefaultValue("");

            logEntity.Property(l => l.NuGetPackages)
                     .HasDefaultValue("[]");

            logEntity.Property(l => l.MachineName)
                     .HasDefaultValue(Environment.MachineName);

            logEntity.Property(l => l.UserName)
                     .HasDefaultValue(Environment.UserName);
        }

        /// <summary>
        /// 配置应用程序设置实体
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        private static void ConfigureAppSettings(ModelBuilder modelBuilder)
        {
            var settingsEntity = modelBuilder.Entity<AppSettings>();

            // 配置唯一索引
            settingsEntity.HasIndex(s => s.Key)
                          .IsUnique()
                          .HasDatabaseName("IX_AppSettings_Key");

            // 配置其他索引
            settingsEntity.HasIndex(s => s.CreatedAt)
                          .HasDatabaseName("IX_AppSettings_CreatedAt");

            settingsEntity.HasIndex(s => s.LastModified)
                          .HasDatabaseName("IX_AppSettings_LastModified");

            // 配置默认值
            settingsEntity.Property(s => s.CreatedAt)
                          .HasDefaultValueSql("datetime('now')");

            settingsEntity.Property(s => s.LastModified)
                          .HasDefaultValueSql("datetime('now')");
        }

        #endregion

        #region 数据库生命周期

        /// <summary>
        /// 保存更改前的处理
        /// 自动更新时间戳等
        /// </summary>
        /// <returns>受影响的行数</returns>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// 异步保存更改前的处理
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>受影响的行数</returns>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// 更新时间戳
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

                if (entry.Entity is AppSettings appSettings)
                {
                    if (entry.State == EntityState.Added)
                    {
                        appSettings.CreatedAt = DateTime.Now;
                    }
                    appSettings.LastModified = DateTime.Now;
                }
            }
        }

        #endregion
    }
}