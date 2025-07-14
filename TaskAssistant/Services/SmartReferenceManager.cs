using Microsoft.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using System.IO;
using TaskAssistant.Models;
using TaskAssistant.Services;

namespace TaskAssistant.Services
{
    /// <summary>
    /// 新一代智能引用管理器（优化内存管理版本）
    /// 基于用户配置的一劳永逸解决方案
    /// 实现了自动内存清理和缓存管理
    /// </summary>
    public class SmartReferenceManager : IDisposable
    {
        private static readonly Lazy<SmartReferenceManager> _instance = 
            new(() => new SmartReferenceManager());
        
        public static SmartReferenceManager Instance => _instance.Value;

        private readonly Dictionary<string, MetadataReference> _cachedReferences = new();
        private readonly Dictionary<string, Assembly> _cachedAssemblies = new();
        private readonly object _lock = new();
        private ScriptReferenceSettings? _settings;
        private IAppSettingsService? _settingsService;
        private bool _isInitialized = false;
        private bool _disposed = false;

        // 内存管理
        private Timer? _cacheCleanupTimer;
        private DateTime _lastCacheCleanup = DateTime.Now;
        private readonly int _maxCacheSize = 100; // 最大缓存数量
        private readonly TimeSpan _cacheCleanupInterval = TimeSpan.FromMinutes(10);

        private SmartReferenceManager() 
        {
            StartCacheCleanupTimer();
        }

        /// <summary>
        /// 启动缓存清理定时器
        /// </summary>
        private void StartCacheCleanupTimer()
        {
            _cacheCleanupTimer = new Timer(PerformCacheCleanup, null, 
                _cacheCleanupInterval, _cacheCleanupInterval);
        }

        /// <summary>
        /// 执行缓存清理
        /// </summary>
        private void PerformCacheCleanup(object? state)
        {
            if (_disposed) return;

            lock (_lock)
            {
                try
                {
                    var timeSinceLastCleanup = DateTime.Now - _lastCacheCleanup;
                    
                    // 如果缓存大小超过限制或距离上次清理超过30分钟，执行清理
                    if (_cachedReferences.Count > _maxCacheSize || 
                        timeSinceLastCleanup.TotalMinutes > 30)
                    {
                        // 清理一半的缓存
                        var itemsToRemove = _cachedReferences.Count / 2;
                        var keysToRemove = _cachedReferences.Keys.Take(itemsToRemove).ToList();
                        
                        foreach (var key in keysToRemove)
                        {
                            _cachedReferences.Remove(key);
                        }

                        // 清理程序集缓存
                        var assemblyKeysToRemove = _cachedAssemblies.Keys.Take(_cachedAssemblies.Count / 2).ToList();
                        foreach (var key in assemblyKeysToRemove)
                        {
                            _cachedAssemblies.Remove(key);
                        }

                        _lastCacheCleanup = DateTime.Now;
                        
                        System.Diagnostics.Debug.WriteLine(
                            $"SmartReferenceManager 缓存清理完成: 移除 {keysToRemove.Count} 个引用，{assemblyKeysToRemove.Count} 个程序集");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SmartReferenceManager 缓存清理失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 初始化引用管理器
        /// </summary>
        /// <param name="settingsService">设置服务</param>
        public async Task InitializeAsync(IAppSettingsService settingsService)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SmartReferenceManager));

            try
            {
                _settingsService = settingsService;
                _settings = await settingsService.GetScriptReferenceSettingsAsync();
                
                // 预加载常用引用
                await PreloadCommonReferencesAsync();
                
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SmartReferenceManager 初始化失败: {ex.Message}");
                // 即使初始化失败，也要设置一个默认配置
                _settings = CreateDefaultSettings();
                _isInitialized = true;
            }
        }

        /// <summary>
        /// 创建默认设置
        /// </summary>
        private ScriptReferenceSettings CreateDefaultSettings()
        {
            return new ScriptReferenceSettings
            {
                AutoLoadAllAssemblies = true,
                EnableSmartSuggestions = true,
                CommonAssemblies = new List<AssemblyReference>
                {
                    new() { Name = "System.Private.CoreLib", Description = "核心运行时库", IsEnabled = true },
                    new() { Name = "System.Console", Description = "控制台操作", IsEnabled = true },
                    new() { Name = "System.Net.Http", Description = "HTTP客户端", IsEnabled = true },
                    new() { Name = "System.Text.Json", Description = "JSON序列化", IsEnabled = true },
                    new() { Name = "System.IO.FileSystem", Description = "文件系统操作", IsEnabled = true },
                    new() { Name = "System.Linq", Description = "LINQ查询", IsEnabled = true },
                },
                CommonNuGetPackages = new List<NuGetReference>(),
                ExcludedAssemblyPatterns = new List<string>
                {
                    "*.Native.dll",
                    "*Native*.dll",
                    "api-ms-*.dll",
                    "clr*.dll",
                    "coreclr.dll",
                    "hostfxr.dll",
                    "hostpolicy.dll",
                    "mscor*.dll",
                    "msquic.dll",
                    "dbgshim.dll"
                }
            };
        }

        /// <summary>
        /// 获取脚本运行所需的所有引用（优化版本）
        /// </summary>
        /// <param name="code">脚本代码</param>
        /// <returns>引用列表</returns>
        public async Task<List<MetadataReference>> GetReferencesForCodeAsync(string code)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SmartReferenceManager));

            var references = new List<MetadataReference>();

            // 如果未初始化，使用安全的备用方案
            if (!_isInitialized)
            {
                return GetSafeBasicReferences();
            }

            await Task.Run(() =>
            {
                lock (_lock)
                {
                    try
                    {
                        // 1. 添加自动加载的程序集（如果启用）
                        if (_settings?.AutoLoadAllAssemblies == true)
                        {
                            references.AddRange(GetAutoLoadedAssemblies());
                        }

                        // 2. 添加用户配置的常用程序集
                        references.AddRange(GetConfiguredAssemblies());

                        // 3. 智能分析代码并添加相关引用（如果启用）
                        if (_settings?.EnableSmartSuggestions == true)
                        {
                            references.AddRange(AnalyzeCodeAndGetReferences(code));
                        }

                        // 4. 如果引用数量太少，添加基础引用作为保险
                        if (references.Count < 5)
                        {
                            references.AddRange(GetSafeBasicReferences());
                        }

                        // 去重处理
                        references = references.Distinct().ToList();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"获取引用时出错: {ex.Message}");
                        // 出错时返回安全的基础引用
                        references.Clear();
                        references.AddRange(GetSafeBasicReferences());
                    }
                }
            });

            return references;
        }

        /// <summary>
        /// 获取安全的基础引用
        /// </summary>
        private List<MetadataReference> GetSafeBasicReferences()
        {
            var references = new List<MetadataReference>();

            try
            {
                var basicTypes = new[]
                {
                    typeof(object),                           // System.Private.CoreLib
                    typeof(Console),                          // System.Console
                    typeof(System.Collections.Generic.List<>), // System.Collections
                    typeof(System.Linq.Enumerable),          // System.Linq
                    typeof(System.Threading.Tasks.Task),     // System.Threading.Tasks
                    typeof(System.Net.Http.HttpClient),      // System.Net.Http
                    typeof(System.IO.File),                  // System.IO
                    typeof(System.Text.StringBuilder),       // System.Text
                    typeof(System.Text.RegularExpressions.Regex), // System.Text.RegularExpressions
                    typeof(System.Text.Json.JsonSerializer), // System.Text.Json
                    typeof(System.ComponentModel.Component), // System.ComponentModel
                    typeof(System.Xml.XmlDocument),         // System.Xml
                    typeof(System.Diagnostics.Process),     // System.Diagnostics
                };

                foreach (var type in basicTypes)
                {
                    try
                    {
                        var assembly = type.Assembly;
                        if (!string.IsNullOrEmpty(assembly.Location))
                        {
                            var reference = MetadataReference.CreateFromFile(assembly.Location);
                            references.Add(reference);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"加载基础程序集 {type.Name} 失败: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取安全基础引用失败: {ex.Message}");
            }

            return references;
        }

        /// <summary>
        /// 获取NuGet包引用列表
        /// </summary>
        /// <param name="code">脚本代码</param>
        /// <returns>NuGet包引用列表</returns>
        public List<(string PackageId, string Version)> GetNuGetReferences(string code)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SmartReferenceManager));

            var references = new List<(string, string)>();

            try
            {
                // 1. 从代码中解析显式的NuGet引用
                references.AddRange(ParseExplicitNuGetReferences(code));

                // 2. 添加用户配置的常用NuGet包（仅启用的）
                if (_settings?.CommonNuGetPackages != null)
                {
                    var enabledPackages = _settings.CommonNuGetPackages
                        .Where(p => p.IsEnabled)
                        .Select(p => (p.PackageId, p.Version));
                    
                    references.AddRange(enabledPackages);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取NuGet引用失败: {ex.Message}");
            }

            return references.Distinct().ToList();
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public void ClearCache()
        {
            if (_disposed) return;

            lock (_lock)
            {
                try
                {
                    _cachedReferences.Clear();
                    _cachedAssemblies.Clear();
                    _lastCacheCleanup = DateTime.Now;
                    
                    // 强制垃圾回收
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    
                    System.Diagnostics.Debug.WriteLine("SmartReferenceManager 缓存已清理");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"清理缓存失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 更新设置并刷新缓存
        /// </summary>
        /// <param name="newSettings">新的设置</param>
        public async Task UpdateSettingsAsync(ScriptReferenceSettings newSettings)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SmartReferenceManager));

            try
            {
                if (_settingsService != null)
                {
                    await _settingsService.SaveScriptReferenceSettingsAsync(newSettings);
                    _settings = newSettings;
                    
                    // 清理缓存并重新加载
                    ClearCache();
                    await PreloadCommonReferencesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加新的程序集引用到用户配置
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <param name="description">描述</param>
        public async Task AddUserAssemblyAsync(string assemblyName, string description)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SmartReferenceManager));

            try
            {
                if (_settings == null || _settingsService == null) return;

                var existing = _settings.CommonAssemblies
                    .FirstOrDefault(a => a.Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    _settings.CommonAssemblies.Add(new AssemblyReference
                    {
                        Name = assemblyName,
                        Description = description,
                        IsEnabled = true
                    });

                    await _settingsService.SaveScriptReferenceSettingsAsync(_settings);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加用户程序集失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加新的NuGet包引用到用户配置
        /// </summary>
        /// <param name="packageId">包ID</param>
        /// <param name="version">版本</param>
        /// <param name="description">描述</param>
        public async Task AddUserNuGetPackageAsync(string packageId, string version, string description)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SmartReferenceManager));

            try
            {
                if (_settings == null || _settingsService == null) return;

                var existing = _settings.CommonNuGetPackages
                    .FirstOrDefault(p => p.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    _settings.CommonNuGetPackages.Add(new NuGetReference
                    {
                        PackageId = packageId,
                        Version = version,
                        Description = description,
                        IsEnabled = false // 默认不启用，让用户手动启用
                    });

                    await _settingsService.SaveScriptReferenceSettingsAsync(_settings);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加用户NuGet包失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 预加载常用引用
        /// </summary>
        private async Task PreloadCommonReferencesAsync()
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    try
                    {
                        // 预加载基础运行时程序集
                        LoadBasicRuntimeAssemblies();

                        // 预加载用户配置的程序集
                        LoadUserConfiguredAssemblies();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"预加载引用失败: {ex.Message}");
                    }
                }
            });
        }

        /// <summary>
        /// 获取自动加载的程序集
        /// </summary>
        private List<MetadataReference> GetAutoLoadedAssemblies()
        {
            var references = new List<MetadataReference>();

            try
            {
                // 基础运行时程序集
                references.AddRange(LoadBasicRuntimeAssemblies());

                // 当前应用程序域中的程序集（限制数量避免过多）
                try
                {
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                        .Where(a => !IsExcludedAssembly(a.Location))
                        .Take(50); // 限制数量

                    foreach (var assembly in assemblies)
                    {
                        TryAddAssemblyReference(assembly, references);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载应用程序域程序集失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取自动加载程序集失败: {ex.Message}");
            }

            return references;
        }

        /// <summary>
        /// 获取用户配置的程序集
        /// </summary>
        private List<MetadataReference> GetConfiguredAssemblies()
        {
            var references = new List<MetadataReference>();

            try
            {
                if (_settings?.CommonAssemblies != null)
                {
                    foreach (var assemblyRef in _settings.CommonAssemblies.Where(a => a.IsEnabled))
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(assemblyRef.Path) && File.Exists(assemblyRef.Path))
                            {
                                // 从指定路径加载
                                TryAddAssemblyFromPath(assemblyRef.Path, references);
                            }
                            else
                            {
                                // 从名称加载
                                TryLoadAssemblyByName(assemblyRef.Name, references);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"加载配置程序集 {assemblyRef.Name} 失败: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取配置程序集失败: {ex.Message}");
            }

            return references;
        }

        /// <summary>
        /// 加载基础运行时程序集
        /// </summary>
        private List<MetadataReference> LoadBasicRuntimeAssemblies()
        {
            var references = new List<MetadataReference>();

            try
            {
                var basicTypes = new[]
                {
                    typeof(object),                           // System.Private.CoreLib
                    typeof(Console),                          // System.Console
                    typeof(System.Collections.Generic.List<>), // System.Collections
                    typeof(System.Linq.Enumerable),          // System.Linq
                    typeof(System.Threading.Tasks.Task),     // System.Threading.Tasks
                    typeof(System.Net.Http.HttpClient),      // System.Net.Http
                    typeof(System.IO.File),                  // System.IO
                    typeof(System.Text.StringBuilder),       // System.Text
                    typeof(System.Text.RegularExpressions.Regex), // System.Text.RegularExpressions
                    typeof(System.Text.Json.JsonSerializer), // System.Text.Json
                    typeof(System.ComponentModel.Component), // System.ComponentModel
                    typeof(System.Xml.XmlDocument),         // System.Xml
                    typeof(System.Diagnostics.Process),     // System.Diagnostics
                };

                foreach (var type in basicTypes)
                {
                    TryAddAssemblyReference(type.Assembly, references);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载基础运行时程序集失败: {ex.Message}");
            }

            return references;
        }

        /// <summary>
        /// 加载用户配置的程序集
        /// </summary>
        private void LoadUserConfiguredAssemblies()
        {
            try
            {
                if (_settings?.CommonAssemblies == null) return;

                foreach (var assemblyRef in _settings.CommonAssemblies.Where(a => a.IsEnabled))
                {
                    try
                    {
                        Assembly assembly;
                        
                        if (!string.IsNullOrEmpty(assemblyRef.Path) && File.Exists(assemblyRef.Path))
                        {
                            assembly = Assembly.LoadFrom(assemblyRef.Path);
                        }
                        else
                        {
                            assembly = Assembly.Load(assemblyRef.Name);
                        }

                        var key = assembly.GetName().Name ?? assemblyRef.Name;
                        if (!_cachedAssemblies.ContainsKey(key) && _cachedAssemblies.Count < _maxCacheSize)
                        {
                            _cachedAssemblies[key] = assembly;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"缓存用户程序集 {assemblyRef.Name} 失败: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载用户配置程序集失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 智能分析代码并获取相关引用
        /// </summary>
        private List<MetadataReference> AnalyzeCodeAndGetReferences(string code)
        {
            var references = new List<MetadataReference>();

            try
            {
                // 分析using语句
                var usingMatches = Regex.Matches(code, @"using\s+([a-zA-Z0-9_.]+)\s*;", RegexOptions.Multiline);
                foreach (Match match in usingMatches)
                {
                    var namespaceName = match.Groups[1].Value;
                    TryLoadAssemblyForNamespace(namespaceName, references);
                }

                // 分析类型使用模式
                var typePatterns = new Dictionary<string, string[]>
                {
                    [@"\bHttpClient\b"] = new[] { "System.Net.Http" },
                    [@"\bJsonSerializer\b"] = new[] { "System.Text.Json" },
                    [@"\bJsonConvert\b"] = new[] { "Newtonsoft.Json" },
                    [@"\bJObject\b"] = new[] { "Newtonsoft.Json" },
                    [@"\bDataTable\b"] = new[] { "System.Data.Common" },
                    [@"\bXmlDocument\b"] = new[] { "System.Xml.ReaderWriter" },
                    [@"\bRegex\b"] = new[] { "System.Text.RegularExpressions" },
                    [@"\bProcess\b"] = new[] { "System.Diagnostics.Process" },
                    [@"\bFile\b"] = new[] { "System.IO.FileSystem" },
                    [@"\bSqlConnection\b"] = new[] { "System.Data.SqlClient" },
                    [@"\bSqliteConnection\b"] = new[] { "Microsoft.Data.Sqlite" },
                };

                foreach (var pattern in typePatterns)
                {
                    if (Regex.IsMatch(code, pattern.Key, RegexOptions.IgnoreCase))
                    {
                        foreach (var assemblyName in pattern.Value)
                        {
                            TryLoadAssemblyByName(assemblyName, references);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"智能分析代码失败: {ex.Message}");
            }

            return references;
        }

        /// <summary>
        /// 解析显式的NuGet引用
        /// </summary>
        private List<(string, string)> ParseExplicitNuGetReferences(string code)
        {
            var references = new List<(string, string)>();
            
            try
            {
                // NuGet引用正则表达式
                var patterns = new[]
                {
                    new Regex(@"#r\s+""nuget:\s*([^,\s]+)\s*,\s*([^""]+)\s*""", RegexOptions.IgnoreCase),
                    new Regex(@"#r\s+""nuget:\s*([^,\s""]+)\s*""", RegexOptions.IgnoreCase)
                };

                foreach (var pattern in patterns)
                {
                    var matches = pattern.Matches(code);
                    foreach (Match match in matches)
                    {
                        var packageId = match.Groups[1].Value.Trim();
                        var version = match.Groups.Count > 2 && match.Groups[2].Success 
                            ? match.Groups[2].Value.Trim() 
                            : "*";
                        
                        if (!references.Any(r => r.Item1.Equals(packageId, StringComparison.OrdinalIgnoreCase)))
                        {
                            references.Add((packageId, version));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析NuGet引用失败: {ex.Message}");
            }

            return references;
        }

        /// <summary>
        /// 尝试为命名空间加载程序集
        /// </summary>
        private void TryLoadAssemblyForNamespace(string namespaceName, List<MetadataReference> references)
        {
            try
            {
                var namespaceAssemblyMap = new Dictionary<string, string[]>
                {
                    ["System.Data"] = new[] { "System.Data.Common" },
                    ["System.Net.Http"] = new[] { "System.Net.Http" },
                    ["System.Text.Json"] = new[] { "System.Text.Json" },
                    ["System.Text.RegularExpressions"] = new[] { "System.Text.RegularExpressions" },
                    ["System.Xml"] = new[] { "System.Xml.ReaderWriter" },
                    ["System.IO"] = new[] { "System.IO.FileSystem" },
                    ["Microsoft.Data.Sqlite"] = new[] { "Microsoft.Data.Sqlite" },
                    ["System.Data.SqlClient"] = new[] { "System.Data.SqlClient" },
                };

                if (namespaceAssemblyMap.TryGetValue(namespaceName, out var assemblyNames))
                {
                    foreach (var assemblyName in assemblyNames)
                    {
                        TryLoadAssemblyByName(assemblyName, references);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"为命名空间 {namespaceName} 加载程序集失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试通过名称加载程序集
        /// </summary>
        private void TryLoadAssemblyByName(string assemblyName, List<MetadataReference> references)
        {
            try
            {
                if (_cachedAssemblies.TryGetValue(assemblyName, out var cachedAssembly))
                {
                    TryAddAssemblyReference(cachedAssembly, references);
                    return;
                }

                var assembly = Assembly.Load(assemblyName);
                if (_cachedAssemblies.Count < _maxCacheSize)
                {
                    _cachedAssemblies[assemblyName] = assembly;
                }
                TryAddAssemblyReference(assembly, references);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"通过名称加载程序集 {assemblyName} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试从路径添加程序集
        /// </summary>
        private void TryAddAssemblyFromPath(string path, List<MetadataReference> references)
        {
            try
            {
                if (IsExcludedAssembly(path)) return;

                var key = Path.GetFileNameWithoutExtension(path);
                if (_cachedReferences.ContainsKey(key)) 
                {
                    references.Add(_cachedReferences[key]);
                    return;
                }

                var reference = MetadataReference.CreateFromFile(path);
                if (_cachedReferences.Count < _maxCacheSize)
                {
                    _cachedReferences[key] = reference;
                }
                references.Add(reference);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从路径添加程序集 {path} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试添加程序集引用
        /// </summary>
        private void TryAddAssemblyReference(Assembly assembly, List<MetadataReference> references)
        {
            try
            {
                if (string.IsNullOrEmpty(assembly.Location)) return;
                if (IsExcludedAssembly(assembly.Location)) return;

                var key = assembly.GetName().Name ?? Path.GetFileNameWithoutExtension(assembly.Location);
                if (_cachedReferences.ContainsKey(key)) 
                {
                    references.Add(_cachedReferences[key]);
                    return;
                }

                var reference = MetadataReference.CreateFromFile(assembly.Location);
                if (_cachedReferences.Count < _maxCacheSize)
                {
                    _cachedReferences[key] = reference;
                }
                references.Add(reference);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加程序集引用失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查程序集是否应该被排除
        /// </summary>
        private bool IsExcludedAssembly(string assemblyPath)
        {
            try
            {
                if (string.IsNullOrEmpty(assemblyPath)) return true;

                var fileName = Path.GetFileName(assemblyPath);
                
                // 检查用户配置的排除模式
                if (_settings?.ExcludedAssemblyPatterns != null)
                {
                    foreach (var pattern in _settings.ExcludedAssemblyPatterns)
                    {
                        var regex = "^" + pattern.Replace(".", @"\.").Replace("*", ".*") + "$";
                        if (Regex.IsMatch(fileName, regex, RegexOptions.IgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检查排除程序集失败: {ex.Message}");
                return true; // 出错时保险起见排除
            }
        }

        #region IDisposable 实现

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的具体实现
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    // 停止缓存清理定时器
                    _cacheCleanupTimer?.Dispose();
                    _cacheCleanupTimer = null;

                    // 清理缓存
                    lock (_lock)
                    {
                        _cachedReferences.Clear();
                        _cachedAssemblies.Clear();
                    }

                    _disposed = true;
                    System.Diagnostics.Debug.WriteLine("SmartReferenceManager 资源释放完成");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"释放 SmartReferenceManager 资源时发生异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~SmartReferenceManager()
        {
            Dispose(false);
        }

        #endregion
    }
}