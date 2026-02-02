using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RateListener.ExtensionMethods;

public static class EnumerableExtensions
{
    /// <summary>
    /// Асинхронный ForEach для любого IEnumerable&lt;T&gt;. <c>action</c>-ы выполняются в параллельных тасках
    /// для обеспечения максимальной скорости распределённых запросов.
    /// </summary>
    /// <param name="source">Коллекция, для элементов которой надо выполнить ряд параллельных <c>action</c>-ов.</param>
    /// <param name="action">Асинхронное действие:<code>async item => { await SomeMethod(); ... }</code></param>
    /// <param name="maxTasksCount">Количество Task-ов, выполняемых одновременно. Если <c>maxTasksCount &lt;= 0</c> - число параллельных задач не ограничено.</param>
    public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action, int maxTasksCount = 0)
    {
        var sourceArr = source.ToArray();
        maxTasksCount = maxTasksCount <= 0 ? sourceArr.Length : maxTasksCount;
        var processed = 0;
        do
        {
            foreach (var task in sourceArr
                         .Skip(processed)
                         .Take(maxTasksCount)
                         .Select(action)
                         .ToArray())
                await task;

            processed += maxTasksCount;
        } while (processed < sourceArr.Length);
    }
}
