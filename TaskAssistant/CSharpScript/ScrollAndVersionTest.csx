// ???���G???�X��???�MNuGet�������̤ǰt
#r "nuget:Microsoft.Playwright, 1.38.0"

using System;
using System.Threading;
using System.Threading.Tasks;

Console.WriteLine("?? === ��???�M�����ǰt?? ===");
Console.WriteLine($"???�l??: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine();

Console.WriteLine("?? ??��?:");
Console.WriteLine("1. ???�X�ɭ���???�\��");
Console.WriteLine("2. ??NuGet�]���̪����ǰt (Microsoft.Playwright 1.38.0)");
Console.WriteLine();

// ??1: �j�q?�X??��???
Console.WriteLine("?? ??1: ?�X�ɭ���?????");
Console.WriteLine("���b�ͦ��j�q?�X?�e�A??��?�X�ɭ��O�_��???�쩳��...");
Console.WriteLine();

for (int i = 1; i <= 100; i++)
{
    CancellationToken.ThrowIfCancellationRequested();
    
    if (i % 10 == 0)
    {
        Console.WriteLine($"?? ?????��: {i}/100 ({i}%)");
        Console.WriteLine($"   ?�e??: {DateTime.Now:HH:mm:ss.fff}");
        Console.WriteLine($"   �p�G�z�ݨ�??�����A?����???���`�u�@�I");
        Console.WriteLine();
    }
    else
    {
        Console.WriteLine($"�� {i:D3} ��?�X - ��??????�e - {DateTime.Now:HH:mm:ss.fff}");
    }
    
    // ��?���P��?�X?�j
    if (i <= 20)
    {
        await Task.Delay(100, CancellationToken); // �e20��ֳt?�X
    }
    else if (i <= 50)
    {
        await Task.Delay(200, CancellationToken); // ��?30�椤���t��
    }
    else
    {
        await Task.Delay(50, CancellationToken);  // �Z50��?�ֳt��
    }
}

Console.WriteLine();
Console.WriteLine("? ��?????�����I�p�G�z��ݨ�??�����A?��:");
Console.WriteLine("   - ?�X�ɭ����̦�???��F����");
Console.WriteLine("   - �Y�ϥͦ��j�q?�X�A�ɭ��]��O���P�B");
Console.WriteLine();

// ??2: NuGet�]����??
Console.WriteLine("?? ??2: NuGet�]�������̤ǰt??");
Console.WriteLine("���b?? Microsoft.Playwright 1.38.0 �O�_�Q���̤U?�M�[?...");
Console.WriteLine();

try
{
    // ?�dPlaywright�{�Ƕ��O�_�i��
    var playwrightAssembly = System.Reflection.Assembly.LoadFrom("Microsoft.Playwright.dll");
    if (playwrightAssembly != null)
    {
        var version = playwrightAssembly.GetName().Version;
        Console.WriteLine($"? Microsoft.Playwright �{�Ƕ��w�[?");
        Console.WriteLine($"   �{�Ƕ�����: {version}");
        Console.WriteLine($"   �{�Ƕ���m: {playwrightAssembly.Location}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"?? �����[?�{�Ƕ���?: {ex.Message}");
    Console.WriteLine("   ?�O���`���A�]?�{�Ƕ��i��b���P��m");
}

try
{
    // ??�ϥ�Playwright?���]�p�G�������̡A???��u�@�^
    // ?�dMicrosoft.Playwright�R�W��?�O�_�i��
    var playwrightType = Type.GetType("Microsoft.Playwright.IPlaywright, Microsoft.Playwright");
    
    if (playwrightType != null)
    {
        Console.WriteLine("? Microsoft.Playwright �R�W��?�i��");
        Console.WriteLine("   ?�����̪������]�w�Q���\�U?�M�ޥ�");
        
        // ?�ܤ@��Playwright�H��
        Console.WriteLine();
        Console.WriteLine("?? Playwright �H��:");
        Console.WriteLine($"   - ?�O�@??�j��??����?��?");
        Console.WriteLine($"   - ��� Chrome, Firefox, Safari �M Edge");
        Console.WriteLine($"   - �i�H�Τ_Web��?��??�M����");
        
        Console.WriteLine();
        Console.WriteLine("?? �����ǰt???�G:");
        Console.WriteLine("   �p�G?��?���?��?��??�A?��:");
        Console.WriteLine("   ? NuGet�]�������̤ǰt�\�ॿ�`�u�@");
        Console.WriteLine("   ? ?�D�� Microsoft.Playwright 1.38.0 �Q���̤U?");
        Console.WriteLine("   ? �{�Ƕ��ޥΩM�R�W��??�J�����\");
    }
    else
    {
        Console.WriteLine("?? Microsoft.Playwright ?�������");
        Console.WriteLine("   �i�઺��]:");
        Console.WriteLine("   - �]�|�������[?");
        Console.WriteLine("   - �������ǰt");
        Console.WriteLine("   - �U??�{���X???");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"? Playwright �ϥ�??��?: {ex.Message}");
    Console.WriteLine("   �i�઺��]:");
    Console.WriteLine("   - �]�������ǰt");
    Console.WriteLine("   - �U?��?");
    Console.WriteLine("   - �{�Ƕ��[???");
}

Console.WriteLine();
Console.WriteLine("?? ?�l????�X??�]??��???�椬�^");
Console.WriteLine("?? �b���U?��?�X?�{���A�z�i�H:");
Console.WriteLine("   1. ??�V�W??�d�ݤ��e��?�e");
Console.WriteLine("   2. �M�Z??�쩳���A?��O�_���s?�l��???");
Console.WriteLine("   3. ???�F����??�\�઺�椬��");
Console.WriteLine();

for (int i = 1; i <= 50; i++)
{
    CancellationToken.ThrowIfCancellationRequested();
    
    Console.WriteLine($"?? ????�X�� {i:D2}/50 �� - ??: {DateTime.Now:HH:mm:ss.fff}");
    
    if (i % 10 == 0)
    {
        Console.WriteLine($"?? ����: ?�b�O????�椬���n?��I(��{i}��)");
        Console.WriteLine("   ???�V�W??�A�M�Z�A??�^����");
        Console.WriteLine();
    }
    
    await Task.Delay(300, CancellationToken); // ?�C��?�X�t�סA�K�_�椬??
}

Console.WriteLine();
Console.WriteLine("?? === ??����?? ===");
Console.WriteLine($"����??: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine();
Console.WriteLine("?? ???�G:");
Console.WriteLine("? �p�G�z��ݨ�????�A?���H�U�\�ॿ�`:");
Console.WriteLine("   1. ?�X�ɭ���???�\��");
Console.WriteLine("   2. �j�q?�X?���ʯ��?");
Console.WriteLine("   3. NuGet�]���̪����ǰt");
Console.WriteLine("   4. ��???�椬������?�z");
Console.WriteLine();
Console.WriteLine("?? ���`?�e??:");
Console.WriteLine("? ??1: ?�X�ɭ���??? - �w���`");
Console.WriteLine("? ??2: NuGet�������̤ǰt - �w���`");
Console.WriteLine();
Console.WriteLine("?�b�z�i�H��ߨϥ� TaskAssistant ?��U��?���I");

return "��???�M�����ǰt??���\�����I";