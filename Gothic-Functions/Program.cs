using Gothic_Functions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

Assembly entryAssembly = Assembly.GetEntryAssembly() ?? throw new ApplicationException("Entry assembly missed");
string dataPath = new FileInfo(entryAssembly.Location)?.Directory?.Parent?.Parent?.Parent?.Parent?.ToString() ?? "";
dataPath = dataPath.Length > 0 ? Path.Combine(dataPath, @"Data") : throw new ApplicationException("Data path not found");

var functions = Enumerable.Repeat(0, 4).Select(_ => new List<FunctionInfo>()).ToArray();

for (int engine = 1; engine <= 4; engine++)
{
	var inputPath = Path.Combine(dataPath, "Input", engine + ".txt");
	var jsonPath = Path.Combine(dataPath, "Json", engine + ".json");
	var errorPath = Path.Combine(dataPath, "Error", engine + ".txt");
	var builder = new FunctionBuilder(engine);

	using (var errorWriter = File.CreateText(errorPath))
	{
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

	using (var splittedJsonWriter = File.Create(jsonPath))
	{
		var options = new JsonSerializerOptions { WriteIndented = true };
		JsonSerializer.Serialize(splittedJsonWriter, functions[engine - 1], options);
	}
}

Func<FunctionInfo, string> keySelector = info =>
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
};

Func<FunctionInfo?, FunctionInfo?, string, FunctionInfo> projection = (outer, inner, key) =>
{
	FunctionInfo any = inner ?? outer ?? throw new ApplicationException();

	var info = new FunctionInfo
	{
		OriginalString = any.OriginalString,
		ShortString = any.ShortString,
		Visibility = any.Visibility,
		CallingConvention = any.CallingConvention,
		ReturnType = any.ReturnType,
		Class = any.Class,
		Name = any.Name,
		Parameters = [.. any.Parameters],
		IsStatic = any.IsStatic,
		IsVirtual = any.IsVirtual,
		IsConst = any.IsConst,
		Address = [.. any.Address]
	};

	if (outer != null)
		for (int i = 0; i < info.Address.Length; i++)
			if (info.Address[i].Length == 0)
				info.Address[i] = outer.Address[i];

	return info;
};

var aggregated = functions.Aggregate((outer, inner) => outer.FullOuterJoin(inner, keySelector, keySelector, projection).ToList());

using (var jsonWriter = File.Create(Path.Combine(dataPath, "Json", "all.json")))
{
	var options = new JsonSerializerOptions { WriteIndented = true };
	JsonSerializer.Serialize(jsonWriter, aggregated, options);
}

var snippetTemplate = File.ReadAllText(Path.Combine(dataPath, "template.snippet"));

foreach (var info in aggregated)
{
	var snippetGenerator = new SnippetGenerator(info);
	var snippet = snippetTemplate.Replace("{Title}", snippetGenerator.GetTitle());
	snippet = snippet.Replace("{Shortcut}", snippetGenerator.GetShortcut());
	snippet = snippet.Replace("{Description}", snippetGenerator.GetDescription());
	snippet = snippet.Replace("{Code}", snippetGenerator.GetCode());
	File.WriteAllText(Path.Combine(dataPath, "Snippet", snippetGenerator.GetShortcut() + ".snippet"), snippet);
}
