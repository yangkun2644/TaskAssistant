// NuGet�U??��???��
// ��?���Τ_??NuGet�]�U??��???��?�X�\��

#r "nuget:Newtonsoft.Json"
#r "nuget:HtmlAgilityPack"

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using HtmlAgilityPack;

Console.WriteLine("?�l??NuGet�]�U??�ץ\��...");

// ??JSON?�z
var testData = new { Name = "TaskAssistant", Version = "1.0.0", Description = "??NuGet�U??��" };
var json = JsonConvert.SerializeObject(testData, Formatting.Indented);
Console.WriteLine("JSON�ǦC��?�G:");
Console.WriteLine(json);

// ??HTML�ѪR
var htmlDoc = new HtmlDocument();
htmlDoc.LoadHtml("<html><head><title>???��</title></head><body><h1>Hello World</h1></body></html>");
var title = htmlDoc.DocumentNode.SelectSingleNode("//title")?.InnerText;
Console.WriteLine($"�ѪR��HTML??: {title}");

Console.WriteLine("NuGet�]�U?�M�{�Ƕ��[???�����I");

return "??���\����";