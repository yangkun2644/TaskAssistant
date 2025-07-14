// ??Microsoft.CSharp???定功能
Console.WriteLine("=== Microsoft.CSharp ???定?? ===");

// ??1: 基本???象
dynamic testObj = new { Name = "???象", Value = 42 };
Console.WriteLine($"???象?性: {testObj.Name}, 值: {testObj.Value}");

// ??2: ExpandoObject??添加?性
dynamic expando = new System.Dynamic.ExpandoObject();
expando.DynamicProperty = "??添加的?性";
expando.Calculate = new Func<int, int, int>((a, b) => a + b);
Console.WriteLine($"ExpandoObject?性: {expando.DynamicProperty}");
Console.WriteLine($"??方法?用?果: {expando.Calculate(10, 20)}");

// ??3: ??字典??
dynamic dict = new Dictionary<string, object>
{
    ["Key1"] = "??字典值1",
    ["Key2"] = 100
};

// ?种方式需要RuntimeBinder支持
Console.WriteLine($"??字典??: Key1={dict["Key1"]}, Key2={dict["Key2"]}");

// ??4: ???型??
dynamic number = "123";
int converted = (int)Convert.ToInt32(number);
Console.WriteLine($"???型??: {number} -> {converted}");

Console.WriteLine("? Microsoft.CSharp ???定??完成！");
Console.WriteLine("所有??操作均正常工作，RuntimeBinder??已修复。");