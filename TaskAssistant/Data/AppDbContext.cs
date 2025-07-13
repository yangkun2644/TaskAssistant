using Microsoft.EntityFrameworkCore;
using TaskAssistant.Models;

namespace TaskAssistant.Data
{
    /// <summary>
    /// ?�ε{��?�u?�W�U��
    /// ??�޲z�Ҧ�?�u??�^�M?�u??��
    /// </summary>
    public class AppDbContext : DbContext
    {
        #region �۳y��?

        /// <summary>
        /// ��l��?�u?�W�U��
        /// </summary>
        /// <param name="options">?�u?�W�U��??</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        #endregion

        #region DbSet ?��

        /// <summary>
        /// ?���H��?�u��
        /// </summary>
        public DbSet<ScriptInfo> Scripts { get; set; }

        /// <summary>
        /// ��?�H��?�u��
        /// </summary>
        public DbSet<TaskInfo> Tasks { get; set; }

        #endregion

        #region �ҫ��t�m

        /// <summary>
        /// �t�m?�u?�ҫ�
        /// </summary>
        /// <param name="modelBuilder">�ҫ��۫ؾ�</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // �t�m?���H��?�^
            ConfigureScriptInfo(modelBuilder);

            // �t�m��?�H��?�^
            ConfigureTaskInfo(modelBuilder);
        }

        /// <summary>
        /// �t�m?���H��?�^
        /// </summary>
        /// <param name="modelBuilder">�ҫ��۫ؾ�</param>
        private static void ConfigureScriptInfo(ModelBuilder modelBuilder)
        {
            var scriptEntity = modelBuilder.Entity<ScriptInfo>();

            // �t�m����
            scriptEntity.HasIndex(s => s.Name)
                        .IsUnique()
                        .HasDatabaseName("IX_Scripts_Name");

            scriptEntity.HasIndex(s => s.Category)
                        .HasDatabaseName("IX_Scripts_Category");

            scriptEntity.HasIndex(s => s.CreatedAt)
                        .HasDatabaseName("IX_Scripts_CreatedAt");

            scriptEntity.HasIndex(s => s.IsEnabled)
                        .HasDatabaseName("IX_Scripts_IsEnabled");

            // �t�m�q?��
            scriptEntity.Property(s => s.CreatedAt)
                        .HasDefaultValueSql("datetime('now')");

            scriptEntity.Property(s => s.LastModified)
                        .HasDefaultValueSql("datetime('now')");

            scriptEntity.Property(s => s.IsEnabled)
                        .HasDefaultValue(true);

            scriptEntity.Property(s => s.ExecutionCount)
                        .HasDefaultValue(0);

            scriptEntity.Property(s => s.Category)
                        .HasDefaultValue("�q?");

            scriptEntity.Property(s => s.Tags)
                        .HasDefaultValue("[]");

            scriptEntity.Property(s => s.Version)
                        .HasDefaultValue("1.0.0");
        }

        /// <summary>
        /// �t�m��?�H��?�^
        /// </summary>
        /// <param name="modelBuilder">�ҫ��۫ؾ�</param>
        private static void ConfigureTaskInfo(ModelBuilder modelBuilder)
        {
            var taskEntity = modelBuilder.Entity<TaskInfo>();

            // �t�m�~??�t
            taskEntity.HasOne(t => t.Script)
                      .WithMany()
                      .HasForeignKey(t => t.ScriptId)
                      .OnDelete(DeleteBehavior.SetNull);

            // �t�m����
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

            // �t�m�q?��
            taskEntity.Property(t => t.CreatedAt)
                      .HasDefaultValueSql("datetime('now')");

            taskEntity.Property(t => t.LastModified)
                      .HasDefaultValueSql("datetime('now')");

            taskEntity.Property(t => t.Status)
                      .HasDefaultValue("��?��");

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

        #region ?�u?�ͩR�P��

        /// <summary>
        /// �O�s���e��?�z
        /// ��?��s??�W��
        /// </summary>
        /// <returns>���v?����?</returns>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// �ݨB�O�s���e��?�z
        /// </summary>
        /// <param name="cancellationToken">�����O�P</param>
        /// <returns>���v?����?</returns>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// ��s??�W
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