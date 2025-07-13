# TaskAssistant

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![WPF](https://img.shields.io/badge/UI-WPF-purple)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![Entity Framework Core](https://img.shields.io/badge/ORM-EF%20Core%209.0-orange)](https://docs.microsoft.com/en-us/ef/core/)
[![SQLite](https://img.shields.io/badge/Database-SQLite-lightblue)](https://www.sqlite.org/)
[![MVVM](https://img.shields.io/badge/Pattern-MVVM-green)](https://docs.microsoft.com/en-us/xaml/xaml-overview)

TaskAssistant 是一?功能?大的 C# ?本管理和?行工具，基于 WPF 和 .NET 8 构建。它?用?提供了一?直?的界面??建、管理和?行 C# ?本，同?支持任??度和系??控功能。

## ? 主要特性

### ?? ?本管理
- **可?化?本??器**：集成 AvalonEdit 代???器，支持?法高亮、代?折?和智能?全
- **???本?行**：支持 C# ?本的即??行，?置取消机制
- **?本模板系?**：提供多种??模板，快速?始?本??
- **分?和??管理**：?活的?本分?和??系?
- **全文搜索**：支持按名?、描述、作者?行快速搜索
- **版本管理**：?本版本跟?和?行??

### ?? 任??度
- **定?任?**：支持基于??的任??度
- **任??控**：???控任??行??和?果
- **?行?史**：完整的任??行?史??
- **批量操作**：支持批量?用/禁用任?

### ?? 用?界面
- **?代化??**：采用 Material Design ?格
- **??式布局**：支持窗口大小?整和多?示器
- **主?系?**：可定制的界面主?
- **多?言支持**：完整的中文界面

### ?? 技?特性
- **性能优化**：?量?查?优化，避免大文本字段影?列表加?性能
- **依?注入**：基于 Microsoft.Extensions.DependencyInjection
- **?据持久化**：Entity Framework Core + SQLite
- **MVVM 架构**：使用 CommunityToolkit.Mvvm ??完整的 MVVM 模式

## ??? 技??

### 核心框架
- **.NET 8.0**：最新的 .NET 框架，提供出色的性能和功能
- **WPF (Windows Presentation Foundation)**：?代的 Windows 桌面?用程序框架
- **C# 12.0**：最新的 C# ?言特性

### 依?包<!-- UI 和 MVVM -->
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
<PackageReference Include="AvalonEdit" Version="6.3.1.120" />
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.3351.48" />

<!-- ?据?? -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.7" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.7" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.7" />

<!-- ?本?行 -->
<PackageReference Include="Microsoft.CodeAnalysis.CSharp.Scripting" Version="4.14.0" />

<!-- 依?注入和配置 -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.7" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.7" />

<!-- 其他工具 -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
## ?? 快速?始

### ?境要求
- **操作系?**：Windows 10 版本 1903 或更高版本
- **?行?**：.NET 8.0 Runtime 或 .NET 8.0 SDK
- **?存**：最少 2GB RAM，推荐 4GB 或更多
- **存?空?**：至少 500MB 可用空?

### 安?步?

1. **克隆??**git clone https://github.com/your-username/TaskAssistant.git
cd TaskAssistant
2. **?原依?包**dotnet restore
3. **构建?目**dotnet build --configuration Release
4. **?行?用程序**dotnet run --project TaskAssistant
### 首次?行
- ?用程序?自??建 SQLite ?据?
- ?据?文件位置：`%LocalAppData%\TaskAssistant\Data\TaskAssistant.db`
- 首次???初始化示例?本模板

## ?? 使用指南

### ?本管理

#### ?建新?本
1. ??"?本管理"?入?本管理界面
2. ??"新建?本"按?
3. ??合适的?本模板或?空白?始
4. 填??本基本信息（名?、描述、作者等）
5. 在代???器中?? C# 代?
6. ??"保存"按?保存?本

#### ?行?本
1. 在?本??器中??"?行"按?
2. 或在?本列表中???本?
3. ?本?在?立的?行?境中?行
4. 支持???出和??信息?示
5. 可以??取消正在?行的?本

#### ?本模板示例

**基?循?示例**// 基?循??出示例（支持取消）
for (int i = 1; i <= 100; i++)
{
    // ?查是否需要取消 - ?是支持取消操作的??代?
    CancellationToken.ThrowIfCancellationRequested();
    
    Console.WriteLine($"正在?理第 {i} ?...");
    await Task.Delay(50, CancellationToken); // 延?50毫秒，支持取消
    
    // 每?理10??示一次?度
    if (i % 10 == 0)
    {
        Console.WriteLine($"已完成 {i}%");
    }
}
Console.WriteLine("所有任??行完成！");
**?据?理示例**
// ?据?理示例（支持取消）
var numbers = new List<int>();
for (int i = 1; i <= 100; i++)
{
    if (i % 10 == 0)
        CancellationToken.ThrowIfCancellationRequested();
    
    numbers.Add(i);
    Console.WriteLine($"添加?字: {i}");
}

// 使用 LINQ ?行?据分析
var evenNumbers = numbers.Where(n => n % 2 == 0).ToList();
var oddNumbers = numbers.Where(n => n % 2 != 0).ToList();

Console.WriteLine($"偶???: {evenNumbers.Count}");
Console.WriteLine($"奇???: {oddNumbers.Count}");
Console.WriteLine($"偶?和: {evenNumbers.Sum()}");
Console.WriteLine($"奇?和: {oddNumbers.Sum()}");

return $"?理完成，??: {numbers.Count}";
### 任??度

#### ?建定?任?
1. ?入"任?管理"界面
2. ??"新建任?"
3. ??要??的?本
4. ?置?行??和重复??
5. 配置任???
6. ?用任??始?度

#### ?控任??行
- 任?????更新
- 查看?行?史和?果
- 支持手?触?任??行
- 批量管理多?任?

### 系??控

#### 系???
- ?本??和?行次???
- 任??行??分布
- 系??源使用情?
- ?据?大小和性能指?

## ??? ?目?构
TaskAssistant/
├── ?? Assets/                     # ?源文件
│   └── ?? Fonts/                  # 字体文件
├── ?? Common/                     # 通用?件
│   └── ?? ResourceManager.cs      # ?源管理器
├── ?? Data/                       # ?据???
│   ├── ?? Repositories/           # ??模式??
│   │   ├── ?? IRepository.cs      # ??接口
│   │   ├── ?? Repository.cs       # 基?????
│   │   ├── ?? IScriptRepository.cs # ?本??接口
│   │   ├── ?? ScriptRepository.cs  # ?本????
│   │   ├── ?? ITaskRepository.cs   # 任???接口
│   │   └── ?? TaskRepository.cs    # 任?????
│   ├── ?? Services/               # ?据服??
│   │   ├── ?? IDataService.cs     # ?据服?接口
│   │   └── ?? DataService.cs      # ?据服???
│   ├── ?? Utilities/              # ?据工具
│   │   └── ?? DatabaseManager.cs  # ?据?管理器
│   ├── ?? AppDbContext.cs         # EF Core ?据?上下文
│   └── ?? DataServiceCollectionExtensions.cs # 依?注入?展
├── ?? Models/                     # ?据模型
│   ├── ?? ScriptInfo.cs           # ?本信息模型
│   ├── ?? TaskInfo.cs             # 任?信息模型
│   ├── ?? ScriptTemplate.cs       # ?本模板模型
│   └── ?? SystemStatistics.cs     # 系???模型
├── ?? Services/                   # ?用服?
│   ├── ?? INavigationService.cs   # ?航服?接口
│   ├── ?? NavigationService.cs    # ?航服???
│   └── ?? SystemStatisticsService.cs # 系???服?
├── ?? ViewModels/                 # ??模型
│   ├── ?? MainWindowViewModel.cs  # 主窗口??模型
│   ├── ?? HomeViewModel.cs        # 首???模型
│   ├── ?? ScriptManageViewModel.cs # ?本????模型
│   ├── ?? ScriptManageListViewModel.cs # ?本列表??模型
│   └── ?? TasksManageViewModel.cs # 任?管理??模型
├── ?? View/                       # ??
│   ├── ?? MainWindow.xaml/cs      # 主窗口
│   ├── ?? Home.xaml/cs            # 首?
│   ├── ?? ScriptManage.xaml/cs    # ?本管理列表
│   ├── ?? ScriptManageView.xaml/cs # ?本??器
│   ├── ?? TasksManage.xaml/cs     # 任?管理
│   ├── ?? ScriptRunDialog.xaml/cs # ?本?行??框
│   ├── ?? FullScreenCodeEditorWindow.xaml/cs # 全屏代???器
│   └── ?? ThemedDialog.xaml/cs    # 主?化??框
├── ?? App.xaml/cs                 # ?用程序入口
└── ?? TaskAssistant.csproj        # ?目文件
## ?? 核心功能?解

### ?本?行引擎
- 基于 **Microsoft.CodeAnalysis.CSharp.Scripting**
- 支持完整的 C# ?法和 .NET API
- ?置取消令牌支持，可??中??行
- 安全的沙箱?行?境

### ?据??架构
- **??模式 (Repository Pattern)**：封??据????
- **工作?元模式 (Unit of Work)**：确保事?一致性
- **延?加?优化**：避免加?大文本字段影?性能
- **?接池管理**：优化?据??接?源使用

### MVVM 架构??
- **CommunityToolkit.Mvvm**：?代化的 MVVM 框架
- **RelayCommand**：高性能的命令??
- **ObservableProperty**：自??性?更通知
- **依?注入**：松耦合的?件??

## ?? 性能优化

### ?据?查?优化// ?量?查? - 不包含 Code 字段
var scripts = await _scriptRepository.GetScriptListAsync(
    category: "工具?本", 
    isEnabled: true, 
    pageIndex: 0, 
    pageSize: 20
);

// 完整查? - ?在需要?加? Code 字段
var fullScript = await _scriptRepository.GetByIdAsync(scriptId);
### UI 渲染优化
- **??化列表**：大?据集的高性能?示
- **异步加?**：避免 UI ?程阻塞
- **增量更新**：最小化 UI 重?
- **?存管理**：及??放不需要的?源

## ?? 配置和部署

### ?用程序配置{
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
### 部署??

#### ?立部署dotnet publish -c Release -r win-x64 --self-contained true
#### 框架依?部署dotnet publish -c Release -r win-x64 --self-contained false
## ?? ??指南

我??迎社???！?遵循以下步?：

### ???境?置
1. **安? Visual Studio 2022** 或 **VS Code** + C# ?展
2. **安? .NET 8.0 SDK**
3. **克隆??并?原包**
4. **?行?元??确保?境正常**

### 提交流程
1. **Fork ?目**到您的 GitHub ??
2. **?建功能分支**：`git checkout -b feature/amazing-feature`
3. **提交更改**：`git commit -m 'Add some amazing feature'`
4. **推送分支**：`git push origin feature/amazing-feature`
5. **?建 Pull Request**

### 代??范
- 遵循 C# ???定和命名?范
- 添加必要的 XML 文?注?
- ???元??覆?新功能
- 确保代?通?所有?有??

## ?? 更新日志

### v1.0.0 (2024-01-XX)
- ? 初始版本?布
- ?? 基??本管理功能
- ?? 任??度系?
- ?? ?代化 UI ??
- ?? 性能优化??

## ?? ?可?

本?目采用 MIT ?可?。??信息?查看 [LICENSE](LICENSE) 文件。

## ?? 支持和反?

- **Issue 跟?**：[GitHub Issues](https://github.com/your-username/TaskAssistant/issues)
- **功能建?**：[GitHub Discussions](https://github.com/your-username/TaskAssistant/discussions)
- **?件?系**：your-email@example.com

## ?? 致?

感?以下?源?目和??者：

- [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) - ?大的代???器?件
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - ?代化的 MVVM 框架
- [Entity Framework Core](https://github.com/dotnet/efcore) - 优秀的 ORM 框架
- [Material Design](https://material.io/) - ???感?源

---

**TaskAssistant** - ? C# ?本管理?得??高效！ ??