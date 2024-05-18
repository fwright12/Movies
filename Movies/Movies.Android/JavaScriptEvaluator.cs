using Android.Content;
using Android.Net.Http;
using Android.Util;
using Android.Webkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Movies.Droid
{
    public class JavaScriptEvaluatorFactory : IJavaScriptEvaluatorFactory
    {
        public Context Context;

        public JavaScriptEvaluatorFactory(Context context)
        {
            Context = context;
        }

        public IJavaScriptEvaluator Create(string url = null) => new JavaScriptEvaluator(Context, url);

        private class JavaScriptEvaluator : IJavaScriptEvaluator
        {
            public Context Context;

            private static readonly int TIMEOUT = 5000;

            private WebView WebView;
            private MyWebChromeClient Client;
            private Task LoadUrlTask;

            public JavaScriptEvaluator(Context context, string url = null)
            {
                Context = context;

                WebView = new WebView(Context);
                WebView.SetWebChromeClient(Client = new MyWebChromeClient());
                WebView.Settings.JavaScriptEnabled = true;

                LoadUrlTask = url == null ? Task.CompletedTask : LoadUrl(url);
            }

            private Task LoadUrl(string url)
            {
                var client = new MyWebViewClient();
                WebView.SetWebViewClient(client);

                var source = new TaskCompletionSource<bool>();
                client.PageFinished += (sender, e) =>
                {
                    source.TrySetResult(true);
                };

#if DEBUG
                WebView.LoadData(Base64.EncodeToString(new Java.Lang.String(Data.Dummy.RottenTomatoes.HARRY_POTTER_7_PART_2).GetBytes(), Base64Flags.NoPadding), "text/html", "base64");
#else
                WebView.LoadUrl(url);
#endif

                TimeoutSource(source, TIMEOUT);
                return source.Task;
            }

            public async Task<string> Evaluate(string javaScript)
            {
                await LoadUrlTask;
                
                Client.ClearConsole();

                var callback = new Callback();
                WebView.EvaluateJavascript(javaScript, callback);
                var result = await callback.Result;

                var errors = Client.Messages.Where(message => message.InvokeMessageLevel().Name() == "ERROR").ToArray();
                if (errors.Length > 0)
                {
                    throw new Exception(string.Join("\n", errors.Select(error => error.Message())));
                }

                return result;
            }

            public void Dispose()
            {
                WebView.Dispose();
            }

            private static async void TimeoutSource<T>(TaskCompletionSource<T> source, int timeout)
            {
                await Task.Delay(timeout);
                source.TrySetException(new TimeoutException());
            }

            private class MyWebChromeClient : WebChromeClient
            {
                public IReadOnlyList<ConsoleMessage> Messages => _Messages;

                private List<ConsoleMessage> _Messages = new List<ConsoleMessage>();

                public override bool OnConsoleMessage(ConsoleMessage consoleMessage)
                {
                    _Messages.Add(consoleMessage);
                    return base.OnConsoleMessage(consoleMessage);
                }

                public void ClearConsole()
                {
                    _Messages.Clear();
                }
            }

            private class Callback : Java.Lang.Object, IValueCallback
            {
                public Task<string> Result { get; }
                private TaskCompletionSource<string> Source;

                public Callback()
                {
                    Source = new TaskCompletionSource<string>();
                    Result = Source.Task;
                }

                public void OnReceiveValue(Java.Lang.Object value) => Source.SetResult(value.ToString());
            }

            private class MyWebViewClient : WebViewClient
            {
                public event EventHandler PageFinished;

                public override void OnPageFinished(WebView view, string url)
                {
                    base.OnPageFinished(view, url);
                    PageFinished?.Invoke(this, EventArgs.Empty);
                }

                public override void OnReceivedError(WebView view, IWebResourceRequest request, WebResourceError error)
                {
                    base.OnReceivedError(view, request, error);
                    PageFinished?.Invoke(this, EventArgs.Empty);
                }

                public override void OnReceivedHttpError(WebView view, IWebResourceRequest request, WebResourceResponse errorResponse)
                {
                    base.OnReceivedHttpError(view, request, errorResponse);
                    PageFinished?.Invoke(this, EventArgs.Empty);
                }

                public override void OnReceivedSslError(WebView view, SslErrorHandler handler, SslError error)
                {
                    base.OnReceivedSslError(view, handler, error);
                    PageFinished?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}