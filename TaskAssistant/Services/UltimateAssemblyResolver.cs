using Microsoft.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.Loader;
using System.IO;

namespace TaskAssistant.Services
{
    /// <summary>
    /// 终极智能程序集解析器
    /// 一劳永逸地处理任何未知的脚本引用
    /// </summary>
    public class UltimateAssemblyResolver
    {
        private static readonly Lazy<UltimateAssemblyResolver> _instance = new(() => new UltimateAssemblyResolver());
        public static UltimateAssemblyResolver Instance => _instance.Value;

        private readonly Dictionary<string, MetadataReference> _cachedReferences = new();
        private readonly HashSet<string> _loadedPaths = new();
        private readonly Dictionary<string, Assembly> _loadedAssemblies = new();
        private readonly object _lock = new object();

        private UltimateAssemblyResolver() { }

        /// <summary>
        /// 智能分析代码并获取所需的所有引用
        /// </summary>
        public async Task<List<MetadataReference>> GetReferencesForCodeAsync(string code)
        {
            var references = new List<MetadataReference>();

            await Task.Run(() =>
            {
                lock (_lock)
                {
                    // 1. 加载基础运行时引用
                    references.AddRange(GetBaseRuntimeReferences());

                    // 2. 分析代码中的using语句，智能推断需要的程序集
                    references.AddRange(AnalyzeAndLoadUsingStatements(code));

                    // 3. 分析代码中使用的类型，动态加载相关程序集
                    references.AddRange(AnalyzeAndLoadTypes(code));

                    // 4. 扫描并加载所有可能用到的程序集
                    references.AddRange(LoadAllPossibleAssemblies());

                    // 5. 加载GAC中的程序集
                    references.AddRange(LoadGACAssemblies());
                }
            });

            return references.Distinct().ToList();
        }

        /// <summary>
        /// 获取基础运行时引用
        /// </summary>
        private List<MetadataReference> GetBaseRuntimeReferences()
        {
            var references = new List<MetadataReference>();

            var baseTypes = new[]
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
                typeof(System.Data.DataTable),          // System.Data
                typeof(System.Xml.XmlDocument),         // System.Xml
                typeof(System.Diagnostics.Process),     // System.Diagnostics
                typeof(System.Security.Cryptography.SHA256), // System.Security.Cryptography
                typeof(System.Drawing.Color),           // System.Drawing (如果可用)
            };

            foreach (var type in baseTypes)
            {
                TryAddAssemblyFromType(type, references);
            }

            // 添加Microsoft.CSharp程序集支持动态绑定
            TryLoadAssemblyByName("Microsoft.CSharp", references);

            return references;
        }

        /// <summary>
        /// 分析using语句并加载相应的程序集
        /// </summary>
        private List<MetadataReference> AnalyzeAndLoadUsingStatements(string code)
        {
            var references = new List<MetadataReference>();
            var usingRegex = new Regex(@"using\s+([a-zA-Z0-9_.]+)\s*;", RegexOptions.Multiline);
            var matches = usingRegex.Matches(code);

            var namespacesToAssemblies = new Dictionary<string, string[]>
            {
                // 常见命名空间到程序集的映射
                ["System"] = new[] { "System.Private.CoreLib", "System.Runtime" },
                ["System.Collections"] = new[] { "System.Collections", "System.Collections.Concurrent" },
                ["System.Collections.Generic"] = new[] { "System.Collections", "System.Private.CoreLib" },
                ["System.Linq"] = new[] { "System.Linq", "System.Linq.Expressions" },
                ["System.Threading"] = new[] { "System.Threading", "System.Threading.Tasks" },
                ["System.Threading.Tasks"] = new[] { "System.Threading.Tasks", "System.Threading" },
                ["System.IO"] = new[] { "System.IO.FileSystem", "System.Private.CoreLib" },
                ["System.Net"] = new[] { "System.Net.Primitives", "System.Net.NetworkInformation" },
                ["System.Net.Http"] = new[] { "System.Net.Http" },
                ["System.Text"] = new[] { "System.Private.CoreLib", "System.Text.Encoding" },
                ["System.Text.Json"] = new[] { "System.Text.Json" },
                ["System.Text.Regular.Expressions"] = new[] { "System.Text.RegularExpressions" },
                ["System.Xml"] = new[] { "System.Xml.ReaderWriter", "System.Private.Xml" },
                ["System.Data"] = new[] { "System.Data.Common", "System.Data" },
                ["System.ComponentModel"] = new[] { "System.ComponentModel", "System.ComponentModel.Primitives" },
                ["System.Diagnostics"] = new[] { "System.Diagnostics.Process", "System.Diagnostics.TraceSource" },
                ["System.Security.Cryptography"] = new[] { "System.Security.Cryptography.Algorithms" },
                ["System.Drawing"] = new[] { "System.Drawing.Common", "System.Drawing.Primitives" },
                ["Microsoft.Extensions"] = new[] { "Microsoft.Extensions.DependencyInjection", "Microsoft.Extensions.Logging" },
                ["Newtonsoft.Json"] = new[] { "Newtonsoft.Json" },
            };

            foreach (Match match in matches)
            {
                var namespaceName = match.Groups[1].Value;
                
                // 尝试精确匹配
                if (namespacesToAssemblies.TryGetValue(namespaceName, out var assemblyNames))
                {
                    foreach (var assemblyName in assemblyNames)
                    {
                        TryLoadAssemblyByName(assemblyName, references);
                    }
                }
                else
                {
                    // 尝试部分匹配
                    foreach (var kvp in namespacesToAssemblies)
                    {
                        if (namespaceName.StartsWith(kvp.Key))
                        {
                            foreach (var assemblyName in kvp.Value)
                            {
                                TryLoadAssemblyByName(assemblyName, references);
                            }
                        }
                    }
                }
            }

            return references;
        }

        /// <summary>
        /// 分析代码中使用的类型并加载相关程序集
        /// </summary>
        private List<MetadataReference> AnalyzeAndLoadTypes(string code)
        {
            var references = new List<MetadataReference>();

            // 常见类型模式
            var typePatterns = new Dictionary<string, string[]>
            {
                [@"\bHttpClient\b"] = new[] { "System.Net.Http" },
                [@"\bJsonSerializer\b"] = new[] { "System.Text.Json" },
                [@"\bJsonConvert\b"] = new[] { "Newtonsoft.Json" },
                [@"\bJObject\b"] = new[] { "Newtonsoft.Json" },
                [@"\bJArray\b"] = new[] { "Newtonsoft.Json" },
                [@"\bDataTable\b"] = new[] { "System.Data.Common" },
                [@"\bXmlDocument\b"] = new[] { "System.Xml.ReaderWriter" },
                [@"\bRegex\b"] = new[] { "System.Text.RegularExpressions" },
                [@"\bProcess\b"] = new[] { "System.Diagnostics.Process" },
                [@"\bFile\b"] = new[] { "System.IO.FileSystem" },
                [@"\bDirectory\b"] = new[] { "System.IO.FileSystem" },
                [@"\bPath\b"] = new[] { "System.IO.FileSystem" },
                [@"\bStream\b"] = new[] { "System.IO" },
                [@"\bEncoding\b"] = new[] { "System.Text.Encoding" },
                [@"\bSHA256\b"] = new[] { "System.Security.Cryptography.Algorithms" },
                [@"\bMD5\b"] = new[] { "System.Security.Cryptography.Algorithms" },
                [@"\bZipFile\b"] = new[] { "System.IO.Compression.ZipFile" },
                [@"\bWebClient\b"] = new[] { "System.Net.WebClient" },
                [@"\bSmtpClient\b"] = new[] { "System.Net.Mail" },
                [@"\bMailMessage\b"] = new[] { "System.Net.Mail" },
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

            return references;
        }

        /// <summary>
        /// 加载所有可能用到的程序集
        /// </summary>
        private List<MetadataReference> LoadAllPossibleAssemblies()
        {
            var references = new List<MetadataReference>();

            // 获取当前应用程序域中的所有程序集
            var currentAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .ToList();

            foreach (var assembly in currentAssemblies)
            {
                TryAddAssemblyFromAssembly(assembly, references);
            }

            // 扫描运行时目录
            try
            {
                var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
                var dllFiles = Directory.GetFiles(runtimeDir, "System.*.dll")
                    .Concat(Directory.GetFiles(runtimeDir, "Microsoft.*.dll"))
                    .Where(f => !IsNativeLibrary(Path.GetFileName(f)))
                    .Take(50); // 限制数量

                foreach (var dllFile in dllFiles)
                {
                    TryAddAssemblyFromPath(dllFile, references);
                }
            }
            catch { /* 忽略错误 */ }

            return references;
        }

        /// <summary>
        /// 加载GAC中的程序集
        /// </summary>
        private List<MetadataReference> LoadGACAssemblies()
        {
            var references = new List<MetadataReference>();

            // 常用的GAC程序集名称
            var gacAssemblies = new[]
            {
                "System",
                "System.Core",
                "System.Data",
                "System.Xml",
                "System.Web",
                "System.Windows.Forms",
                "System.Drawing",
                "Microsoft.CSharp",
                "System.Runtime.Serialization",
                "System.ServiceModel",
                "System.Transactions",
                "System.Configuration",
            };

            foreach (var assemblyName in gacAssemblies)
            {
                TryLoadAssemblyByName(assemblyName, references);
            }

            return references;
        }

        /// <summary>
        /// 尝试通过名称加载程序集
        /// </summary>
        private void TryLoadAssemblyByName(string assemblyName, List<MetadataReference> references)
        {
            try
            {
                if (_loadedAssemblies.TryGetValue(assemblyName, out var cachedAssembly))
                {
                    TryAddAssemblyFromAssembly(cachedAssembly, references);
                    return;
                }

                var assembly = Assembly.Load(assemblyName);
                _loadedAssemblies[assemblyName] = assembly;
                TryAddAssemblyFromAssembly(assembly, references);
            }
            catch
            {
                // 忽略加载失败的程序集
            }
        }

        /// <summary>
        /// 尝试从类型添加程序集
        /// </summary>
        private void TryAddAssemblyFromType(Type type, List<MetadataReference> references)
        {
            try
            {
                var assembly = type.Assembly;
                TryAddAssemblyFromAssembly(assembly, references);
            }
            catch { /* 忽略错误 */ }
        }

        /// <summary>
        /// 尝试从程序集对象添加引用
        /// </summary>
        private void TryAddAssemblyFromAssembly(Assembly assembly, List<MetadataReference> references)
        {
            try
            {
                if (string.IsNullOrEmpty(assembly.Location) || _loadedPaths.Contains(assembly.Location))
                    return;

                if (!IsValidManagedAssembly(assembly.Location))
                    return;

                var reference = MetadataReference.CreateFromFile(assembly.Location);
                var key = assembly.GetName().Name ?? assembly.Location;

                if (!_cachedReferences.ContainsKey(key))
                {
                    _cachedReferences[key] = reference;
                    _loadedPaths.Add(assembly.Location);
                    references.Add(reference);
                }
            }
            catch { /* 忽略错误 */ }
        }

        /// <summary>
        /// 尝试从路径添加程序集
        /// </summary>
        private void TryAddAssemblyFromPath(string path, List<MetadataReference> references)
        {
            try
            {
                if (_loadedPaths.Contains(path) || !IsValidManagedAssembly(path))
                    return;

                var reference = MetadataReference.CreateFromFile(path);
                var key = Path.GetFileNameWithoutExtension(path);

                if (!_cachedReferences.ContainsKey(key))
                {
                    _cachedReferences[key] = reference;
                    _loadedPaths.Add(path);
                    references.Add(reference);
                }
            }
            catch { /* 忽略错误 */ }
        }

        /// <summary>
        /// 检查是否为有效的托管程序集
        /// </summary>
        private bool IsValidManagedAssembly(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var fileName = Path.GetFileName(filePath);
                
                // 排除本地库
                if (IsNativeLibrary(fileName))
                    return false;

                // 尝试验证
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
        /// 检查是否为本地库
        /// </summary>
        private bool IsNativeLibrary(string fileName)
        {
            var nativePatterns = new[]
            {
                "*.Native.dll", "*Native*.dll", "api-ms-*.dll", "clr*.dll", "coreclr.dll",
                "hostfxr.dll", "hostpolicy.dll", "mscor*.dll", "msquic.dll", "dbgshim.dll"
            };

            return nativePatterns.Any(pattern =>
            {
                var regex = "^" + pattern.Replace(".", @"\.").Replace("*", ".*") + "$";
                return Regex.IsMatch(fileName, regex, RegexOptions.IgnoreCase);
            });
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public void ClearCache()
        {
            lock (_lock)
            {
                _cachedReferences.Clear();
                _loadedPaths.Clear();
                _loadedAssemblies.Clear();
            }
        }
    }
}