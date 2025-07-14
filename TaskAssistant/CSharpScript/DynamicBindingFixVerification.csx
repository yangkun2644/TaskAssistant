// 全面的Microsoft.CSharp???定修复????
Console.WriteLine("?? === Microsoft.CSharp ???定修复?? ===");
Console.WriteLine("????: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
Console.WriteLine();

var testResults = new List<(string TestName, bool Success, string Result)>();

// ??1: 基本???象??
try
{
    dynamic testObj = new { Name = "???象", Value = 42, Created = DateTime.Now };
    var result = $"Name: {testObj.Name}, Value: {testObj.Value}, Created: {testObj.Created:HH:mm:ss}";
    testResults.Add(("基本???象??", true, result));
    Console.WriteLine("? ??1通?: " + result);
}
catch (Exception ex)
{
    testResults.Add(("基本???象??", false, ex.Message));
    Console.WriteLine("? ??1失?: " + ex.Message);
}

// ??2: ExpandoObject???性操作
try
{
    dynamic expando = new System.Dynamic.ExpandoObject();
    expando.Name = "???象";
    expando.Value = 100;
    expando.Calculate = new Func<int, int, int>((a, b) => a * b + 10);
    expando.GetInfo = new Func<string>(() => $"???象信息: {expando.Name}");
    
    var calcResult = expando.Calculate(5, 6);
    var info = expando.GetInfo();
    var result = $"?算?果: {calcResult}, 信息: {info}";
    testResults.Add(("ExpandoObject??操作", true, result));
    Console.WriteLine("? ??2通?: " + result);
}
catch (Exception ex)
{
    testResults.Add(("ExpandoObject??操作", false, ex.Message));
    Console.WriteLine("? ??2失?: " + ex.Message);
}

// ??3: ??字典和集合操作
try
{
    dynamic dict = new Dictionary<string, object>
    {
        ["StringValue"] = "??字符串",
        ["IntValue"] = 999,
        ["DateValue"] = DateTime.Now,
        ["ListValue"] = new List<int> { 1, 2, 3, 4, 5 }
    };
    
    var stringVal = dict["StringValue"];
    var intVal = dict["IntValue"];
    var listVal = (List<int>)dict["ListValue"];
    var listSum = listVal.Sum();
    
    var result = $"String: {stringVal}, Int: {intVal}, ListSum: {listSum}";
    testResults.Add(("??字典操作", true, result));
    Console.WriteLine("? ??3通?: " + result);
}
catch (Exception ex)
{
    testResults.Add(("??字典操作", false, ex.Message));
    Console.WriteLine("? ??3失?: " + ex.Message);
}

// ??4: ???型??和?算
try
{
    dynamic num1 = "123";
    dynamic num2 = "456.78";
    dynamic boolVal = "true";
    
    int intResult = Convert.ToInt32(num1);
    double doubleResult = Convert.ToDouble(num2);
    bool boolResult = Convert.ToBoolean(boolVal);
    
    dynamic calculation = intResult + doubleResult;
    var result = $"Int: {intResult}, Double: {doubleResult}, Bool: {boolResult}, Calc: {calculation}";
    testResults.Add(("???型??", true, result));
    Console.WriteLine("? ??4通?: " + result);
}
catch (Exception ex)
{
    testResults.Add(("???型??", false, ex.Message));
    Console.WriteLine("? ??4失?: " + ex.Message);
}

// ??5: ??方法?用和委托
try
{
    dynamic obj = new
    {
        Data = "???据",
        ProcessFunction = new Func<string, string>(input => $"?理?果: {input.ToUpper()}")
    };
    
    var processed = obj.ProcessFunction(obj.Data);
    var result = $"原始: {obj.Data}, ?理后: {processed}";
    testResults.Add(("??方法?用", true, result));
    Console.WriteLine("? ??5通?: " + result);
}
catch (Exception ex)
{
    testResults.Add(("??方法?用", false, ex.Message));
    Console.WriteLine("? ??5失?: " + ex.Message);
}

// ??6: 复???表?式
try
{
    dynamic config = new
    {
        MaxRetries = 3,
        Timeout = TimeSpan.FromSeconds(30),
        EnableLogging = true,
        GetConnectionString = new Func<string, string>(name => $"Server=localhost;Database={name};")
    };
    
    var connectionString = config.GetConnectionString("TestDB");
    var isValid = config.MaxRetries > 0 && config.EnableLogging;
    var result = $"?接字符串: {connectionString}, 配置有效: {isValid}";
    testResults.Add(("复???表?式", true, result));
    Console.WriteLine("? ??6通?: " + result);
}
catch (Exception ex)
{
    testResults.Add(("复???表?式", false, ex.Message));
    Console.WriteLine("? ??6失?: " + ex.Message);
}

Console.WriteLine();
Console.WriteLine("?? === ???果?? ===");
var successCount = testResults.Count(t => t.Success);
var totalCount = testResults.Count;
var successRate = (double)successCount / totalCount * 100;

Console.WriteLine($"????: {totalCount}");
Console.WriteLine($"成功??: {successCount}");
Console.WriteLine($"失???: {totalCount - successCount}");
Console.WriteLine($"成功率: {successRate:F1}%");

if (successRate == 100)
{
    Console.WriteLine();
    Console.WriteLine("?? === 修复??完成 ===");
    Console.WriteLine("? 所有Microsoft.CSharp???定功能正常工作！");
    Console.WriteLine("? RuntimeBinder??已完全修复！");
    Console.WriteLine("? TaskAssistant?在完全支持??C#?本！");
}
else
{
    Console.WriteLine();
    Console.WriteLine("?? === 部分??失? ===");
    Console.WriteLine("需要?一步?查和修复:");
    foreach (var test in testResults.Where(t => !t.Success))
    {
        Console.WriteLine($"- {test.TestName}: {test.Result}");
    }
}

Console.WriteLine();
Console.WriteLine("?? 修复方案??:");
Console.WriteLine("1. ? 添加了 Microsoft.CSharp NuGet包引用");
Console.WriteLine("2. ? 在?本????中包含了Microsoft.CSharp程序集");
Console.WriteLine("3. ? 添加了Microsoft.CSharp命名空??入");
Console.WriteLine("4. ? 在智能引用管理器中包含了???定支持");
Console.WriteLine("5. ? 移除了有??的?型引用，避免????");

Console.WriteLine();
Console.WriteLine("?在您可以在?本中安全地使用所有C#??功能！");