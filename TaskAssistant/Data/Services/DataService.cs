using Microsoft.EntityFrameworkCore;
using System.IO;
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

        #region 脚本管理实现

        /// <summary>
        /// 保存脚本
        /// </summary>
        /// <param name="script">脚本信息</param>
        /// <returns>保存后的脚本信息</returns>
        public async Task<ScriptInfo> SaveScriptAsync(ScriptInfo script)
        {
            try
            {
                ScriptInfo result;

                if (script.Id == 0)
                {
                    // 新增脚本
                    // 检查名称是否重复
                    if (await _scriptRepository.IsNameExistsAsync(script.Name))
                    {
                        throw new InvalidOperationException($"脚本名称 '{script.Name}' 已存在");
                    }

                    result = await _scriptRepository.AddAsync(script);
                }
                else
                {
                    // 更新脚本
                    // 检查名称是否重复（排除当前脚本）
                    if (await _scriptRepository.IsNameExistsAsync(script.Name, script.Id))
                    {
                        throw new InvalidOperationException($"脚本名称 '{script.Name}' 已存在");
                    }

                    result = await _scriptRepository.UpdateAsync(script);
                }

                await _scriptRepository.SaveChangesAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存脚本失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取脚本列表（轻量级，不包含Code字段）
        /// </summary>
        /// <param name="category">分类过滤（可选）</param>
        /// <param name="isEnabled">启用状态过滤（可选）</param>
        /// <returns>脚本列表</returns>
        public async Task<IEnumerable<ScriptInfo>> GetScriptsAsync(string? category = null, bool? isEnabled = null)
        {
            try
            {
                // 使用新的轻量级方法
                return await _scriptRepository.GetScriptListAsync(category, isEnabled);
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
                    return await _scriptRepository.GetByCategoryWithCodeAsync(category);
                }

                if (isEnabled.HasValue)
                {
                    return await _scriptRepository.FindAsync(s => s.IsEnabled == isEnabled.Value);
                }

                return await _scriptRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取完整脚本列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除脚本
        /// </summary>
        /// <param name="scriptId">脚本ID</param>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeleteScriptAsync(int scriptId)
        {
            try
            {
                // 检查是否有关联的任务
                var relatedTasks = await _taskRepository.GetByScriptIdAsync(scriptId);
                if (relatedTasks.Any())
                {
                    throw new InvalidOperationException("无法删除脚本，存在关联的任务");
                }

                await _scriptRepository.DeleteAsync(scriptId);
                await _scriptRepository.SaveChangesAsync();
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
            try
            {
                return await _scriptRepository.UpdateExecutionStatsAsync(scriptId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"更新脚本执行统计失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 任务管理实现

        /// <summary>
        /// 保存任务
        /// </summary>
        /// <param name="task">任务信息</param>
        /// <returns>保存后的任务信息</returns>
        public async Task<TaskInfo> SaveTaskAsync(TaskInfo task)
        {
            try
            {
                TaskInfo result;

                if (task.Id == 0)
                {
                    // 新增任务
                    result = await _taskRepository.AddAsync(task);
                }
                else
                {
                    // 更新任务
                    result = await _taskRepository.UpdateAsync(task);
                }

                await _taskRepository.SaveChangesAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存任务失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取任务列表
        /// </summary>
        /// <param name="status">状态过滤（可选）</param>
        /// <param name="isEnabled">启用状态过滤（可选）</param>
        /// <returns>任务列表</returns>
        public async Task<IEnumerable<TaskInfo>> GetTasksAsync(string? status = null, bool? isEnabled = null)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(status))
                {
                    return await _taskRepository.GetByStatusAsync(status);
                }

                if (isEnabled.HasValue)
                {
                    return await _taskRepository.FindAsync(t => t.IsEnabled == isEnabled.Value);
                }

                return await _taskRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取任务列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否删除成功</returns>
        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            try
            {
                // 检查任务是否正在执行
                var task = await _taskRepository.GetByIdAsync(taskId);
                if (task?.Status == "执行中")
                {
                    throw new InvalidOperationException("无法删除正在执行的任务");
                }

                await _taskRepository.DeleteAsync(taskId);
                await _taskRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"删除任务失败: {ex.Message}", ex);
            }
        }

        // 修改以下方法以解决类型转换问题
        /// <summary>
        /// 获取任务统计信息
        /// </summary>
        /// <returns>任务统计信息</returns>
        public async Task<Repositories.TaskStatistics> GetTaskStatisticsAsync()
        {
            try
            {
                // 显式转换 TaskAssistant.Data.Repositories.TaskStatistics 到 TaskAssistant.Models.TaskStatistics
                var repositoryStatistics = await _taskRepository.GetStatisticsAsync();
                return new Repositories.TaskStatistics
                {
                    TotalTasks = repositoryStatistics.TotalTasks,
                    CompletedTasks = repositoryStatistics.CompletedTasks,
                    FailedTasks = repositoryStatistics.FailedTasks,
                    PendingTasks = repositoryStatistics.PendingTasks,
                    RunningTasks = repositoryStatistics.RunningTasks
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取任务统计信息失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 数据库管理实现

        /// <summary>
        /// 初始化数据库
        /// </summary>
        /// <returns>是否初始化成功</returns>
        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                // 检查数据库是否可以连接
                var canConnect = await _context.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    // 数据库存在，检查是否有必要的表
                    try
                    {
                        // 尝试查询Scripts表，如果失败说明表结构有问题
                        await _context.Scripts.CountAsync();
                        await _context.Tasks.CountAsync();
                        
                        // 表结构正常，初始化完成
                        return true;
                    }
                    catch
                    {
                        // 表结构有问题，删除并重新创建数据库
                        await _context.Database.EnsureDeletedAsync();
                    }
                }
                
                // 创建数据库和表结构
                await _context.Database.EnsureCreatedAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"初始化数据库失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 备份数据库
        /// </summary>
        /// <param name="backupPath">备份文件路径</param>
        /// <returns>是否备份成功</returns>
        public async Task<bool> BackupDatabaseAsync(string backupPath)
        {
            try
            {
                // 获取数据库文件路径
                var connectionString = _context.Database.GetConnectionString();
                var dbPath = GetDatabasePath(connectionString);

                if (!File.Exists(dbPath))
                {
                    throw new FileNotFoundException("数据库文件不存在");
                }

                // 确保备份目录存在
                var backupDir = Path.GetDirectoryName(backupPath);
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir!);
                }

                // 复制数据库文件
                await Task.Run(() => File.Copy(dbPath, backupPath, true));

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"备份数据库失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 还原数据库
        /// </summary>
        /// <param name="backupPath">备份文件路径</param>
        /// <returns>是否还原成功</returns>
        public async Task<bool> RestoreDatabaseAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    throw new FileNotFoundException("备份文件不存在");
                }

                // 获取数据库文件路径
                var connectionString = _context.Database.GetConnectionString();
                var dbPath = GetDatabasePath(connectionString);

                // 关闭数据库连接
                await _context.Database.CloseConnectionAsync();

                // 复制备份文件到数据库位置
                await Task.Run(() => File.Copy(backupPath, dbPath, true));

                // 重新打开连接
                await _context.Database.OpenConnectionAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"还原数据库失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 清理过期数据
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        /// <returns>清理的记录数量</returns>
        public async Task<int> CleanupOldDataAsync(int daysToKeep = 90)
        {
            try
            {
                var totalCleaned = 0;

                // 清理已完成的任务
                totalCleaned += await _taskRepository.CleanupCompletedTasksAsync(daysToKeep);

                // 清理未使用的脚本
                totalCleaned += await _scriptRepository.CleanupUnusedScriptsAsync(daysToKeep);

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
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("连接字符串为空");
            }

            // 解析 SQLite 连接字符串
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var keyValue = part.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLower();
                    var value = keyValue[1].Trim();

                    if (key == "data source" || key == "datasource")
                    {
                        return value;
                    }
                }
            }

            throw new ArgumentException("连接字符串中未找到数据库文件路径");
        }

        #endregion
    }
}