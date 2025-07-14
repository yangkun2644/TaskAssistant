# TaskAssistant

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![WPF](https://img.shields.io/badge/UI-WPF-purple)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![Entity Framework Core](https://img.shields.io/badge/ORM-EF%20Core%209.0-orange)](https://docs.microsoft.com/en-us/ef/core/)
[![SQLite](https://img.shields.io/badge/Database-SQLite-lightblue)](https://www.sqlite.org/)
[![MVVM](https://img.shields.io/badge/Pattern-MVVM-green)](https://docs.microsoft.com/en-us/xaml/xaml-overview)

TaskAssistant 是一个功能强大的 C# 脚本管理和执行工具，基于 WPF 和 .NET 8 构建。它为用户提供了一个直观的界面来创建、管理和执行 C# 脚本，同时支持任务调度和系统监控功能。

## ✨ 主要特性

### 🚀 脚本管理
- **可视化脚本编辑器**：集成 AvalonEdit 代码编辑器，支持语法高亮、代码折叠和智能补全
- **实时脚本执行**：支持 C# 脚本的即时执行，内置取消机制
- **脚本模板系统**：提供多种预设模板，快速开始脚本开发
- **智能引用管理**：支持程序集和 NuGet 包的自动解析和加载
- **全屏代码编辑器**：提供独立的全屏编辑窗口，支持字体调整等高级功能
- **脚本执行监控**：独立的执行对话框，实时显示输出和错误信息
- **分类和标签管理**：灵活的脚本分类和标签系统
- **全文搜索**：支持按名称、描述、作者进行快速搜索
- **版本管理**：脚本版本跟踪和执行统计
<img width="1375" height="1000" alt="image" src="https://github.com/user-attachments/assets/198bc980-bd73-4fd6-9ce4-b1e06b2efe10" />
<img width="1375" height="1000" alt="image" src="https://github.com/user-attachments/assets/7c6e7a7b-01b6-4c6c-90ef-2700096feacd" />
<img width="1500" height="1062" alt="image" src="https://github.com/user-attachments/assets/130f9218-95ea-4247-8aac-4bcf3a593f96" />




### 📊 任务调度
- **定时任务**：支持基于时间的任务调度
- **任务监控**：实时监控任务执行状态和结果
- **执行历史**：完整的任务执行历史记录
- **批量操作**：支持批量启用/禁用任务

### 🔧 引用设置管理
- **程序集引用配置**：统一管理常用程序集引用，支持启用/禁用控制
- **NuGet 包管理**：集成 NuGet 包引用管理，支持版本控制和预加载设置
- **智能建议系统**：根据代码内容自动分析并建议相关引用
- **排除模式配置**：支持通配符模式的程序集排除设置
- **批量操作**：支持批量添加、删除和配置引用设置
<img width="1375" height="1000" alt="image" src="https://github.com/user-attachments/assets/98f6b760-dd2f-4b27-a3f9-1faedef9eb8c" />
<img width="1375" height="1000" alt="image" src="https://github.com/user-attachments/assets/19d811ed-9a2b-4048-ab9b-e4f595b47211" />



### 🎨 用户界面
- **现代化设计**：采用 Material Design 风格
- **响应式布局**：支持窗口大小调整和多显示器
- **主题系统**：可定制的界面主题
- **独立对话框**：专用的脚本执行、全屏编辑等功能对话框
- **主题化对话框**：统一的确认、信息和错误提示对话框
<img width="1375" height="1000" alt="image" src="https://github.com/user-attachments/assets/a8d25f52-9311-48d8-b296-83f9cf3c43fc" />



### 🔧 技术特性
- **性能优化**：轻量级查询优化，避免大文本字段影响列表加载性能
- **依赖注入**：基于 Microsoft.Extensions.DependencyInjection
- **数据持久化**：Entity Framework Core + SQLite
- **MVVM 架构**：使用 CommunityToolkit.Mvvm 实现完整的 MVVM 模式
- **智能程序集解析**：多层级的程序集和 NuGet 包解析系统

## 🛠️ 技术栈

### 核心框架
- **.NET 8.0**：最新的 .NET 框架，提供出色的性能和功能
- **WPF (Windows Presentation Foundation)**：现代的 Windows 桌面应用程序框架
- **C# 12.0**：最新的 C# 语言特性

### 依赖包<!-- UI 和 MVVM -->
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
<PackageReference Include="AvalonEdit" Version="6.3.1.120" />
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.3351.48" />

<!-- 数据访问 -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.7" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.7" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.7" />

<!-- 脚本执行 -->
<PackageReference Include="Microsoft.CodeAnalysis.CSharp.Scripting" Version="4.14.0" />

<!-- 依赖注入和配置 -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.7" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.7" />

<!-- 其他工具 -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
## 🚀 快速开始

### 环境要求
- **操作系统**：Windows 10 版本 1903 或更高版本
- **运行时**：.NET 8.0 Runtime 或 .NET 8.0 SDK
- **内存**：最少 2GB RAM，推荐 4GB 或更多
- **存储空间**：至少 500MB 可用空间

### 安装步骤

1. **克隆仓库**git clone https://github.com/your-username/TaskAssistant.git
cd TaskAssistant
2. **还原依赖包**dotnet restore
3. **构建项目**dotnet build --configuration Release
4. **运行应用程序**dotnet run --project TaskAssistant
### 首次运行
- 应用程序会自动创建 SQLite 数据库
- 数据库文件位置：`%LocalAppData%\TaskAssistant\Data\TaskAssistant.db`
- 首次启动会初始化示例脚本模板

## 📖 使用指南

### 脚本管理

#### 创建新脚本
1. 点击"脚本管理"进入脚本管理界面
2. 点击"新建脚本"按钮
3. 选择合适的脚本模板或从空白开始
4. 填写脚本基本信息（名称、描述、作者等）
5. 在代码编辑器中编写 C# 代码
6. 点击"保存"按钮保存脚本

#### 执行脚本
1. 在脚本编辑器中点击"执行"按钮
2. 或在脚本列表中双击脚本项
3. 脚本将在独立的执行环境中运行
4. 支持实时输出和错误信息显示
5. 可以随时取消正在运行的脚本

#### 脚本模板示例

**基础循环示例**// 基础循环输出示例（支持取消）
for (int i = 1; i <= 100; i++)
{
    // 检查是否需要取消 - 这是支持取消操作的关键代码
    CancellationToken.ThrowIfCancellationRequested();
    
    Console.WriteLine($"正在处理第 {i} 项...");
    await Task.Delay(50, CancellationToken); // 延时50毫秒，支持取消
    
    // 每处理10项显示一次进度
    if (i % 10 == 0)
    {
        Console.WriteLine($"已完成 {i}%");
    }
}
Console.WriteLine("所有任务执行完成！");
**数据处理示例**
// 数据处理示例（支持取消）
var numbers = new List<int>();
for (int i = 1; i <= 100; i++)
{
    if (i % 10 == 0)
        CancellationToken.ThrowIfCancellationRequested();
    
    numbers.Add(i);
    Console.WriteLine($"添加数字: {i}");
}

// 使用 LINQ 进行数据分析
var evenNumbers = numbers.Where(n => n % 2 == 0).ToList();
var oddNumbers = numbers.Where(n => n % 2 != 0).ToList();

Console.WriteLine($"偶数个数: {evenNumbers.Count}");
Console.WriteLine($"奇数个数: {oddNumbers.Count}");
Console.WriteLine($"偶数和: {evenNumbers.Sum()}");
Console.WriteLine($"奇数和: {oddNumbers.Sum()}");

return $"处理完成，总数: {numbers.Count}";
### 任务调度

#### 创建定时任务
1. 进入"任务管理"界面
2. 点击"新建任务"
3. 选择要关联的脚本
4. 设置执行时间和重复规则
5. 配置任务参数
6. 启用任务开始调度

#### 监控任务执行
- 任务状态实时更新
- 查看执行历史和结果
- 支持手动触发任务执行
- 批量管理多个任务

### 系统监控

#### 系统统计
- 脚本总数和执行次数统计
- 任务执行状态分布
- 系统资源使用情况
- 数据库大小和性能指标

## 🏗️ 项目结构
TaskAssistant/
├── 📁 Assets/                     # 资源文件
│   └── 📁 Fonts/                  # 字体文件
├── 📁 Common/                     # 通用组件
│   └── 📄 ResourceManager.cs      # 资源管理器
├── 📁 Data/                       # 数据访问层
│   ├── 📁 Repositories/           # 仓储模式实现
│   │   ├── 📄 IRepository.cs      # 仓储接口
│   │   ├── 📄 Repository.cs       # 基础仓储实现
│   │   ├── 📄 IScriptRepository.cs # 脚本仓储接口
│   │   ├── 📄 ScriptRepository.cs  # 脚本仓储实现
│   │   ├── 📄 ITaskRepository.cs   # 任务仓储接口
│   │   └── 📄 TaskRepository.cs    # 任务仓储实现
│   ├── 📁 Services/               # 数据服务层
│   │   ├── 📄 IDataService.cs     # 数据服务接口
│   │   └── 📄 DataService.cs      # 数据服务实现
│   ├── 📁 Utilities/              # 数据工具
│   │   └── 📄 DatabaseManager.cs  # 数据库管理器
│   ├── 📄 AppDbContext.cs         # EF Core 数据库上下文
│   └── 📄 DataServiceCollectionExtensions.cs # 依赖注入扩展
├── 📁 Models/                     # 数据模型
│   ├── 📄 ScriptInfo.cs           # 脚本信息模型
│   ├── 📄 TaskInfo.cs             # 任务信息模型
│   ├── 📄 ScriptTemplate.cs       # 脚本模板模型
│   └── 📄 SystemStatistics.cs     # 系统统计模型
├── 📁 Services/                   # 应用服务
│   ├── 📄 INavigationService.cs   # 导航服务接口
│   ├── 📄 NavigationService.cs    # 导航服务实现
│   └── 📄 SystemStatisticsService.cs # 系统统计服务
├── 📁 ViewModels/                 # 视图模型
│   ├── 📄 MainWindowViewModel.cs  # 主窗口视图模型
│   ├── 📄 HomeViewModel.cs        # 首页视图模型
│   ├── 📄 ScriptManageViewModel.cs # 脚本编辑视图模型
│   ├── 📄 ScriptManageListViewModel.cs # 脚本列表视图模型
│   └── 📄 TasksManageViewModel.cs # 任务管理视图模型
├── 📁 View/                       # 视图
│   ├── 📄 MainWindow.xaml/cs      # 主窗口
│   ├── 📄 Home.xaml/cs            # 首页
│   ├── 📄 ScriptManage.xaml/cs    # 脚本管理列表
│   ├── 📄 ScriptManageView.xaml/cs # 脚本编辑器
│   ├── 📄 TasksManage.xaml/cs     # 任务管理
│   ├── 📄 ScriptRunDialog.xaml/cs # 脚本执行对话框
│   ├── 📄 FullScreenCodeEditorWindow.xaml/cs # 全屏代码编辑器
│   └── 📄 ThemedDialog.xaml/cs    # 主题化对话框
├── 📄 App.xaml/cs                 # 应用程序入口
└── 📄 TaskAssistant.csproj        # 项目文件
## 🎯 核心功能详解

### 脚本执行引擎
- 基于 **Microsoft.CodeAnalysis.CSharp.Scripting**
- 支持完整的 C# 语法和 .NET API
- 内置取消令牌支持，可随时中断执行
- 安全的沙箱执行环境

### 数据访问架构
- **仓储模式 (Repository Pattern)**：封装数据访问逻辑
- **工作单元模式 (Unit of Work)**：确保事务一致性
- **延迟加载优化**：避免加载大文本字段影响性能
- **连接池管理**：优化数据库连接资源使用

### MVVM 架构实现
- **CommunityToolkit.Mvvm**：现代化的 MVVM 框架
- **RelayCommand**：高性能的命令实现
- **ObservableProperty**：自动属性变更通知
- **依赖注入**：松耦合的组件设计

## 📊 性能优化
### UI 渲染优化
- **虚拟化列表**：大数据集的高性能显示
- **异步加载**：避免 UI 线程阻塞
- **增量更新**：最小化 UI 重绘
- **内存管理**：及时释放不需要的资源

## 🔧 配置和部署

### 应用程序配置{
  "Database": {
    "ConnectionString": "Data Source=%LocalAppData%\\TaskAssistant\\Data\\TaskAssistant.db",
    "CommandTimeout": 30,
    "EnableLogging": false
  },
  "UI": {
    "Theme": "Light",
    "Language": "zh-CN",
    "AutoSave": true,
    "AutoSaveInterval": 300
  },
  "Script": {
    "ExecutionTimeout": 300,
    "EnableDebugging": true,
    "AllowFileAccess": false,
    "AllowNetworkAccess": false
  }
}
### 部署选项

#### 独立部署dotnet publish -c Release -r win-x64 --self-contained true
#### 框架依赖部署dotnet publish -c Release -r win-x64 --self-contained false
## 🤝 贡献指南

我们欢迎社区贡献！请遵循以下步骤：

### 开发环境设置
1. **安装 Visual Studio 2022** 或 **VS Code** + C# 扩展
2. **安装 .NET 8.0 SDK**
3. **克隆仓库并还原包**
4. **运行单元测试确保环境正常**

### 提交流程
1. **Fork 项目**到您的 GitHub 账户
2. **创建功能分支**：`git checkout -b feature/amazing-feature`
3. **提交更改**：`git commit -m 'Add some amazing feature'`
4. **推送分支**：`git push origin feature/amazing-feature`
5. **创建 Pull Request**

### 代码规范
- 遵循 C# 编码约定和命名规范
- 添加必要的 XML 文档注释
- 编写单元测试覆盖新功能
- 确保代码通过所有现有测试

## 📝 更新日志

### v1.0.0 (2024-01-XX)
- ✨ 初始版本发布
- 🚀 基础脚本管理功能
- 📊 任务调度系统
- 🎨 现代化 UI 设计
- 🔧 性能优化实现

## 📄 许可证

本项目采用 MIT 许可证。详细信息请查看 [LICENSE](LICENSE) 文件。


## 🙏 致谢

感谢以下开源项目和贡献者：

- [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) - 强大的代码编辑器组件
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - 现代化的 MVVM 框架
- [Entity Framework Core](https://github.com/dotnet/efcore) - 优秀的 ORM 框架
- [Material Design](https://material.io/) - 设计灵感来源

---

**TaskAssistant** - 让 C# 脚本管理变得简单高效！ 🚀
