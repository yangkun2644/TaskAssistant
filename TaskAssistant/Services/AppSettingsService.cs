using System.Text.Json;
using System.Collections.Concurrent;
using TaskAssistant.Data.Services;
using TaskAssistant.Models;

namespace TaskAssistant.Services
{
    /// <summary>
    /// 应用程序设置服务接口
    /// </summary>
    public interface IAppSettingsService
    {
        /// <summary>
        /// 获取脚本引用设置
        /// </summary>
        Task<ScriptReferenceSettings> GetScriptReferenceSettingsAsync();

        /// <summary>
        /// 保存脚本引用设置
        /// </summary>
        Task SaveScriptReferenceSettingsAsync(ScriptReferenceSettings settings);

        /// <summary>
        /// 获取设置值
        /// </summary>
        Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default);

        /// <summary>
        /// 保存设置值
        /// </summary>
        Task SaveSettingAsync<T>(string key, T value, string? description = null);

        /// <summary>
        /// 重置为默认设置
        /// </summary>
        Task ResetToDefaultsAsync();

        /// <summary>
        /// 批量获取设置
        /// </summary>
        Task<Dictionary<string, T?>> GetSettingsAsync<T>(IEnumerable<string> keys);

        /// <summary>
        /// 批量保存设置
        /// </summary>
        Task SaveSettingsAsync<T>(Dictionary<string, T> settings, string? description = null);

        /// <summary>
        /// 清除缓存
        /// </summary>
        void ClearCache();

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        (int CachedItems, DateTime LastCleanup) GetCacheStatistics();
    }

    /// <summary>
    /// 应用程序设置服务实现（优化版本）
    /// </summary>
    public class AppSettingsService : IAppSettingsService, IDisposable
    {
        #region 常量

        private const string SCRIPT_REFERENCE_SETTINGS_KEY = "ScriptReferenceSettings";
        private const int CacheExpirationMinutes = 30;
        private const int MaxCacheSize = 100;

        #endregion

        #region 私有字段

        private readonly IDataService _dataService;
        private readonly ConcurrentDictionary<string, CachedItem<object>> _cache = new();
        private readonly Timer _cacheCleanupTimer;
        private readonly object _lock = new();
        private readonly JsonSerializerOptions _jsonOptions;
        
        private ScriptReferenceSettings? _cachedScriptSettings;
        private DateTime _lastCacheCleanup = DateTime.Now;
        private bool _disposed;

        #endregion

        #region 构造函数

        public AppSettingsService(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));

            // 配置JSON序列化选项
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            // 启动缓存清理定时器
            _cacheCleanupTimer = new Timer(CleanupExpiredCache, null, 
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

            // 注册到资源管理器
            Common.ResourceManager.RegisterResource(this);
        }

        #endregion

        #region 缓存管理

        /// <summary>
        /// 缓存项
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        private record CachedItem<T>(T Value, DateTime ExpiryTime, int AccessCount = 0)
        {
            public int AccessCount { get; init; } = AccessCount;
            public bool IsExpired => DateTime.Now > ExpiryTime;
        }

        /// <summary>
        /// 获取缓存项
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存的值或默认值</returns>
        private T? GetFromCache<T>(string key)
        {
            if (_cache.TryGetValue(key, out var cachedItem) && !cachedItem.IsExpired)
            {
                // 更新访问计数
                _cache.TryUpdate(key, cachedItem with { AccessCount = cachedItem.AccessCount + 1 }, cachedItem);
                
                try
                {
                    return (T?)cachedItem.Value;
                }
                catch
                {
                    // 类型转换失败，移除缓存项
                    _cache.TryRemove(key, out _);
                }
            }

            return default;
        }

        /// <summary>
        /// 设置缓存项
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expirationMinutes">过期时间（分钟）</param>
        private void SetCache<T>(string key, T value, int expirationMinutes = CacheExpirationMinutes)
        {
            if (value == null) return;

            var expiryTime = DateTime.Now.AddMinutes(expirationMinutes);
            var cachedItem = new CachedItem<object>(value, expiryTime);

            _cache.AddOrUpdate(key, cachedItem, (_, _) => cachedItem);

            // 检查缓存大小
            if (_cache.Count > MaxCacheSize)
            {
                CleanupExpiredCache(null);
            }
        }

        /// <summary>
        /// 清理过期缓存
        /// </summary>
        /// <param name="state">定时器状态</param>
        private void CleanupExpiredCache(object? state)
        {
            if (_disposed) return;

            try
            {
                var expiredKeys = _cache
                    .Where(kvp => kvp.Value.IsExpired)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _cache.TryRemove(key, out _);
                }

                // 如果缓存仍然过大，移除访问次数最少的项
                if (_cache.Count > MaxCacheSize)
                {
                    var leastUsedKeys = _cache
                        .OrderBy(kvp => kvp.Value.AccessCount)
                        .Take(_cache.Count - MaxCacheSize)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in leastUsedKeys)
                    {
                        _cache.TryRemove(key, out _);
                    }
                }

                _lastCacheCleanup = DateTime.Now;

                System.Diagnostics.Debug.WriteLine($"缓存清理完成，移除了 {expiredKeys.Count} 个过期项，当前缓存大小: {_cache.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"缓存清理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            _cachedScriptSettings = null;
            _lastCacheCleanup = DateTime.Now;
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存统计</returns>
        public (int CachedItems, DateTime LastCleanup) GetCacheStatistics()
        {
            return (_cache.Count, _lastCacheCleanup);
        }

        #endregion

        #region 脚本引用设置

        /// <summary>
        /// 获取脚本引用设置（优化版本）
        /// </summary>
        public async Task<ScriptReferenceSettings> GetScriptReferenceSettingsAsync()
        {
            if (_cachedScriptSettings != null)
                return _cachedScriptSettings;

            try
            {
                _cachedScriptSettings = await GetSettingAsync<ScriptReferenceSettings>(SCRIPT_REFERENCE_SETTINGS_KEY)
                    .ConfigureAwait(false) ?? CreateDefaultScriptReferenceSettings();

                return _cachedScriptSettings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取脚本引用设置失败: {ex.Message}");
                return CreateDefaultScriptReferenceSettings();
            }
        }

        /// <summary>
        /// 保存脚本引用设置（优化版本）
        /// </summary>
        public async Task SaveScriptReferenceSettingsAsync(ScriptReferenceSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            try
            {
                await SaveSettingAsync(SCRIPT_REFERENCE_SETTINGS_KEY, settings, "脚本引用配置")
                    .ConfigureAwait(false);
                    
                _cachedScriptSettings = settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存脚本引用设置失败: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 通用设置管理

        /// <summary>
        /// 获取设置值（优化版本，支持缓存）
        /// </summary>
        public async Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            // 尝试从缓存获取
            var cachedValue = GetFromCache<T>(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            try
            {
                var settingValue = await _dataService.GetSettingAsync(key).ConfigureAwait(false);
                if (string.IsNullOrEmpty(settingValue))
                    return defaultValue;

                var deserializedValue = JsonSerializer.Deserialize<T>(settingValue, _jsonOptions);
                
                // 缓存结果
                if (deserializedValue != null)
                {
                    SetCache(key, deserializedValue);
                }

                return deserializedValue ?? defaultValue;
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"反序列化设置 '{key}' 失败: {ex.Message}");
                return defaultValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取设置 '{key}' 失败: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// 保存设置值（优化版本）
        /// </summary>
        public async Task SaveSettingAsync<T>(string key, T value, string? description = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            try
            {
                var jsonValue = JsonSerializer.Serialize(value, _jsonOptions);

                await _dataService.SaveSettingAsync(key, jsonValue, description).ConfigureAwait(false);
                
                // 更新缓存
                SetCache(key, value);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"序列化设置 '{key}' 失败: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存设置 '{key}' 失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量获取设置
        /// </summary>
        public async Task<Dictionary<string, T?>> GetSettingsAsync<T>(IEnumerable<string> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);

            var keyList = keys.ToList();
            var results = new Dictionary<string, T?>();

            // 并行获取设置
            var tasks = keyList.Select(async key =>
            {
                var value = await GetSettingAsync<T>(key).ConfigureAwait(false);
                return new KeyValuePair<string, T?>(key, value);
            });

            var keyValuePairs = await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var kvp in keyValuePairs)
            {
                results[kvp.Key] = kvp.Value;
            }

            return results;
        }

        /// <summary>
        /// 批量保存设置
        /// </summary>
        public async Task SaveSettingsAsync<T>(Dictionary<string, T> settings, string? description = null)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (!settings.Any()) return;

            // 并行保存设置
            var tasks = settings.Select(async kvp =>
            {
                await SaveSettingAsync(kvp.Key, kvp.Value, description).ConfigureAwait(false);
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// 重置为默认设置
        /// </summary>
        public async Task ResetToDefaultsAsync()
        {
            try
            {
                var defaultSettings = CreateDefaultScriptReferenceSettings();
                await SaveScriptReferenceSettingsAsync(defaultSettings).ConfigureAwait(false);
                
                // 清除其他可能的设置缓存
                ClearCache();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"重置默认设置失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 默认设置

        /// <summary>
        /// 创建默认的脚本引用设置
        /// </summary>
        private static ScriptReferenceSettings CreateDefaultScriptReferenceSettings()
        {
            return new ScriptReferenceSettings
            {
                AutoLoadAllAssemblies = true,
                EnableSmartSuggestions = true,
                CommonAssemblies = new List<AssemblyReference>
                {
                    // 基础程序集（优化后的列表）
                    new() { Name = "System.Private.CoreLib", Description = "核心运行时库", IsEnabled = true },
                    new() { Name = "System.Console", Description = "控制台操作", IsEnabled = true },
                    new() { Name = "System.Net.Http", Description = "HTTP客户端", IsEnabled = true },
                    new() { Name = "System.Text.Json", Description = "JSON序列化", IsEnabled = true },
                    new() { Name = "System.IO.FileSystem", Description = "文件系统操作", IsEnabled = true },
                    new() { Name = "System.Data.Common", Description = "数据库操作", IsEnabled = true },
                    new() { Name = "System.Text.RegularExpressions", Description = "正则表达式", IsEnabled = true },
                    new() { Name = "System.Xml.ReaderWriter", Description = "XML处理", IsEnabled = true },
                    new() { Name = "System.Threading.Tasks", Description = "异步任务", IsEnabled = true },
                    new() { Name = "System.Linq", Description = "LINQ查询", IsEnabled = true },
                    new() { Name = "System.Collections", Description = "集合类型", IsEnabled = true },
                    new() { Name = "System.ComponentModel", Description = "组件模型", IsEnabled = true },
                    new() { Name = "System.Diagnostics.Process", Description = "进程管理", IsEnabled = true },
                    new() { Name = "System.Security.Cryptography", Description = "加密功能", IsEnabled = true },
                },
                CommonNuGetPackages = new List<NuGetReference>
                {
                    // 常用的NuGet包（更新版本）
                    new() { PackageId = "Newtonsoft.Json", Version = "13.0.3", Description = "JSON.NET - 强大的JSON处理库", IsEnabled = false },
                    new() { PackageId = "HtmlAgilityPack", Version = "1.11.46", Description = "HTML解析库", IsEnabled = false },
                    new() { PackageId = "Microsoft.Data.Sqlite", Version = "9.0.0", Description = "SQLite数据库", IsEnabled = false },
                    new() { PackageId = "System.Data.SqlClient", Version = "4.8.5", Description = "SQL Server数据库", IsEnabled = false },
                    new() { PackageId = "CsvHelper", Version = "33.0.1", Description = "CSV文件处理", IsEnabled = false },
                    new() { PackageId = "RestSharp", Version = "112.0.0", Description = "REST API客户端", IsEnabled = false },
                    new() { PackageId = "AutoMapper", Version = "13.0.1", Description = "对象映射", IsEnabled = false },
                    new() { PackageId = "FluentValidation", Version = "11.9.0", Description = "数据验证", IsEnabled = false },
                    new() { PackageId = "Serilog", Version = "4.0.0", Description = "结构化日志", IsEnabled = false },
                    new() { PackageId = "Polly", Version = "8.2.0", Description = "弹性和故障处理", IsEnabled = false },
                    new() { PackageId = "Microsoft.Extensions.Logging", Version = "9.0.0", Description = "日志框架", IsEnabled = false },
                    new() { PackageId = "Microsoft.Extensions.Configuration", Version = "9.0.0", Description = "配置管理", IsEnabled = false },
                    new() { PackageId = "System.Drawing.Common", Version = "9.0.0", Description = "图像处理", IsEnabled = false },
                    new() { PackageId = "EPPlus", Version = "7.0.0", Description = "Excel文件处理", IsEnabled = false },
                    new() { PackageId = "StackExchange.Redis", Version = "2.7.4", Description = "Redis客户端", IsEnabled = false },
                },
                ExcludedAssemblyPatterns = new List<string>
                {
                    // 优化的排除模式（增强非托管DLL检测）
                    "*.Native.dll",
                    "*Native*.dll",
                    "api-ms-*.dll",
                    "clr*.dll",
                    "coreclr.dll",
                    "hostfxr.dll",
                    "hostpolicy.dll",
                    "mscor*.dll",
                    "msquic.dll",
                    "dbgshim.dll",
                    "*.resources.dll",
                    "*.XmlSerializers.dll",
                    "*vshost*.dll",
                    
                    // Chrome/Chromium相关非托管DLL
                    "*chrome*.dll",
                    "*chromium*.dll",
                    "*cef*.dll",
                    "vulkan*.dll",
                    "*vulkan*.dll",
                    
                    // 图形渲染相关
                    "*d3d*.dll",
                    "*opengl*.dll",
                    "*gpu*.dll",
                    "*angle*.dll",
                    "*swiftshader*.dll",
                    
                    // 其他常见非托管库
                    "*sqlite3*.dll",
                    "*ffmpeg*.dll",
                    "*avcodec*.dll",
                    "*avformat*.dll",
                    "*avutil*.dll",
                    "*curl*.dll",
                    "*ssl*.dll",
                    "*crypto*.dll",
                    "*zlib*.dll",
                    "*libskia*.dll",
                    "*icu*.dll",
                    
                    // 运行时相关
                    "ucrtbase*.dll",
                    "vcruntime*.dll",
                    "msvcp*.dll",
                    "msvcr*.dll"
                }
            };
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _cacheCleanupTimer?.Dispose();
                _cache.Clear();
                _disposed = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"释放 AppSettingsService 资源时发生异常: {ex.Message}");
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~AppSettingsService()
        {
            Dispose();
        }

        #endregion
    }
}