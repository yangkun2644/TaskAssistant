using System.Windows.Controls;
using TaskAssistant.ViewModels;

namespace TaskAssistant.View
{
    /// <summary>
    /// 脚本管理列表用户控件的交互逻辑类
    /// 显示已保存的脚本列表，提供脚本的浏览、搜索、分类和管理功能
    /// 作为脚本管理系统的主入口，提供到脚本编辑页面的导航
    /// 使用 MVVM 模式，业务逻辑通过 ScriptManageListViewModel 处理
    /// </summary>
    public partial class ScriptManage : UserControl
    {
        #region 构造函数

        /// <summary>
        /// 初始化脚本管理列表用户控件
        /// 设置 XAML 组件并准备脚本列表界面的数据绑定
        /// </summary>
        public ScriptManage()
        {
            // 初始化 XAML 中定义的所有组件
            InitializeComponent();
            
            // 设置数据上下文为脚本列表管理视图模型
            // 如果使用依赖注入，这里会从容器中获取
            var navigationService = App.GetService<Services.INavigationService>();
            if (navigationService != null)
            {
                DataContext = new ScriptManageListViewModel(navigationService);
            }
        }

        #endregion

        #region 界面事件处理

        /// <summary>
        /// 处理用户控件加载完成事件
        /// </summary>
        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // 当界面加载完成时，刷新数据
            if (DataContext is ScriptManageListViewModel viewModel)
            {
                viewModel.RefreshCommand.Execute(null);
            }
        }

        #endregion
    }
}
