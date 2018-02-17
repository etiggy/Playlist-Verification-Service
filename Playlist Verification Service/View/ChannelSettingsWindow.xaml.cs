using System.Collections.ObjectModel;
using System.Windows;

namespace Playlist_Verification_Service
{
    /// <summary>
    /// Interaction logic for ChannelSettingsWindow.xaml
    /// </summary>
    public partial class ChannelSettingsWindow : Window
    {
        AppStateClass CurrentState = ((App)Application.Current).AppState;
        ObservableCollection<ChannelSettingsItem> settingslist;

        public ChannelSettingsWindow()
        {
            InitializeComponent();
            settingslist = CurrentState.GetSettingslist();
            SettingsListbox.ItemsSource = settingslist;
            SettingsListbox.SelectedItem = settingslist[0];
        }

        private void button1_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ChannelSettingsItem newitem = new ChannelSettingsItem(settingslist[0].ToString().Replace("DEFAULT", "NEW"));
            settingslist.Add(newitem);
            SettingsListbox.SelectedItem = newitem;
        }

        private void button2_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SettingsListbox.SelectedItem != null)
            {
                if (((ChannelSettingsItem)SettingsListbox.SelectedItem).ChannelID != "DEFAULT")
                {
                    settingslist.Remove((ChannelSettingsItem)SettingsListbox.SelectedItem);
                }
            }
            SettingsListbox.SelectedItem = settingslist[0];
        }

        private void button3_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SettingsListbox.SelectedItem = null;
            settingslist.Clear();
            CurrentState.GetDefaultChannelSettings();
            SettingsListbox.SelectedItem = settingslist[0];
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void MinimiseWindow(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximiseWindow(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.WindowState != WindowState.Maximized)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void CloseWindow(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
