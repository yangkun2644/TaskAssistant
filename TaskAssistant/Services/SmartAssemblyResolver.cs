using Microsoft.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace TaskAssistant.Services
{
    /// <summary>
    /// 智能程序集解析器（修复版本）
    /// 自动过滤本地库，只加载托管程序集
    /// </summary>
    public class SmartAssemblyResolver
    {
        private static readonly Lazy<SmartAssemblyResolver> _instance = new(() => new SmartAssemblyResolver());
        public static SmartAssemblyResolver Instance => _instance.Value;

        private readonly Dictionary<string, MetadataReference> _cachedReferences = new();
        private readonly HashSet<string> _loadedAssemblyPaths = new();
        private bool _isInitialized = false;

        // 需要排除的本地库文件名模式
        private static readonly HashSet<string> _nativeLibraryPatterns = new(StringComparer.OrdinalIgnoreCase)
        {
            "*.Native.dll",
            "*.native.dll",
            "*Native*.dll",
            "api-ms-*.dll",
            "Microsoft.DiaSymReader.Native.*.dll",
            "System.IO.Compression.Native.dll",
            "System.Native.dll",
            "System.Net.Security.Native.dll",
            "System.Security.Cryptography.Native.*.dll",
            "clrcompression.dll",
            "clretwrc.dll",
            "clrjit.dll",
            "coreclr.dll",
            "createdump.exe",
            "dbgshim.dll",
            "hostfxr.dll",
            "hostpolicy.dll",
            "Microsoft.Win32.SystemEvents.dll", // 这个也可能有问题
            "mscordaccore.dll",
            "mscordbi.dll",
            "mscorrc.dll",
            "msquic.dll",
            "WindowsBase.dll" // WPF相关，但可能有问题
        };

        private SmartAssemblyResolver() { }

        /// <summary>
        /// 获取所有可用的程序集引用（过滤本地库）
        /// </summary>
        public async Task<List<MetadataReference>> GetAllReferencesAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
                _isInitialized = true;
            }

            return _cachedReferences.Values.ToList();
        }

        /// <summary>
        /// 初始化所有程序集引用
        /// </summary>
        private async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                // 1. 加载运行时核心程序集（最安全）
                LoadRuntimeAssemblies();

                // 2. 加载当前应用程序域的托管程序集
                LoadAppDomainManagedAssemblies();

                // 3. 加载项目引用的程序集
                LoadProjectReferences();

                // 4. 有选择地加载系统程序集
                LoadSelectedSystemAssemblies();
            });
        }

        /// <summary>
        /// 加载运行时核心程序集（最安全的方式）
        /// </summary>
        private void LoadRuntimeAssemblies()
        {
            var coreTypes = new[]
            {
                // 核心类型
                typeof(object),                           // System.Private.CoreLib
                typeof(Console),                          // System.Console
                typeof(System.Collections.Generic.List<>), // System.Collections
                typeof(System.Linq.Enumerable),          // System.Linq
                typeof(System.Threading.Tasks.Task),     // System.Threading.Tasks
                typeof(System.Net.Http.HttpClient),      // System.Net.Http
                typeof(System.IO.File),                  // System.IO
                typeof(System.Text.StringBuilder),       // System.Text
                typeof(System.Text.RegularExpressions.Regex), // System.Text.RegularExpressions
                
                // 常用类型
                typeof(System.ComponentModel.Component), // System.ComponentModel
                typeof(System.Xml.XmlDocument),         // System.Xml
                typeof(System.Data.DataTable),          // System.Data
                typeof(System.Threading.Thread),        // System.Threading
                typeof(System.Diagnostics.Process),     // System.Diagnostics
            };

            foreach (var type in coreTypes)
            {
                TryAddAssemblyFromType(type);
            }
        }

        /// <summary>
        /// 加载当前应用程序域的托管程序集
        /// </summary>
        private void LoadAppDomainManagedAssemblies()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic &&
                           !string.IsNullOrEmpty(a.Location) &&
                           IsManagedAssembly(a.Location));

            foreach (var assembly in assemblies)
            {
                TryAddAssembly(assembly);
            }
        }

        /// <summary>
        /// 加载项目引用的程序集
        /// </summary>
        private void LoadProjectReferences()
        {
            try
            {
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    var referencedAssemblies = entryAssembly.GetReferencedAssemblies();
                    foreach (var assemblyName in referencedAssemblies)
                    {
                        try
                        {
                            var assembly = Assembly.Load(assemblyName);
                            if (!string.IsNullOrEmpty(assembly.Location) &&
                                IsManagedAssembly(assembly.Location))
                            {
                                TryAddAssembly(assembly);
                            }
                        }
                        catch
                        {
                            // 忽略加载失败的程序集
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载项目引用失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 有选择地加载系统程序集
        /// </summary>
        private void LoadSelectedSystemAssemblies()
        {
            try
            {
                // 安全的系统程序集名称列表
                var safeSystemAssemblies = new[]
                {
                    "System.Runtime",
                    "System.Runtime.Extensions",
                    "System.Collections",
                    "System.Collections.Concurrent",
                    "System.Linq",
                    "System.Linq.Expressions",
                    "System.Threading.Tasks",
                    "System.Net.Http",
                    "System.Net.Primitives",
                    "System.IO.FileSystem",
                    "System.Text.Encoding",
                    "System.Text.RegularExpressions",
                    "System.Text.Json",
                    "System.Reflection",
                    "System.ComponentModel",
                    "System.ComponentModel.Primitives",
                    "System.Xml.ReaderWriter",
                    "System.Xml.XDocument",
                    "System.Data.Common",
                    "Microsoft.CSharp", // 添加动态绑定支持
                    "Newtonsoft.Json" // 如果项目中有引用
                };

                foreach (var assemblyName in safeSystemAssemblies)
                {
                    try
                    {
                        var assembly = Assembly.Load(assemblyName);
                        if (!string.IsNullOrEmpty(assembly.Location) &&
                            IsManagedAssembly(assembly.Location))
                        {
                            TryAddAssembly(assembly);
                        }
                    }
                    catch
                    {
                        // 忽略加载失败的程序集
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载系统程序集失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查是否为托管程序集（排除本地库）
        /// </summary>
        private bool IsManagedAssembly(string filePath)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);

                // 检查是否匹配本地库模式
                foreach (var pattern in _nativeLibraryPatterns)
                {
                    if (IsPatternMatch(fileName, pattern))
                    {
                        return false;
                    }
                }

                // 检查文件扩展名
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (extension != ".dll" && extension != ".exe")
                {
                    return false;
                }

                // 尝试验证是否为托管程序集
                return IsValidManagedAssembly(filePath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 模式匹配检查
        /// </summary>
        private bool IsPatternMatch(string fileName, string pattern)
        {
            if (pattern.Contains("*"))
            {
                var regexPattern = "^" + pattern.Replace(".", @"\.").Replace("*", ".*") + "$";
                return System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            return string.Equals(fileName, pattern, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 验证是否为有效的托管程序集
        /// </summary>
        private bool IsValidManagedAssembly(string filePath)
        {
            try
            {
                // 尝试创建 MetadataReference 来验证
                using var stream = File.OpenRead(filePath);
                var reference = MetadataReference.CreateFromStream(stream);
                return reference != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从类型安全地添加程序集
        /// </summary>
        private void TryAddAssemblyFromType(Type type)
        {
            try
            {
                var assembly = type.Assembly;
                if (!string.IsNullOrEmpty(assembly.Location) &&
                    IsManagedAssembly(assembly.Location))
                {
                    TryAddAssembly(assembly);
                }
            }
            catch
            {
                // 忽略添加失败的程序集
            }
        }

        /// <summary>
        /// 尝试添加程序集
        /// </summary>
        private void TryAddAssembly(Assembly assembly)
        {
            try
            {
                if (!string.IsNullOrEmpty(assembly.Location) &&
                    !_loadedAssemblyPaths.Contains(assembly.Location) &&
                    IsManagedAssembly(assembly.Location))
                {
                    var reference = MetadataReference.CreateFromFile(assembly.Location);
                    var key = assembly.GetName().Name ?? assembly.Location;

                    if (!_cachedReferences.ContainsKey(key))
                    {
                        _cachedReferences[key] = reference;
                        _loadedAssemblyPaths.Add(assembly.Location);
                    }
                }
            }
            catch
            {
                // 忽略添加失败的程序集
            }
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public void ClearCache()
        {
            _cachedReferences.Clear();
            _loadedAssemblyPaths.Clear();
            _isInitialized = false;
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public (int Count, long SizeBytes) GetCacheInfo()
        {
            var count = _cachedReferences.Count;
            var size = _loadedAssemblyPaths.Sum(path =>
            {
                try
                {
                    return new FileInfo(path).Length;
                }
                catch
                {
                    return 0;
                }
            });

            return (count, size);
        }
    }
}