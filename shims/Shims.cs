using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace System
{
    public static class Shims
    {
        public static bool isEmpty<T>(this ICollection<T> collection) => collection.Count == 0;

        public static void addAll<T>(this ICollection<T> collection, IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                collection.Add(item);
            }
        }
    }
}