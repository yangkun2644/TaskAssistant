using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Text.RegularExpressions;

namespace TaskAssistant.Services
{
    /// <summary>
    /// 智能NuGet包解析器
    /// 自动识别代码中可能需要的NuGet包
    /// </summary>
    public static class SmartNuGetResolver
    {
        /// <summary>
        /// 智能分析代码并推荐NuGet包
        /// </summary>
        public static List<(string PackageId, string Version)> AnalyzeCodeForNuGetPackages(string code)
        {
            var packages = new List<(string PackageId, string Version)>();

            // 类型到NuGet包的映射
            var typeToPackage = new Dictionary<string, (string PackageId, string Version)>
            {
                // Web开发
                [@"\bHttpClient\b"] = ("System.Net.Http", "*"),
                [@"\bHttpContext\b"] = ("Microsoft.AspNetCore.Http.Abstractions", "*"),
                [@"\bController\b"] = ("Microsoft.AspNetCore.Mvc", "*"),
                [@"\bWebApplication\b"] = ("Microsoft.AspNetCore.App", "*"),

                // JSON处理
                [@"\bJObject\b"] = ("Newtonsoft.Json", "*"),
                [@"\bJArray\b"] = ("Newtonsoft.Json", "*"),
                [@"\bJsonConvert\b"] = ("Newtonsoft.Json", "*"),

                // 数据库
                [@"\bSqlConnection\b"] = ("System.Data.SqlClient", "*"),
                [@"\bDbContext\b"] = ("Microsoft.EntityFrameworkCore", "*"),
                [@"\bSqliteConnection\b"] = ("Microsoft.Data.Sqlite", "*"),
                [@"\bNpgsqlConnection\b"] = ("Npgsql", "*"),
                [@"\bMySqlConnection\b"] = ("MySql.Data", "*"),

                // 测试框架
                [@"\bTest\b.*\bAttribute\b"] = ("Microsoft.NET.Test.Sdk", "*"),
                [@"\bFact\b"] = ("xunit", "*"),
                [@"\bAssert\b"] = ("xunit.assert", "*"),
                [@"\bMock\b"] = ("Moq", "*"),

                // 日志
                [@"\bILogger\b"] = ("Microsoft.Extensions.Logging", "*"),
                [@"\bSerilog\b"] = ("Serilog", "*"),

                // 配置
                [@"\bIConfiguration\b"] = ("Microsoft.Extensions.Configuration", "*"),

                // HTTP客户端
                [@"\bRestClient\b"] = ("RestSharp", "*"),
                [@"\bHttpClientFactory\b"] = ("Microsoft.Extensions.Http", "*"),

                // 序列化
                [@"\bMessagePackSerializer\b"] = ("MessagePack", "*"),
                [@"\bProtoBuf\b"] = ("protobuf-net", "*"),

                // 图像处理
                [@"\bImage\b"] = ("System.Drawing.Common", "*"),
                [@"\bBitmap\b"] = ("System.Drawing.Common", "*"),

                // Excel
                [@"\bExcelPackage\b"] = ("EPPlus", "*"),
                [@"\bWorkbook\b"] = ("ClosedXML", "*"),

                // PDF
                [@"\bPdfDocument\b"] = ("iTextSharp", "*"),

                // 网页抓取
                [@"\bHtmlDocument\b"] = ("HtmlAgilityPack", "*"),
                [@"\bWebDriver\b"] = ("Selenium.WebDriver", "*"),
                [@"\bChromeDriver\b"] = ("Selenium.WebDriver.ChromeDriver", "*"),

                // 压缩
                [@"\bZipFile\b"] = ("System.IO.Compression.ZipFile", "*"),
                [@"\bGZipStream\b"] = ("System.IO.Compression", "*"),

                // 缓存
                [@"\bMemoryCache\b"] = ("Microsoft.Extensions.Caching.Memory", "*"),
                [@"\bRedis\b"] = ("StackExchange.Redis", "*"),

                // 消息队列
                [@"\bRabbitMQ\b"] = ("RabbitMQ.Client", "*"),
                [@"\bKafka\b"] = ("Confluent.Kafka", "*"),

                // 文档生成
                [@"\bSwagger\b"] = ("Swashbuckle.AspNetCore", "*"),

                // 验证
                [@"\bFluentValidation\b"] = ("FluentValidation", "*"),

                // 映射
                [@"\bAutoMapper\b"] = ("AutoMapper", "*"),

                // 时间处理
                [@"\bNodaTime\b"] = ("NodaTime", "*"),

                // 数学计算
                [@"\bMathNet\b"] = ("MathNet.Numerics", "*"),
            };

            foreach (var pattern in typeToPackage)
            {
                if (Regex.IsMatch(code, pattern.Key, RegexOptions.IgnoreCase))
                {
                    packages.Add(pattern.Value);
                }
            }

            return packages.Distinct().ToList();
        }
    }
}