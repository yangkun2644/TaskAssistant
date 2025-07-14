using System.Windows;
using System.Windows.Controls;
using TaskAssistant.ViewModels;

namespace TaskAssistant.View
{
    /// <summary>
    /// ?本引用?置用?控件
    /// </summary>
    public partial class ScriptReferenceSettingsView : UserControl
    {
        public ScriptReferenceSettingsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ?除程序集按???事件
        /// </summary>
        private void RemoveAssemblyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && 
                button.Tag is ViewModels.AssemblyReferenceViewModel assembly &&
                DataContext is ViewModels.ScriptReferenceSettingsViewModel viewModel)
            {
                viewModel.RemoveAssemblyCommand.Execute(assembly);
            }
        }

        /// <summary>
        /// ?除NuGet包按???事件
        /// </summary>
        private void RemoveNuGetPackageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && 
                button.Tag is ViewModels.NuGetReferenceViewModel package &&
                DataContext is ViewModels.ScriptReferenceSettingsViewModel viewModel)
            {
                viewModel.RemoveNuGetPackageCommand.Execute(package);
            }
        }

        /// <summary>
        /// ?除排除模式按???事件
        /// </summary>
        private void RemoveExcludePatternButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && 
                button.Tag is string pattern &&
                DataContext is ViewModels.ScriptReferenceSettingsViewModel viewModel)
            {
                viewModel.RemoveExcludePatternCommand.Execute(pattern);
            }
        }
    }
}