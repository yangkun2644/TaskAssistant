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
    /// 脚本列表管理视图模型类
    /// 负责管理脚本列表界面的数据和业务逻辑，包括脚本列表显示、搜索、分类等功能
    /// 使用 CommunityToolkit.Mvvm 库实现 MVVM 模式，支持属性变更通知和命令绑定
    /// </summary>
    public partial class ScriptManageListViewModel : ObservableObject
    {
        #region 私有字段

        /// <summary>
        /// 导航服务实例，用于页面间的导航跳转
        /// 通过依赖注入方式获取，确保与主窗口的导航系统集成
        /// </summary>
        private readonly INavigationService _navigationService;

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
            
            // 异步加载数据
            _ = LoadDataAsync();
        }

        #endregion

        #region 命令定义和实现

        /// <summary>
        /// 新建脚本命令 - 导航到脚本编辑页面创建新脚本
        /// </summary>
        [RelayCommand]
        private void CreateScript()
        {
            // 导航到脚本编辑页面，开始创建新脚本
            _navigationService.NavigateTo("ScriptManageViewButton");
        }

        /// <summary>
        /// 编辑脚本命令
        /// </summary>
        [RelayCommand]
        private void EditScript(ScriptInfo? script)
        {
            if (script != null)
            {
                // 传递脚本信息到编辑页面
                var parameters = new Dictionary<string, object>
                {
                    { "EditScript", script }
                };
                _navigationService.NavigateToWithParameters("ScriptManageViewButton", parameters);
            }
        }

        /// <summary>
        /// 删除脚本命令
        /// </summary>
        [RelayCommand]
        private async Task DeleteScript(ScriptInfo? script)
        {
            if (script == null) return;

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
            await LoadScriptsAsync();
        }

        /// <summary>
        /// 刷新命令 - 重新执行当前的搜索/筛选条件
        /// 不重新加载分类数据，只刷新脚本列表以保持用户当前的搜索状态
        /// </summary>
        [RelayCommand]
        private async Task Refresh()
        {
            IsLoading = true;
            try
            {
                // 只重新加载脚本列表，保持当前的搜索关键词和分类筛选
                await LoadScriptsAsync();
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
            _ = LoadScriptsAsync();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载数据
        /// </summary>
        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                await LoadCategoriesAsync();
                await LoadScriptsAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载脚本列表
        /// </summary>
        private async Task LoadScriptsAsync()
        {
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

                // 更新脚本列表
                Scripts.Clear();
                var orderedScripts = scripts.OrderByDescending(s => s.LastModified).ToList();
                foreach (var script in orderedScripts)
                {
                    Scripts.Add(script);
                }

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
                
                // 显示错误消息给用户
                // TODO: 可以考虑添加错误消息属性来在UI中显示
            }
        }

        /// <summary>
        /// 加载分类列表
        /// </summary>
        private async Task LoadCategoriesAsync()
        {
            try
            {
                var dataService = App.GetService<IDataService>();
                if (dataService == null) return;

                var categories = await dataService.Scripts.GetAllCategoriesAsync();
                
                Categories.Clear();
                Categories.Add("全部");
                
                foreach (var category in categories.OrderBy(c => c))
                {
                    Categories.Add(category);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载分类列表失败: {ex.Message}");
            }
        }

        #endregion
    }
}