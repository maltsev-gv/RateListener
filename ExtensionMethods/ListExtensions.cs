using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RateListener.ExtensionMethods;

public static class ListExtensions
{
    /// <summary>
    /// ForEach для любого IEnumerable
    /// </summary>
    extension(IEnumerable source)
    {
        public void ForEach(Action<object> action)
        {
            if (source != null)
            {
                foreach (var item in source)
                    action(item);
            }
        }
    }

    extension<T>(IEnumerable<T> source)
    {
        public void ForEach(Action<T> action)
        {
            if (source != null)
            {
                foreach (var item in source)
                    action(item);
            }
        }
    }

    extension<T>(ObservableCollection<T> sourceCollection)
    {
        public void RemoveAll(Predicate<T> match)
        {
            List<T> list = [];
            foreach (T item in sourceCollection)
            {
                if (match(item))
                {
                    list.Add(item);
                }
            }
            list.ForEach(id => sourceCollection.Remove(id));
        }
    }

    extension<T>(List<T> sourceList)
    {
        public List<T> ToNullIfEmpty()
        {
            if (sourceList == null || !sourceList.Any())
            {
                return null;
            }
            return sourceList;
        }
    }

    extension(ICollection sourceCollection)
    {
        public List<T> ToList<T>() =>
            [.. sourceCollection.OfType<T>()];

        public T[] ToArray<T>() =>
            sourceCollection?.OfType<T>().ToArray();
    }
}
