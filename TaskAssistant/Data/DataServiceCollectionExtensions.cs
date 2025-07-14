using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory; // 添加此命名空间以解决 UseInMemoryDatabase 方法未找到的问题
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using TaskAssistant.Data.Repositories;
using TaskAssistant.Data.Services;
using TaskAssistant.Models;
using TaskAssistant.Services;

namespace TaskAssistant.Data
{
    /// <summary>
    /// 数据层依赖注入配置
    /// 负责注册数据访问相关的服务
    /// </summary>
    public static class DataServiceCollectionExtensions
    {
        /// <summary>
        /// 添加数据访问服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDataServices(this IServiceCollection services, string? connectionString = null)
        {
            // 如果没有提供连接字符串，使用默认配置
            connectionString ??= GetDefaultConnectionString();

            // 注册 DbContext
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(connectionString);
                
                // 开发环境下启用敏感数据日志记录
                #if DEBUG
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
                #endif
            });

            // 注册仓储服务
            services.AddScoped<IRepository<ScriptInfo>, Repository<ScriptInfo>>();
            services.AddScoped<IRepository<TaskInfo>, Repository<TaskInfo>>();
            services.AddScoped<IScriptRepository, ScriptRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();

            // 注册数据服务
            services.AddScoped<IDataService, DataService>();

            // 注册脚本执行服务
            services.AddScoped<IScriptExecutionService, ScriptExecutionService>();

            return services;
        }

        /// <summary>
        /// 添加数据访问服务（使用内存数据库，主要用于测试）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="databaseName">内存数据库名称</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddInMemoryDataServices(this IServiceCollection services, string databaseName = "TaskAssistantTestDb")
        {
            // 注册内存数据库 DbContext
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // 注册仓储服务
            services.AddScoped<IRepository<ScriptInfo>, Repository<ScriptInfo>>();
            services.AddScoped<IRepository<TaskInfo>, Repository<TaskInfo>>();
            services.AddScoped<IScriptRepository, ScriptRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();

            // 注册数据服务
            services.AddScoped<IDataService, DataService>();

            // 注册脚本执行服务
            services.AddScoped<IScriptExecutionService, ScriptExecutionService>();

            return services;
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        /// <returns>异步任务</returns>
        public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
            await dataService.InitializeDatabaseAsync();
        }

        /// <summary>
        /// 获取默认数据库连接字符串
        /// </summary>
        /// <returns>连接字符串</returns>
        private static string GetDefaultConnectionString()
        {
            // 获取应用程序数据目录
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "TaskAssistant");
            
            // 确保目录存在
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            // 数据库文件路径
            var dbPath = Path.Combine(appFolder, "TaskAssistant.db");
            
            return $"Data Source={dbPath};Cache=Shared;";
        }
    }
}