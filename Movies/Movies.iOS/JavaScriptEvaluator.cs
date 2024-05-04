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
    }

    public class JavaScriptEvaluator : IJavaScriptEvaluator
    {
        private WKWebView WebView;

        private Task LoadUrlTask;

        public JavaScriptEvaluator(string url = null)
        {
            var config = new WKWebViewConfiguration();
            WebView = new WKWebView(CGRect.Empty, config);

            LoadUrlTask = url == null ? Task.CompletedTask : LoadUrl(url);
        }

        private async Task LoadUrl(string url)
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
                webView.LoadRequest(new NSUrlRequest(new NSUrl(url)));
#endif

            await source.Task;
            //webView.RemoveFromSuperview();
        }

        public async Task<string> Evaluate(string javaScript)
        {
            await LoadUrlTask;

            try
            {
                var result = await WebView.EvaluateJavaScriptAsync(javaScript);
                return NSJsonSerialization.Serialize(result, NSJsonWritingOptions.FragmentsAllowed | NSJsonWritingOptions.WithoutEscapingSlashes, out _)?.ToString();
            }
            catch (NSErrorException e)
            {
                throw new Exception(e.UserInfo["WKJavaScriptExceptionMessage"].ToString());
            }
        }

        public void Dispose()
        {
            WebView.Dispose();
        }

        public class CustomWebViewDelegate : WKNavigationDelegate
        {
            public event EventHandler PageFinished;

            public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
            {
                PageFinished?.Invoke(webView, new EventArgs());
            }

            public override void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error)
            {
                PageFinished?.Invoke(webView, new EventArgs());
            }
        }
    }
}