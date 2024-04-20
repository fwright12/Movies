using Android.Content;
using Android.Util;
using Android.Webkit;
using System;
using System.Threading.Tasks;

namespace Movies.Droid
{
    public class JavaScriptEvaluator : IJavaScriptEvaluator
    {
        public Context Context;

        public JavaScriptEvaluator(Context context)
        {
            Context = context;
        }

        public async Task<string> Evaluate(string javaScript, string url = null)
        {
            var client = new MyWebViewClient();

            var webView = new WebView(Context);
            webView.SetWebViewClient(client);
            webView.SetWebChromeClient(new MyWebChromeClient());
            webView.Settings.JavaScriptEnabled = true;

            if (url != null)
            {
                var source = new TaskCompletionSource<bool>();
                client.PageFinished += (sender, e) =>
                {
                    source.TrySetResult(true);
                };

#if DEBUG
                webView.LoadData(Base64.EncodeToString(new Java.Lang.String(Data.Dummy.RottenTomatoes.HARRY_POTTER_7_PART_2).GetBytes(), Base64Flags.NoPadding), "text/html", "base64");
#else
                webView.LoadUrl(url);
#endif

                await source.Task;
            }

            var callback = new Callback();
            webView.EvaluateJavascript(javaScript, callback);

            var result = await callback.Result;
            webView.Dispose();
            return result;
        }

        private class MyWebChromeClient : WebChromeClient
        {
            public override void OnConsoleMessage(string message, int lineNumber, string sourceID)
            {
                base.OnConsoleMessage(message, lineNumber, sourceID);
                System.Diagnostics.Debug.WriteLine(message);
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
                PageFinished?.Invoke(this, new EventArgs());
            }
        }
    }
}