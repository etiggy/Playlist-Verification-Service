using System.IO;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace Playlist_Verification_Service
{
    /// <summary>
    /// Interaction logic for ManageFoldersWindow.xaml
    /// </summary>
    public partial class ManageFoldersWindow : Window
    {
        AppStateClass CurrentState = ((App)Application.Current).AppState;

        public ManageFoldersWindow()
        {
            InitializeComponent();            
            FolderListbox.ItemsSource = CurrentState.GetFolderlist();
        }

        private void button2_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (FolderListbox.SelectedItem != null)
            {
                CurrentState.RemoveFolder((DirectoryInfo)FolderListbox.SelectedItem);
            }
        }

        private void button1_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            WinForms.FolderBrowserDialog folder = new WinForms.FolderBrowserDialog();
            WinForms.DialogResult result = folder.ShowDialog();
            if (result == WinForms.DialogResult.OK)
            {
                CurrentState.AddFolder(new DirectoryInfo(folder.SelectedPath));
            }
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
