using System.Text.RegularExpressions;

namespace Pukpukpuk.DataFeed.Utils
{
    public static class StringUtils
    {
        public static string RemoveTags(string s)
        {
            return Regex.Replace(s, "<.*?>", string.Empty);
        }
        
        public static string AddSpaces(string camelizedName)
        {
            var result = "";

            foreach (var ch in camelizedName)
            {
                if (char.IsUpper(ch) || char.IsDigit(ch)) result += " ";
                result += ch;
            }

            return result.Trim();
        }
    }
}