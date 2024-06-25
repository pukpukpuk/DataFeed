using System.Text.RegularExpressions;

namespace Pukpukpuk.DataFeed.Utils
{
    public static class StringUtils
    {
        public static string RemoveTags(string s)
        {
            return Regex.Replace(s, "<.*?>", string.Empty);
        }
    }
}