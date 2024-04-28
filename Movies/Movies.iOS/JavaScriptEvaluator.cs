using CoreGraphics;
using Foundation;
using System;
using System.Linq;
using System.Threading.Tasks;
using UIKit;
using WebKit;

namespace Movies.iOS
{
    public class JavaScriptEvaluatorFactory : IJavaScriptEvaluatorFactory
    {
        public IJavaScriptEvaluator Create(string url = null)
        {
            throw new NotImplementedException();
        }
    }

    public class JavaScriptEvaluator : IJavaScriptEvaluator
    {
        public async Task<string> Evaluate(string javaScript, string url = null)
        {
            var config = new WKWebViewConfiguration();
            var webView = new WKWebView(CGRect.Empty, config);

            if (url != null)
            {
                //UIApplication.SharedApplication.Windows.FirstOrDefault()?.AddSubview(webView);
                var source = new TaskCompletionSource<bool>();

                var del = new CustomWebViewDelegate();
                del.PageFinished += (sender, e) =>
                {
                    source.TrySetResult(true);
                };
                webView.NavigationDelegate = del;

#if DEBUG
                webView.LoadHtmlString(Data.Dummy.RottenTomatoes.HARRY_POTTER_7_PART_2, null);
#else
                webView.LoadRequest(new NSUrlRequest(new NSUrl(url)));
#endif

                await source.Task;
                //webView.RemoveFromSuperview();
            }

            var result = await webView.EvaluateJavaScriptAsync(javaScript);
            var data = NSJsonSerialization.Serialize(result, NSJsonWritingOptions.FragmentsAllowed | NSJsonWritingOptions.WithoutEscapingSlashes, out var error);

            webView.Dispose();

            return data?.ToString();
        }
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