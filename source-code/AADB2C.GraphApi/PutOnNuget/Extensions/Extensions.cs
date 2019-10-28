using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace AADB2C.GraphApi.PutOnNuget.Extensions
{
    [Flags]
    public enum MinifyOptions { None = 1, RemoveNulls = 2, RemoveEmpty = 4, RemovePrivate = 8 }

    public static class Extensions
    {
        #region string

        // Split string into strings of a max size
        public static IEnumerable<string> Split(this string str, int size)
        {
            var skip = 0;
            var res = new List<string>();
            var cstr = str.Select(c => c.ToString()).ToArray();

            while (skip < str.Length)
            {
                res.Add(cstr.Skip(skip).Take(size).Join(""));
                skip += size;
            }

            return res;
        }

        public static string Join(this IEnumerable<string> coll, string separator) => string.Join(separator, coll.ToArray());

        public static bool IsNullOrWhitespace(this string str) => string.IsNullOrEmpty(str?.Trim());

        public static string IfNullOrWhitespace(this string source, string ifEmpty) => source.IsNullOrWhitespace() ? ifEmpty : source;

        public static bool Contains(this string source, string value, StringComparison comparisonType) => source.IndexOf(value, comparisonType) > -1;

        public static DateTime? ParseDate(this string str) => DateTime.TryParse(str, out var i) ? i : (DateTime?) null;

        public static DateTime? ParseDate(this string str, string format, IFormatProvider provider = null, DateTimeStyles style = DateTimeStyles.AssumeLocal) =>
            DateTime.TryParseExact(str, format, provider ?? CultureInfo.CurrentCulture, style, out var i) ? i : (DateTime?) null;

        public static long? ParseLong(this string str) => long.TryParse(str, out var i) ? i : (long?) null;

        public static int? ParseInt(this string str) => int.TryParse(str, out var i) ? i : (int?) null;

        public static Guid? ParseGuid(this string str) => Guid.TryParse(str, out var i) ? i : (Guid?) null;

        public static uint? ParseUInt(this string str) => uint.TryParse(str, out var i) ? i : (uint?) null;

        public static bool? ParseBool(this string str)
        {
            if (str.IsNullOrWhitespace()) return null;

            if (!bool.TryParse(str, out var i))
            {
                str = str.ToLower();

                if (str == "1" || str == "true" || str == "t" || str == "y" || str == "yes")
                {
                    return true;
                }

                if (str == "0" || str == "false" || str == "f" || str == "n" || str == "no")
                {
                    return false;
                }

                return null;
            }

            return i;
        }

        public static Uri ParseWebUrl(this string str, UriKind kind = UriKind.Absolute)
        {
            if (str.IsNullOrWhitespace())
            {
                return null;
            }

            str = str.Trim();

            if (kind == UriKind.Absolute && !str.StartsWith("http"))
            {
                str = "http://" + str;
            }

            Uri.TryCreate(str, UriKind.Absolute, out var u);

            return u;
        }

        public static object ParseBestGuess(this string rawValue)
        {
            if (rawValue == null) return null;
            if (rawValue == "null" || rawValue == "undefined") return null;
            if (rawValue.StartsWith('"') && rawValue.EndsWith('"')) return rawValue.Trim('"');
            if (int.TryParse(rawValue, out var i)) return i;
            if (double.TryParse(rawValue, out var d)) return d;
            if (long.TryParse(rawValue, out var l)) return l;
            if (bool.TryParse(rawValue, out var b)) return b;
            if (DateTime.TryParse(rawValue, out var dt)) return dt;
            if (Guid.TryParse(rawValue, out var g)) return g;

            return rawValue;
        }

        public static bool IsMatch(this string str, Regex rx)
        {
            if (str.IsNullOrWhitespace())
            {
                return false;
            }

            var m = rx.Match(str);

            return m.Success;
        }

        public static string Match(this string str, Regex rx)
        {
            var m = rx.Match(str);

            return m.Success ? m.Value : null;
        }

        public static string Match(this string str, Regex rx, string groupName)
        {
            var m = rx.Match(str);

            return m.Success ? m.Groups[groupName]?.Value : null;
        }

        public static IEnumerable<string> Matches(this string str, Regex rx) => rx.Matches(str).Cast<Match>().Select(m => m.Value);

        #endregion

        #region Exceptions

        public static string UnwrapForLog(this Exception ex, bool outputStack = true)
        {
            var result = new StringBuilder();
            var stack = ex.StackTrace;
            result.AppendLine(ex.Message);

            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                result.AppendLine("\t" + ex.Message);
                stack = ex.StackTrace;
            }

            if (outputStack && stack != null)
            {
                result.AppendLine(stack.Replace("   ", "\t"));
            }

            return result.ToString();
        }

        public static string UnwrapForLog(this AggregateException aggEx, bool outputStack = true)
        {
            var result = new StringBuilder();

            for (var i = 0; i < aggEx.InnerExceptions.Count; i++)
            {
                var ex = aggEx.InnerExceptions[i];
                var stack = ex.StackTrace;

                result.AppendLine($"[{i}] : {ex.Message}");

                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    result.AppendLine("\t" + ex.Message);
                    stack = ex.StackTrace;
                }

                if (outputStack)
                {
                    result.AppendLine(stack.Replace("   ", "\t"));
                }
            }

            return result.ToString();
        }

        #endregion

        #region Collections

        public static bool GetNext<T>(this IEnumerator<T> enumerator, out T current)
        {
            if (enumerator.MoveNext())
            {
                current = enumerator.Current;
                return true;
            }

            current = default;

            return false;
        }

        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            if (source == null) yield break;

            var i = 0;

            foreach (var s in source)
            {
                yield return selector(s, i);

                i++;
            }
        }

#if NETSTANDARD2_0
        // These will be included in NetStandard2.1
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key)
        {
            return dictionary.GetValueOrDefault<TKey, TValue>(key, default(TValue));
        }

        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            TValue obj;
            if (!dictionary.TryGetValue(key, out obj))
                return defaultValue;

            return obj;
        }
#endif

        public static bool Remove<TKey, TValue>(this Dictionary<TKey, TValue> source, Func<KeyValuePair<TKey, TValue>, bool> selector)
        {
            var removals = source.Where(selector).ToArray();

            if (removals.Any())
            {
                removals.ForEach(r=>source.Remove(r.Key));
                
                return true;
            }

            return false;
        }

        public static Dictionary<TKey, TValue> Merge<TKey, TValue>(this Dictionary<TKey, TValue> source, Dictionary<TKey, TValue> merge,
            Func<(TKey key, TValue source, TValue merge), TValue> valueSelector)
        {
            if (valueSelector == null) throw new ArgumentException($"{nameof(valueSelector)} cannot be null");

            if (source?.Any() != true) return merge ?? new Dictionary<TKey, TValue>();

            if (merge?.Any() != true) return source;

            var keys = source.Keys.Union(merge.Keys).Distinct();
            var result = new Dictionary<TKey, TValue>();

            foreach (var key in keys)
            {
                TValue val;
                var inSrc = source.ContainsKey(key);
                var inMrg = merge.ContainsKey(key);

                if (inSrc && !inMrg) val = source[key];
                else if (!inSrc && inMrg) val = merge[key];
                else val = valueSelector((key, source[key], merge[key]));

                result[key] = val;
            }

            return result;
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>?> source)
            => source.Where(s => s.HasValue).ToDictionary(s => s.Value.Key, s => s.Value.Value);

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
            => source.ToDictionary(s => s.Key, s => s.Value);

        public static Dictionary<string, string[]> ToDictionary(this NameValueCollection nvc) => nvc.Cast<string>().ToDictionary(k => k, nvc.GetValues);

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey? key) where TKey : struct =>
            key != null && d.ContainsKey(key.Value) ? d[key.Value] : default;

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey? key, TValue defaultValue) where TKey : struct =>
            key != null && d.ContainsKey(key.Value) ? d[key.Value] : defaultValue;

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, int, TKey> keySelector,
            Func<TSource, int, TElement> elementSelector
        ) => source.Select((s, i) => new {s, i}).ToDictionary(p => keySelector(p.s, p.i), p => elementSelector(p.s, p.i));

        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> body)
        {
            if (source == null)
            {
                var i = 0;

                foreach (var c in source)
                {
                    body.Invoke(c, i);
                    i++;
                }
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> body)
        {
            if (source != null)
            {
                foreach (var i in source)
                {
                    body.Invoke(i);
                }
            }
        }

        public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> body)
        {
            if (source != null)
            {
                foreach (var i in source)
                {
                    await body.Invoke(i);
                }
            }
        }

        //https://blogs.msdn.microsoft.com/pfxteam/2012/03/05/implementing-a-simple-foreachasync-part-2/
        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }

        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, CancellationToken token, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }, token));
        }

        public static Task<TAccumulate> AggregateAsync<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, Task<TAccumulate>> func) =>
            source.Aggregate(Task.FromResult(seed), async (a, s) => await func(a.Result, s));

        public static Stopwatch Restart(this Stopwatch sw, Action first)
        {
            first?.Invoke();
            sw.Restart();
            return sw;
        }

        public static Task WhenAll(this IEnumerable<Task> tasks) => Task.WhenAll(tasks);

        public static Task WhenAny(this IEnumerable<Task> tasks) => Task.WhenAny(tasks);

        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> col, Func<T, TKey> keySelector, bool takeLast = false)
        {
            var grp = col.GroupBy(keySelector);

            if (takeLast)
            {
                return grp.Select(g => g.Last());
            }

            return grp.Select(g => g.First());
        }

        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<T> initialSet, params IEnumerable<T>[] sets)
        {
            var finalSet = initialSet.Select(x => new[] {x});

            foreach (var set in sets)
            {
                var cp = finalSet.SelectMany(fs => set, (fs, s) => new {fs, s});

                finalSet = cp.Select(x =>
                {
                    var a = new T[x.fs.Length + 1];

                    x.fs.CopyTo(a, 0);
                    a[x.fs.Length] = x.s;

                    return a;
                });
            }

            return finalSet;
        }

        public static IEnumerable<T> Union<T>(this IEnumerable<T> source, T item) => source.Union(new T[] {item});

        public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(this IEnumerable<TSource> source, int size) => Batch(source, size, x => x);

        // Split a collection into batches of a max size
        //https://github.com/morelinq/MoreLINQ/blob/master/MoreLinq/Batch.cs
        public static IEnumerable<TResult> Batch<TSource, TResult>(this IEnumerable<TSource> source, int size,
            Func<IEnumerable<TSource>, TResult> resultSelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if (resultSelector == null)
            {
                throw new ArgumentNullException(nameof(resultSelector));
            }

            return _();

            IEnumerable<TResult> _()
            {
                TSource[] bucket = null;
                var count = 0;

                foreach (var item in source)
                {
                    if (bucket == null)
                    {
                        bucket = new TSource[size];
                    }

                    bucket[count++] = item;

                    // The bucket is fully buffered before it's yielded
                    if (count != size)
                    {
                        continue;
                    }

                    yield return resultSelector(bucket);

                    bucket = null;
                    count = 0;
                }

                // Return the last bucket with all remaining elements
                if (bucket != null && count > 0)
                {
                    Array.Resize(ref bucket, count);
                    yield return resultSelector(bucket);
                }
            }
        }

        public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dic, IEnumerable<(TKey key, TValue value)> collection)
        {
            foreach (var i in collection)
            {
                dic.Add(i.key, i.value);
            }
        }

        #endregion

        #region IO

        public static DirectoryInfo Subdirectory(this DirectoryInfo di, params string[] parts)
        {
            //
            // Union does not garantee preserved order
            //
            var arr = new string[parts.Length + 1];

            arr[0] = di.FullName;

            for (var i = 0; i < parts.Length; i++)
            {
                arr[i + 1] = parts[i];
            }

            return new DirectoryInfo(Path.Combine(arr));
        }

        public static FileInfo File(this DirectoryInfo di, params string[] parts)
        {
            //
            // Union does not garantee preserved order
            //
            var arr = new string[parts.Length + 1];

            arr[0] = di.FullName;

            for (var i = 0; i < parts.Length; i++)
            {
                arr[i + 1] = parts[i];
            }

            return new FileInfo(Path.Combine(arr));
        }

        public static void Rename(this DirectoryInfo di, string newName)
        {
            if (di.Parent == null)
            {
                return;
            }

            di.MoveTo(Path.Combine(di.Parent.FullName, newName));
        }

        public static void Rename(this FileInfo fi, string newName)
        {
            if (fi.Directory == null)
            {
                return;
            }

            fi.MoveTo(Path.Combine(fi.Directory.FullName, newName));
        }

        public static async Task<string> ReadAllTextAsync(this FileInfo fi)
        {
            using (var fr = fi.OpenText())
            {
                return await fr.ReadToEndAsync();
            }
        }

        #endregion

        #region JSON

        public static bool IsEmpty<TJ>(this TJ json) where TJ : JToken
        {
            if (json == null) return true;

            if (json is JArray ja) return !ja.Any();

            if (json is JObject jo2) return !jo2.Properties().Any();

            return json.ToString().IsNullOrWhitespace();
        }

        public static TJ Minify<TJ>(this TJ json, MinifyOptions options) where TJ : JToken
        {
            if (json == null || options.HasFlag(MinifyOptions.None)) return json;

            var removeEmpty = options.HasFlag(MinifyOptions.RemoveEmpty);
            var removeNulls = removeEmpty || options.HasFlag(MinifyOptions.RemoveNulls);
            var removePrivate = options.HasFlag(MinifyOptions.RemovePrivate);

            if (json is JArray ja)
            {
                var minified = new JArray();

                foreach (var j in ja)
                {
                    var jmin = j.Minify(options);
                    var keep = !(removeNulls && jmin == null) && !(removeEmpty && j.IsEmpty());

                    if (keep) minified.Add(j.Minify(options));
                }

                return minified as TJ;
            }

            if (json is JObject jo)
            {
                var minified = new JObject();

                foreach (var p in jo.Properties())
                {
                    var pmin = p.Value.Minify(options);
                    var keep = !(removeNulls && pmin == null) && !(removeEmpty && pmin.IsEmpty()) && !(removePrivate && p.Name.StartsWith("_"));

                    if (keep) minified[p.Name] = pmin;
                }

                return minified as TJ;
            }

            return json.DeepClone() as TJ;
        }

        public static string ToString<TJ>(this TJ json, Newtonsoft.Json.Formatting formatting, MinifyOptions options) where TJ : JToken
            => json.Minify(options).ToString(formatting);

        #endregion

        #region Web

        public static Uri Combine(this Uri baseUri, string path)
        {
            if (baseUri.Scheme.StartsWith("http"))
            {
                var pathUri = new Uri(baseUri, path);
                var q = HttpUtility.ParseQueryString(baseUri.Query);

                q.Add(HttpUtility.ParseQueryString(pathUri.Query));
                var query = q.HasKeys()
                    ? $"?{q.ToDictionary().Select(p => $"{p.Key}={p.Value.LastOrDefault()}").ToArray().Join("&")}"
                    : "";

                return new UriBuilder(baseUri.Scheme, baseUri.Host, baseUri.Port, baseUri.LocalPath + pathUri.LocalPath, query).Uri;
            }

            throw new NotImplementedException();
        }

        public static string UrlEncodedId(this TimeZoneInfo tz) => HttpUtility.UrlEncode(tz.Id);

        #endregion
    }
}