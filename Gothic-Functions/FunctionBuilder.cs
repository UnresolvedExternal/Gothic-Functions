using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gothic_Functions
{
	public partial class FunctionBuilder(int engine)
	{
		public FunctionInfo Build(string text)
		{
			if (Info.OriginalString.Length > 0)
				Info = new FunctionInfo();

			Info.OriginalString = text;
			text = PreprocessText(text);
			var tokens = Tokenize(text);
			ParserException.Assert(IsFunction(tokens), "Calling convention missed");
			tokens = ExtractSimpleProperties(tokens);
			tokens = UniteTemplates(tokens);
			tokens = ExtractReturnTypeAndConvention(tokens);
			tokens = ExtractClassName(tokens);
			tokens = ExtractFunctionName(tokens);
			ExtractParameters(tokens);
			return Info;
		}

		private static string PreprocessText(string text)
		{
			text = Regex.Replace(text, @"(`[^']*')+", match =>
			{
				var text = Regex.Replace(match.Value, @"[`']", "");
				var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
					.Select(t => char.ToUpper(t[0]) + t[1..]);

				return string.Join("", tokens);
			});

			text = Regex.Replace(text, @"\bclass |\bstruct |\benum ", "");
			text = text.Replace(" *", "*").Replace(" &", "&");
			text = text.Replace("(void)", "() ");
			text = text.Replace(",", ", ");
			text = text.Replace("[thunk]:", "");
			return text;
		}

		private static List<string> Tokenize(string text)
		{
			var pattern = @"^0x\d{8]" +
				@"|\bpublic:|\bprotected:|\bprivate:" +
				@"|\boperator[^(]+" +
				@"|::|<|>|\(|\)|,";

			var tokens = new List<string>();
			int index = 0;

			foreach (Match match in Regex.Matches(text, pattern))
			{
				if (match.Index > index)
					tokens.Add(text.Substring(index, match.Index - index));

				tokens.Add(match.Value);
				index = match.Index + match.Length;
			}

			if (index < text.Length)
				tokens.Add(text.Substring(index));

			tokens = tokens
				.SelectMany(t => t.Split(" "))
				.Select(t => t.Trim())
				.Where(t => t.Length > 0)
				.ToList();

			return tokens;
		}

		private static bool IsFunction(IEnumerable<string> type)
		{
			var counter = new BracketCounter();

			foreach (var token in type)
			{
				counter.Append(token);

				if (counter.Level == 0 && CallingConventionRegex().IsMatch(token))
					return true;
			}

			return false;
		}

		private List<string> ExtractSimpleProperties(IEnumerable<string> tokens)
		{
			var result = tokens.ToList();
			ParserException.Assert(result.Count > 0, "Address not found");
			Info.Address[Engine - 1] = result[0];
			result.RemoveAt(0);

			var sb = new StringBuilder();

			foreach (var token in result)
				UniteTokens(sb, token);

			Info.ShortString = sb.ToString();

			if (result.Last() == "const")
			{
				Info.IsConst = true;
				result.RemoveAt(result.Count - 1);
			}

			var counter = new BracketCounter();

			for (int i = 0; i < result.Count; i++)
			{
				var token = result[i];
				counter.Append(token);

				if (counter.ParenthesisBlocks != 0 || counter.TriangleBracketBlocks != 0)
					break;

				if (!Info.IsVirtual && token == "virtual")
				{
					Info.IsVirtual = true;
					result.RemoveAt(i--);
				} else if (!Info.IsStatic && token == "static")
				{
					Info.IsStatic = true;
					result.RemoveAt(i--);
				} else if (Info.Visibility.Length == 0 && VisibilityRegex().IsMatch(token))
				{
					Info.Visibility = token.Substring(0, token.Length - 1);
					result.RemoveAt(i--);
				}
			}

			return result;
		}

		private static List<string> UniteTemplates(List<string> tokens)
		{
			var result = tokens.Select(t => new StringBuilder(t)).ToList();
			var counter = new BracketCounter();

			for (int i = 0; i < result.Count; i++)
			{
				var token = result[i].ToString();
				bool concat = counter.TriangleBracketLevel > 0 || token == "<";
				counter.Append(token);

				if (!concat)
					continue;

				UniteTokens(result[i - 1], token);
				result.RemoveAt(i--);
			}

			return result.Select(t => t.ToString()).ToList();
		}

		private List<string> ExtractReturnTypeAndConvention(List<string> tokens)
		{
			var counter = new BracketCounter();
			var result = new StringBuilder();

			for (int i = 0; i < tokens.Count; i++)
			{
				var token = tokens[i];
				counter.Append(token);

				if (counter.Level == 0 && CallingConventionRegex().IsMatch(token))
				{
					Info.ReturnType = result.ToString();
					Info.CallingConvention = token;
					return tokens.Skip(i + 1).ToList();
				}

				UniteTokens(result, token);
			}

			throw new ParserException("Calling convention not found");
		}

		private List<string> ExtractClassName(List<string> tokens)
		{
			if (Info.Visibility.Length == 0)
				return tokens;

			var counter = new BracketCounter();
			int separatorIndex = -1;

			for (int i = 0; i < tokens.Count; i++)
			{
				var token = tokens[i];
				counter.Append(token);

				if (counter.Level == 0 && token == "::")
					separatorIndex = i;
			}

			ParserException.Assert(separatorIndex > 0, "Class/method separator not found");
			var result = new StringBuilder();

			for (int i = 0; i < separatorIndex; i++)
				UniteTokens(result, tokens[i]);

			Info.Class = result.ToString();
			return tokens.Skip(separatorIndex + 1).ToList();
		}
		private List<string> ExtractFunctionName(List<string> tokens)
		{
			var counter = new BracketCounter();
			var result = new StringBuilder();
			int index = -1;

			for (int i = 0; i < tokens.Count; i++)
			{
				var token = tokens[i];
				counter.Append(token);

				if (counter.ParenthesisLevel > 0)
				{
					index = i;
					break;
				}

				UniteTokens(result, token);
			}

			ParserException.Assert(index > 0, "Function name not found");
			Info.Name = result.ToString();

			return tokens.Skip(index).ToList();
		}
		private void ExtractParameters(List<string> tokens)
		{
			ParserException.Assert(tokens.First() == "(" && tokens.Last() == ")", "Parameter list must be between parenthesis");

			var parameter = new StringBuilder();
			var counter = new BracketCounter();
			counter.Append(tokens.First());

			foreach (var token in tokens.Skip(1))
			{
				if (counter.IsInGlobalParenthesisBlock && (token == "," || token == ")") && parameter.Length > 0)
				{
					Info.Parameters.Add(parameter.ToString());
					parameter.Clear();
				}

				if (token != "," && token != ")" || !counter.IsInGlobalParenthesisBlock)
					UniteTokens(parameter, token);

				counter.Append(token);
			}
		}

		private static void UniteTokens(StringBuilder current, string next)
		{
			if (current.Length == 0)
			{
				current.Append(next);
				return;
			}

			var token = next.ToString();

			if (token == ",")
			{
				current.Append(token);
				current.Append(' ');
				return;
			}

			char lastChar = current[^1];
			char newChar = token[0];

			if (char.IsLetter(newChar) || newChar == '_')
				if (char.IsLetter(lastChar) || char.IsDigit(lastChar) || lastChar == '*' || lastChar == '&' || lastChar == ':' && !current.ToString().EndsWith("::"))
					current.Append(' ');

			current.Append(token);
		}

		private readonly int Engine = (engine >= 1 && engine <= 4) ? engine : throw new ArgumentException("Invalid engine value");
		internal FunctionInfo Info { get; set; } = new FunctionInfo();

		[GeneratedRegex(@"^(public:|private:|protected:)$")]
		private static partial Regex VisibilityRegex();

		[GeneratedRegex(@"^(__thiscall|__stdcall|__fastcall|__cdecl)$")]
		private static partial Regex CallingConventionRegex();
	}
}
