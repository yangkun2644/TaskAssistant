// NuGet下??度???本
// 此?本用于??NuGet包下??的???度?出功能

#r "nuget:Newtonsoft.Json"
#r "nuget:HtmlAgilityPack"

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using HtmlAgilityPack;

Console.WriteLine("?始??NuGet包下??度功能...");

// ??JSON?理
var testData = new { Name = "TaskAssistant", Version = "1.0.0", Description = "??NuGet下??度" };
var json = JsonConvert.SerializeObject(testData, Formatting.Indented);
Console.WriteLine("JSON序列化?果:");
Console.WriteLine(json);

// ??HTML解析
var htmlDoc = new HtmlDocument();
htmlDoc.LoadHtml("<html><head><title>???面</title></head><body><h1>Hello World</h1></body></html>");
var title = htmlDoc.DocumentNode.SelectSingleNode("//title")?.InnerText;
Console.WriteLine($"解析的HTML??: {title}");

Console.WriteLine("NuGet包下?和程序集加???完成！");

return "??成功完成";