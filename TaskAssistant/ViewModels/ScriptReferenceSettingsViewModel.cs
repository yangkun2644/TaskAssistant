using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskAssistant.Models;
using TaskAssistant.Services;
using TaskAssistant.View;

namespace TaskAssistant.ViewModels
{
    /// <summary>
    /// 脚本引用设置视图模型
    /// </summary>
    public partial class ScriptReferenceSettingsViewModel : ObservableObject
    {
        private readonly IAppSettingsService _settingsService;
        private readonly INavigationService _navigationService;


        #region 属性

        /// <summary>
        /// 是否自动加载所有可用程序集
        /// </summary>
        [ObservableProperty]
        private bool _autoLoadAllAssemblies = true;

        /// <summary>
        /// 是否启用智能引用建议
        /// </summary>
        [ObservableProperty]
        private bool _enableSmartSuggestions = true;

        /// <summary>
        /// 常用程序集列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<AssemblyReferenceViewModel> _commonAssemblies = new();

        /// <summary>
        /// 常用NuGet包列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<NuGetReferenceViewModel> _commonNuGetPackages = new();

        /// <summary>
        /// 排除的程序集模式列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _excludedAssemblyPatterns = new();

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        private bool _isLoading = false;

        /// <summary>
        /// 新程序集名称
        /// </summary>
        [ObservableProperty]
        private string _newAssemblyName = string.Empty;

        /// <summary>
        /// 新程序集描述
        /// </summary>
        [ObservableProperty]
        private string _newAssemblyDescription = string.Empty;

        /// <summary>
        /// 新程序集路径
        /// </summary>
        [ObservableProperty]
        private string _newAssemblyPath = string.Empty;

        /// <summary>
        /// 新NuGet包ID
        /// </summary>
        [ObservableProperty]
        private string _newNuGetPackageId = string.Empty;

        /// <summary>
        /// 新NuGet包版本
        /// </summary>
        [ObservableProperty]
        private string _newNuGetVersion = "*";

        /// <summary>
        /// 新NuGet包描述
        /// </summary>
        [ObservableProperty]
        private string _newNuGetDescription = string.Empty;

        /// <summary>
        /// 新排除模式
        /// </summary>
        [ObservableProperty]
        private string _newExcludePattern = string.Empty;

        #endregion

        #region 构造函数

        public ScriptReferenceSettingsViewModel(IAppSettingsService settingsService, INavigationService navigationService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            
            _ = LoadSettingsAsync();
        }

        #endregion

        #region 命令

        /// <summary>
        /// 保存设置命令
        /// </summary>
        [RelayCommand]
        private async Task SaveSettings()
        {
            try
            {
                IsLoading = true;

                var settings = new ScriptReferenceSettings
                {
                    AutoLoadAllAssemblies = AutoLoadAllAssemblies,
                    EnableSmartSuggestions = EnableSmartSuggestions,
                    CommonAssemblies = CommonAssemblies.Select(vm => vm.ToModel()).ToList(),
                    CommonNuGetPackages = CommonNuGetPackages.Select(vm => vm.ToModel()).ToList(),
                    ExcludedAssemblyPatterns = ExcludedAssemblyPatterns.ToList()
                };

                await _settingsService.SaveScriptReferenceSettingsAsync(settings);

                // 更新引用管理器
                await SmartReferenceManager.Instance.UpdateSettingsAsync(settings);

                var mainWindow = _navigationService.GetMainWindow();
                ThemedDialog.ShowInformation("保存成功", "设置已保存并生效！", mainWindow);
            }
            catch (Exception ex)
            {
                var mainWindow = _navigationService.GetMainWindow();
                ThemedDialog.ShowError("保存失败", $"保存设置时发生错误：{ex.Message}", mainWindow);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 重置为默认设置命令
        /// </summary>
        [RelayCommand]
        private async Task ResetToDefaults()
        {
            var mainWindow = _navigationService.GetMainWindow();
            var result = ThemedDialog.ShowQuestion("确认重置", "确定要重置为默认设置吗？这将清除所有自定义配置。", ThemedDialogButton.YesNo, mainWindow);
            
            if (result == ThemedDialogResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    await _settingsService.ResetToDefaultsAsync();
                    await LoadSettingsAsync();

                    ThemedDialog.ShowInformation("重置成功", "设置已重置为默认值！", mainWindow);
                }
                catch (Exception ex)
                {
                    ThemedDialog.ShowError("重置失败", $"重置设置时发生错误：{ex.Message}", mainWindow);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 添加程序集命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddAssembly))]
        private void AddAssembly()
        {
            var viewModel = new AssemblyReferenceViewModel
            {
                Name = NewAssemblyName,
                Description = NewAssemblyDescription,
                Path = NewAssemblyPath,
                IsEnabled = true
            };

            CommonAssemblies.Add(viewModel);

            // 清空输入框
            NewAssemblyName = string.Empty;
            NewAssemblyDescription = string.Empty;
            NewAssemblyPath = string.Empty;
        }

        private bool CanAddAssembly() => !string.IsNullOrWhiteSpace(NewAssemblyName);

        /// <summary>
        /// 删除程序集命令
        /// </summary>
        [RelayCommand]
        private void RemoveAssembly(AssemblyReferenceViewModel? assembly)
        {
            if (assembly != null && CommonAssemblies.Contains(assembly))
            {
                CommonAssemblies.Remove(assembly);
            }
        }

        /// <summary>
        /// 添加NuGet包命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddNuGetPackage))]
        private void AddNuGetPackage()
        {
            var viewModel = new NuGetReferenceViewModel
            {
                PackageId = NewNuGetPackageId,
                Version = NewNuGetVersion,
                Description = NewNuGetDescription,
                IsEnabled = false // 默认不启用
            };

            CommonNuGetPackages.Add(viewModel);

            // 清空输入框
            NewNuGetPackageId = string.Empty;
            NewNuGetVersion = "*";
            NewNuGetDescription = string.Empty;
        }

        private bool CanAddNuGetPackage() => !string.IsNullOrWhiteSpace(NewNuGetPackageId);

        /// <summary>
        /// 删除NuGet包命令
        /// </summary>
        [RelayCommand]
        private void RemoveNuGetPackage(NuGetReferenceViewModel? package)
        {
            if (package != null && CommonNuGetPackages.Contains(package))
            {
                CommonNuGetPackages.Remove(package);
            }
        }

        /// <summary>
        /// 添加排除模式命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddExcludePattern))]
        private void AddExcludePattern()
        {
            if (!ExcludedAssemblyPatterns.Contains(NewExcludePattern))
            {
                ExcludedAssemblyPatterns.Add(NewExcludePattern);
                NewExcludePattern = string.Empty;
            }
        }

        private bool CanAddExcludePattern() => !string.IsNullOrWhiteSpace(NewExcludePattern);

        /// <summary>
        /// 删除排除模式命令
        /// </summary>
        [RelayCommand]
        private void RemoveExcludePattern(string? pattern)
        {
            if (!string.IsNullOrEmpty(pattern) && ExcludedAssemblyPatterns.Contains(pattern))
            {
                ExcludedAssemblyPatterns.Remove(pattern);
            }
        }

        /// <summary>
        /// 浏览程序集文件命令
        /// </summary>
        [RelayCommand]
        private void BrowseAssemblyFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择程序集文件",
                Filter = "程序集文件 (*.dll)|*.dll|所有文件 (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                NewAssemblyPath = dialog.FileName;
                
                // 自动填充程序集名称
                if (string.IsNullOrWhiteSpace(NewAssemblyName))
                {
                    NewAssemblyName = Path.GetFileNameWithoutExtension(dialog.FileName);
                }
            }
        }

        /// <summary>
        /// 返回命令
        /// </summary>
        [RelayCommand]
        private void Return()
        {
            _navigationService.NavigateTo("ScriptManageButton");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载设置
        /// </summary>
        private async Task LoadSettingsAsync()
        {
            try
            {
                IsLoading = true;

                var settings = await _settingsService.GetScriptReferenceSettingsAsync();

                AutoLoadAllAssemblies = settings.AutoLoadAllAssemblies;
                EnableSmartSuggestions = settings.EnableSmartSuggestions;

                CommonAssemblies.Clear();
                foreach (var assembly in settings.CommonAssemblies)
                {
                    CommonAssemblies.Add(new AssemblyReferenceViewModel
                    {
                        Name = assembly.Name,
                        Path = assembly.Path,
                        Description = assembly.Description,
                        IsEnabled = assembly.IsEnabled
                    });
                }

                CommonNuGetPackages.Clear();
                foreach (var package in settings.CommonNuGetPackages)
                {
                    CommonNuGetPackages.Add(new NuGetReferenceViewModel
                    {
                        PackageId = package.PackageId,
                        Version = package.Version,
                        Description = package.Description,
                        IsEnabled = package.IsEnabled,
                        PreLoad = package.PreLoad
                    });
                }

                ExcludedAssemblyPatterns.Clear();
                foreach (var pattern in settings.ExcludedAssemblyPatterns)
                {
                    ExcludedAssemblyPatterns.Add(pattern);
                }
            }
            catch (Exception ex)
            {
                var mainWindow = _navigationService.GetMainWindow();
                ThemedDialog.ShowError("加载失败", $"加载设置时发生错误：{ex.Message}", mainWindow);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 属性变更处理
        /// </summary>
        partial void OnNewAssemblyNameChanged(string value)
        {
            AddAssemblyCommand.NotifyCanExecuteChanged();
        }

        partial void OnNewNuGetPackageIdChanged(string value)
        {
            AddNuGetPackageCommand.NotifyCanExecuteChanged();
        }

        partial void OnNewExcludePatternChanged(string value)
        {
            AddExcludePatternCommand.NotifyCanExecuteChanged();
        }

        #endregion
    }

    /// <summary>
    /// 程序集引用视图模型
    /// </summary>
    public partial class AssemblyReferenceViewModel : ObservableObject
    {
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string? _path;
        [ObservableProperty] private string? _description;
        [ObservableProperty] private bool _isEnabled;

        public AssemblyReference ToModel() => new()
        {
            Name = Name,
            Path = Path,
            Description = Description,
            IsEnabled = IsEnabled
        };
    }

    /// <summary>
    /// NuGet包引用视图模型
    /// </summary>
    public partial class NuGetReferenceViewModel : ObservableObject
    {
        [ObservableProperty] private string _packageId = string.Empty;
        [ObservableProperty] private string _version = "*";
        [ObservableProperty] private string? _description;
        [ObservableProperty] private bool _isEnabled;
        [ObservableProperty] private bool _preLoad;

        public NuGetReference ToModel() => new()
        {
            PackageId = PackageId,
            Version = Version,
            Description = Description,
            IsEnabled = IsEnabled,
            PreLoad = PreLoad
        };
    }
}