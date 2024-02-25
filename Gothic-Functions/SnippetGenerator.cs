using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Gothic_Functions
{
	public class SnippetGenerator
	{
		public SnippetGenerator(FunctionInfo info)
		{
			Info = info;
		}

		public string GetTitle()
		{
			var sb = new StringBuilder();
			
			if (Info.Class.Length > 0)
				sb.Append($"{Info.Class}::");

			sb.Append($"{Info.Name}(");
			sb.Append(string.Join(", ", Info.Parameters));
			sb.Append(")");

			if (Info.ReturnType.Length > 0)
				sb.Append($": {Info.ReturnType}");

			if (Info.IsConst)
				sb.Append(" [const]");

			return sb.ToString();
		}

		public string GetShortcut()
		{
			var sb = new StringBuilder("__");

			if (Info.Class.Length > 0)
				sb.Append($"{Info.Class}_");

			sb.Append($"{Info.Name}");
			sb.Length = Math.Min(sb.Length, 64);
			sb.Append("_");

			foreach (var address in Info.Address.Reverse())
				if (address.Length > 0)
				{
					sb.Append(address);
					break;
				}

			ReplaceInvalidChars(sb);
			return sb.ToString();
		}

		public string GetCode()
		{
			var sb = new StringBuilder();
			string? warning = GetWarning();

			if (warning != null)
				sb.AppendLine(warning);

			string convention = Info.CallingConvention == "__thiscall" ? "" : $" {Info.CallingConvention}";
			string constString = Info.IsConst ? " const" : "";

			if (Info.Class.Length > 0 && !Info.IsStatic)
			{
				sb.AppendLine($"struct {GetStructName()} : {Info.Class} {{ {GetReturnType()}{convention} operator(){GetParameterList(false)}{constString}; }};");
				sb.AppendLine($"BindedHook Ivk_{GetStructName()}{{ {GetAddresses()}, &{GetStructName()}::operator() }};");
				sb.AppendLine($"{GetReturnType()}{convention} {GetStructName()}::operator(){GetParameterList(true)}{constString}");
				sb.AppendLine("{");

				if (GetReturnType() == "void")
					sb.AppendLine($"\tTHISCALL(Ivk_{GetStructName()}){GetParameterNameList()};");
				else
				{
					sb.Append('\t');
					sb.Append(GetDeclaration(GetReturnType(), "result"));
					sb.AppendLine($" = THISCALL(Ivk_{GetStructName()}){GetParameterNameList()};");
					sb.AppendLine("\treturn result;");
				}

				sb.AppendLine("}");
			}
			else
			{
				convention = Info.CallingConvention == "__cdecl" ? "" : $" {Info.CallingConvention}";
				sb.AppendLine($"{GetReturnType()}{convention} {GetHookName(false)}{GetParameterList(false)};");
				sb.AppendLine($"BindedHook {GetHookName(true)}{{ {GetAddresses()}, {GetHookName(false)} }};");
				sb.AppendLine($"{GetReturnType()}{convention} {GetHookName(false)}{GetParameterList(true)}");
				sb.AppendLine("{");

				if (GetReturnType() == "void")
					sb.AppendLine($"\t{GetHookName(true)}{GetParameterNameList()};");
				else
				{
					sb.Append('\t');
					sb.Append(GetDeclaration(GetReturnType(), "result"));
					sb.AppendLine($" = {GetHookName(true)}{GetParameterNameList()};");
					sb.AppendLine("\treturn result;");
				}

				sb.AppendLine("}");
			}

			return sb.ToString();
		}

		public string GetDescription() => Info.ShortString;

		private string GetHookName(bool ivk)
		{
			var prefix = ivk ? "Ivk_" : "Hook_";
			var sb = new StringBuilder(prefix);

			if (Info.Class.Length > 0)
				sb.Append($"{Info.Class}_");

			sb.Append($"{Info.Name}");
			ReplaceInvalidChars(sb);
			return sb.ToString();
		}

		private string? GetWarning()
		{
			if (Info.Address.All(a => a.Length > 0))
				return null;

			var sb = new StringBuilder("// WARNING!!! Supported versions:");
			string[] names = ["G1", "G1A", "G2", "G2A"];
			bool once = true;

			for (int i = 0; i < 4; i++)
				if (Info.Address[i].Length > 0)
				{
					sb.Append(once ? " " : ", ");
					sb.Append(names[i]);
					once = false;
				}

			return sb.ToString();
		}

		private string GetAddresses()
		{
			var sb = new StringBuilder("ZENFOR(");
			var addresses = Info.Address.Select(a => a.Length > 0 ? a : "0x00000000");
			sb.Append(string.Join(", ", addresses));
			sb.Append(')');
			return sb.ToString();
		}

		private string GetStructName()
		{
			var name = IsDestructor() ? "Destructor" : Info.Name;
			var sb = new StringBuilder($"{Info.Class}_{name}");
			ReplaceInvalidChars(sb);
			return sb.ToString();
		}

		private string GetReturnType()
		{
			if (Info.ReturnType.Length > 0)
				return Info.ReturnType;

			if (IsConstructor())
				return $"{Info.Class}*";

			return "void";
		}

		private bool IsConstructor() => Info.Class.Equals(Info.Name, StringComparison.InvariantCulture);

		private bool IsDestructor() => Info.Name.StartsWith('~');

		private string GetParameterList(bool generateNames)
		{
			IEnumerable<string> parameters = generateNames ? Info.Parameters.Select((p, i) => GetDeclaration(p, $"a{i}")) : Info.Parameters;
			return $"({string.Join(", ", parameters)})";
		}

		private string GetParameterNameList()
		{
			IEnumerable<string> parameters = Info.Parameters.Select((p, i) => $"a{i}");
			return $"({string.Join(", ", parameters)})";
		}

		private static string GetDeclaration(string type, string name)
		{
			bool inParenthesis = false;
			int asteriskIndex = -1;
			var counter = new BracketCounter();

			for (int i = 0; i < type.Length; i++)
			{
				counter.Append(type[i]);

				if (counter.IsInGlobalParenthesisBlock)
				{
					inParenthesis = true;

					if (type[i] == '*')
						asteriskIndex = i;
				}
				else if (inParenthesis)
					break;
            }

			if (asteriskIndex == -1)
				return $"{type} {name}";

			return type.Insert(asteriskIndex + 1, $" {name}");
		}

		private static void ReplaceInvalidChars(StringBuilder sb)
		{
			for (int i = 0; i < sb.Length; i++)
				if (!char.IsLetter(sb[i]) && !char.IsDigit(sb[i]) && sb[i] != '_')
					sb[i] = '_';
		}

		private readonly FunctionInfo Info;
	}
}
