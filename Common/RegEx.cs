using System.Text.RegularExpressions;

namespace Common
{
    public static partial class RegEx
    {
        [GeneratedRegex(@"^[a-zA-Z\s-]+$")]
        public static partial Regex NameRegex();
    }
}
