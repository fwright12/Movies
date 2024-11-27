using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;

namespace Movies.ViewModels
{
    public class LoginEventArgs
    {
        public object Credentials { get; set; }
    }

    public class AccountViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<LoginEventArgs> LoggedIn;
        public event EventHandler LoggedOut;

        public IAccount Account { get; }

        public ICommand LoginCommand { get; }
        public ICommand LogoutCommand { get; }

        public bool IsLoggedIn
        {
            get => _IsLoggedIn;
            private set
            {
                if (value != _IsLoggedIn)
                {
                    _IsLoggedIn = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoggedIn)));
                }
            }
        }

        public Uri RedirectUri => new Uri(App.BASE_OAUTH_REDIRECT_URI, Account.Name.ToLower());

        private AppLinkEntry RedirectLink;
        private bool _IsLoggedIn;

        public AccountViewModel(IAccount account)
        {
            Account = account;

            LoginCommand = new Command(() => _ = Login());
            LogoutCommand = new Command(() => _ = Logout());
        }

        private async Task<bool> WebLogin(string url)
        {
            if (!await App.TryOpenUrlAsync(url))
            {
                return false;
            }

            //await App.OpenUrl("https://www.themoviedb.org/");
            await App.Return();
            return true;
        }

        public async Task Login()
        {
            // TODO Xamarin.Forms.Device.RuntimePlatform is no longer supported. Use Microsoft.Maui.Devices.DeviceInfo.Platform instead. For more details see https://learn.microsoft.com/en-us/dotnet/maui/migration/forms-projects#device-changes
            if (Device.RuntimePlatform == Device.iOS)
            {
                Application.Current.AppLinks.RegisterLink(RedirectLink = new AppLinkEntry
                {
                    AppLinkUri = RedirectUri,
                    Title = "Nexus OAuth Redirect",
                    Description = "Use this as a callback for services that authenticate with OAuth 2.0",
                    IsLinkActive = true,
                    Thumbnail = ImageSource.FromResource("Movies.Logos.TMDbLogo.png")
                });
            }

            await Microsoft.Maui.ApplicationModel.Browser.OpenAsync(await Account.GetOAuthURL(RedirectUri));
        }

        public async Task Login(object credentials)
        {
            if (RedirectLink != null)
            {
                Application.Current.AppLinks.DeregisterLink(RedirectLink);
                RedirectLink = null;
            }
            credentials = await Account.Login(credentials);

#if DEBUG
            Print.Log(credentials);
#endif

            if (credentials != null)
            {
                IsLoggedIn = true;
                LoggedIn?.Invoke(this, new LoginEventArgs { Credentials = credentials });
            }
        }

        public async Task Logout()
        {
            if (!await Account.Logout())
            {
                await Application.Current.MainPage.DisplayAlert("Issue logging out", "It's probably safe to ignore this message, but you may want to visit your account online to be sure this session is terminated", "OK");
            }

            IsLoggedIn = false;
            LoggedOut?.Invoke(this, EventArgs.Empty);
        }
    }
}
