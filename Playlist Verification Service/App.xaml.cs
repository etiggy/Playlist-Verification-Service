using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Playlist_Verification_Service
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _mutex = null;
        public AppStateClass AppState;

        public App()
        {
            AppState = new AppStateClass();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew = false;

            _mutex = new Mutex(true, "PLAYLIST VERIFICATION SERVICE", out createdNew);

            if (!createdNew)
            {
                Application.Current.Shutdown();
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                AppState.SaveSettings();
            }
            finally
            {
                base.OnExit(e);
            }
        }
    }
}
