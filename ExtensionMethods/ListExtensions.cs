using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RateListener.ExtensionMethods;

namespace RateListener.ExtensionMethods
{
    public static class ListExtensions
    {
        /// <summary>
        /// ForEach для любого IEnumerable
        /// </summary>
        public static void ForEach(this IEnumerable source, Action<object> action)
        {
            if (source != null)
            {
                foreach (var item in source)
                    action(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source != null)
            {
                foreach (var item in source)
                    action(item);
            }
        }

        public static void RemoveAll<T>(this ObservableCollection<T> source, Predicate<T> match)
        {
            List<T> list = new List<T>();
            foreach (T item in source)
            {
                if (match(item))
                {
                    list.Add(item);
                }
            }
            list.ForEach(id => source.Remove(id));
        }

        public static List<T> ToNullIfEmpty<T>(this List<T> source)
        {
            if (source == null || !source.Any())
            {
                return null;
            }
            return source;
        }

        public static List<T> ToList<T>(this ICollection source)
        {
            return new List<T>(source.OfType<T>());
        }

        public static T[] ToArray<T>(this ICollection source)
        {
            return source?.OfType<T>().ToArray();
        }
    }
}
