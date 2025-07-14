// 使用Selenium在百度搜索示例脚本
// 注意：运行前需要安装Selenium.WebDriver和对应的浏览器驱动

#r "nuget:Selenium.WebDriver,4.15.0"
#r "nuget:Selenium.WebDriver.ChromeDriver,119.0.6045.10500"

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

Console.WriteLine("开始执行Selenium百度搜索示例...");

IWebDriver driver = null;
try
{
    // 检查取消请求
    CancellationToken.ThrowIfCancellationRequested();

    Console.WriteLine("正在初始化Chrome浏览器...");

    // 配置Chrome选项
    var chromeOptions = new ChromeOptions();
    chromeOptions.AddArgument("--no-sandbox");
    chromeOptions.AddArgument("--disable-dev-shm-usage");
    chromeOptions.AddArgument("--disable-gpu");
    chromeOptions.AddArgument("--window-size=1920,1080");

    // 如果不想显示浏览器界面，可以取消下面这行的注释
    // chromeOptions.AddArgument("--headless");

    // 创建ChromeDriver实例
    driver = new ChromeDriver(chromeOptions);
    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

    Console.WriteLine("浏览器启动成功，正在导航到百度首页...");

    // 导航到百度首页
    driver.Navigate().GoToUrl("https://www.baidu.com");
    await Task.Delay(2000, CancellationToken); // 等待页面加载

    Console.WriteLine("页面加载完成，正在查找搜索框...");

    // 查找搜索框元素
    var searchBox = driver.FindElement(By.Id("kw"));

    Console.WriteLine("找到搜索框，正在输入搜索关键词...");

    // 输入搜索关键词"Selenium"
    searchBox.Clear();
    searchBox.SendKeys("Selenium");

    Console.WriteLine("关键词输入完成，正在点击搜索按钮...");

    // 查找并点击搜索按钮
    var searchButton = driver.FindElement(By.Id("su"));
    searchButton.Click();

    Console.WriteLine("搜索请求已提交，等待搜索结果...");

    // 等待搜索结果页面加载
    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    wait.Until(d => d.FindElements(By.CssSelector(".result")).Count > 0);

    Console.WriteLine("搜索结果页面加载完成，正在获取搜索结果...");

    // 获取搜索结果
    var searchResults = driver.FindElements(By.CssSelector(".result"));

    Console.WriteLine($"共找到 {searchResults.Count} 个搜索结果：");
    Console.WriteLine(new string('-', 50));

    // 显示前5个搜索结果
    for (int i = 0; i < Math.Min(5, searchResults.Count); i++)
    {
        // 检查取消请求
        CancellationToken.ThrowIfCancellationRequested();

        try
        {
            var result = searchResults[i];

            // 获取标题
            var titleElement = result.FindElement(By.CssSelector("h3 a"));
            string title = titleElement.Text;
            string url = titleElement.GetAttribute("href");

            // 获取描述（如果存在）
            string description = "";
            try
            {
                var descElement = result.FindElement(By.CssSelector(".c-abstract"));
                description = descElement.Text;
            }
            catch (NoSuchElementException)
            {
                description = "无描述信息";
            }

            Console.WriteLine($"结果 {i + 1}:");
            Console.WriteLine($"标题: {title}");
            Console.WriteLine($"链接: {url}");
            Console.WriteLine($"描述: {description}");
            Console.WriteLine(new string('-', 30));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取第 {i + 1} 个结果时出错: {ex.Message}");
        }
    }

    Console.WriteLine("搜索结果获取完成！");

    // 可选：截图保存
    try
    {
        Console.WriteLine("正在保存页面截图...");
        var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
        var fileName = $"baidu_search_selenium_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
        screenshot.SaveAsFile(filePath);
        Console.WriteLine($"截图已保存到桌面: {fileName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"保存截图时出错: {ex.Message}");
    }

    // 等待3秒让用户观察结果
    Console.WriteLine("等待3秒后关闭浏览器...");
    await Task.Delay(3000, CancellationToken);

    return "百度搜索Selenium完成！";
}
catch (OperationCanceledException)
{
    Console.WriteLine("搜索操作被用户取消");
    return "操作已取消";
}
catch (WebDriverException ex)
{
    Console.Error.WriteLine($"WebDriver错误: {ex.Message}");
    Console.Error.WriteLine("请确保已安装Chrome浏览器和对应版本的ChromeDriver");
    return "WebDriver错误";
}
catch (Exception ex)
{·
    Console.Error.WriteLine($"执行过程中发生错误: {ex.Message}");
    Console.Error.WriteLine($"错误详情: {ex.StackTrace}");
    return "执行失败";
}
finally
{
    // 确保关闭浏览器
    try
    {
        if (driver != null)
        {
            Console.WriteLine("正在关闭浏览器...");
            driver.Quit();
            driver.Dispose();
            Console.WriteLine("浏览器已关闭");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"关闭浏览器时出错: {ex.Message}");
    }
}