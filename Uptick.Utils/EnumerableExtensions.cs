using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uptick.Utils
{
    public static class EnumerableExtensions
    {
        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body, CancellationToken token)
        {
            return Task.WhenAll(from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                    {
                        while (partition.MoveNext() && !token.IsCancellationRequested)
                        {
                            await body(partition.Current);
                        }
                    }
                }, token));
        }

        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return ForEachAsync(source, dop, body, CancellationToken.None);
        }

        public static IEnumerable<IEnumerable<T>> ChunkByLength<T>(this IEnumerable<T> source, int chunkSize)
        {
            var subResult = new List<T>();
            var count = 0;
            foreach (var item in source)
            {
                ++count;
                if (count > chunkSize)
                {
                    yield return subResult;
                    subResult = new List<T>();
                    count = 1;
                }

                subResult.Add(item);
            }

            if (subResult.Any())
            {
                yield return subResult;
            }
        }
        
        public static IEnumerable<IEnumerable<TItem>> ChunkBySize<TItem>(this IEnumerable<TItem> source, Func<TItem, int> sizeGetter, int maxChunkSumSize)
        {
            var chunkPart = new List<TItem>();
            var chunkSize = 0;
            foreach (var item in source)
            {
                chunkSize  += sizeGetter(item);
                if (chunkSize <= maxChunkSumSize || !chunkPart.Any())
                {
                    chunkPart.Add(item);
                }
                else
                {
                    yield return chunkPart;
                    chunkSize = sizeGetter(item);
                    chunkPart = new List<TItem> {item};
                }
            }

            if (chunkPart.Any())
            {
                yield return chunkPart;
            }
        }
    }
}