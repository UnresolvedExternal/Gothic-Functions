using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Gothic_Functions
{
    public class FunctionInfo
    {
        public string OriginalString { get; set; } = string.Empty;
        public string ShortString { get; set; } = string.Empty;
        public string Visibility { get; set; } = string.Empty;
        public string CallingConvention { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Parameters { get; set; } = [];
        public bool IsStatic { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsConst { get; set; }
        public string[] Address { get; set; } = Enumerable.Repeat(string.Empty, 4).ToArray();
	}
}
