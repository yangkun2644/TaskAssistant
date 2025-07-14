using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TaskAssistant.Data;
using TaskAssistant.Data.Services;
using TaskAssistant.Models;
using TaskAssistant.Common;

namespace TaskAssistant.Services
{
    /// <summary>
    /// 脚本执行服务实现（优化内存管理版本）
    /// 提供完整的脚本编译、执行、日志记录功能
    /// 实现了IDisposable接口，确保资源正确释放
    /// </summary>
    public class ScriptExecutionService : IScriptExecutionService, IDisposable
    {
        #region 私有字段

        /// <summary>
        /// 数据库上下文
        /// </summary>
        private readonly AppDbContext _context;

        /// <summary>
        /// NuGet 包缓存目录
        /// </summary>
        private readonly string _nugetCacheDir;

        /// <summary>
        /// NuGet 包引用正则表达式
        /// </summary>
        private static readonly Regex NugetReferenceRegex = new Regex(
            @"^\s*#r\s+""nuget:\s*([^,\s]+)(?:\s*,\s*([^""]+))?\s*""\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        /// <summary>
        /// 是否已释放资源
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// 执行过程中创建的临时资源列表
        /// </summary>
        private readonly List<IDisposable> _temporaryResources = new();

        /// <summary>
        /// 内存清理定时器
        /// </summary>
        private Timer? _memoryCleanupTimer;

        /// <summary>
        /// 上次内存清理时间
        /// </summary>
        private DateTime _lastMemoryCleanup = DateTime.Now;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化脚本执行服务
        /// </summary>
        /// <param name="context">数据库上下文</param>
        public ScriptExecutionService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _nugetCacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget", "packages");

            // 注册到资源管理器
            ResourceManager.RegisterResource(this);

            // 启动定期内存清理
            StartPeriodicMemoryCleanup();
        }

        #endregion

        #region 事件定义

        /// <summary>
        /// 输出事件
        /// </summary>
        public event Action<string>? OutputReceived;

        /// <summary>
        /// 错误输出事件
        /// </summary>
        public event Action<string>? ErrorReceived;

        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event Action<string>? StatusChanged;

        #endregion

        #region 内存管理

        /// <summary>
        /// 启动定期内存清理
        /// </summary>
        private void StartPeriodicMemoryCleanup()
        {
            _memoryCleanupTimer = new Timer(PerformPeriodicMemoryCleanupCallback,
                null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// 执行定期内存清理的回调方法
        /// </summary>
        private void PerformPeriodicMemoryCleanupCallback(object? state)
        {
            _ = Task.Run(async () => await PerformPeriodicMemoryCleanupAsync());
        }

        /// <summary>
        /// 执行定期内存清理
        /// </summary>
        private async Task PerformPeriodicMemoryCleanupAsync()
        {
            if (_disposed || ResourceManager.IsShuttingDown) return;

            try
            {
                var timeSinceLastCleanup = DateTime.Now - _lastMemoryCleanup;
                if (timeSinceLastCleanup.TotalMinutes >= 5)
                {
                    await PerformMemoryCleanupAsync();
                    _lastMemoryCleanup = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"定期内存清理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行内存清理
        /// </summary>
        private async Task PerformMemoryCleanupAsync()
        {
            try
            {
                // 清理临时资源
                CleanupTemporaryResources();

                // 清理智能引用管理器缓存
                SmartReferenceManager.Instance.ClearCache();

                // 执行垃圾回收
                await Task.Run(() =>
                {
                    GC.Collect(2, GCCollectionMode.Optimized, true, true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Optimized, true, true);
                });

                System.Diagnostics.Debug.WriteLine("脚本执行服务内存清理完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"内存清理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理临时资源
        /// </summary>
        private void CleanupTemporaryResources()
        {
            lock (_temporaryResources)
            {
                foreach (var resource in _temporaryResources)
                {
                    try
                    {
                        resource?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"释放临时资源失败: {ex.Message}");
                    }
                }
                _temporaryResources.Clear();
            }
        }

        /// <summary>
        /// 注册临时资源
        /// </summary>
        private void RegisterTemporaryResource(IDisposable resource)
        {
            if (resource != null && !_disposed)
            {
                lock (_temporaryResources)
                {
                    _temporaryResources.Add(resource);
                }
            }
        }

        #endregion

        #region 执行方法实现

        /// <summary>
        /// 执行脚本代码（优化内存管理版本）
        /// </summary>
        public async Task<ScriptExecutionResult> ExecuteAsync(
           string code,
           string title = "脚本执行",
           CancellationToken cancellationToken = default,
           int? scriptId = null,
           int? taskId = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ScriptExecutionService));

            var result = new ScriptExecutionResult
            {
                Code = code,
                Title = title
            };

            var originalOut = Console.Out;
            var originalError = Console.Error;
            ScriptTextWriter? outputWriter = null;
            ScriptTextWriter? errorWriter = null;

            try
            {
                // 设置控制台重定向
                outputWriter = new ScriptTextWriter(text => {
                    result.AppendOutput(text);
                    OnOutputReceived(text);
                });
                errorWriter = new ScriptTextWriter(text => {
                    result.AppendError(text);
                    OnErrorReceived(text);
                });

                Console.SetOut(outputWriter);
                Console.SetError(errorWriter);

                OnStatusChanged("正在智能分析...");

                OnOutputReceived($"开始执行脚本: {title}\n");
                OnOutputReceived($"代码长度: {code.Length} 字符\n");
                OnOutputReceived($"执行时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
                OnOutputReceived("🤖 启用智能引用解析 - 自动处理所有未知引用\n");
                OnOutputReceived(new string('-', 50) + "\n");

                // 智能推荐NuGet包
                var recommendedPackages = SmartNuGetResolver.AnalyzeCodeForNuGetPackages(code);
                if (recommendedPackages.Any())
                {
                    OnOutputReceived("💡 智能推荐的NuGet包:\n");
                    foreach (var pkg in recommendedPackages.Take(5))
                    {
                        OnOutputReceived($"   • {pkg.PackageId}\n");
                    }
                    OnOutputReceived("\n");
                }

                cancellationToken.ThrowIfCancellationRequested();

                // 使用终极编译执行方法
                var returnValue = await CompileAndExecuteScript(code, result, cancellationToken);

                // 显示执行结果
                OnOutputReceived(new string('-', 50) + "\n");
                if (returnValue != null)
                {
                    OnOutputReceived($"✅ 返回值: {returnValue} (类型: {returnValue.GetType().Name})\n");
                }
                else
                {
                    OnOutputReceived("✅ 脚本执行完成，无返回值\n");
                }
                OnOutputReceived($"⏱ 执行完成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

                result.MarkSuccess(returnValue);
                OnStatusChanged("执行成功");
            }
            catch (OperationCanceledException)
            {
                result.MarkCancelled();
                OnOutputReceived("\n⏹ 脚本执行已被用户取消\n");
                OnStatusChanged("已取消");
            }
            catch (Exception ex)
            {
                result.MarkFailed(ex);
                OnErrorReceived($"\n❌ 执行错误: {ex.Message}\n");
                OnStatusChanged("执行失败");
            }
            finally
            {
                // 恢复控制台输出
                Console.SetOut(originalOut);
                Console.SetError(originalError);

                // 释放文本写入器
                outputWriter?.Dispose();
                errorWriter?.Dispose();

                // 执行脚本后强化内存清理
                _ = Task.Run(async () => await ResourceManager.PerformPostScriptExecutionCleanupAsync());
            }

            return result;
        }

        /// <summary>
        /// 执行脚本并保存日志到数据库
        /// </summary>
        public async Task<(ScriptExecutionResult Result, int LogId)> ExecuteAndLogAsync(
            string code, 
            string title = "脚本执行", 
            CancellationToken cancellationToken = default,
            int? scriptId = null,
            int? taskId = null)
        {
            var result = await ExecuteAsync(code, title, cancellationToken, scriptId, taskId);
            var logId = await SaveExecutionLogAsync(result, scriptId, taskId);
            return (result, logId);
        }

        #endregion

        #region 脚本编译和执行

        /// <summary>
        /// 编译并执行脚本（优化内存管理版本）
        /// </summary>
        private async Task<object?> CompileAndExecuteScript(
            string code, 
            ScriptExecutionResult result, 
            CancellationToken cancellationToken)
        {
            OnOutputReceived("正在分析脚本引用需求...\n");

            List<MetadataReference> assemblyRefs = null;
            List<(string, string)> nugetRefs = null;
            Script<object>? compiledScript = null;
            ScriptGlobals? globals = null;

            try
            {
                // 使用智能引用管理器分析代码并获取所需引用
                assemblyRefs = await SmartReferenceManager.Instance.GetReferencesForCodeAsync(code);
                nugetRefs = SmartReferenceManager.Instance.GetNuGetReferences(code);

                OnOutputReceived($"✓ 发现 {assemblyRefs.Count} 个程序集引用\n");
                if (nugetRefs.Any())
                {
                    OnOutputReceived($"✓ 发现 {nugetRefs.Count} 个 NuGet 包引用\n");
                }

                // 移除脚本中的显式 NuGet 引用指令
                var cleanCode = RemoveNugetReferences(code);

                // 创建脚本选项
                var options = ScriptOptions.Default
                    .WithReferences(assemblyRefs)
                    .WithImports(
                        // 全面的命名空间导入
                        "System",
                        "System.IO",
                        "System.Threading",
                        "System.Threading.Tasks",
                        "System.Linq",
                        "System.Collections.Generic",
                        "System.Collections.Concurrent",
                        "System.Text",
                        "System.Text.RegularExpressions",
                        "System.Text.Json",
                        "System.Net",
                        "System.Net.Http",
                        "System.ComponentModel",
                        "System.Diagnostics",
                        "System.Reflection",
                        "System.Security.Cryptography",
                        "System.Xml",
                        "System.Xml.Linq",
                        "System.Data",
                        "System.Runtime.Serialization",
                        "System.Globalization",
                        "Microsoft.CSharp" // 添加动态绑定支持
                    );

                // 处理NuGet包引用
                if (nugetRefs.Count > 0)
                {
                    OnOutputReceived($"正在处理 NuGet 包...\n");
                    var nugetAssemblyRefs = await LoadNugetPackagesAsync(nugetRefs, result, cancellationToken);

                    if (nugetAssemblyRefs.Count > 0)
                    {
                        options = options.WithReferences(options.MetadataReferences.Concat(nugetAssemblyRefs));
                        OnOutputReceived($"✓ 添加了 {nugetAssemblyRefs.Count} 个 NuGet 程序集引用\n");
                    }

                    // 记录到结果中
                    foreach (var (packageId, version) in nugetRefs)
                    {
                        result.NuGetPackages.Add($"{packageId}:{version}");
                    }
                }

                // 手动添加Microsoft.CSharp程序集支持动态绑定
                try
                {
                    var microsoftCSharpAssembly = Assembly.Load("Microsoft.Csharp");
                    if (!string.IsNullOrEmpty(microsoftCSharpAssembly.Location))
                    {
                        var microsoftCSharpRef = MetadataReference.CreateFromFile(microsoftCSharpAssembly.Location);
                        options = options.WithReferences(options.MetadataReferences.Append(microsoftCSharpRef));
                        OnOutputReceived($"✓ 添加了 Microsoft.CSharp 动态绑定支持\n");
                    }
                }
                catch (Exception ex)
                {
                    OnOutputReceived($"⚠️ 无法加载 Microsoft.CSharp: {ex.Message}\n");
                }

                OnOutputReceived($"总计 {options.MetadataReferences.Count()} 个程序集引用可用\n");
                OnOutputReceived(new string('-', 50) + "\n");

                // 创建脚本执行环境
                globals = new ScriptGlobals { CancellationToken = cancellationToken };

                // 编译脚本
                OnOutputReceived("正在编译脚本...\n");
                compiledScript = CSharpScript.Create(cleanCode, options, typeof(ScriptGlobals));

                // 在独立任务中执行脚本
                OnOutputReceived("开始执行脚本...\n");
                var returnValue = await Task.Run(async () =>
                {
                    var scriptState = await compiledScript.RunAsync(globals, cancellationToken);
                    return scriptState.ReturnValue;
                }, cancellationToken);

                return returnValue;
            }
            finally
            {
                // 清理编译过程中的资源
                assemblyRefs?.Clear();
                nugetRefs?.Clear();
                globals = null;
                // 注意：Script对象在.NET中没有显式的Dispose方法，依赖GC
                compiledScript = null;
            }
        }

        /// <summary>
        /// 移除脚本中的 NuGet 引用指令
        /// </summary>
        private string RemoveNugetReferences(string code)
        {
            return NugetReferenceRegex.Replace(code, "");
        }

        #endregion

        #region NuGet 包管理

        /// <summary>
        /// 下载并加载 NuGet 包（优化内存管理版本）
        /// </summary>
        private async Task<List<MetadataReference>> LoadNugetPackagesAsync(
            List<(string PackageId, string Version)> packageReferences,
            ScriptExecutionResult result,
            CancellationToken cancellationToken)
        {
            var references = new List<MetadataReference>();

            if (packageReferences.Count == 0)
                return references;

            OnOutputReceived($"📦 开始处理 {packageReferences.Count} 个 NuGet 包引用...\n");
            OnOutputReceived($"{'='* 60}\n");
            OnStatusChanged($"准备下载 {packageReferences.Count} 个包...");

            SourceRepositoryProvider? sourceRepositoryProvider = null;
            SourceCacheContext? cacheContext = null;

            try
            {
                // 创建 NuGet 配置
                OnOutputReceived($"⚙️ 正在初始化 NuGet 配置...\n");
                var settings = Settings.LoadDefaultSettings(null);
                sourceRepositoryProvider = new SourceRepositoryProvider(
                    new PackageSourceProvider(settings), Repository.Provider.GetCoreV3());

                RegisterTemporaryResource(sourceRepositoryProvider as IDisposable);

                var nugetFramework = NuGetFramework.ParseFolder("net8.0");
                var logger = new NullLogger();
                cacheContext = new SourceCacheContext();

                OnOutputReceived($"🎯 目标框架: {nugetFramework.GetShortFolderName()}\n");
                OnOutputReceived($"📡 NuGet源数量: {sourceRepositoryProvider.GetRepositories().Count()}\n");
                OnOutputReceived($"{'='* 60}\n");

                var totalPackages = packageReferences.Count;
                var processedPackages = 0;
                var successfulPackages = 0;
                var failedPackages = 0;
                var totalAssembliesLoaded = 0;

                foreach (var (packageId, versionString) in packageReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    processedPackages++;

                    try
                    {
                        OnOutputReceived($"\n🔄 [{processedPackages}/{totalPackages}] 正在处理包: {packageId}\n");
                        OnOutputReceived($"🏷️ 请求版本: {versionString}\n");
                        OnStatusChanged($"处理包 {processedPackages}/{totalPackages}: {packageId}");

                        // 解析版本
                        var versionRange = versionString == "*"
                            ? VersionRange.All
                            : VersionRange.Parse(versionString);

                        OnOutputReceived($"🔍 版本范围: {versionRange}\n");

                        // 查找并下载包
                        var packageProcessStartTime = DateTime.Now;
                        var packagePath = await DownloadPackageAsync(
                            packageId, versionRange, sourceRepositoryProvider,
                            nugetFramework, logger, cacheContext, cancellationToken);

                        if (!string.IsNullOrEmpty(packagePath))
                        {
                            // 加载包中的程序集
                            OnOutputReceived($"🔧 正在从包中加载程序集...\n");
                            var assemblyRefs = LoadAssembliesFromPackage(packagePath, nugetFramework);
                            references.AddRange(assemblyRefs);
                            totalAssembliesLoaded += assemblyRefs.Count;

                            var packageProcessDuration = DateTime.Now - packageProcessStartTime;
                            successfulPackages++;

                            OnOutputReceived($"✅ 包处理成功: {packageId}\n");
                            OnOutputReceived($"📚 加载程序集: {assemblyRefs.Count} 个\n");
                            OnOutputReceived($"⏱️ 处理耗时: {packageProcessDuration.TotalSeconds:F2} 秒\n");
                            OnOutputReceived($"📊 总体进度: {processedPackages}/{totalPackages} ({(double)processedPackages/totalPackages*100:F1}%)\n");
                        }
                        else
                        {
                            failedPackages++;
                            OnErrorReceived($"❌ 无法下载包: {packageId}\n");
                        }
                    }
                    catch (Exception ex)
                    {
                        failedPackages++;
                        OnErrorReceived($"❌ 处理包 {packageId} 时出错: {ex.Message}\n");
                        
                        // 如果是关键错误，记录详细信息
                        if (ex is TaskCanceledException || ex is OperationCanceledException)
                        {
                            OnErrorReceived($"⏹️ 包处理被取消: {packageId}\n");
                            throw; // 重新抛出取消异常
                        }
                    }

                    OnOutputReceived($"{new string('-', 50)}\n");
                }

                // 输出最终统计信息
                OnOutputReceived($"\n{'='* 60}\n");
                OnOutputReceived($"📋 NuGet 包处理完成统计报告:\n");
                OnOutputReceived($"{'='* 60}\n");
                OnOutputReceived($"📦 总包数: {totalPackages}\n");
                OnOutputReceived($"✅ 成功: {successfulPackages} 个\n");
                OnOutputReceived($"❌ 失败: {failedPackages} 个\n");
                OnOutputReceived($"📚 总程序集: {totalAssembliesLoaded} 个\n");
                OnOutputReceived($"📊 成功率: {(double)successfulPackages/totalPackages*100:F1}%\n");
                OnOutputReceived($"{'='* 60}\n");

                if (successfulPackages > 0)
                {
                    OnOutputReceived($"🎉 NuGet 包处理成功，共加载了 {references.Count} 个程序集引用\n");
                    OnStatusChanged($"包处理完成: {successfulPackages}/{totalPackages} 成功");
                }
                else
                {
                    OnErrorReceived($"😞 所有 NuGet 包都无法成功加载\n");
                    OnStatusChanged("包处理失败");
                }
            }
            catch (OperationCanceledException)
            {
                OnOutputReceived($"\n⏹️ NuGet 包处理被用户取消\n");
                OnStatusChanged("包处理已取消");
                throw;
            }
            catch (Exception ex)
            {
                OnErrorReceived($"❌ NuGet 包处理发生严重错误: {ex.Message}\n");
                OnStatusChanged("包处理错误");
            }
            finally
            {
                // 清理NuGet相关资源
                cacheContext?.Dispose();
            }

            return references;
        }

        /// <summary>
        /// 下载 NuGet 包（优化版本）
        /// </summary>
        private async Task<string?> DownloadPackageAsync(
            string packageId,
            VersionRange versionRange,
            SourceRepositoryProvider sourceRepositoryProvider,
            NuGetFramework targetFramework,
            ILogger logger,
            SourceCacheContext cacheContext,
            CancellationToken cancellationToken)
        {
            try
            {
                OnOutputReceived($"🔍 开始查找包: {packageId}\n");
                OnStatusChanged($"正在查找 {packageId}...");
                
                var repositories = sourceRepositoryProvider.GetRepositories();

                OnOutputReceived($"📡 可用的NuGet源: {repositories.Count()} 个\n");

                foreach (var (repository, index) in repositories.Select((repo, idx) => (repo, idx + 1)))
                {
                    try
                    {
                        OnOutputReceived($"🌐 [{index}/{repositories.Count()}] 正在尝试源: {repository.PackageSource.Source}\n");
                        OnStatusChanged($"正在从源 {index} 查找包...");

                        var findPackageByIdResource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
                        if (findPackageByIdResource == null) 
                        {
                            OnOutputReceived($"⚠️ 源 {repository.PackageSource.Source} 不支持查找包\n");
                            continue;
                        }

                        OnOutputReceived($"🔎 正在查询包版本信息...\n");
                        var versions = await findPackageByIdResource.GetAllVersionsAsync(packageId, cacheContext, logger, cancellationToken);
                        
                        if (!versions.Any())
                        {
                            OnOutputReceived($"❌ 在此源中未找到包 {packageId}\n");
                            continue;
                        }

                        OnOutputReceived($"📦 找到 {versions.Count()} 个可用版本\n");

                        // 选择最佳版本
                        var bestVersion = SelectBestVersion(versions, versionRange);
                        
                        if (bestVersion == null) 
                        {
                            OnOutputReceived($"⚠️ 没有满足版本要求的包版本\n");
                            continue;
                        }

                        OnOutputReceived($"✅ 最终选定版本: {bestVersion}\n");

                        // 构建本地包路径
                        var packagePath = Path.Combine(_nugetCacheDir, packageId.ToLowerInvariant(), bestVersion.ToString());
                        var nupkgPath = Path.Combine(packagePath, $"{packageId.ToLowerInvariant()}.{bestVersion}.nupkg");

                        // 如果包已存在，返回路径
                        if (Directory.Exists(packagePath) && File.Exists(nupkgPath))
                        {
                            OnOutputReceived($"💾 使用本地缓存: {packageId} v{bestVersion}\n");
                            OnStatusChanged($"使用缓存包 {packageId}");
                            return packagePath;
                        }

                        // 下载包
                        return await DownloadPackageToPathAsync(
                            packageId, bestVersion, packagePath, nupkgPath,
                            findPackageByIdResource, cacheContext, logger, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        OnErrorReceived($"❌ 从源 {repository.PackageSource.Source} 下载包时出错: {ex.Message}\n");
                        OnOutputReceived($"🔄 继续尝试下一个源...\n");
                    }
                }

                OnErrorReceived($"❌ 所有源都无法下载包: {packageId}\n");
                OnStatusChanged($"下载失败 - {packageId}");
                return null;
            }
            catch (Exception ex)
            {
                OnErrorReceived($"❌ 下载包 {packageId} 时发生错误: {ex.Message}\n");
                OnStatusChanged($"下载错误 - {packageId}");
                return null;
            }
        }

        /// <summary>
        /// 选择最佳版本
        /// </summary>
        private NuGetVersion? SelectBestVersion(IEnumerable<NuGetVersion> versions, VersionRange versionRange)
        {
            var versionString = versionRange.OriginalString ?? "*";
            if (string.IsNullOrEmpty(versionString))
            {
                versionString = versionRange.ToString();
            }

            // 如果用户指定了精确版本，优先查找精确匹配
            if (versionString != "*" && !versionRange.IsFloating)
            {
                var exactVersion = versions.FirstOrDefault(v => v.ToString() == versionString);
                if (exactVersion != null)
                {
                    OnOutputReceived($"🎯 找到精确版本匹配: {exactVersion}\n");
                    return exactVersion;
                }

                // 查找满足条件的最接近版本
                var bestVersion = versions.Where(v => versionRange.Satisfies(v))
                                         .OrderBy(v => Math.Abs(v.CompareTo(versionRange.MinVersion ?? v)))
                                         .FirstOrDefault();
                
                if (bestVersion != null)
                {
                    OnOutputReceived($"⚠️ 精确版本 {versionString} 不存在，选择最接近版本: {bestVersion}\n");
                }
                return bestVersion;
            }
            else
            {
                // 选择最新版本
                var bestVersion = versions.Where(v => versionRange.Satisfies(v))
                                         .OrderByDescending(v => v)
                                         .FirstOrDefault();
                
                if (bestVersion != null)
                {
                    OnOutputReceived($"🔄 选择范围内最新版本: {bestVersion}\n");
                }
                return bestVersion;
            }
        }

        /// <summary>
        /// 下载包到指定路径
        /// </summary>
        private async Task<string?> DownloadPackageToPathAsync(
            string packageId,
            NuGetVersion bestVersion,
            string packagePath,
            string nupkgPath,
            FindPackageByIdResource findPackageByIdResource,
            SourceCacheContext cacheContext,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            OnOutputReceived($"⬇️ 开始下载包: {packageId} v{bestVersion}\n");
            OnStatusChanged($"正在下载 {packageId} v{bestVersion}...");

            Directory.CreateDirectory(packagePath);

            ProgressReportingFileStream? packageStream = null;
            try
            {
                // 使用带进度监控的下载流
                packageStream = new ProgressReportingFileStream(
                    nupkgPath, 
                    (bytesDownloaded, totalBytes) => {
                        if (totalBytes > 0)
                        {
                            var percentage = (double)bytesDownloaded / totalBytes * 100;
                            OnOutputReceived($"\r📥 下载进度: {FormatBytes(bytesDownloaded)}/{FormatBytes(totalBytes)} ({percentage:F1}%)");
                            OnStatusChanged($"下载中 {percentage:F0}% - {packageId}");
                        }
                        else
                        {
                            OnOutputReceived($"\r📥 已下载: {FormatBytes(bytesDownloaded)}");
                            OnStatusChanged($"下载中 - {packageId}");
                        }
                    });

                RegisterTemporaryResource(packageStream);

                var downloadStartTime = DateTime.Now;
                var downloadSuccess = await findPackageByIdResource.CopyNupkgToStreamAsync(
                    packageId, bestVersion, packageStream, cacheContext, logger, cancellationToken);

                var downloadDuration = DateTime.Now - downloadStartTime;
                var fileSizeBytes = packageStream.Length;

                if (downloadSuccess)
                {
                    OnOutputReceived($"\n✅ 下载完成: {packageId} v{bestVersion}\n");
                    OnOutputReceived($"📊 文件大小: {FormatBytes(fileSizeBytes)}\n");
                    OnOutputReceived($"⏱️ 下载耗时: {downloadDuration.TotalSeconds:F2} 秒\n");
                    
                    if (downloadDuration.TotalSeconds > 0)
                    {
                        var downloadSpeed = fileSizeBytes / downloadDuration.TotalSeconds;
                        OnOutputReceived($"🚀 平均速度: {FormatBytes((long)downloadSpeed)}/秒\n");
                    }
                    
                    OnStatusChanged($"下载完成 - {packageId}");
                    return packagePath;
                }
                else
                {
                    OnErrorReceived($"❌ 下载失败: {packageId} v{bestVersion}\n");
                    // 清理失败的下载文件
                    try { File.Delete(nupkgPath); } catch { }
                    return null;
                }
            }
            finally
            {
                packageStream?.Dispose();
            }
        }

        /// <summary>
        /// 格式化字节数为可读的大小
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        /// <summary>
        /// 带进度报告的文件流（优化版本）
        /// </summary>
        private class ProgressReportingFileStream : FileStream
        {
            private readonly Action<long, long> _progressCallback;
            private long _totalBytesWritten = 0;
            private long _totalSize = 0;
            private DateTime _lastProgressReport = DateTime.MinValue;

            public ProgressReportingFileStream(string path, Action<long, long> progressCallback) 
                : base(path, FileMode.Create, FileAccess.Write)
            {
                _progressCallback = progressCallback;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                base.Write(buffer, offset, count);
                _totalBytesWritten += count;
                ReportProgress();
            }

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                await base.WriteAsync(buffer, offset, count, cancellationToken);
                _totalBytesWritten += count;
                ReportProgress();
            }

            private void ReportProgress()
            {
                // 限制进度报告频率，避免输出过于频繁
                if (DateTime.Now - _lastProgressReport > TimeSpan.FromMilliseconds(500))
                {
                    _progressCallback?.Invoke(_totalBytesWritten, _totalSize);
                    _lastProgressReport = DateTime.Now;
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // 最后一次进度报告
                    _progressCallback?.Invoke(_totalBytesWritten, _totalSize);
                }
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// 从 NuGet 包中加载程序集（优化版本）
        /// </summary>
        private List<MetadataReference> LoadAssembliesFromPackage(string packagePath, NuGetFramework targetFramework)
        {
            var references = new List<MetadataReference>();

            try
            {
                OnOutputReceived($"📁 正在分析包目录: {Path.GetFileName(packagePath)}\n");
                
                // 查找 lib 目录
                var libPath = Path.Combine(packagePath, "lib");
                if (!Directory.Exists(libPath))
                {
                    OnOutputReceived($"📦 lib目录不存在，尝试解压nupkg文件...\n");
                    
                    // 尝试解压 nupkg 文件
                    var nupkgFiles = Directory.GetFiles(packagePath, "*.nupkg");
                    if (nupkgFiles.Length > 0)
                    {
                        OnOutputReceived($"🗜️ 找到nupkg文件: {Path.GetFileName(nupkgFiles[0])}\n");
                        ExtractNupkgFile(nupkgFiles[0], packagePath);
                        OnOutputReceived($"✅ nupkg文件解压完成\n");
                    }
                    else
                    {
                        OnErrorReceived($"❌ 未找到nupkg文件\n");
                        return references;
                    }
                }

                if (Directory.Exists(libPath))
                {
                    var compatibleFramework = FindCompatibleFramework(libPath, targetFramework);
                    if (compatibleFramework != null)
                    {
                        var frameworkPath = Path.Combine(libPath, compatibleFramework);
                        var dllFiles = Directory.GetFiles(frameworkPath, "*.dll", SearchOption.AllDirectories);

                        foreach (var dllFile in dllFiles)
                        {
                            try
                            {
                                if (!IsUnmanagedDll(dllFile))
                                {
                                    var metadataRef = MetadataReference.CreateFromFile(dllFile);
                                    references.Add(metadataRef);
                                }
                            }
                            catch
                            {
                                // 忽略加载失败的程序集
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorReceived($"❌ 从包 {Path.GetFileName(packagePath)} 加载程序集时出错: {ex.Message}\n");
            }

            return references;
        }

        /// <summary>
        /// 解压 nupkg 文件
        /// </summary>
        private void ExtractNupkgFile(string nupkgPath, string extractPath)
        {
            try
            {
                using var archive = ZipFile.OpenRead(nupkgPath);
                foreach (var entry in archive.Entries)
                {
                    try
                    {
                        var entryPath = Path.Combine(extractPath, entry.FullName);
                        var entryDir = Path.GetDirectoryName(entryPath);

                        if (!string.IsNullOrEmpty(entryDir))
                            Directory.CreateDirectory(entryDir);

                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            entry.ExtractToFile(entryPath, true);
                        }
                    }
                    catch
                    {
                        // 忽略解压失败的文件
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorReceived($"❌ 解压包文件失败: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 查找兼容的目标框架
        /// </summary>
        private string? FindCompatibleFramework(string libPath, NuGetFramework targetFramework)
        {
            try
            {
                var frameworks = Directory.GetDirectories(libPath)
                    .Select(d => Path.GetFileName(d))
                    .Select(f => NuGetFramework.ParseFolder(f))
                    .Where(f => f != null)
                    .ToList();

                var reducer = new FrameworkReducer();
                var bestFramework = reducer.GetNearest(targetFramework, frameworks);
                return bestFramework?.GetShortFolderName();
            }
            catch
            {
                var dirs = Directory.GetDirectories(libPath);
                return dirs.Length > 0 ? Path.GetFileName(dirs[0]) : null;
            }
        }

        /// <summary>
        /// 检查是否为非托管DLL文件
        /// </summary>
        private bool IsUnmanagedDll(string dllPath)
        {
            try
            {
                var fileName = Path.GetFileName(dllPath).ToLowerInvariant();
                
                var unmanagedPatterns = new[]
                {
                    "chrome", "chromium", "libcef", "cef_", "vulkan", "d3d", "opengl", "gpu",
                    "ffmpeg", "avcodec", "curl", "ssl", "crypto", "zlib", "_native", ".native",
                    "native_", "native.", "win32", "x64", "x86", "api-ms-", "ucrtbase",
                    "vcruntime", "msvcp", "msvcr", "sqlite3", "libskia", "icu", "node_"
                };

                return unmanagedPatterns.Any(pattern => fileName.Contains(pattern));
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 控制台重定向

        /// <summary>
        /// 脚本专用文本写入器（优化版本）
        /// </summary>
        private class ScriptTextWriter : TextWriter, IDisposable
        {
            private readonly Action<string> _writeAction;
            private bool _disposed = false;

            public ScriptTextWriter(Action<string> writeAction)
            {
                _writeAction = writeAction;
            }

            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(char value)
            {
                if (!_disposed)
                    _writeAction?.Invoke(value.ToString());
            }

            public override void Write(string? value)
            {
                if (!_disposed && !string.IsNullOrEmpty(value))
                    _writeAction?.Invoke(value);
            }

            public override void WriteLine(string? value)
            {
                if (!_disposed)
                    _writeAction?.Invoke((value ?? "") + Environment.NewLine);
            }

            public override void WriteLine()
            {
                if (!_disposed)
                    _writeAction?.Invoke(Environment.NewLine);
            }

            protected override void Dispose(bool disposing)
            {
                if (!_disposed && disposing)
                {
                    _disposed = true;
                }
                base.Dispose(disposing);
            }
        }

        #endregion

        #region 日志管理实现

        /// <summary>
        /// 保存执行日志到数据库
        /// </summary>
        public async Task<int> SaveExecutionLogAsync(ScriptExecutionResult result, int? scriptId = null, int? taskId = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ScriptExecutionService));

            try
            {
                await EnsureExecutionLogTableExists();

                var log = result.ToExecutionLog(scriptId, taskId);
                
                // 验证和修正数据
                if (string.IsNullOrWhiteSpace(log.Title))
                    log.Title = "脚本执行";
                
                if (string.IsNullOrWhiteSpace(log.Code))
                    log.Code = "";

                // 限制文本字段长度
                log.Title = TruncateString(log.Title, 200);
                log.Output = log.Output ?? "";
                log.ErrorOutput = log.ErrorOutput ?? "";
                log.Exception = TruncateString(log.Exception, 8000);
                log.ReturnValue = TruncateString(log.ReturnValue, 4000);
                log.ReturnValueType = TruncateString(log.ReturnValueType, 200);
                log.MachineName = TruncateString(log.MachineName ?? Environment.MachineName, 100);
                log.UserName = TruncateString(log.UserName ?? Environment.UserName, 100);
                log.Status = TruncateString(log.Status ?? "Failed", 50);

                // 验证外键约束
                if (log.ScriptId.HasValue)
                {
                    var scriptExists = await _context.Scripts.AnyAsync(s => s.Id == log.ScriptId.Value);
                    if (!scriptExists)
                        log.ScriptId = null;
                }

                if (log.TaskId.HasValue)
                {
                    var taskExists = await _context.Tasks.AnyAsync(t => t.Id == log.TaskId.Value);
                    if (!taskExists)
                        log.TaskId = null;
                }

                if (log.StartTime == default)
                    log.StartTime = DateTime.Now;

                if (log.Duration < 0)
                    log.Duration = 0;

                _context.ScriptExecutionLogs.Add(log);
                await _context.SaveChangesAsync();
                return log.Id;
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException?.Message ?? "无内部异常";
                var detailedMessage = $"保存执行日志失败: {ex.Message}. 内部异常: {innerException}";
                throw new InvalidOperationException(detailedMessage, ex);
            }
        }

        /// <summary>
        /// 确保执行日志表存在
        /// </summary>
        private async Task EnsureExecutionLogTableExists()
        {
            try
            {
                await _context.ScriptExecutionLogs.CountAsync();
            }
            catch (Exception)
            {
                var dataService = App.GetRequiredService<IDataService>();
                await dataService.InitializeDatabaseAsync();
            }
        }

        /// <summary>
        /// 截断字符串到指定长度
        /// </summary>
        private static string? TruncateString(string? input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return input;
                
            return input.Length <= maxLength ? input : input.Substring(0, maxLength);
        }

        /// <summary>
        /// 获取执行日志列表
        /// </summary>
        public async Task<IEnumerable<ScriptExecutionLog>> GetExecutionLogsAsync(
            int? scriptId = null, 
            int? taskId = null, 
            string? status = null,
            int pageIndex = 0, 
            int pageSize = 50)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ScriptExecutionService));

            try
            {
                var query = _context.ScriptExecutionLogs
                    .Include(l => l.Script)
                    .Include(l => l.Task)
                    .AsQueryable();

                if (scriptId.HasValue)
                    query = query.Where(l => l.ScriptId == scriptId.Value);

                if (taskId.HasValue)
                    query = query.Where(l => l.TaskId == taskId.Value);

                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(l => l.Status == status);

                return await query
                    .OrderByDescending(l => l.StartTime)
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取执行日志失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取执行日志详情
        /// </summary>
        public async Task<ScriptExecutionLog?> GetExecutionLogAsync(int logId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ScriptExecutionService));

            try
            {
                return await _context.ScriptExecutionLogs
                    .Include(l => l.Script)
                    .Include(l => l.Task)
                    .FirstOrDefaultAsync(l => l.Id == logId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取执行日志详情失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除执行日志
        /// </summary>
        public async Task<bool> DeleteExecutionLogAsync(int logId)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ScriptExecutionService));

            try
            {
                var log = await _context.ScriptExecutionLogs.FindAsync(logId);
                if (log != null)
                {
                    _context.ScriptExecutionLogs.Remove(log);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"删除执行日志失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 清理过期的执行日志
        /// </summary>
        public async Task<int> CleanupOldLogsAsync(int daysToKeep = 30)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ScriptExecutionService));

            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var oldLogs = await _context.ScriptExecutionLogs
                    .Where(l => l.StartTime < cutoffDate)
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    _context.ScriptExecutionLogs.RemoveRange(oldLogs);
                    await _context.SaveChangesAsync();
                }

                return oldLogs.Count;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"清理过期日志失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取执行统计信息
        /// </summary>
        public async Task<ExecutionStatistics> GetExecutionStatisticsAsync(int? scriptId = null, int? taskId = null, int days = 30)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ScriptExecutionService));

            try
            {
                var startDate = DateTime.Now.AddDays(-days);
                var query = _context.ScriptExecutionLogs.AsQueryable();

                if (scriptId.HasValue)
                    query = query.Where(l => l.ScriptId == scriptId.Value);

                if (taskId.HasValue)
                    query = query.Where(l => l.TaskId == taskId.Value);

                query = query.Where(l => l.StartTime >= startDate);

                var logs = await query.ToListAsync();

                return new ExecutionStatistics
                {
                    TotalExecutions = logs.Count,
                    SuccessfulExecutions = logs.Count(l => l.Status == "Success"),
                    FailedExecutions = logs.Count(l => l.Status == "Failed"),
                    CancelledExecutions = logs.Count(l => l.Status == "Cancelled"),
                    AverageExecutionTime = logs.Any() ? logs.Average(l => l.Duration) : 0,
                    LastExecutionTime = logs.Any() ? logs.Max(l => l.StartTime) : null
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取执行统计信息失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 事件触发方法

        protected virtual void OnOutputReceived(string text)
        {
            if (!_disposed)
                OutputReceived?.Invoke(text);
        }

        protected virtual void OnErrorReceived(string text)
        {
            if (!_disposed)
                ErrorReceived?.Invoke(text);
        }

        protected virtual void OnStatusChanged(string status)
        {
            if (!_disposed)
                StatusChanged?.Invoke(status);
        }

        #endregion

        #region 辅助类

        public class ScriptGlobals
        {
            public CancellationToken CancellationToken { get; set; }
        }

        private class NullLogger : ILogger
        {
            public void LogDebug(string data) { }
            public void LogVerbose(string data) { }
            public void LogInformation(string data) { }
            public void LogMinimal(string data) { }
            public void LogWarning(string data) { }
            public void LogError(string data) { }
            public void LogInformationSummary(string data) { }
            public void Log(LogLevel level, string data) { }
            public void Log(ILogMessage message) { }
            public Task LogAsync(LogLevel level, string data) => Task.CompletedTask;
            public Task LogAsync(ILogMessage message) => Task.CompletedTask;
        }

        #endregion

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
                    // 停止定期内存清理
                    _memoryCleanupTimer?.Dispose();
                    _memoryCleanupTimer = null;

                    // 清理临时资源
                    CleanupTemporaryResources();

                    // 清理智能引用管理器缓存
                    SmartReferenceManager.Instance.ClearCache();

                    // 执行垃圾回收
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    _disposed = true;
                    System.Diagnostics.Debug.WriteLine("ScriptExecutionService 资源释放完成");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"释放 ScriptExecutionService 资源时发生异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~ScriptExecutionService()
        {
            Dispose(false);
        }

        #endregion
    }
}