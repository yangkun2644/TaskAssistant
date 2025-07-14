using System.Windows.Controls;
using TaskAssistant.ViewModels;


namespace TaskAssistant.View
{
    /// <summary>
    /// 主页用户控件的交互逻辑类
    /// 应用程序的欢迎页面，提供基本的导航入口和应用程序信息展示
    /// 使用 MVVM 模式，所有数据和逻辑都通过 HomeViewModel 处理
    /// </summary>
    public partial class Home : UserControl
    {
        #region 属性

        /// <summary>
        /// 获取当前视图绑定的主页视图模型
        /// </summary>
        public HomeViewModel ViewModel => (HomeViewModel)DataContext;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化主页用户控件
        /// 设置 XAML 组件并准备数据绑定
        /// </summary>
        public Home()
        {
            // 初始化 XAML 中定义的所有组件
            InitializeComponent();
        }

        #endregion
    }
}
