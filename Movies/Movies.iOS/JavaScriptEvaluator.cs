using CoreGraphics;
using Foundation;
using System;
using System.Threading.Tasks;
using WebKit;

namespace Movies.iOS
{
    public class JavaScriptEvaluator : IJavaScriptEvaluator
    {
        public async Task<string> Evaluate(string javaScript, string url = null)
        {
            try
            {
                var config = new WKWebViewConfiguration();
                var webView = new WKWebView(CGRect.Empty, config);

                if (url == null)
                {
                    //webView.LoadHtmlString(new NSString(""), null);
                    //webView.LoadData(Base64.EncodeToString(new Java.Lang.String("").GetBytes(), Base64Flags.NoPadding), "text/html", "base64");
                }
                else
                {
                    var source = new TaskCompletionSource<bool>();

                    var del = new CustomWebViewDelegate();
                    del.PageFinished += (sender, e) =>
                    {
                        source.TrySetResult(true);
                    };
                    webView.NavigationDelegate = del;

#if DEBUG
                    webView.LoadHtmlString(new NSString(Data.Dummy.RottenTomatoes.HARRY_POTTER_7_PART_2), null);
#else
                    webView.LoadRequest(new NSUrlRequest(new NSUrl(url)));
#endif

                    await source.Task;
                    del.Dispose();
                }

                System.Diagnostics.Debug.WriteLine("executing javascript:\n" + javaScript);
                var result = await webView.EvaluateJavaScriptAsync(new NSString(javaScript));
                string value = null;

                try
                {
                    var data = NSJsonSerialization.Serialize(result, NSJsonWritingOptions.FragmentsAllowed | NSJsonWritingOptions.WithoutEscapingSlashes, out var error);
                    value = data?.ToString();
                }
                catch (Exception e1)
                {
                    System.Diagnostics.Debug.WriteLine(e1);
                }
                System.Diagnostics.Debug.WriteLine("result: " + result);
                System.Diagnostics.Debug.WriteLine(result.Class + ", " + result.Superclass + ", " + result.GetType());

                //webView.Dispose();

                System.Diagnostics.Debug.WriteLine("result json: " + value);
                return value;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return null;
            }
        }

        public class CustomWebViewDelegate : WKNavigationDelegate
        {
            public event EventHandler PageFinished;

            public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
            {
                base.DidFinishNavigation(webView, navigation);
                System.Diagnostics.Debug.WriteLine("finished loading");
                PageFinished?.Invoke(this, new EventArgs());
            }
        }
    }
}