using CoreGraphics;
using Foundation;
using System;
using System.Threading.Tasks;
using WebKit;

namespace Movies.iOS
{
    public class JavaScriptEvaluatorFactory : IJavaScriptEvaluatorFactory
    {
        public IJavaScriptEvaluator Create(string url = null) => new JavaScriptEvaluator(url);

        private class JavaScriptEvaluator : IJavaScriptEvaluator
        {
            private static readonly NSObject JAVASCRIPT_EXCEPTION_KEY = new NSString("WKJavaScriptExceptionMessage");
            private static readonly int TIMEOUT = 5000;

            private WKWebView WebView;
            private Task LoadUrlTask;

            public JavaScriptEvaluator(string url = null)
            {
                var config = new WKWebViewConfiguration();
                WebView = new WKWebView(CGRect.Empty, config);

                LoadUrlTask = url == null ? Task.CompletedTask : LoadUrl(url);
            }

            private Task LoadUrl(string url)
            {
                //UIApplication.SharedApplication.Windows.FirstOrDefault()?.AddSubview(webView);
                var source = new TaskCompletionSource<bool>();

                var del = new CustomWebViewDelegate();
                del.PageFinished += (sender, e) =>
                {
                    source.TrySetResult(true);
                };
                WebView.NavigationDelegate = del;

#if DEBUG
                WebView.LoadHtmlString(Data.Dummy.RottenTomatoes.HARRY_POTTER_7_PART_2, null);
#else
                WebView.LoadRequest(new NSUrlRequest(new NSUrl(url)));
#endif

                TimeoutSource(source, TIMEOUT);
                return source.Task;
                //webView.RemoveFromSuperview();
            }

            public async Task<string> Evaluate(string javaScript)
            {
                await LoadUrlTask;
                NSObject result;

                try
                {
                    result = await WebView.EvaluateJavaScriptAsync(javaScript);
                }
                catch (NSErrorException e)
                {
                    if (e.UserInfo.TryGetValue(JAVASCRIPT_EXCEPTION_KEY, out var value))
                    {
                        throw new Exception(value.ToString());
                    }
                    else
                    {
                        throw e;
                    }
                }

                return NSJsonSerialization.Serialize(result, NSJsonWritingOptions.FragmentsAllowed | NSJsonWritingOptions.WithoutEscapingSlashes, out _)?.ToString();
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

            private class CustomWebViewDelegate : WKNavigationDelegate
            {
                public event EventHandler PageFinished;

                public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
                {
                    decisionHandler(WKNavigationActionPolicy.Allow);
                }

                public override void DecidePolicy(WKWebView webView, WKNavigationResponse navigationResponse, Action<WKNavigationResponsePolicy> decisionHandler)
                {
                    decisionHandler(WKNavigationResponsePolicy.Allow);
                }

                public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
                {
                    PageFinished?.Invoke(webView, EventArgs.Empty);
                }

                public override void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error)
                {
                    PageFinished?.Invoke(webView, EventArgs.Empty);
                }

                public override void DidFailProvisionalNavigation(WKWebView webView, WKNavigation navigation, NSError error)
                {
                    PageFinished?.Invoke(webView, EventArgs.Empty);
                }

                public override void ContentProcessDidTerminate(WKWebView webView)
                {
                    PageFinished?.Invoke(webView, EventArgs.Empty);
                }
            }
        }
    }
}