// ??Microsoft.CSharp???�w�\��
Console.WriteLine("=== Microsoft.CSharp ???�w?? ===");

// ??1: ��???�H
dynamic testObj = new { Name = "???�H", Value = 42 };
Console.WriteLine($"???�H?��: {testObj.Name}, ��: {testObj.Value}");

// ??2: ExpandoObject??�K�[?��
dynamic expando = new System.Dynamic.ExpandoObject();
expando.DynamicProperty = "??�K�[��?��";
expando.Calculate = new Func<int, int, int>((a, b) => a + b);
Console.WriteLine($"ExpandoObject?��: {expando.DynamicProperty}");
Console.WriteLine($"??��k?��?�G: {expando.Calculate(10, 20)}");

// ??3: ??�r��??
dynamic dict = new Dictionary<string, object>
{
    ["Key1"] = "??�r���1",
    ["Key2"] = 100
};

// ?���覡�ݭnRuntimeBinder���
Console.WriteLine($"??�r��??: Key1={dict["Key1"]}, Key2={dict["Key2"]}");

// ??4: ???��??
dynamic number = "123";
int converted = (int)Convert.ToInt32(number);
Console.WriteLine($"???��??: {number} -> {converted}");

Console.WriteLine("? Microsoft.CSharp ???�w??�����I");
Console.WriteLine("�Ҧ�??�ާ@�����`�u�@�ARuntimeBinder??�w���`�C");