using System.Windows;
using System.Windows.Controls;
using TaskAssistant.ViewModels;

namespace TaskAssistant.View
{
    /// <summary>
    /// ?���ޥ�?�m��?����
    /// </summary>
    public partial class ScriptReferenceSettingsView : UserControl
    {
        public ScriptReferenceSettingsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ?���{�Ƕ���???�ƥ�
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
        /// ?��NuGet�]��???�ƥ�
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
        /// ?���ư��Ҧ���???�ƥ�
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