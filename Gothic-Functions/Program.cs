using Gothic_Functions;
using System.Reflection;
using System.Text;
using System.Text.Json;

static string GetDataPath()
{
	Assembly entryAssembly = Assembly.GetEntryAssembly() ?? throw new ApplicationException("Entry assembly not found");
	string dataPath = new FileInfo(entryAssembly.Location)?.Directory?.Parent?.Parent?.Parent?.Parent?.ToString() ?? "";
	dataPath = dataPath.Length > 0 ? Path.Combine(dataPath, "Data") : throw new ApplicationException("Data path not found");
	return dataPath;
}

static List<FunctionInfo>[] ParseFunctions(string dataPath)
{
	var functions = Enumerable.Repeat(0, 4).Select(_ => new List<FunctionInfo>()).ToArray();

	for (int engine = 1; engine <= 4; engine++)
	{
		var inputPath = Path.Combine(dataPath, "Input", engine + ".txt");
		var errorPath = Path.Combine(dataPath, "Error", engine + ".txt");
		var builder = new FunctionBuilder(engine);

		using (var errorWriter = File.CreateText(errorPath))
			foreach (var line in File.ReadLines(inputPath))
				try
				{
					var info = builder.Build(line);
					functions[engine - 1].Add(info);
				}
				catch (ParserException e)
				{
					errorWriter.WriteLine($"{line} [[{e.Message}]]");
				}
	}

	return functions;
}

static void SerializeFunctions(List<FunctionInfo>[] functions, string dataPath, JsonSerializerOptions jsonOptions)
{
	for (int engine = 1; engine <= 4; engine++)
	{
		var jsonPath = Path.Combine(dataPath, "Json", engine + ".json");

		using (var stream = File.Create(jsonPath))
			JsonSerializer.Serialize(stream, functions[engine - 1], jsonOptions);
	}
}

static string GetSignatureKey(FunctionInfo info)
{
	var sb = new StringBuilder();
	sb.AppendLine(info.Class);
	sb.AppendLine(info.Name);
	sb.AppendLine(info.CallingConvention);
	sb.AppendLine(info.IsStatic.ToString());
	sb.AppendLine(info.ReturnType);

	foreach (var parameter in info.Parameters)
		sb.AppendLine(parameter);

	return sb.ToString();
}

static FunctionInfo CombineFunctions(FunctionInfo? outer, FunctionInfo? inner, string signatureKey)
{
	FunctionInfo mostImportantInfo = inner ?? outer ?? throw new ApplicationException();

	var info = new FunctionInfo
	{
		OriginalString = mostImportantInfo.OriginalString,
		ShortString = mostImportantInfo.ShortString,
		Visibility = mostImportantInfo.Visibility,
		CallingConvention = mostImportantInfo.CallingConvention,
		ReturnType = mostImportantInfo.ReturnType,
		Class = mostImportantInfo.Class,
		Name = mostImportantInfo.Name,
		Parameters = [.. mostImportantInfo.Parameters],
		IsStatic = mostImportantInfo.IsStatic,
		IsVirtual = mostImportantInfo.IsVirtual,
		IsConst = mostImportantInfo.IsConst,
		Address = [.. mostImportantInfo.Address]
	};

	if (outer != null)
		for (int i = 0; i < info.Address.Length; i++)
			if (info.Address[i].Length == 0)
				info.Address[i] = outer.Address[i];

	return info;
}

static void AssertNoConstFuncAmbiguity(List<FunctionInfo>[] functions)
{
	foreach (var list in functions)
		foreach (var group in list.ToLookup(GetSignatureKey))
			if (group.Count() != 1)
				throw new ApplicationException($"Const func ambiguity [[{group.First().OriginalString}]]");
}

static void SerializeUnitedFunctions(List<FunctionInfo> unitedFunctions, string dataPath, JsonSerializerOptions jsonOptions)
{
	using (var stream = File.Create(Path.Combine(dataPath, "Json", "all.json")))
		JsonSerializer.Serialize(stream, unitedFunctions, jsonOptions);
}

static void GenerateSnippetFiles(List<FunctionInfo> unitedFunctions, string dataPath)
{
	var snippetTemplate = File.ReadAllText(Path.Combine(dataPath, "template.snippet"));

	foreach (var info in unitedFunctions)
	{
		var snippetGenerator = new SnippetGenerator(info);
		var snippet = snippetTemplate.Replace("{Title}", snippetGenerator.GetTitle());
		snippet = snippet.Replace("{Shortcut}", snippetGenerator.GetShortcut());
		snippet = snippet.Replace("{Description}", snippetGenerator.GetDescription());
		snippet = snippet.Replace("{Code}", snippetGenerator.GetCode());
		File.WriteAllText(Path.Combine(dataPath, "Snippet", snippetGenerator.GetShortcut() + ".snippet"), snippet);
	}
}

var dataPath = GetDataPath();
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
var functions = ParseFunctions(dataPath);
AssertNoConstFuncAmbiguity(functions);
SerializeFunctions(functions, dataPath, jsonOptions);
var unitedFunctions = functions.Aggregate((outer, inner) => outer.FullOuterJoin(inner, GetSignatureKey, GetSignatureKey, CombineFunctions).ToList());
SerializeUnitedFunctions(unitedFunctions, dataPath, jsonOptions);
GenerateSnippetFiles(unitedFunctions, dataPath);
