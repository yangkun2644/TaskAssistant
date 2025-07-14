// PuppeteerSharp非托管DLL处理测试脚本
// 测试系统是否能正确处理包含非托管DLL的NuGet包

#r "nuget:PuppeteerSharp,20.2.0"

using System;
using System.Threading.Tasks;
using PuppeteerSharp;

Console.WriteLine("开始PuppeteerSharp非托管DLL处理测试...");

try
{
    Console.WriteLine("正在初始化PuppeteerSharp...");

    // 下载Chromium（如果还没有下载）
    Console.WriteLine("检查Chromium浏览器...");
    await new BrowserFetcher().DownloadAsync();
    Console.WriteLine("✅ Chromium浏览器准备就绪");

    // 启动浏览器
    Console.WriteLine("正在启动浏览器...");
    using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
    {
        Headless = false, // 改为显示浏览器窗口
        Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
    });

    Console.WriteLine("✅ 浏览器启动成功");

    // 创建新页面
    using var page = await browser.NewPageAsync();
    Console.WriteLine("✅ 创建新页面成功");

    // 导航到示例页面
    Console.WriteLine("正在访问测试页面...");
    await page.GoToAsync("https://www.baidu.com/Index.htm");
    Console.WriteLine("✅ 页面加载成功");

    // 获取页面标题
    var title = await page.GetTitleAsync();
    Console.WriteLine($"📄 页面标题: {title}");

    // 获取页面内容长度
    var content = await page.GetContentAsync();
    Console.WriteLine($"📝 页面内容长度: {content.Length} 字符");

    Console.WriteLine("✅ PuppeteerSharp功能测试成功！");
    Console.WriteLine("🎉 非托管DLL过滤机制工作正常！");

    await Task.Delay(5000); // 等待5秒，方便查看浏览器窗口

    return "PuppeteerSharp测试成功完成";
}
catch (Exception ex)
{
    Console.WriteLine($"❌ 测试过程中出现错误: {ex.Message}");
    Console.WriteLine($"错误类型: {ex.GetType().Name}");

    if (ex.InnerException != null)
    {
        Console.WriteLine($"内部错误: {ex.InnerException.Message}");
    }

    return "测试失败";
}