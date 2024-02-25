using System.Text;

namespace Gothic_Functions
{
	class BracketCounter
    {
        public int Level => brackets.Length;
        public int ParenthesisLevel { get; private set; }
        public int TriangleBracketLevel { get; private set; }
        public bool IsInParenthesisBlock => ParenthesisLevel > 0 && brackets[0] == '(';
        public bool IsInTriangleBracketBlock => TriangleBracketLevel > 0 && brackets[0] == '<';
        public bool IsInGlobalParenthesisBlock => Level == 1 && ParenthesisLevel == 1;
        public bool IsInGlobalTriangleBracketBlock => Level == 1 && TriangleBracketLevel == 1;
        public int ParenthesisBlocks { get; private set; }
        public int TriangleBracketBlocks { get; private set; }

        public void Append(char ch)
        {
            switch (ch)
            {
                case '(':
                    brackets.Append(ch);
                    ParenthesisLevel += 1;

                    if (brackets.Length == 1)
                        ParenthesisBlocks += 1;

                    break;

                case '<':
                    brackets.Append(ch);
                    TriangleBracketLevel += 1;

                    if (brackets.Length == 1)
                        TriangleBracketBlocks += 1;

                    break;

                case ')':
					ParserException.Assert(brackets.Length > 0 && brackets[^1] == '(', "Parenthesis mistmatch");
                    brackets.Remove(brackets.Length - 1, 1);
                    ParenthesisLevel -= 1;
                    break;

                case '>':
					ParserException.Assert(brackets.Length > 0 && brackets[^1] == '<', "Triangle bracket mistmatch");
					brackets.Remove(brackets.Length - 1, 1);
					TriangleBracketLevel -= 1;
					break;

                default:
                    break;
			}
        }

        public void Append(string text)
        {
            if (text.Length == 1)
                Append(text[0]);
        }

        public bool IsBracket(string text) => text == "(" || text == ")" || text == "<" || text == ">";

        private StringBuilder brackets = new StringBuilder();
    }
}
