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

        #endregion

        #region 可观察属性

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
            _scriptTemplates = new ObservableCollection<ScriptTemplate>
        {
            // 基础循环示例 - 演示简单的循环和取消机制
            new("基础循环示例", @"// 基础循环输出示例（支持取消)
for (int i = 1; i <= 100; i++)
{
    // 检查是否需要取消 - 这是支持取消操作的关键代码
    CancellationToken.ThrowIfCancellationRequested();
    
    Console.WriteLine($""正在处理第 {i} 项..."");
    await Task.Delay(50, CancellationToken); // 延时50毫秒，支持取消
    
    // 每处理10项显示一次进度
    if (i % 10 == 0)
    {
        Console.WriteLine($""已完成 {i}%"");
    }
}
Console.WriteLine(""所有任务执行完成！"");"),

            // 数据处理示例 - 演示集合操作和 LINQ 查询
            new("数据处理示例", @"// 数据处理示例（支持取消)
var numbers = new List<int>();
for (int i = 1; i <= 100; i++)
{
    // 每10个数字检查一次取消状态，平衡性能和响应性
    if (i % 10 == 0)
        CancellationToken.ThrowIfCancellationRequested();
    
    numbers.Add(i);
    Console.WriteLine($""添加数字: {i}"");
}

// 使用 LINQ 进行数据分析
var evenNumbers = numbers.Where(n => n % 2 == 0).ToList();
var oddNumbers = numbers.Where(n => n % 2 != 0).ToList();

// 输出统计结果
Console.WriteLine($""偶数个数: {evenNumbers.Count}"");
Console.WriteLine($""奇数个数: {oddNumbers.Count}"");
Console.WriteLine($""偶数和: {evenNumbers.Sum()}"");
Console.WriteLine($""奇数和: {oddNumbers.Sum()}"");

return $""处理完成，总数: {numbers.Count}"";"),

            // 异步任务示例 - 演示并行任务处理
            new("异步任务示例", @"// 异步任务处理示例（支持取消)
var tasks = new List<Task>();

// 创建多个并行任务
for (int i = 1; i <= 10; i++)
{
    CancellationToken.ThrowIfCancellationRequested();
    
    int taskId = i; // 捕获循环变量
    var task = Task.Run(async () =>
    {
        Console.WriteLine($""任务 {taskId} 开始执行"");
        await Task.Delay(1000, CancellationToken); // 模拟耗时操作，支持取消
        Console.WriteLine($""任务 {taskId} 执行完成"");
        return taskId;
    }, CancellationToken);
    tasks.Add(task);
}

// 等待所有任务完成
Console.WriteLine(""等待所有任务完成..."");
await Task.WhenAll(tasks);
Console.WriteLine(""所有异步任务执行完成！"");"),

            // 错误处理示例 - 演示异常处理和资源清理
            new("错误处理示例", @"// 错误处理示例（支持取消)
try
{
    for (int i = 1; i <= 10; i++)
    {
        // 定期检查取消请求
        CancellationToken.ThrowIfCancellationRequested();
        
        Console.WriteLine($""处理项目 {i}"");
        
        // 模拟可能出现的错误情况
        if (i == 5)
        {
            Console.WriteLine(""模拟错误情况..."");
            // throw new Exception(""这是一个测试异常"");
        }
        
        Console.WriteLine($""项目 {i} 处理成功"");
        await Task.Delay(200, CancellationToken); // 添加延时，支持取消
    }
    
    return ""所有项目处理完成"";
}
catch (OperationCanceledException)
{
    // 处理用户取消操作
    Console.WriteLine(""脚本被用户取消"");
    return ""处理被取消"";
}
catch (Exception ex)
{
    // 处理其他异常
    Console.Error.WriteLine($""发生错误: {ex.Message}"");
    return ""处理失败"";
}
finally
{
    // 确保资源得到清理
    Console.WriteLine(""清理资源..."");
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

            _currentEditingScript = script;

            // 加载脚本信息到表单
            ScriptName = script.Name;
            Description = script.Description ?? string.Empty;
            Version = script.Version ?? "1.0.0";
            Author = script.Author ?? string.Empty;
            Code = script.Code ?? string.Empty;

            // 清除模板选择，因为这是现有脚本
            SelectedTemplate = null;

            // 更新页面标题
            PageTitle = $"编辑脚本 - {script.Name}";

            // 更新工具提示
            UpdateTooltips();
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
            // 如果选中了有效的模板，更新代码内容
            if (value != null)
            {
                Code = value.Code;
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

            // 创建脚本运行对话框
            var dialog = new ScriptRunDialog(Code);
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