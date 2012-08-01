using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcIndexer.Extensions
{
    public static class ExtensionMethods
    {
        public static IEnumerable<T> Map<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            foreach (var item in source)
                action(item);

            return source;
        }

        /// <summary>
        /// Compares items
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="source">Source IEnumerable</param>
        /// <param name="item">The item that you are unsure is in the list</param>
        /// <param name="comparer">A lambda accepting the item (first) and an item from the list to compare with</param>
        /// <returns></returns>
        public static Boolean Contains<T>(this IEnumerable<T> source, T item, Comparer<T, T> comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            Boolean contains = false;
            foreach (var i in source)
                contains = contains | comparer(item, i);
            return contains;
        }

        public delegate Boolean Comparer<in T1, in T2>(T1 arg1, T2 arg2);
    }
}
