using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniSharp.Roslyn.Utilities
{
    internal static class ListExtensions
    {
        public static List<T> Replace<T>(this List<T> list, int index, int count, IEnumerable<T> replace_with)
        {
            list.RemoveRange(index, count);
            list.InsertRange(index, replace_with);

            return list;
        }
    }
}
