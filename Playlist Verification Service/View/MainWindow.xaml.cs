using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Playlist_Verification_Service
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AppStateClass CurrentState = ((App)Application.Current).AppState;
        ICollectionView FileListView;
        ICollectionView ErrorListView;
        ICollectionView PlaylistView;
        DispatcherTimer FilelistSearchbox_TextChangedTimer;
        DispatcherTimer ErrorSearchboxSearchbox_TextChangedTimer;
        DispatcherTimer PlaylistSearchbox_TextChangedTimer;

        public MainWindow()
        {
            InitializeComponent();
            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));
            FilelistSearchbox_TextChangedTimer = new DispatcherTimer();
            FilelistSearchbox_TextChangedTimer.Interval = TimeSpan.FromMilliseconds(350);
            FilelistSearchbox_TextChangedTimer.Tick += FilelistSearchbox_TextChangedTimer_Tick;
            ErrorSearchboxSearchbox_TextChangedTimer = new DispatcherTimer();
            ErrorSearchboxSearchbox_TextChangedTimer.Interval = TimeSpan.FromMilliseconds(350);
            ErrorSearchboxSearchbox_TextChangedTimer.Tick += ErrorSearchboxSearchbox_TextChangedTimer_Tick;
            PlaylistSearchbox_TextChangedTimer = new DispatcherTimer();
            PlaylistSearchbox_TextChangedTimer.Interval = TimeSpan.FromMilliseconds(350);
            PlaylistSearchbox_TextChangedTimer.Tick += PlaylistSearchbox_TextChangedTimer_Tick;

            FileListView = (CollectionViewSource.GetDefaultView(CurrentState.GetFilelist()));
            using (FileListView.DeferRefresh())
            {
                FileListView.SortDescriptions.Add(new SortDescription("LastWriteTime", ListSortDirection.Descending));
                FileListView.Filter = delegate (object item)
                {
                    if (string.IsNullOrEmpty(FilelistSearchbox.Text))
                    {
                        return true;
                    }
                    else
                    {
                        bool contains = true;
                        string[] temparray = FilelistSearchbox.Text.Trim().Split(' ');
                        for (int i = 0; i < temparray.Length; i++)
                        {
                            if (!((FilelistItemClass)item).ToString().ToLower().Contains(temparray[i].ToLower()))
                            {
                                contains = false;
                            }
                        }
                        return contains;
                    }
                };
                ICollectionViewLiveShaping FileListViewRefresh = FileListView as ICollectionViewLiveShaping;
                if (FileListViewRefresh.CanChangeLiveFiltering)
                {
                    FileListViewRefresh.LiveFilteringProperties.Add("state");
                    FileListViewRefresh.LiveFilteringProperties.Add("FileName");
                    FileListViewRefresh.LiveFilteringProperties.Add("LastWriteTimeString");
                    FileListViewRefresh.IsLiveFiltering = true;
                }
            }
            FileListbox.ItemsSource = FileListView;
        }

        private void FilelistListbox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (FileListbox.SelectedItem != null)
            {
                CurrentState.selectedfile = (FilelistItemClass)FileListbox.SelectedItem;
                PlaylistView = CollectionViewSource.GetDefaultView(CurrentState.selectedfile.GetPlaylist(false));
                using (PlaylistView.DeferRefresh())
                {
                    PlaylistView.Filter = delegate (object item)
                    {
                        if (string.IsNullOrEmpty(PlaylistSearchbox.Text))
                        {
                            return true;
                        }
                        else
                        {
                            bool contains = true;
                            string[] temparray = PlaylistSearchbox.Text.Trim().Split(' ');
                            for (int i = 0; i < temparray.Length; i++)
                            {
                                if (!((PlaylistItemClass)item).ToString().ToLower().Contains(temparray[i].ToLower()))
                                {
                                    contains = false;
                                }
                            }
                            return contains;
                        }
                    };
                }
                PlayListbox.ItemsSource = PlaylistView;
                if (!PlaylistView.IsEmpty)
                {
                    PlayListbox.ScrollIntoView(PlayListbox.Items[0]);
                }

                ErrorListView = CollectionViewSource.GetDefaultView(CurrentState.selectedfile.GetErrorlist());
                using (ErrorListView.DeferRefresh())
                {
                    ErrorListView.SortDescriptions.Add(new SortDescription("check", ListSortDirection.Ascending));
                    ErrorListView.SortDescriptions.Add(new SortDescription("index", ListSortDirection.Ascending));
                    ErrorListView.Filter = delegate (object item)
                    {
                        if (string.IsNullOrEmpty(ErrorSearchbox.Text))
                        {
                            return true;
                        }
                        else
                        {
                            bool contains = true;
                            string[] temparray = ErrorSearchbox.Text.Trim().Split(' ');
                            for (int i = 0; i < temparray.Length; i++)
                            {
                                if (!((ErrorItemClass)item).ToString().ToLower().Contains(temparray[i].ToLower()))
                                {
                                    contains = false;
                                }
                            }
                            return contains;
                        }
                    };
                }
                ErrorListbox.ItemsSource = ErrorListView;
                if (!ErrorListView.IsEmpty)
                {
                    ErrorListbox.ScrollIntoView(ErrorListbox.Items[0]);
                }
            }
            else
            {
                CurrentState.selectedfile = null;
                ErrorListbox.ItemsSource = null;
                PlayListbox.ItemsSource = null;
            }
        }

        private void ErrorListbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ErrorListbox.SelectedItem != null)
            {
                if (!((ErrorItemClass)ErrorListbox.SelectedItem).noindexitem)
                {
                    PlaylistSearchbox.Text = string.Empty;
                    PlaylistSearchbox_TextChangedTimer_Tick(null, null);
                    PlayListbox.SelectedItem = (PlayListbox.Items[((ErrorItemClass)ErrorListbox.SelectedItem).index]);
                    PlayListbox.ScrollIntoView(PlayListbox.SelectedItem);
                }
            }
        }

        private void MenuItem_ManageFolders(object sender, RoutedEventArgs e)
        {
            ManageFoldersWindow settings = new ManageFoldersWindow();
            settings.ShowDialog();
            CurrentState.SaveSettings();
        }

        private void MenuItem_ChannelSettings(object sender, RoutedEventArgs e)
        {
            FileListbox.SelectedItem = null;
            CurrentState.selectedfile = null;
            ChannelSettingsWindow settings = new ChannelSettingsWindow();
            settings.ShowDialog();
            CurrentState.ResetAllPlaylist();
            CurrentState.SaveSettings();
        }

        private void MenuItem_BatchProcessAllNew(object sender, RoutedEventArgs e)
        {
            CurrentState.RunBatchChecksOnNew();
        }

        private void MenuItem_BatchProcessAll(object sender, RoutedEventArgs e)
        {
            FileListbox.SelectedItem = null;
            CurrentState.selectedfile = null;
            CurrentState.RunBatchChecksOnAll();
        }

        private void MenuItem_Quit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void FilelistSearchbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (FileListView != null)
            {
                FilelistSearchbox_TextChangedTimer.Stop();
                FilelistSearchbox_TextChangedTimer.Start();
            }
        }

        private void ErrorSearchbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ErrorListView != null)
            {
                ErrorSearchboxSearchbox_TextChangedTimer.Stop();
                ErrorSearchboxSearchbox_TextChangedTimer.Start();
            }
        }

        private void PlaylistSearchbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PlaylistView != null)
            {
                PlaylistSearchbox_TextChangedTimer.Stop();
                PlaylistSearchbox_TextChangedTimer.Start();
            }
        }

        private void FilelistSearchbox_TextChangedTimer_Tick(object sender, EventArgs e)
        {
            FilelistSearchbox_TextChangedTimer.Stop();
            FileListView.Refresh();
        }

        private void ErrorSearchboxSearchbox_TextChangedTimer_Tick(object sender, EventArgs e)
        {
            ErrorSearchboxSearchbox_TextChangedTimer.Stop();
            ErrorListView.Refresh();
        }

        private void PlaylistSearchbox_TextChangedTimer_Tick(object sender, EventArgs e)
        {
            PlaylistSearchbox_TextChangedTimer.Stop();
            PlaylistView.Refresh();
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
            Application.Current.Shutdown();
        }
    }
}
