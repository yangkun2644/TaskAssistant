using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Collections.Concurrent;
using TaskAssistant.Data.Repositories;
using TaskAssistant.Models;

namespace TaskAssistant.Data.Services
{
    /// <summary>
    /// 数据服务实现类
    /// 提供统一的数据访问功能，封装仓储层的复杂性
    /// </summary>
    public class DataService : IDataService
    {
        #region 私有字段

        /// <summary>
        /// 数据库上下文
        /// </summary>
        private readonly AppDbContext _context;

        /// <summary>
        /// 脚本仓储
        /// </summary>
        private readonly IScriptRepository _scriptRepository;

        /// <summary>
        /// 任务仓储
        /// </summary>
        private readonly ITaskRepository _taskRepository;

        /// <summary>
        /// 缓存字典，用于临时缓存频繁访问的数据
        /// </summary>
        private readonly ConcurrentDictionary<string, object> _cache = new();

        /// <summary>
        /// 缓存过期时间（分钟）
        /// </summary>
        private const int CacheExpirationMinutes = 5;

        /// <summary>
        /// 缓存项过期时间字典
        /// </summary>
        private readonly ConcurrentDictionary<string, DateTime> _cacheExpiration = new();

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化数据服务
        /// </summary>
        /// <param name="context">数据库上下文</param>
        /// <param name="scriptRepository">脚本仓储</param>
        /// <param name="taskRepository">任务仓储</param>
        public DataService(AppDbContext context, IScriptRepository scriptRepository, ITaskRepository taskRepository)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _scriptRepository = scriptRepository ?? throw new ArgumentNullException(nameof(scriptRepository));
            _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        }

        #endregion

        #region 仓储属性

        /// <summary>
        /// 脚本仓储
        /// </summary>
        public IScriptRepository Scripts => _scriptRepository;

        /// <summary>
        /// 任务仓储
        /// </summary>
        public ITaskRepository Tasks => _taskRepository;

        #endregion

        #region 缓存管理

        /// <summary>
        /// 获取缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存项或默认值</returns>
        private T? GetCachedItem<T>(string key) where T : class
        {
            if (_cache.TryGetValue(key, out var item) && 
                _cacheExpiration.TryGetValue(key, out var expiration) &&
                DateTime.Now < expiration)
            {
                return item as T;
            }

            // 清除过期项
            _cache.TryRemove(key, out _);
            _cacheExpiration.TryRemove(key, out _);
            return null;
        }

        /// <summary>
        /// 设置缓存项
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        private void SetCachedItem(string key, object value)
        {
            _cache[key] = value;
            _cacheExpiration[key] = DateTime.Now.AddMinutes(CacheExpirationMinutes);
        }

        /// <summary>
        /// 清除指定前缀的缓存
        /// </summary>
        /// <param name="prefix">缓存键前缀</param>
        private void ClearCacheByPrefix(string prefix)
        {
            var keysToRemove = _cache.Keys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
                _cacheExpiration.TryRemove(key, out _);
            }
        }

        #endregion

        #region 脚本管理实现

        /// <summary>
        /// 保存脚本（优化版本）
        /// </summary>
        /// <param name="script">脚本信息</param>
        /// <returns>保存后的脚本信息</returns>
        public async Task<ScriptInfo> SaveScriptAsync(ScriptInfo script)
        {
            ArgumentNullException.ThrowIfNull(script);

            try
            {
                ScriptInfo result;

                if (script.Id == 0)
                {
                    // 新增脚本 - 使用异步检查
                    var nameExists = await _scriptRepository.IsNameExistsAsync(script.Name).ConfigureAwait(false);
                    if (nameExists)
                    {
                        throw new InvalidOperationException($"脚本名称 '{script.Name}' 已存在");
                    }

                    result = await _scriptRepository.AddAsync(script).ConfigureAwait(false);
                }
                else
                {
                    // 更新脚本 - 使用异步检查（排除当前脚本）
                    var nameExists = await _scriptRepository.IsNameExistsAsync(script.Name, script.Id).ConfigureAwait(false);
                    if (nameExists)
                    {
                        throw new InvalidOperationException($"脚本名称 '{script.Name}' 已存在");
                    }

                    result = await _scriptRepository.UpdateAsync(script).ConfigureAwait(false);
                }

                await _scriptRepository.SaveChangesAsync().ConfigureAwait(false);
                
                // 清除相关缓存
                ClearCacheByPrefix("scripts_");
                
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存脚本失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取脚本列表（优化版本，支持缓存）
        /// </summary>
        /// <param name="category">分类过滤（可选）</param>
        /// <param name="isEnabled">启用状态过滤（可选）</param>
        /// <returns>脚本列表</returns>
        public async Task<IEnumerable<ScriptInfo>> GetScriptsAsync(string? category = null, bool? isEnabled = null)
        {
            var cacheKey = $"scripts_{category ?? "all"}_{isEnabled?.ToString() ?? "all"}";
            
            // 尝试从缓存获取
            var cachedResult = GetCachedItem<IEnumerable<ScriptInfo>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            try
            {
                // 使用轻量级方法获取数据
                var result = await _scriptRepository.GetScriptListAsync(category, isEnabled).ConfigureAwait(false);
                
                // 缓存结果
                SetCachedItem(cacheKey, result);
                
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取脚本列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取脚本列表（完整版本，包含Code字段）
        /// </summary>
        /// <param name="category">分类过滤（可选）</param>
        /// <param name="isEnabled">启用状态过滤（可选）</param>
        /// <returns>完整脚本列表</returns>
        public async Task<IEnumerable<ScriptInfo>> GetScriptsWithCodeAsync(string? category = null, bool? isEnabled = null)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(category))
                {
                    return await _scriptRepository.GetByCategoryWithCodeAsync(category).ConfigureAwait(false);
                }

                if (isEnabled.HasValue)
                {
                    return await _scriptRepository.FindAsync(s => s.IsEnabled == isEnabled.Value).ConfigureAwait(false);
                }

                return await _scriptRepository.GetAllAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取完整脚本列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除脚本（优化版本）
        /// </summary>
        /// <param name="scriptId">脚本ID</param>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeleteScriptAsync(int scriptId)
        {
            if (scriptId <= 0)
                throw new ArgumentException("脚本ID必须大于0", nameof(scriptId));

            try
            {
                // 使用异步方法检查关联任务
                var relatedTasks = await _taskRepository.GetByScriptIdAsync(scriptId).ConfigureAwait(false);
                if (relatedTasks.Any())
                {
                    throw new InvalidOperationException("无法删除脚本，存在关联的任务");
                }

                await _scriptRepository.DeleteAsync(scriptId).ConfigureAwait(false);
                await _scriptRepository.SaveChangesAsync().ConfigureAwait(false);
                
                // 清除相关缓存
                ClearCacheByPrefix("scripts_");
                
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"删除脚本失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行脚本（更新统计信息）
        /// </summary>
        /// <param name="scriptId">脚本ID</param>
        /// <returns>更新后的脚本信息</returns>
        public async Task<ScriptInfo?> ExecuteScriptAsync(int scriptId)
        {
            if (scriptId <= 0)
                throw new ArgumentException("脚本ID必须大于0", nameof(scriptId));

            try
            {
                var result = await _scriptRepository.UpdateExecutionStatsAsync(scriptId).ConfigureAwait(false);
                
                // 清除相关缓存
                ClearCacheByPrefix("scripts_");
                
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"更新脚本执行统计失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 任务管理实现

        /// <summary>
        /// 保存任务（优化版本）
        /// </summary>
        /// <param name="task">任务信息</param>
        /// <returns>保存后的任务信息</returns>
        public async Task<TaskInfo> SaveTaskAsync(TaskInfo task)
        {
            ArgumentNullException.ThrowIfNull(task);

            try
            {
                TaskInfo result;

                if (task.Id == 0)
                {
                    result = await _taskRepository.AddAsync(task).ConfigureAwait(false);
                }
                else
                {
                    result = await _taskRepository.UpdateAsync(task).ConfigureAwait(false);
                }

                await _taskRepository.SaveChangesAsync().ConfigureAwait(false);
                
                // 清除相关缓存
                ClearCacheByPrefix("tasks_");
                
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存任务失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取任务列表（优化版本，支持缓存）
        /// </summary>
        /// <param name="status">状态过滤（可选）</param>
        /// <param name="isEnabled">启用状态过滤（可选）</param>
        /// <returns>任务列表</returns>
        public async Task<IEnumerable<TaskInfo>> GetTasksAsync(string? status = null, bool? isEnabled = null)
        {
            var cacheKey = $"tasks_{status ?? "all"}_{isEnabled?.ToString() ?? "all"}";
            
            // 尝试从缓存获取
            var cachedResult = GetCachedItem<IEnumerable<TaskInfo>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            try
            {
                IEnumerable<TaskInfo> result;

                if (!string.IsNullOrWhiteSpace(status))
                {
                    result = await _taskRepository.GetByStatusAsync(status).ConfigureAwait(false);
                }
                else if (isEnabled.HasValue)
                {
                    result = await _taskRepository.FindAsync(t => t.IsEnabled == isEnabled.Value).ConfigureAwait(false);
                }
                else
                {
                    result = await _taskRepository.GetAllAsync().ConfigureAwait(false);
                }

                // 缓存结果
                SetCachedItem(cacheKey, result);
                
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取任务列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除任务（优化版本）
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            if (taskId <= 0)
                throw new ArgumentException("任务ID必须大于0", nameof(taskId));

            try
            {
                // 检查任务是否正在执行
                var task = await _taskRepository.GetByIdAsync(taskId).ConfigureAwait(false);
                if (task?.Status == "执行中")
                {
                    throw new InvalidOperationException("无法删除正在执行的任务");
                }

                await _taskRepository.DeleteAsync(taskId).ConfigureAwait(false);
                await _taskRepository.SaveChangesAsync().ConfigureAwait(false);
                
                // 清除相关缓存
                ClearCacheByPrefix("tasks_");
                
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"删除任务失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取任务统计信息（优化版本，支持缓存）
        /// </summary>
        /// <returns>任务统计信息</returns>
        public async Task<Repositories.TaskStatistics> GetTaskStatisticsAsync()
        {
            const string cacheKey = "task_statistics";
            
            // 尝试从缓存获取
            var cachedResult = GetCachedItem<Repositories.TaskStatistics>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            try
            {
                var repositoryStatistics = await _taskRepository.GetStatisticsAsync().ConfigureAwait(false);
                var result = new Repositories.TaskStatistics
                {
                    TotalTasks = repositoryStatistics.TotalTasks,
                    CompletedTasks = repositoryStatistics.CompletedTasks,
                    FailedTasks = repositoryStatistics.FailedTasks,
                    PendingTasks = repositoryStatistics.PendingTasks,
                    RunningTasks = repositoryStatistics.RunningTasks
                };

                // 缓存结果（统计信息缓存时间较短）
                _cache[cacheKey] = result;
                _cacheExpiration[cacheKey] = DateTime.Now.AddMinutes(1);

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取任务统计信息失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 设置管理实现

        /// <summary>
        /// 获取设置值（优化版本）
        /// </summary>
        /// <param name="key">设置键名</param>
        /// <returns>设置值</returns>
        public async Task<string?> GetSettingAsync(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            try
            {
                var setting = await _context.AppSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Key == key)
                    .ConfigureAwait(false);
                    
                return setting?.Value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取设置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 保存设置值（优化版本）
        /// </summary>
        /// <param name="key">设置键名</param>
        /// <param name="value">设置值</param>
        /// <param name="description">设置描述</param>
        public async Task SaveSettingAsync(string key, string value, string? description = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(value);

            try
            {
                var existing = await _context.AppSettings
                    .FirstOrDefaultAsync(s => s.Key == key)
                    .ConfigureAwait(false);

                if (existing != null)
                {
                    existing.Value = value;
                    existing.LastModified = DateTime.Now;
                    if (!string.IsNullOrEmpty(description))
                        existing.Description = description;
                }
                else
                {
                    _context.AppSettings.Add(new AppSettings
                    {
                        Key = key,
                        Value = value,
                        Description = description,
                        CreatedAt = DateTime.Now,
                        LastModified = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存设置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除设置（优化版本）
        /// </summary>
        /// <param name="key">设置键名</param>
        public async Task DeleteSettingAsync(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            try
            {
                var setting = await _context.AppSettings
                    .FirstOrDefaultAsync(s => s.Key == key)
                    .ConfigureAwait(false);

                if (setting != null)
                {
                    _context.AppSettings.Remove(setting);
                    await _context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"删除设置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取所有设置（优化版本）
        /// </summary>
        /// <returns>设置列表</returns>
        public async Task<IEnumerable<AppSettings>> GetAllSettingsAsync()
        {
            try
            {
                return await _context.AppSettings
                    .AsNoTracking()
                    .OrderBy(s => s.Key)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取所有设置失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 数据库管理实现

        /// <summary>
        /// 初始化数据库（优化版本）
        /// </summary>
        /// <returns>是否初始化成功</returns>
        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                // 检查数据库连接
                var canConnect = await _context.Database.CanConnectAsync().ConfigureAwait(false);
                
                if (canConnect)
                {
                    // 验证表结构完整性
                    if (await ValidateTableStructureAsync().ConfigureAwait(false))
                    {
                        return true;
                    }
                    
                    // 表结构不完整，重新创建
                    await RecreateDatabase().ConfigureAwait(false);
                    return true;
                }
                
                // 数据库不存在，创建新数据库
                await _context.Database.EnsureCreatedAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"初始化数据库失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 验证表结构完整性
        /// </summary>
        /// <returns>表结构是否完整</returns>
        private async Task<bool> ValidateTableStructureAsync()
        {
            try
            {
                // 并行检查所有表
                var tasks = new[]
                {
                    CheckTableExistsAsync(() => _context.Scripts.CountAsync()),
                    CheckTableExistsAsync(() => _context.Tasks.CountAsync()),
                    CheckTableExistsAsync(() => _context.ScriptExecutionLogs.CountAsync()),
                    CheckTableExistsAsync(() => _context.AppSettings.CountAsync())
                };

                var results = await Task.WhenAll(tasks).ConfigureAwait(false);
                return results.All(r => r);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查表是否存在
        /// </summary>
        /// <param name="operation">检查操作</param>
        /// <returns>表是否存在</returns>
        private static async Task<bool> CheckTableExistsAsync(Func<Task<int>> operation)
        {
            try
            {
                await operation().ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 重新创建数据库（优化版本）
        /// </summary>
        private async Task RecreateDatabase()
        {
            try
            {
                // 备份现有数据
                var (scripts, tasks, settings) = await BackupExistingDataAsync().ConfigureAwait(false);

                // 重新创建数据库
                await _context.Database.EnsureDeletedAsync().ConfigureAwait(false);
                await _context.Database.EnsureCreatedAsync().ConfigureAwait(false);

                // 恢复数据
                await RestoreDataAsync(scripts, tasks, settings).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"重新创建数据库失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 备份现有数据
        /// </summary>
        /// <returns>备份的数据</returns>
        private async Task<(List<ScriptInfo> scripts, List<TaskInfo> tasks, List<AppSettings> settings)> BackupExistingDataAsync()
        {
            var scripts = new List<ScriptInfo>();
            var tasks = new List<TaskInfo>();
            var settings = new List<AppSettings>();

            try
            {
                scripts = await _context.Scripts.AsNoTracking().ToListAsync().ConfigureAwait(false);
            }
            catch { /* 忽略错误 */ }

            try
            {
                tasks = await _context.Tasks.AsNoTracking().ToListAsync().ConfigureAwait(false);
            }
            catch { /* 忽略错误 */ }

            try
            {
                settings = await _context.AppSettings.AsNoTracking().ToListAsync().ConfigureAwait(false);
            }
            catch { /* 忽略错误 */ }

            return (scripts, tasks, settings);
        }

        /// <summary>
        /// 恢复数据
        /// </summary>
        /// <param name="scripts">脚本数据</param>
        /// <param name="tasks">任务数据</param>
        /// <param name="settings">设置数据</param>
        private async Task RestoreDataAsync(List<ScriptInfo> scripts, List<TaskInfo> tasks, List<AppSettings> settings)
        {
            if (scripts.Count > 0)
            {
                // 重置ID以避免主键冲突
                foreach (var script in scripts)
                {
                    script.Id = 0;
                }
                
                _context.Scripts.AddRange(scripts);
            }

            if (tasks.Count > 0)
            {
                // 重置ID并重新建立脚本关联
                foreach (var task in tasks)
                {
                    task.Id = 0;
                    if (task.ScriptId.HasValue)
                    {
                        var matchingScript = scripts.FirstOrDefault(s => s.Name == task.Script?.Name);
                        task.ScriptId = matchingScript?.Id;
                    }
                }
                
                _context.Tasks.AddRange(tasks);
            }

            if (settings.Count > 0)
            {
                foreach (var setting in settings)
                {
                    setting.Id = 0;
                }
                
                _context.AppSettings.AddRange(settings);
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// 备份数据库（优化版本）
        /// </summary>
        /// <param name="backupPath">备份文件路径</param>
        /// <returns>是否备份成功</returns>
        public async Task<bool> BackupDatabaseAsync(string backupPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);

            try
            {
                var connectionString = _context.Database.GetConnectionString();
                var dbPath = GetDatabasePath(connectionString);

                if (!File.Exists(dbPath))
                {
                    throw new FileNotFoundException("数据库文件不存在");
                }

                // 确保备份目录存在
                var backupDir = Path.GetDirectoryName(backupPath);
                if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                // 使用异步文件复制
                using var sourceStream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                using var destinationStream = new FileStream(backupPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
                
                await sourceStream.CopyToAsync(destinationStream).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"备份数据库失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 还原数据库（优化版本）
        /// </summary>
        /// <param name="backupPath">备份文件路径</param>
        /// <returns>是否还原成功</returns>
        public async Task<bool> RestoreDatabaseAsync(string backupPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);

            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException("备份文件不存在");
            }

            try
            {
                var connectionString = _context.Database.GetConnectionString();
                var dbPath = GetDatabasePath(connectionString);

                // 关闭数据库连接
                await _context.Database.CloseConnectionAsync().ConfigureAwait(false);

                // 使用异步文件复制
                using var sourceStream = new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                using var destinationStream = new FileStream(dbPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
                
                await sourceStream.CopyToAsync(destinationStream).ConfigureAwait(false);

                // 重新打开连接
                await _context.Database.OpenConnectionAsync().ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"还原数据库失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 清理过期数据（优化版本）
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        /// <returns>清理的记录数量</returns>
        public async Task<int> CleanupOldDataAsync(int daysToKeep = 90)
        {
            if (daysToKeep <= 0)
                throw new ArgumentException("保留天数必须大于0", nameof(daysToKeep));

            try
            {
                // 并行执行清理操作
                var cleanupTasks = new[]
                {
                    _taskRepository.CleanupCompletedTasksAsync(daysToKeep),
                    _scriptRepository.CleanupUnusedScriptsAsync(daysToKeep)
                };

                var results = await Task.WhenAll(cleanupTasks).ConfigureAwait(false);
                var totalCleaned = results.Sum();

                // 清除所有缓存
                _cache.Clear();
                _cacheExpiration.Clear();

                return totalCleaned;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"清理过期数据失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 从连接字符串中提取数据库文件路径
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <returns>数据库文件路径</returns>
        private static string GetDatabasePath(string? connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            // 解析 SQLite 连接字符串
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var keyValue = part.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLowerInvariant();
                    var value = keyValue[1].Trim();

                    if (key is "data source" or "datasource")
                    {
                        return value;
                    }
                }
            }

            throw new ArgumentException("连接字符串中未找到数据库文件路径");
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _cache.Clear();
            _cacheExpiration.Clear();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}