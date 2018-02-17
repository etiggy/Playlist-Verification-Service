using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Playlist_Verification_Service
{
    public class PlaylistClass : FileOperationsClass
    {
        AppStateClass CurrentState = ((App)Application.Current).AppState;
        private FileInfo _file;
        private DateTime _date;
        private string _channelid;
        private bool _iscorrectplaylist;
        private bool _haserrors;
        private ObservableCollection<PlaylistItemClass> _playlistitems;
        private static object _playlistlock;
        private ObservableCollection<ErrorItemClass> _errorlist;
        private static object _errorlistlock;

        public event EventHandler ChecksFinished;

        public bool HasErrors
        {
            get
            {
                return _haserrors;
            }
        }

        public bool IsCorrectPlaylist
        {
            get
            {
                return _iscorrectplaylist;
            }
        }

        public ObservableCollection<PlaylistItemClass> GetPlaylist
        {
            get
            {
                return _playlistitems;
            }
        }
        public ObservableCollection<ErrorItemClass> GetErrorlist
        {
            get
            {
                return _errorlist;
            }
        }

        public PlaylistClass(FileInfo file)
        {
            _file = file;
            _playlistitems = new ObservableCollection<PlaylistItemClass>();
            _playlistlock = new object();
            BindingOperations.EnableCollectionSynchronization(_playlistitems, _playlistlock);
            _errorlist = new ObservableCollection<ErrorItemClass>();
            _errorlistlock = new object();
            BindingOperations.EnableCollectionSynchronization(_errorlist, _errorlistlock);

            AsyncWorker();
        }

        private async void AsyncWorker()
        {
            await Task.Run(() =>
            {
                try
                {
                    List<string> input = ReadFile(_file,true);
                    if (input.Count > 0)
                    {
                        _channelid = input[0].Substring(13, 4).ToUpper();
                        _date = new DateTime(Convert.ToInt32(input[0].Substring(11, 2)), Convert.ToInt32(input[0].Substring(9, 2)), Convert.ToInt32(input[0].Substring(7, 2)));
                        for (int i = 1; i < input.Count; i++)
                        {
                            if (input[i].Trim().Length > 0)
                            {
                                _playlistitems.Add(new PlaylistItemClass(input[i], CurrentState.GetCurrentChannelSettings(_channelid).Framerate));
                            }
                        }
                        if (_playlistitems[_playlistitems.Count-1].type == "EOF")
                        {
                            _playlistitems[_playlistitems.Count - 1].starttime = _playlistitems[0].starttime;
                        }
                        
                        PlaylistChecksClass checks = new PlaylistChecksClass(_playlistitems, _errorlist, CurrentState.GetCurrentChannelSettings(_channelid), _date, _file.Name.ToUpper());
                        _haserrors = checks.HasErrors;
                        _iscorrectplaylist = true;
                    }
                }
                catch (Exception excp)
                {
                    _iscorrectplaylist = false;
                }
            });

            EventHandler finished = ChecksFinished;
            if (finished != null)
            {
                finished(this, new EventArgs());
            }
        }
    }
}
