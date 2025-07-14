using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskAssistant.Data.Services;
using TaskAssistant.Models;
using TaskAssistant.Services;
using TaskAssistant.View;
using TaskAssistant.Common;

namespace TaskAssistant.ViewModels
{
    /// <summary>
    /// 脚本列表管理视图模型类（优化内存管理版本）
    /// 负责管理脚本列表界面的数据和业务逻辑，包括脚本列表显示、搜索、分类等功能
    /// 使用 CommunityToolkit.Mvvm 库实现 MVVM 模式，支持属性变更通知和命令绑定
    /// 实现了IDisposable接口，确保资源正确释放
    /// </summary>
    public partial class ScriptManageListViewModel : ObservableObject, IDisposable
    {
        #region 私有字段

        /// <summary>
        /// 导航服务实例，用于页面间的导航跳转
        /// 通过依赖注入方式获取，确保与主窗口的导航系统集成
        /// </summary>
        private readonly INavigationService _navigationService;

        /// <summary>
        /// 内存清理定时器
        /// </summary>
        private Timer? _memoryCleanupTimer;

        /// <summary>
        /// 是否已释放资源
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// 上次内存清理时间
        /// </summary>
        private DateTime _lastMemoryCleanup = DateTime.Now;

        #endregion

        #region 可观察属性

        /// <summary>
        /// 页面标题属性
        /// 显示在脚本管理页面顶部的标题文本
        /// 可以根据当前状态或上下文动态调整
        /// </summary>
        [ObservableProperty]
        private string _title = "脚本管理";

        /// <summary>
        /// 脚本列表
        /// 包含所有脚本信息的集合
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ScriptInfo> _scripts = new();

        /// <summary>
        /// 搜索关键词
        /// </summary>
        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        /// <summary>
        /// 选中的分类
        /// </summary>
        [ObservableProperty]
        private string _selectedCategory = "全部";

        /// <summary>
        /// 分类列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _categories = new();

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        private bool _isLoading = false;

        /// <summary>
        /// 搜索框占位符文本
        /// </summary>
        [ObservableProperty]
        private string _searchPlaceholder = "搜索脚本名称、描述或作者...";

        /// <summary>
        /// 脚本总数
        /// </summary>
        [ObservableProperty]
        private int _totalScriptsCount = 0;

        /// <summary>
        /// 当前显示的脚本数量
        /// </summary>
        [ObservableProperty]
        private int _displayedScriptsCount = 0;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化脚本列表管理视图模型
        /// 设置默认标题和脚本列表管理相关的初始化逻辑
        /// </summary>
        /// <param name="navigationService">导航服务</param>
        public ScriptManageListViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            
            // 初始化分类列表
            Categories.Add("全部");
            
            // 启动定期内存清理
            StartPeriodicMemoryCleanup();

            // 注册到资源管理器
            ResourceManager.RegisterResource(this);
            
            // 异步加载数据
            _ = LoadDataAsync();
        }

        #endregion

        #region 内存管理

        /// <summary>
        /// 启动定期内存清理
        /// </summary>
        private void StartPeriodicMemoryCleanup()
        {
            _memoryCleanupTimer = new Timer(async _ => await PerformPeriodicMemoryCleanupAsync(),
                null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
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
                if (timeSinceLastCleanup.TotalMinutes >= 10)
                {
                    // 清理脚本集合中的冗余数据
                    await CleanupScriptCollectionAsync();
                    
                    // 执行垃圾回收
                    GC.Collect(1, GCCollectionMode.Optimized);
                    
                    _lastMemoryCleanup = DateTime.Now;
                    System.Diagnostics.Debug.WriteLine("ScriptManageListViewModel 内存清理完成");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"定期内存清理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理脚本集合
        /// </summary>
        private async Task CleanupScriptCollectionAsync()
        {
            if (_disposed) return;

            await Task.Run(() =>
            {
                try
                {
                    // 如果脚本数量过多，保留最新的1000个
                    if (Scripts.Count > 1000)
                    {
                        var scriptsToKeep = Scripts.OrderByDescending(s => s.LastModified)
                                                  .Take(1000)
                                                  .ToList();
                        
                        Scripts.Clear();
                        foreach (var script in scriptsToKeep)
                        {
                            Scripts.Add(script);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"清理脚本集合: 保留 {Scripts.Count} 个最新脚本");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"清理脚本集合失败: {ex.Message}");
                }
            });
        }

        #endregion

        #region 命令定义和实现

        /// <summary>
        /// 新建脚本命令 - 导航到脚本编辑页面创建新脚本
        /// </summary>
        [RelayCommand]
        private void CreateScript()
        {
            if (_disposed) return;
            
            // 导航到脚本编辑页面，开始创建新脚本
            _navigationService.NavigateTo("ScriptManageViewButton");
        }

        /// <summary>
        /// 编辑脚本命令
        /// </summary>
        [RelayCommand]
        private void EditScript(ScriptInfo? script)
        {
            if (_disposed || script == null) return;

            // 传递脚本信息到编辑页面
            var parameters = new Dictionary<string, object>
            {
                { "EditScript", script }
            };
            _navigationService.NavigateToWithParameters("ScriptManageViewButton", parameters);
        }

        /// <summary>
        /// 删除脚本命令（优化版本）
        /// </summary>
        [RelayCommand]
        private async Task DeleteScript(ScriptInfo? script)
        {
            if (_disposed || script == null) return;

            try
            {
                // 获取主窗口作为父窗口，确保对话框位置固定
                var mainWindow = _navigationService.GetMainWindow();
                
                // 使用主题化确认对话框，明确设置父窗口
                var result = ThemedDialog.ShowConfirmation(
                    "确认删除",
                    $"确定要删除脚本 \"{script.Name}\" 吗？\n\n此操作不可撤销，请谨慎操作.",
                    mainWindow);

                if (result != ThemedDialogResult.Yes)
                    return;

                var dataService = App.GetService<IDataService>();
                if (dataService != null)
                {
                    // 删除脚本
                    await dataService.DeleteScriptAsync(script.Id);
                    
                    // 重新执行当前的搜索/筛选条件，而不是刷新整个界面
                    // 这样可以保持用户当前的搜索关键词和选择的分类
                    await LoadScriptsAsync();

                    // 显示成功消息，同样设置父窗口
                    ThemedDialog.ShowInformation("删除成功", $"脚本 \"{script.Name}\" 已成功删除。", mainWindow);

                    // 执行内存清理
                    _ = Task.Run(async () => await PerformPeriodicMemoryCleanupAsync());
                }
            }
            catch (Exception ex)
            {
                // 显示错误消息，同样设置父窗口
                var mainWindow = _navigationService.GetMainWindow();
                ThemedDialog.ShowError("删除失败", $"删除脚本时发生错误：\n{ex.Message}", mainWindow);
                System.Diagnostics.Debug.WriteLine($"删除脚本失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索命令
        /// </summary>
        [RelayCommand]
        private async Task Search()
        {
            if (_disposed) return;
            await LoadScriptsAsync();
        }

        /// <summary>
        /// 刷新命令 - 重新执行当前的搜索/筛选条件（优化版本）
        /// 不重新加载分类数据，只刷新脚本列表以保持用户当前的搜索状态
        /// </summary>
        [RelayCommand]
        private async Task Refresh()
        {
            if (_disposed) return;

            IsLoading = true;
            try
            {
                // 只重新加载脚本列表，保持当前的搜索关键词和分类筛选
                await LoadScriptsAsync();

                // 执行内存清理
                await PerformPeriodicMemoryCleanupAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region 属性变更处理

        /// <summary>
        /// 处理分类变更
        /// </summary>
        partial void OnSelectedCategoryChanged(string value)
        {
            if (!_disposed)
            {
                _ = LoadScriptsAsync();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载数据（优化版本）
        /// </summary>
        private async Task LoadDataAsync()
        {
            if (_disposed) return;

            IsLoading = true;
            try
            {
                await LoadCategoriesAsync();
                await LoadScriptsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载数据失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载脚本列表（优化版本）
        /// </summary>
        private async Task LoadScriptsAsync()
        {
            if (_disposed) return;

            try
            {
                var dataService = App.GetService<IDataService>();
                if (dataService == null) return;

                IEnumerable<ScriptInfo> scripts;

                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    // 执行搜索
                    var category = SelectedCategory == "全部" ? null : SelectedCategory;
                    scripts = await dataService.Scripts.SearchAsync(SearchKeyword, category, true);
                }
                else if (SelectedCategory != "全部")
                {
                    // 按分类过滤
                    scripts = await dataService.Scripts.GetByCategoryAsync(SelectedCategory);
                }
                else
                {
                    // 获取所有启用的脚本
                    scripts = await dataService.GetScriptsAsync(isEnabled: true);
                }

                // 清理并更新脚本列表
                Scripts.Clear();
                var orderedScripts = scripts.OrderByDescending(s => s.LastModified).ToList();
                
                // 分批添加，避免UI阻塞
                await AddScriptsInBatchesAsync(orderedScripts);

                // 更新计数
                DisplayedScriptsCount = Scripts.Count;
                
                // 如果没有搜索和过滤条件，更新总数
                if (string.IsNullOrWhiteSpace(SearchKeyword) && SelectedCategory == "全部")
                {
                    TotalScriptsCount = Scripts.Count;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载脚本列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分批添加脚本，避免UI阻塞
        /// </summary>
        /// <param name="scripts">要添加的脚本列表</param>
        private async Task AddScriptsInBatchesAsync(List<ScriptInfo> scripts)
        {
            if (_disposed) return;

            const int batchSize = 50;
            var totalBatches = (scripts.Count + batchSize - 1) / batchSize;

            for (int i = 0; i < totalBatches; i++)
            {
                if (_disposed) break;

                var batch = scripts.Skip(i * batchSize).Take(batchSize);
                
                foreach (var script in batch)
                {
                    if (_disposed) break;
                    Scripts.Add(script);
                }

                // 每批次后短暂延迟，让UI有机会更新
                if (i < totalBatches - 1)
                {
                    await Task.Delay(10);
                }
            }
        }

        /// <summary>
        /// 加载分类列表
        /// </summary>
        private async Task LoadCategoriesAsync()
        {
            if (_disposed) return;

            try
            {
                var dataService = App.GetService<IDataService>();
                if (dataService == null) return;

                var categories = await dataService.Scripts.GetAllCategoriesAsync();
                
                Categories.Clear();
                Categories.Add("全部");
                
                foreach (var category in categories.OrderBy(c => c))
                {
                    if (_disposed) break;
                    Categories.Add(category);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载分类列表失败: {ex.Message}");
            }
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

                    // 清理集合
                    Scripts?.Clear();
                    Categories?.Clear();

                    _disposed = true;
                    System.Diagnostics.Debug.WriteLine("ScriptManageListViewModel 资源释放完成");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"释放 ScriptManageListViewModel 资源时发生异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~ScriptManageListViewModel()
        {
            Dispose(false);
        }

        #endregion
    }
}