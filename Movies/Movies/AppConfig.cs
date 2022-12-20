using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Movies
{
    public partial class App
    {
        public const string OAUTH_SCHEME = "nexus";
        public const string OAUTH_HOST = "gmlmovies.page.link";
        public static readonly Uri BASE_OAUTH_REDIRECT_URI = new Uri((Device.RuntimePlatform == Device.iOS ? "https" : OAUTH_SCHEME) + "://" + OAUTH_HOST + "/oauth/");

        private static readonly TimeSpan DB_CLEANING_SCHEDULE = new TimeSpan(7, 0, 0, 0);
    }
}
