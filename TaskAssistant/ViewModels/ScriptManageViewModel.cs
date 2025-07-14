using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskAssistant.Data.Services;
using TaskAssistant.Models;
using TaskAssistant.Services;
using TaskAssistant.View;

namespace TaskAssistant.ViewModels
{
    /// <summary>
    /// 脚本管理视图模型类
    /// 负责管理脚本编辑界面的数据和业务逻辑，包括脚本信息管理、模板管理、脚本执行等功能
    /// 使用 CommunityToolkit.Mvvm 库实现 MVVM 模式，支持属性变更通知和命令绑定
    /// </summary> 
    public partial class ScriptManageViewModel : ObservableObject 
    {
        #region 私有字段
        
        /// <summary>
        /// 导航服务实例，用于页面间的导航跳转
        /// 通过依赖注入方式获取，确保与主窗口的导航系统集成
        /// </summary>
        private readonly INavigationService _navigationService;

        /// <summary>
        /// 当前编辑的脚本信息，如果为 null 表示创建新脚本
        /// </summary>
        private ScriptInfo? _currentEditingScript;

        /// <summary>
        /// 标记是否正在加载脚本数据，避免在加载过程中触发模板变更
        /// </summary>
        private bool _isLoadingScript = false;

        #endregion

        #region 可Observable属性

        /// <summary>
        /// 脚本名称属性
        /// 使用 ObservableProperty 特性自动生成属性变更通知代码
        /// 绑定到 UI 中的脚本名称输入框
        /// </summary>
        [ObservableProperty]
        private string _scriptName = string.Empty;

        /// <summary>
        /// 脚本描述属性
        /// 用于存储脚本的详细描述信息
        /// 绑定到 UI 中的描述输入框
        /// </summary>
        [ObservableProperty]
        private string _description = string.Empty;

        /// <summary>
        /// 脚本版本号属性
        /// 默认值为 "1.0.0"，遵循语义化版本规范
        /// 绑定到 UI 中的版本号输入框
        /// </summary>
        [ObservableProperty]
        private string _version = "1.0.0";

        /// <summary>
        /// 脚本作者属性
        /// 用于记录脚本的创建者信息
        /// 绑定到 UI 中的作者输入框
        /// </summary>
        [ObservableProperty]
        private string _author = string.Empty;

        /// <summary>
        /// 脚本代码内容属性
        /// 存储实际的 C# 脚本代码，支持执行和保存
        /// 与 AvalonEdit 代码编辑器通过事件同步
        /// </summary>
        [ObservableProperty]
        private string _code = string.Empty;

        /// <summary>
        /// 当前选中的脚本模板
        /// 当用户选择不同模板时，会自动更新代码内容
        /// 绑定到 UI 中的模板下拉框
        /// </summary>
        [ObservableProperty]
        private ScriptTemplate? _selectedTemplate;

        /// <summary>
        /// 脚本模板集合
        /// 包含预定义的脚本示例，帮助用户快速开始编写脚本
        /// 绑定到 UI 中的模板下拉框的数据源
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ScriptTemplate> _scriptTemplates;

        // 替代转换器的属性

        /// <summary>
        /// 脚本名称错误提示是否可见
        /// 当脚本名称为空时显示错误提示
        /// </summary>
        [ObservableProperty]
        private Visibility _scriptNameHintVisibility = Visibility.Visible;

        /// <summary>
        /// 代码错误提示是否可见
        /// 当代码内容为空时显示错误提示
        /// </summary>
        [ObservableProperty]
        private Visibility _codeHintVisibility = Visibility.Visible;

        /// <summary>
        /// 执行按钮的工具提示文本
        /// 根据代码内容动态更新
        /// </summary>
        [ObservableProperty]
        private string _executeButtonTooltip = "请先输入代码才能执行";

        /// <summary>
        /// 保存按钮的工具提示文本
        /// 根据脚本名称和代码内容动态更新
        /// </summary>
        [ObservableProperty]
        private string _saveButtonTooltip = "请先输入脚本名和代码才能保存";

        /// <summary>
        /// 页面标题，根据是编辑还是新建动态显示
        /// </summary>
        [ObservableProperty]
        private string _pageTitle = "脚本编辑器";

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化脚本管理视图模型
        /// 设置导航服务并初始化脚本模板数据
        /// </summary>
        /// <param name="navigationService">导航服务实例，用于页面导航</param>
        /// <exception cref="ArgumentNullException">当导航服务为 null 时抛出</exception>
        public ScriptManageViewModel(INavigationService navigationService)
        {
            // 验证依赖项并保存引用
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            // 初始化脚本模板集合
            InitializeTemplates();

            // 设置默认选中第一个模板
            SelectedTemplate = ScriptTemplates.FirstOrDefault();

            // 初始化工具提示
            UpdateTooltips();
        }
        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化脚本模板
        /// </summary>
        private void InitializeTemplates()
        {
            ScriptTemplates = new ObservableCollection<ScriptTemplate>
            {
                // 简化的基础示例 - 自动程序集引用
                new("基础循环示例", @"// 基础循环输出示例（自动程序集引用）
using System;
using System.Threading;
using System.Threading.Tasks;

Console.WriteLine(""开始执行基础循环示例..."");

for (int i = 1; i <= 100; i++)
{
    CancellationToken.ThrowIfCancellationRequested();
    
    Console.WriteLine($""正在处理第 {i} 项..."");
    await Task.Delay(50, CancellationToken);
    
    if (i % 10 == 0)
    {
        Console.WriteLine($""已完成 {i}%"");
    }
}

Console.WriteLine(""所有任务执行完成！"");
return ""基础循环示例执行完成"";"),

                // HTTP请求示例 - 使用智能引用
                new("HTTP请求示例", @"// HTTP请求示例（使用智能引用系统）
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

Console.WriteLine(""开始HTTP请求示例..."");

using var client = new HttpClient();
client.DefaultRequestHeaders.Add(""User-Agent"", ""TaskAssistant/1.0"");

try
{
    var response = await client.GetAsync(""https://httpbin.org/json"", CancellationToken);
    
    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($""响应内容: {content}"");
        Console.WriteLine(""HTTP请求成功"");
    }
    else
    {
        Console.WriteLine($""HTTP请求失败，状态码: {response.StatusCode}"");
    }
    
    return ""HTTP请求完成"";
}
catch (Exception ex)
{
    Console.WriteLine($""错误: {ex.Message}"");
    return ""请求失败"";
}"),

                // 文件操作示例
                new("文件操作示例", @"// 文件操作示例（智能引用系统）
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

Console.WriteLine(""开始文件操作示例..."");

try
{
    var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    var fileName = $""TaskAssistant_Test_{DateTime.Now:yyyyMMdd_HHmmss}.txt"";
    var filePath = Path.Combine(desktop, fileName);
    
    Console.WriteLine($""文件路径: {filePath}"");
    
    // 写入文件
    var content = $@""TaskAssistant 测试文件
创建时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
机器名称: {Environment.MachineName}
用户名称: {Environment.UserName}
工作目录: {Environment.CurrentDirectory}
"";
    
    await File.WriteAllTextAsync(filePath, content, CancellationToken);
    Console.WriteLine(""文件写入完成"");
    
    // 读取文件
    var readContent = await File.ReadAllTextAsync(filePath, CancellationToken);
    Console.WriteLine(""文件内容:"");
    Console.WriteLine(readContent);
    
    // 文件信息
    var info = new FileInfo(filePath);
    Console.WriteLine($""文件大小: {info.Length} 字节"");
    Console.WriteLine($""创建时间: {info.CreationTime}"");
    
    return ""文件操作完成"";
}
catch (Exception ex)
{
    Console.WriteLine($""错误: {ex.Message}"");
    return ""操作失败"";
}"),

                // JSON处理示例
                new("JSON处理示例", @"// JSON处理示例（使用内置System.Text.Json）
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

Console.WriteLine(""开始JSON处理示例..."");

try
{
    // 创建示例数据
    var data = new
    {
        Name = ""TaskAssistant"",
        Version = ""1.0.0"",
        CreatedAt = DateTime.Now,
        Features = new[] { ""脚本执行"", ""任务管理"", ""智能引用"" },
        Settings = new
        {
            AutoSave = true,
            MaxRetries = 3
        }
    };
    
    // 序列化为JSON
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    var json = JsonSerializer.Serialize(data, options);
    Console.WriteLine(""JSON序列化结果:"");
    Console.WriteLine(json);
    
    // 反序列化
    using var document = JsonDocument.Parse(json);
    var root = document.RootElement;
    
    Console.WriteLine($""应用名称: {root.GetProperty(""name"").GetString()}"");
    Console.WriteLine($""版本号: {root.GetProperty(""version"").GetString()}"");
    
    var features = root.GetProperty(""features"");
    Console.WriteLine(""功能列表:"");
    foreach (var feature in features.EnumerateArray())
    {
        Console.WriteLine($""  - {feature.GetString()}"");
    }
    
    return ""JSON处理完成"";
}
catch (Exception ex)
{
    Console.WriteLine($""错误: {ex.Message}"");
    return ""处理失败"";
}"),

                // 进程管理示例
                new("进程管理示例", @"// 进程管理示例（智能引用系统）
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

Console.WriteLine(""开始进程管理示例..."");

try
{
    // 获取当前进程信息
    var currentProcess = Process.GetCurrentProcess();
    Console.WriteLine($""当前进程: {currentProcess.ProcessName} (PID: {currentProcess.Id})"");
    Console.WriteLine($""内存使用: {currentProcess.WorkingSet64 / 1024 / 1024} MB"");
    Console.WriteLine($""启动时间: {currentProcess.StartTime}"");
    Console.WriteLine($""运行时间: {DateTime.Now - currentProcess.StartTime}"");
    
    // 获取系统进程信息
    var processes = Process.GetProcesses()
        .Where(p => !string.IsNullOrEmpty(p.ProcessName))
        .OrderByDescending(p => p.WorkingSet64)
        .Take(5)
        .ToList();
    
    Console.WriteLine($""\n内存使用最多的5个进程:"");
    foreach (var process in processes)
    {
        try
        {
            var memoryMB = process.WorkingSet64 / 1024 / 1024;
            Console.WriteLine($""  {process.ProcessName}: {memoryMB} MB"");
        }
        catch
        {
            // 某些进程可能无法访问
        }
    }
    
    // 执行简单的命令
    Console.WriteLine($""\n执行系统命令:"");
    var startInfo = new ProcessStartInfo
    {
        FileName = ""cmd.exe"",
        Arguments = ""/c echo Hello from TaskAssistant!"",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    
    using var process = Process.Start(startInfo);
    if (process != null)
    {
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync(CancellationToken);
        Console.WriteLine($""命令输出: {output.Trim()}"");
    }
    
    return ""进程管理示例完成"";
}
catch (Exception ex)
{
    Console.WriteLine($""错误: {ex.Message}"");
    return ""示例失败"";
}"),

                // 配置需要NuGet包的示例
                new("网页爬虫示例（需要NuGet包）", @"// 网页爬虫示例（需要HtmlAgilityPack包）
// 请在 脚本引用设置 中启用 HtmlAgilityPack 包，或在代码中添加以下引用：
// #r ""nuget:HtmlAgilityPack,1.11.46""

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

Console.WriteLine(""开始网页爬虫示例..."");

using var client = new HttpClient();
client.DefaultRequestHeaders.Add(""User-Agent"", ""Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"");

try
{
    var html = await client.GetStringAsync(""https://httpbin.org/html"");
    
    var doc = new HtmlDocument();
    doc.LoadHtml(html);
    
    // 提取标题
    var title = doc.DocumentNode.SelectSingleNode(""//title"")?.InnerText;
    Console.WriteLine($""页面标题: {title}"");
    
    // 提取所有链接
    var links = doc.DocumentNode.SelectNodes(""//a[@href]"");
    if (links != null)
    {
        Console.WriteLine($""找到 {links.Count} 个链接:"");
        foreach (var link in links.Take(5))
        {
            var href = link.GetAttributeValue(""href"", """");
            var text = link.InnerText.Trim();
            Console.WriteLine($""  {href} : {text}"");
        }
    }
    
    return ""网页爬虫完成"";
}
catch (Exception ex) when (ex.Message.Contains(""HtmlAgilityPack""))
{
    Console.WriteLine(""缺少 HtmlAgilityPack 包，请在设置中启用或在代码中添加引用。"");
    return ""需要安装依赖包"";
}
catch (Exception ex)
{
    Console.WriteLine($""错误: {ex.Message}"");
    return ""爬虫失败"";
}"),

                // SQLite数据库示例
                new("SQLite数据库示例（需要NuGet包）", @"// SQLite数据库示例（需要Microsoft.Data.Sqlite包）
// 请在 脚本引用设置 中启用 Microsoft.Data.Sqlite 包，或在代码中添加以下引用：
// #r ""nuget:Microsoft.Data.Sqlite,8.0.0""

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

Console.WriteLine(""开始SQLite数据库示例..."");

try
{
    var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), ""TaskAssistant_Test.db"");
    var connectionString = $""Data Source={dbPath}"";
    
    Console.WriteLine($""数据库路径: {dbPath}"");
    
    using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync(CancellationToken);
    
    // 创建表
    var createTableCmd = connection.CreateCommand();
    createTableCmd.CommandText = @""
        CREATE TABLE IF NOT EXISTS TestLogs (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Message TEXT NOT NULL,
            CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
        )"";
    await createTableCmd.ExecuteNonQueryAsync(CancellationToken);
    Console.WriteLine(""数据表创建成功"");
    
    // 插入数据
    var insertCmd = connection.CreateCommand();
    insertCmd.CommandText = ""INSERT INTO TestLogs (Message) VALUES (@message)"";
    insertCmd.Parameters.AddWithValue(""@message"", $""TaskAssistant测试记录 - {DateTime.Now}"");
    await insertCmd.ExecuteNonQueryAsync(CancellationToken);
    Console.WriteLine(""数据插入成功"");
    
    // 查询数据
    var selectCmd = connection.CreateCommand();
    selectCmd.CommandText = ""SELECT Id, Message, CreatedAt FROM TestLogs ORDER BY Id DESC LIMIT 5"";
    using var reader = await selectCmd.ExecuteReaderAsync(CancellationToken);
    
    Console.WriteLine(""最近的5条记录:"");
    while (await reader.ReadAsync(CancellationToken))
    {
        Console.WriteLine($""  ID: {reader[""Id""]}, 消息: {reader[""Message""]}, 时间: {reader[""CreatedAt""]}"");
    }
    
    return ""数据库操作完成"";
}
catch (Exception ex) when (ex.Message.Contains(""Microsoft.Data.Sqlite""))
{
    Console.WriteLine(""缺少 Microsoft.Data.Sqlite 包，请在设置中启用或在代码中添加引用。"");
    return ""需要安装依赖包"";
}
catch (Exception ex)
{
    Console.WriteLine($""错误: {ex.Message}"");
    return ""数据库操作失败"";
}")
            };
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 加载现有脚本进行编辑
        /// </summary>
        /// <param name="script">要编辑的脚本信息</param>
        public void LoadScriptForEdit(ScriptInfo script)
        {
            if (script == null) return;

            // 异步加载脚本数据，避免阻塞UI
            _ = Task.Run(async () =>
            {
                try
                {
                    // 从数据库重新加载完整的脚本信息（包含Code）
                    var dataService = App.GetService<Data.Services.IDataService>();
                    if (dataService != null)
                    {
                        var fullScript = await dataService.Scripts.GetByIdAsync(script.Id);
                        if (fullScript != null)
                        {
                            // 在UI线程上更新属性
                            await App.Current.Dispatcher.InvokeAsync(() =>
                            {
                                // 设置加载标记，避免模板变更触发
                                _isLoadingScript = true;
                                
                                try
                                {
                                    _currentEditingScript = fullScript;

                                    // 先清除模板选择，避免模板代码覆盖数据库代码
                                    SelectedTemplate = null;

                                    // 加载脚本信息到表单
                                    ScriptName = fullScript.Name;
                                    Description = fullScript.Description ?? string.Empty;
                                    Version = fullScript.Version ?? "1.0.0";
                                    Author = fullScript.Author ?? string.Empty;
                                    Code = fullScript.Code ?? string.Empty;

                                    // 更新页面标题
                                    PageTitle = $"编辑脚本 - {fullScript.Name}";

                                    // 更新工具提示
                                    UpdateTooltips();
                                    
                                    System.Diagnostics.Debug.WriteLine($"🔥 脚本编辑数据已加载: {fullScript.Name}, Code长度: {fullScript.Code?.Length ?? 0}");
                                }
                                finally
                                {
                                    // 清除加载标记
                                    _isLoadingScript = false;
                                }
                            });
                            return;
                        }
                    }

                    // 如果无法从数据库获取完整信息，使用传入的脚本信息
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _isLoadingScript = true;
                        
                        try
                        {
                            _currentEditingScript = script;

                            // 先清除模板选择
                            SelectedTemplate = null;

                            // 加载脚本信息到表单
                            ScriptName = script.Name;
                            Description = script.Description ?? string.Empty;
                            Version = script.Version ?? "1.0.0";
                            Author = script.Author ?? string.Empty;
                            Code = script.Code ?? string.Empty;

                            // 更新页面标题
                            PageTitle = $"编辑脚本 - {script.Name}";

                            // 更新工具提示
                            UpdateTooltips();
                        }
                        finally
                        {
                            _isLoadingScript = false;
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载脚本进行编辑失败: {ex.Message}");
                    
                    // 出现错误时，仍使用传入的脚本信息
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _isLoadingScript = true;
                        
                        try
                        {
                            _currentEditingScript = script;
                            
                            // 先清除模板选择
                            SelectedTemplate = null;
                            
                            ScriptName = script.Name;
                            Description = script.Description ?? string.Empty;
                            Version = script.Version ?? "1.0.0";
                            Author = script.Author ?? string.Empty;
                            Code = script.Code ?? string.Empty;
                            PageTitle = $"编辑脚本 - {script.Name}";
                            UpdateTooltips();
                        }
                        finally
                        {
                            _isLoadingScript = false;
                        }
                    });
                }
            });
        }

        /// <summary>
        /// 初始化为新建脚本模式
        /// </summary>
        public void InitializeForNewScript()
        {
            _currentEditingScript = null;
            ClearForm();
            PageTitle = "新建脚本";

            // 确保通知 UI 代码已更改，触发代码编辑器更新
            OnPropertyChanged(nameof(Code));
        }

        /// <summary>
        /// 更新代码内容
        /// 用于与 AvalonEdit 代码编辑器同步，因为该控件不支持双向数据绑定
        /// 当用户在代码编辑器中输入内容时，View 层会调用此方法更新 ViewModel
        /// </summary>
        /// <param name="newCode">新的代码内容</param>
        public void UpdateCode(string newCode)
        {
            Code = newCode;
        }

        #endregion

        #region 属性变更处理

        /// <summary>
        /// 处理选中模板变更事件
        /// 当用户选择不同的脚本模板时，自动更新代码内容
        /// 使用 partial 方法，由 CommunityToolkit.Mvvm 自动调用
        /// </summary>
        /// <param name="value">新选中的脚本模板</param>
        partial void OnSelectedTemplateChanged(ScriptTemplate? value)
        {
            // 如果正在加载脚本数据，忽略模板变更（避免覆盖数据库代码）
            if (_isLoadingScript)
            {
                System.Diagnostics.Debug.WriteLine("🔄 加载脚本期间忽略模板变更");
                return;
            }

            // 如果选中了有效的模板，应用模板代码
            if (value != null)
            {
                if (_currentEditingScript == null)
                {
                    System.Diagnostics.Debug.WriteLine($"🎯 新建模式应用模板代码: {value.Name}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✏️ 编辑模式用户选择模板: {value.Name}");
                }
                
                Code = value.Code;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("🚫 模板已清除");
            }
        }

        /// <summary>
        /// 处理脚本名称变更事件
        /// 当脚本名称改变时，通知相关命令重新评估 CanExecute 状态
        /// </summary>
        /// <param name="value">新的脚本名称</param>
        partial void OnScriptNameChanged(string value)
        {
            // 通知保存命令重新评估其可执行状态
            SaveScriptCommand.NotifyCanExecuteChanged();

            // 更新错误提示可见性
            ScriptNameHintVisibility = string.IsNullOrWhiteSpace(value) ? Visibility.Visible : Visibility.Collapsed;

            // 更新工具提示
            UpdateTooltips();
        }

        /// <summary>
        /// 处理代码内容变更事件
        /// 当代码内容改变时，通知相关命令重新评估 CanExecute 状态
        /// </summary>
        /// <param name="value">新的代码内容</param>
        partial void OnCodeChanged(string value)
        {
            // 通知执行脚本命令和保存脚本命令重新评估其可执行状态
            ExecuteScriptCommand.NotifyCanExecuteChanged();
            SaveScriptCommand.NotifyCanExecuteChanged();

            // 更新错误提示可见性
            CodeHintVisibility = string.IsNullOrWhiteSpace(value) ? Visibility.Visible : Visibility.Collapsed;

            // 更新工具提示
            UpdateTooltips();
        }

        /// <summary>
        /// 更新按钮工具提示
        /// </summary>
        private void UpdateTooltips()
        {
            // 更新执行按钮工具提示
            ExecuteButtonTooltip = string.IsNullOrWhiteSpace(Code)
                ? "请先输入代码才能执行"
                : "执行代码";

            // 更新保存按钮工具提示
            if (string.IsNullOrWhiteSpace(ScriptName) && string.IsNullOrWhiteSpace(Code))
            {
                SaveButtonTooltip = "请先输入脚本名和代码才能保存";
            }
            else if (string.IsNullOrWhiteSpace(ScriptName))
            {
                SaveButtonTooltip = "请先输入脚本名才能保存";
            }
            else if (string.IsNullOrWhiteSpace(Code))
            {
                SaveButtonTooltip = "请先输入代码才能保存";
            }
            else
            {
                SaveButtonTooltip = _currentEditingScript != null ? "更新脚本" : "保存脚本";
            }
        }

        #endregion

        #region 命令定义和实现

        /// <summary>
        /// 返回命令 - 导航回脚本管理主页面
        /// 使用 RelayCommand 特性自动生成命令实现
        /// </summary>
        [RelayCommand]
        private void Return()
        {
            // 通过导航服务跳转到脚本管理主页面
            _navigationService.NavigateTo("ScriptManageButton");
        }

        /// <summary>
        /// 执行脚本命令 - 运行当前编辑的脚本代码
        /// 包含 CanExecute 检查，只有在有代码内容时才能执行
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteScript))]
        private async Task ExecuteScript()
        {
            // 验证是否有可执行的代码
            if (string.IsNullOrWhiteSpace(Code))
            {
                // 显示错误提示
                var mainWindow = _navigationService.GetMainWindow();
                ThemedDialog.Show("提示", "请输入要执行的脚本代码", mainWindow);
                return;
            }

            try
            {
                // 如果是编辑现有脚本，更新执行统计
                if (_currentEditingScript != null)
                {
                    var dataService = App.GetService<IDataService>();
                    if (dataService != null)
                    {
                        await dataService.ExecuteScriptAsync(_currentEditingScript.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新脚本统计失败: {ex.Message}");
            }

            // 确定脚本标题
            var scriptTitle = !string.IsNullOrWhiteSpace(ScriptName) ? $"执行脚本: {ScriptName}" : "脚本执行";
            
            // 创建脚本运行对话框，传入脚本ID用于日志记录
            var dialog = new ScriptRunDialog(
                Code, 
                scriptTitle, 
                _currentEditingScript?.Id, 
                null // taskId 为 null，因为这是从脚本管理执行的
            );
            var ownerWindow = _navigationService.GetMainWindow();

            // 设置对话框的父窗口和注册管理
            if (ownerWindow != null)
            {
                dialog.Owner = ownerWindow;
                ownerWindow.RegisterChildWindow(dialog);
            }

            // 显示对话框并执行脚本
            dialog.ShowDialog();
        }

        /// <summary>
        /// 检查是否可以执行脚本
        /// 当代码内容不为空时返回 true
        /// </summary>
        /// <returns>如果可以执行脚本返回 true，否则返回 false</returns>
        private bool CanExecuteScript() => !string.IsNullOrWhiteSpace(Code);

        /// <summary>
        /// 保存脚本命令 - 将当前脚本保存到文件或数据库
        /// 包含 CanExecute 检查，需要脚本名称和代码都不为空
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSaveScript))]
        private async Task SaveScript()
        {
            try
            {
                var dataService = App.GetService<IDataService>();
                if (dataService == null)
                {
                    var mainWindow = _navigationService.GetMainWindow();
                    ThemedDialog.ShowError("错误", "数据服务不可用", mainWindow);
                    return;
                }

                if (_currentEditingScript != null)
                {
                    // 更新现有脚本
                    _currentEditingScript.Name = ScriptName;
                    _currentEditingScript.Description = Description;
                    _currentEditingScript.Version = Version;
                    _currentEditingScript.Author = Author;
                    _currentEditingScript.Code = Code;
                    _currentEditingScript.LastModified = DateTime.Now;

                    // 使用 SaveScriptAsync，它会根据 ID 判断是新增还是更新
                    var updatedScript = await dataService.SaveScriptAsync(_currentEditingScript);
                    
                    // 更新当前编辑的脚本引用
                    _currentEditingScript = updatedScript;

                    var mainWindow = _navigationService.GetMainWindow();
                    ThemedDialog.ShowInformation("更新成功", "脚本更新成功！", mainWindow);
                }
                else
                {
                    // 创建新脚本
                    var scriptInfo = new ScriptInfo
                    {
                        Name = ScriptName,
                        Description = Description,
                        Version = Version,
                        Author = Author,
                        Code = Code
                    };

                    var savedScript = await dataService.SaveScriptAsync(scriptInfo);

                    var mainWindow = _navigationService.GetMainWindow();
                    ThemedDialog.ShowInformation("保存成功", "脚本保存成功！", mainWindow);

                    // 切换为编辑模式
                    _currentEditingScript = savedScript;
                    PageTitle = $"编辑脚本 - {savedScript.Name}";
                }

                // 更新工具提示
                UpdateTooltips();
            }
            catch (Exception ex)
            {
                var mainWindow = _navigationService.GetMainWindow();
                ThemedDialog.ShowError("保存失败", $"保存脚本失败: {ex.Message}", mainWindow);
            }
        }

        /// <summary>
        /// 检查是否可以保存脚本
        /// 需要脚本名称和代码内容都不为空
        /// </summary>
        /// <returns>如果可以保存脚本返回 true，否则返回 false</returns>
        private bool CanSaveScript() =>
            !string.IsNullOrWhiteSpace(ScriptName) && !string.IsNullOrWhiteSpace(Code);

        /// <summary>
        /// 打开全屏编辑器命令 - 在独立窗口中编辑代码
        /// 提供更好的代码编辑体验，支持字体调整等功能
        /// </summary>
        [RelayCommand]
        private void OpenFullScreenEditor()
        {
            // 创建全屏代码编辑器窗口，传入当前代码内容
            var fullScreenEditor = new FullScreenCodeEditorWindow(Code);
            var ownerWindow = _navigationService.GetMainWindow();

            // 设置父窗口和注册管理
            if (ownerWindow != null)
            {
                fullScreenEditor.Owner = ownerWindow;
                ownerWindow.RegisterChildWindow(fullScreenEditor);
            }

            // 显示对话框并处理结果
            if (fullScreenEditor.ShowDialog() == true && fullScreenEditor.IsSaved)
            {
                // 用户保存了修改，更新当前代码内容
                Code = fullScreenEditor.Code;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 清空表单
        /// </summary>
        private void ClearForm()
        {
            ScriptName = string.Empty;
            Description = string.Empty;
            Version = "1.0.0";
            Author = string.Empty;
            Code = string.Empty;
            SelectedTemplate = ScriptTemplates.FirstOrDefault();
        }

        #endregion
    }
}