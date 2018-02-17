using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Playlist_Verification_Service
{
    public class FilelistItemClass : INotifyPropertyChanged, IComparable
    {
        AppStateClass CurrentState = ((App)Application.Current).AppState;
        private string _state;
        private PlaylistClass playlist;
        private FileInfo _file;
        private bool reset;
        DispatcherTimer LifetimeTimer;
        public FileInfo file
        {
            get
            {
                return _file;
            }
            set
            {
                _file = value;
                NotifyPropertyChanged("FileName");
                NotifyPropertyChanged("LastWriteTime");
                NotifyPropertyChanged("LastWriteTimeString");
            }
        }
        public string FileName
        {
            get
            {
                return _file.Name.Split('.')[0].ToUpper();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public string state
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                NotifyPropertyChanged("state");
            }
        }
        public DateTime LastWriteTime
        {
            get
            {
                return file.LastWriteTime;
            }
        }

        public string LastWriteTimeString
        {
            get
            {
                return ("Exp.: " + file.LastWriteTime.ToString());
            }
        }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public FilelistItemClass()
        {
            LifetimeTimer = new DispatcherTimer();
            LifetimeTimer.Interval = TimeSpan.FromMinutes(10);
            LifetimeTimer.Tick += LifetimeTimer_Tick;
        }

        public void ResetPlaylist()
        {
            if (playlist != null && this != CurrentState.selectedfile && !CurrentState.processing.Contains(file.FullName))
            {
                LifetimeTimer.Stop();
                playlist = null;
                GC.Collect();
            }
        }

        public ObservableCollection<PlaylistItemClass> GetPlaylist(bool resetplaylist)
        {
            reset = resetplaylist;
            if (playlist == null || Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt) || reset)
            {
                if (!CurrentState.processing.Contains(file.FullName))
                {
                    CurrentState.processing.Add(file.FullName);
                    playlist = new PlaylistClass(file);
                    playlist.ChecksFinished += Playlist_ChecksFinished;
                }
            }
            else
            {
                LifetimeTimer.Stop();
                LifetimeTimer.Start();
            }

            return playlist.GetPlaylist;
        }

        public ObservableCollection<ErrorItemClass> GetErrorlist()
        {
            return playlist.GetErrorlist;
        }

        private void Playlist_ChecksFinished(object sender, EventArgs e)
        {
            if (CurrentState.processing.Contains(file.FullName))
            {
                CurrentState.processing.Remove(file.FullName);
            }
            if (playlist != null)
            {
                if (playlist.IsCorrectPlaylist)
                {
                    if (playlist.HasErrors)
                    {
                        state = "error";
                        LifetimeTimer.Stop();
                        LifetimeTimer.Start();
                    }
                    else
                    {
                        state = "ok";
                        ResetPlaylist();
                    }
                }
                else
                {
                    state = "fileerror";
                    ResetPlaylist();
                }

                if (reset)
                {
                    ResetPlaylist();
                } 
            }
        }

        private void LifetimeTimer_Tick(object sender, EventArgs e)
        {
            ResetPlaylist();
        }

        public override string ToString()
        {
            return (FileName + " " + state);
        }

        public int CompareTo(object obj)
        {
            switch (this.file.DirectoryName.CompareTo(((FilelistItemClass)obj).file.DirectoryName))
            {
                case -1:
                    return -1;
                case 1:
                    return 1;
                case 0:
                    return (this.file.LastWriteTime.CompareTo(((FilelistItemClass)obj).file.LastWriteTime));
                default:
                    return 0;
            }
        }
    }
}
