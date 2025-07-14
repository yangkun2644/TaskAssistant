// ������Microsoft.CSharp???�w���`????
Console.WriteLine("?? === Microsoft.CSharp ???�w���`?? ===");
Console.WriteLine("????: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
Console.WriteLine();

var testResults = new List<(string TestName, bool Success, string Result)>();

// ??1: ��???�H??
try
{
    dynamic testObj = new { Name = "???�H", Value = 42, Created = DateTime.Now };
    var result = $"Name: {testObj.Name}, Value: {testObj.Value}, Created: {testObj.Created:HH:mm:ss}";
    testResults.Add(("��???�H??", true, result));
    Console.WriteLine("? ??1�q?: " + result);
}
catch (Exception ex)
{
    testResults.Add(("��???�H??", false, ex.Message));
    Console.WriteLine("? ??1��?: " + ex.Message);
}

// ??2: ExpandoObject???�ʾާ@
try
{
    dynamic expando = new System.Dynamic.ExpandoObject();
    expando.Name = "???�H";
    expando.Value = 100;
    expando.Calculate = new Func<int, int, int>((a, b) => a * b + 10);
    expando.GetInfo = new Func<string>(() => $"???�H�H��: {expando.Name}");
    
    var calcResult = expando.Calculate(5, 6);
    var info = expando.GetInfo();
    var result = $"?��?�G: {calcResult}, �H��: {info}";
    testResults.Add(("ExpandoObject??�ާ@", true, result));
    Console.WriteLine("? ??2�q?: " + result);
}
catch (Exception ex)
{
    testResults.Add(("ExpandoObject??�ާ@", false, ex.Message));
    Console.WriteLine("? ??2��?: " + ex.Message);
}

// ??3: ??�r��M���X�ާ@
try
{
    dynamic dict = new Dictionary<string, object>
    {
        ["StringValue"] = "??�r�Ŧ�",
        ["IntValue"] = 999,
        ["DateValue"] = DateTime.Now,
        ["ListValue"] = new List<int> { 1, 2, 3, 4, 5 }
    };
    
    var stringVal = dict["StringValue"];
    var intVal = dict["IntValue"];
    var listVal = (List<int>)dict["ListValue"];
    var listSum = listVal.Sum();
    
    var result = $"String: {stringVal}, Int: {intVal}, ListSum: {listSum}";
    testResults.Add(("??�r��ާ@", true, result));
    Console.WriteLine("? ??3�q?: " + result);
}
catch (Exception ex)
{
    testResults.Add(("??�r��ާ@", false, ex.Message));
    Console.WriteLine("? ??3��?: " + ex.Message);
}

// ??4: ???��??�M?��
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
    testResults.Add(("???��??", true, result));
    Console.WriteLine("? ??4�q?: " + result);
}
catch (Exception ex)
{
    testResults.Add(("???��??", false, ex.Message));
    Console.WriteLine("? ??4��?: " + ex.Message);
}

// ??5: ??��k?�ΩM�e��
try
{
    dynamic obj = new
    {
        Data = "???�u",
        ProcessFunction = new Func<string, string>(input => $"?�z?�G: {input.ToUpper()}")
    };
    
    var processed = obj.ProcessFunction(obj.Data);
    var result = $"��l: {obj.Data}, ?�z�Z: {processed}";
    testResults.Add(("??��k?��", true, result));
    Console.WriteLine("? ??5�q?: " + result);
}
catch (Exception ex)
{
    testResults.Add(("??��k?��", false, ex.Message));
    Console.WriteLine("? ??5��?: " + ex.Message);
}

// ??6: �`???��?��
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
    var result = $"?���r�Ŧ�: {connectionString}, �t�m����: {isValid}";
    testResults.Add(("�`???��?��", true, result));
    Console.WriteLine("? ??6�q?: " + result);
}
catch (Exception ex)
{
    testResults.Add(("�`???��?��", false, ex.Message));
    Console.WriteLine("? ??6��?: " + ex.Message);
}

Console.WriteLine();
Console.WriteLine("?? === ???�G?? ===");
var successCount = testResults.Count(t => t.Success);
var totalCount = testResults.Count;
var successRate = (double)successCount / totalCount * 100;

Console.WriteLine($"????: {totalCount}");
Console.WriteLine($"���\??: {successCount}");
Console.WriteLine($"��???: {totalCount - successCount}");
Console.WriteLine($"���\�v: {successRate:F1}%");

if (successRate == 100)
{
    Console.WriteLine();
    Console.WriteLine("?? === ���`??���� ===");
    Console.WriteLine("? �Ҧ�Microsoft.CSharp???�w�\�ॿ�`�u�@�I");
    Console.WriteLine("? RuntimeBinder??�w�������`�I");
    Console.WriteLine("? TaskAssistant?�b�������??C#?���I");
}
else
{
    Console.WriteLine();
    Console.WriteLine("?? === ����??��? ===");
    Console.WriteLine("�ݭn?�@�B?�d�M���`:");
    foreach (var test in testResults.Where(t => !t.Success))
    {
        Console.WriteLine($"- {test.TestName}: {test.Result}");
    }
}

Console.WriteLine();
Console.WriteLine("?? ���`���??:");
Console.WriteLine("1. ? �K�[�F Microsoft.CSharp NuGet�]�ޥ�");
Console.WriteLine("2. ? �b?��????���]�t�FMicrosoft.CSharp�{�Ƕ�");
Console.WriteLine("3. ? �K�[�FMicrosoft.CSharp�R�W��??�J");
Console.WriteLine("4. ? �b����ޥκ޲z�����]�t�F???�w���");
Console.WriteLine("5. ? �����F��??��?���ޥΡA�קK????");

Console.WriteLine();
Console.WriteLine("?�b�z�i�H�b?�����w���a�ϥΩҦ�C#??�\��I");