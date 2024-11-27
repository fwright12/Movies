using System;
using System.Threading.Tasks;

namespace Movies
{
    public interface IJavaScriptEvaluatorFactory
    {
        public IJavaScriptEvaluator Create(string url = null);
    }

    public interface IJavaScriptEvaluator : IDisposable
    {
        Task<string> Evaluate(string javaScript);
    }
}