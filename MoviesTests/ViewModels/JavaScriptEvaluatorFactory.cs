using System.Collections;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MoviesTests.ViewModels
{
    public class JavaScriptEvaluatorFactory : IJavaScriptEvaluatorFactory, IEnumerable<KeyValuePair<string, Dictionary<string, object>>>
    {
        public static readonly string NULL_KEY = new object().ToString()!;
        public static int Delay { get; set; }
        public ICollection<JavaScriptEvaluator> Instances { get; private set; } = new List<JavaScriptEvaluator>();

        public static Dictionary<string, Dictionary<string, object>> Results { get; } = new Dictionary<string, Dictionary<string, object>>();

        public IJavaScriptEvaluator Create(string url = null!)
        {
            var result = new JavaScriptEvaluator(url);
            Instances.Add(result);
            return result;
        }

        public void Add(string javaScript, object result) => Add(null!, javaScript, result);
        public void Add(string url, string javaScript, object result)
        {
            url ??= NULL_KEY;

            if (!Results.TryGetValue(url, out var dict))
            {
                Results.Add(url, dict = new Dictionary<string, object>());
            }

            dict[javaScript] = result is string str ? JsonSerializer.Serialize(str) : result;
        }

        public IEnumerator<KeyValuePair<string, Dictionary<string, object>>> GetEnumerator() => Results.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public class JavaScriptEvaluator : IJavaScriptEvaluator
        {
            public static readonly Random Random = new Random(3952);
            public ICollection<string> Queries { get; private set; } = new List<string>();

            public string URL { get; }

            public JavaScriptEvaluator(string url)
            {
                URL = url;
            }

            public async Task<string> Evaluate(string javaScript)
            {
                if (IsDisposed)
                {
                    throw new ObjectDisposedException(nameof(JavaScriptEvaluator));
                }

                Queries.Add(javaScript);
                await Task.Delay(Random.Next(100, 200));

                if (Results.TryGetValue(URL ?? NULL_KEY, out var dict))
                {
                    object result;

                    if (!dict.TryGetValue(javaScript, out result!))
                    {
                        foreach (var kvp in dict)
                        {
                            var match = Regex.Match(javaScript, kvp.Key);
                            if (match.Index == 0 && match.Length == javaScript.Length)
                            {
                                result = kvp.Value;
                                break;
                            }
                        }
                    }

                    if (result is Exception e)
                    {
                        throw e;
                    }
                    else if (result != null)
                    {
                        return result.ToString()!;
                    }
                }

                var str = javaScript.Split(";").Where(statement => !string.IsNullOrEmpty(statement)).Last();
                return JsonSerializer.Serialize(JsonDocument.Parse(str));
            }

            private bool IsDisposed = false;
            
            public void Dispose()
            {
                if (IsDisposed)
                {
                    throw new ObjectDisposedException(nameof(JavaScriptEvaluator));
                }

                IsDisposed = true;
            }
        }
    }
}
