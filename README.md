# TaskAssistant

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![WPF](https://img.shields.io/badge/UI-WPF-purple)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![Entity Framework Core](https://img.shields.io/badge/ORM-EF%20Core%209.0-orange)](https://docs.microsoft.com/en-us/ef/core/)
[![SQLite](https://img.shields.io/badge/Database-SQLite-lightblue)](https://www.sqlite.org/)
[![MVVM](https://img.shields.io/badge/Pattern-MVVM-green)](https://docs.microsoft.com/en-us/xaml/xaml-overview)

TaskAssistant �O�@?�\��?�j�� C# ?���޲z�M?��u��A��_ WPF �M .NET 8 �۫ءC��?��?���ѤF�@?��?���ɭ�??�ءB�޲z�M?�� C# ?���A�P?�����??�שM�t??���\��C

## ? �D�n�S��

### ?? ?���޲z
- **�i?��?��??��**�G���� AvalonEdit �N???���A���?�k���G�B�N?��?�M����?��
- **???��?��**�G��� C# ?�����Y??��A?�m�������
- **?���ҪO�t?**�G���Ѧh��??�ҪO�A�ֳt?�l?��??
- **��?�M??�޲z**�G?����?����?�M??�t?
- **����j��**�G������W?�B�y�z�B�@��?��ֳt�j��
- **�����޲z**�G?��������?�M?��??

### ?? ��??��
- **�w?��?**�G�����_??����??��
- **��??��**�G???����??��??�M?�G
- **?��?�v**�G���㪺��??��?�v??
- **��q�ާ@**�G�����q?��/�T�Υ�?

### ?? ��?�ɭ�
- **?�N��??**�G���� Material Design ?��
- **??������**�G������f�j�p?��M�h?�ܾ�
- **�D?�t?**�G�i�w��ɭ��D?
- **�h?�����**�G���㪺����ɭ�

### ?? ��?�S��
- **�ʯ�ɬ��**�G?�q?�d?ɬ�ơA�קK�j�奻�r�q�v?�C��[?�ʯ�
- **��?�`�J**�G��_ Microsoft.Extensions.DependencyInjection
- **?�u���[��**�GEntity Framework Core + SQLite
- **MVVM �[��**�G�ϥ� CommunityToolkit.Mvvm ??���㪺 MVVM �Ҧ�

## ??? ��??

### �֤߮ج[
- **.NET 8.0**�G�̷s�� .NET �ج[�A���ѥX�⪺�ʯ�M�\��
- **WPF (Windows Presentation Foundation)**�G?�N�� Windows �ୱ?�ε{�Ǯج[
- **C# 12.0**�G�̷s�� C# ?���S��

### ��?�]<!-- UI �M MVVM -->
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
<PackageReference Include="AvalonEdit" Version="6.3.1.120" />
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.3351.48" />

<!-- ?�u?? -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.7" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.7" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.7" />

<!-- ?��?�� -->
<PackageReference Include="Microsoft.CodeAnalysis.CSharp.Scripting" Version="4.14.0" />

<!-- ��?�`�J�M�t�m -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.7" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.7" />

<!-- ��L�u�� -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
## ?? �ֳt?�l

### ?�ҭn�D
- **�ާ@�t?**�GWindows 10 ���� 1903 �Χ󰪪���
- **?��?**�G.NET 8.0 Runtime �� .NET 8.0 SDK
- **?�s**�G�̤� 2GB RAM�A���� 4GB �Χ�h
- **�s?��?**�G�ܤ� 500MB �i�Ϊ�?

### �w?�B?

1. **�J��??**git clone https://github.com/your-username/TaskAssistant.git
cd TaskAssistant
2. **?���?�]**dotnet restore
3. **�۫�?��**dotnet build --configuration Release
4. **?��?�ε{��**dotnet run --project TaskAssistant
### ����?��
- ?�ε{��?��??�� SQLite ?�u?
- ?�u?����m�G`%LocalAppData%\TaskAssistant\Data\TaskAssistant.db`
- ����???��l�ƥܨ�?���ҪO

## ?? �ϥΫ��n

### ?���޲z

#### ?�طs?��
1. ??"?���޲z"?�J?���޲z�ɭ�
2. ??"�s��?��"��?
3. ??�X�쪺?���ҪO��?�ť�?�l
4. ��??���򥻫H���]�W?�B�y�z�B�@�̵��^
5. �b�N???����?? C# �N?
6. ??"�O�s"��?�O�s?��

#### ?��?��
1. �b?��??����??"?��"��?
2. �Φb?���C��???��?
3. ?��?�b?�ߪ�?��?�Ҥ�?��
4. ���???�X�M??�H��?��
5. �i�H??�������b?�檺?��

#### ?���ҪO�ܨ�

**��?�`?�ܨ�**// ��?�`??�X�ܨҡ]��������^
for (int i = 1; i <= 100; i++)
{
    // ?�d�O�_�ݭn���� - ?�O��������ާ@��??�N?
    CancellationToken.ThrowIfCancellationRequested();
    
    Console.WriteLine($"���b?�z�� {i} ?...");
    await Task.Delay(50, CancellationToken); // ��?50�@��A�������
    
    // �C?�z10??�ܤ@��?��
    if (i % 10 == 0)
    {
        Console.WriteLine($"�w���� {i}%");
    }
}
Console.WriteLine("�Ҧ���??�槹���I");
**?�u?�z�ܨ�**
// ?�u?�z�ܨҡ]��������^
var numbers = new List<int>();
for (int i = 1; i <= 100; i++)
{
    if (i % 10 == 0)
        CancellationToken.ThrowIfCancellationRequested();
    
    numbers.Add(i);
    Console.WriteLine($"�K�[?�r: {i}");
}

// �ϥ� LINQ ?��?�u���R
var evenNumbers = numbers.Where(n => n % 2 == 0).ToList();
var oddNumbers = numbers.Where(n => n % 2 != 0).ToList();

Console.WriteLine($"��???: {evenNumbers.Count}");
Console.WriteLine($"�_???: {oddNumbers.Count}");
Console.WriteLine($"��?�M: {evenNumbers.Sum()}");
Console.WriteLine($"�_?�M: {oddNumbers.Sum()}");

return $"?�z�����A??: {numbers.Count}";
### ��??��

#### ?�ةw?��?
1. ?�J"��?�޲z"�ɭ�
2. ??"�s�إ�?"
3. ??�n??��?��
4. ?�m?��??�M���`??
5. �t�m��???
6. ?�Υ�??�l?��

#### ?����??��
- ��?????��s
- �d��?��?�v�M?�G
- �����?�D?��??��
- ��q�޲z�h?��?

### �t??��

#### �t???
- ?��??�M?�榸???
- ��??��??����
- �t??���ϥα�?
- ?�u?�j�p�M�ʯ��?

## ??? ?��?��
TaskAssistant/
�u�w�w ?? Assets/                     # ?�����
�x   �|�w�w ?? Fonts/                  # �r�^���
�u�w�w ?? Common/                     # �q��?��
�x   �|�w�w ?? ResourceManager.cs      # ?���޲z��
�u�w�w ?? Data/                       # ?�u???
�x   �u�w�w ?? Repositories/           # ??�Ҧ�??
�x   �x   �u�w�w ?? IRepository.cs      # ??���f
�x   �x   �u�w�w ?? Repository.cs       # ��?????
�x   �x   �u�w�w ?? IScriptRepository.cs # ?��??���f
�x   �x   �u�w�w ?? ScriptRepository.cs  # ?��????
�x   �x   �u�w�w ?? ITaskRepository.cs   # ��???���f
�x   �x   �|�w�w ?? TaskRepository.cs    # ��?????
�x   �u�w�w ?? Services/               # ?�u�A??
�x   �x   �u�w�w ?? IDataService.cs     # ?�u�A?���f
�x   �x   �|�w�w ?? DataService.cs      # ?�u�A???
�x   �u�w�w ?? Utilities/              # ?�u�u��
�x   �x   �|�w�w ?? DatabaseManager.cs  # ?�u?�޲z��
�x   �u�w�w ?? AppDbContext.cs         # EF Core ?�u?�W�U��
�x   �|�w�w ?? DataServiceCollectionExtensions.cs # ��?�`�J?�i
�u�w�w ?? Models/                     # ?�u�ҫ�
�x   �u�w�w ?? ScriptInfo.cs           # ?���H���ҫ�
�x   �u�w�w ?? TaskInfo.cs             # ��?�H���ҫ�
�x   �u�w�w ?? ScriptTemplate.cs       # ?���ҪO�ҫ�
�x   �|�w�w ?? SystemStatistics.cs     # �t???�ҫ�
�u�w�w ?? Services/                   # ?�ΪA?
�x   �u�w�w ?? INavigationService.cs   # ?��A?���f
�x   �u�w�w ?? NavigationService.cs    # ?��A???
�x   �|�w�w ?? SystemStatisticsService.cs # �t???�A?
�u�w�w ?? ViewModels/                 # ??�ҫ�
�x   �u�w�w ?? MainWindowViewModel.cs  # �D���f??�ҫ�
�x   �u�w�w ?? HomeViewModel.cs        # ��???�ҫ�
�x   �u�w�w ?? ScriptManageViewModel.cs # ?��????�ҫ�
�x   �u�w�w ?? ScriptManageListViewModel.cs # ?���C��??�ҫ�
�x   �|�w�w ?? TasksManageViewModel.cs # ��?�޲z??�ҫ�
�u�w�w ?? View/                       # ??
�x   �u�w�w ?? MainWindow.xaml/cs      # �D���f
�x   �u�w�w ?? Home.xaml/cs            # ��?
�x   �u�w�w ?? ScriptManage.xaml/cs    # ?���޲z�C��
�x   �u�w�w ?? ScriptManageView.xaml/cs # ?��??��
�x   �u�w�w ?? TasksManage.xaml/cs     # ��?�޲z
�x   �u�w�w ?? ScriptRunDialog.xaml/cs # ?��?��??��
�x   �u�w�w ?? FullScreenCodeEditorWindow.xaml/cs # ���̥N???��
�x   �|�w�w ?? ThemedDialog.xaml/cs    # �D?��??��
�u�w�w ?? App.xaml/cs                 # ?�ε{�ǤJ�f
�|�w�w ?? TaskAssistant.csproj        # ?�ؤ��
## ?? �֤ߥ\��?��

### ?��?�����
- ��_ **Microsoft.CodeAnalysis.CSharp.Scripting**
- ������㪺 C# ?�k�M .NET API
- ?�m�����O�P����A�i??��??��
- �w�����F�c?��?��

### ?�u??�[��
- **??�Ҧ� (Repository Pattern)**�G��??�u????
- **�u�@?���Ҧ� (Unit of Work)**�G�̫O��?�@�P��
- **��?�[?ɬ��**�G�קK�[?�j�奻�r�q�v?�ʯ�
- **?�����޲z**�Gɬ��?�u??��?���ϥ�

### MVVM �[��??
- **CommunityToolkit.Mvvm**�G?�N�ƪ� MVVM �ج[
- **RelayCommand**�G���ʯ઺�R�O??
- **ObservableProperty**�G��??��?��q��
- **��?�`�J**�G�Q���X��?��??

## ?? �ʯ�ɬ��

### ?�u?�d?ɬ��// ?�q?�d? - ���]�t Code �r�q
var scripts = await _scriptRepository.GetScriptListAsync(
    category: "�u��?��", 
    isEnabled: true, 
    pageIndex: 0, 
    pageSize: 20
);

// ����d? - ?�b�ݭn?�[? Code �r�q
var fullScript = await _scriptRepository.GetByIdAsync(scriptId);
### UI ��Vɬ��
- **??�ƦC��**�G�j?�u�������ʯ�?��
- **�ݨB�[?**�G�קK UI ?�{����
- **�W�q��s**�G�̤p�� UI ��?
- **?�s�޲z**�G��??�񤣻ݭn��?��

## ?? �t�m�M���p

### ?�ε{�ǰt�m{
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
### ���p??

#### ?�߳��pdotnet publish -c Release -r win-x64 --self-contained true
#### �ج[��?���pdotnet publish -c Release -r win-x64 --self-contained false
## ?? ??���n

��??���???�I?��`�H�U�B?�G

### ???��?�m
1. **�w? Visual Studio 2022** �� **VS Code** + C# ?�i
2. **�w? .NET 8.0 SDK**
3. **�J��??�}?��]**
4. **?��?��??�̫O?�ҥ��`**

### ����y�{
1. **Fork ?��**��z�� GitHub ??
2. **?�إ\�����**�G`git checkout -b feature/amazing-feature`
3. **������**�G`git commit -m 'Add some amazing feature'`
4. **���e����**�G`git push origin feature/amazing-feature`
5. **?�� Pull Request**

### �N??�S
- ��` C# ???�w�M�R�W?�S
- �K�[���n�� XML ��?�`?
- ???��??��?�s�\��
- �̫O�N?�q?�Ҧ�?��??

## ?? ��s���

### v1.0.0 (2024-01-XX)
- ? ��l����?��
- ?? ��??���޲z�\��
- ?? ��??�רt?
- ?? ?�N�� UI ??
- ?? �ʯ�ɬ��??

## ?? ?�i?

��?�ت��� MIT ?�i?�C??�H��?�d�� [LICENSE](LICENSE) ���C

## ?? ����M��?

- **Issue ��?**�G[GitHub Issues](https://github.com/your-username/TaskAssistant/issues)
- **�\���?**�G[GitHub Discussions](https://github.com/your-username/TaskAssistant/discussions)
- **?��?�t**�Gyour-email@example.com

## ?? �P?

�P?�H�U?��?�ةM??�̡G

- [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) - ?�j���N???��?��
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - ?�N�ƪ� MVVM �ج[
- [Entity Framework Core](https://github.com/dotnet/efcore) - ɬ�q�� ORM �ج[
- [Material Design](https://material.io/) - ???�P?��

---

**TaskAssistant** - ? C# ?���޲z?�o??���ġI ??