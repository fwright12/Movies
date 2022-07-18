using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Linq
{
    public static class AsyncEnumerable
    {
        public static async IAsyncEnumerable<TSource> Concat<TSource>(this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second)
        {
            await foreach (var item in first)
            {
                yield return item;
            }

            await foreach (var item in second)
            {
                yield return item;
            }
        }

        public static async Task<List<T>> ReadAll<T>(this IAsyncEnumerable<T> items)
        {
            var result = new List<T>();

            await foreach (var item in items)
            {
                result.Add(item);
            }

            return result;
        }

        public static async IAsyncEnumerable<TResult> OfType<TSource, TResult>(this IAsyncEnumerable<TSource> source)
        {
            await foreach (var item in source)
            {
                if (item is TResult result)
                {
                    yield return result;
                }
            }
        }

        public delegate bool TryParseFunc<in TSource, TParsed>(TSource source, out TParsed parsed);

        public static IEnumerable<TResult> TrySelect<TSource, TResult>(this IEnumerable<TSource> source, TryParseFunc<TSource, TResult> selector)
        {
            foreach (var item in source)
            {
                if (selector(item, out var selected))
                {
                    yield return selected;
                }
            }
        }

        public static async IAsyncEnumerable<TResult> TrySelect<TSource, TResult>(this IAsyncEnumerable<TSource> source, TryParseFunc<TSource, TResult> selector)
        {
            await foreach (var item in source)
            {
                if (selector(item, out var selected))
                {
                    yield return selected;
                }
            }
        }

        public static async IAsyncEnumerable<TResult> SelectAsync<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            await foreach (var item in source)
            {
                yield return selector(item);
            }
        }

        public static async IAsyncEnumerable<TResult> SelectAsync<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, Task<TResult>> selector)
        {
            await foreach (var item in source)
            {
                yield return await selector(item);
            }
        }

        public static async IAsyncEnumerable<TSource> WhereAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            await foreach (var item in source)
            {
                if (predicate(item))
                {
                    yield return item;
                }
            }
        }

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(Task<IEnumerable<T>> items)
        {
            foreach (var item in await items)
            {
                yield return item;
            }
        }
    }
}