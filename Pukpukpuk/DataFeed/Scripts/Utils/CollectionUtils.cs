using System.Collections.Generic;

namespace Pukpukpuk.DataFeed.Utils
{
    public static class CollectionUtils
    {
        public static IEnumerable<T> Invert<T>(this IList<T> items)
        {
            for (var i = items.Count - 1; i >= 0; i--) yield return items[i];
        }
    }
}