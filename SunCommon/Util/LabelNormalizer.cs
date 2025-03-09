using System.Text.RegularExpressions;

namespace SunCommon.Util
{
    public partial class LabelNormalizer
    {
        public static string Apply(string str) => Q().Replace(str, string.Empty);

        [GeneratedRegex(@"[^\w_]")]
        private static partial Regex Q();
    }
}
