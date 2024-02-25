using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gothic_Functions
{
    public class ParserException(string message) : Exception(message)
    {
        public static void Throw(string message) => throw new ParserException(message);
        public static void Assert(bool condition, string message)
        {
            if (!condition)
                Throw(message);
        }
    }
}
