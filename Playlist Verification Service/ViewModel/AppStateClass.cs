using System;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using System.ComponentModel;

namespace Playlist_Verification_Service
{
    public class AppStateClass : FileOperationsClass
    {
        public FilelistItemClass selectedfile;
        private ObservableCollection<DirectoryInfo> _folderlist;
        private static object _folderlistlock;
        private List<CustomFileSystemWatcher> _watchfolderlist;
        private static object _watchfolderlistlock;
        private ObservableCollection<ChannelSettingsItem> _channelsettingslist;
        private static object _channelsettingslistlock;
        private ObservableCollection<FilelistItemClass> _filelist;
        private static object _filelistlock;
        private List<string> _filestateslist;
        private static object _filestateslock;
        private ObservableCollection<string> _watchfolderqueue;
        private static object _watchfolderqueuelock;
        private bool queueisprocessing;
        private bool _loadingfinished;
        public List<string> processing;
        private static DirectoryInfo settingspath = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\PVS");
        private static FileInfo folderlistfile = new FileInfo(settingspath.FullName + "\\watchfolders.conf");
        private static FileInfo filelistfile = new FileInfo(settingspath.FullName + "\\playlistfiles.conf");
        private static FileInfo channelsettingsfile = new FileInfo(settingspath.FullName + "\\channelsettings.conf");
        DispatcherTimer watchfolderqueueTimer;

        public AppStateClass()
        {
            _folderlist = new ObservableCollection<DirectoryInfo>();
            _folderlistlock = new object();
            BindingOperations.EnableCollectionSynchronization(_folderlist, _folderlistlock);
            _watchfolderlist = new List<CustomFileSystemWatcher>();
            _watchfolderlistlock = new object();
            BindingOperations.EnableCollectionSynchronization(_watchfolderlist, _watchfolderlistlock);
            _channelsettingslist = new ObservableCollection<ChannelSettingsItem>();
            _channelsettingslistlock = new object();
            BindingOperations.EnableCollectionSynchronization(_channelsettingslist, _channelsettingslistlock);
            _filelist = new ObservableCollection<FilelistItemClass>();
            _filelistlock = new object();
            BindingOperations.EnableCollectionSynchronization(_filelist, _filelistlock);
            _filestateslist = new List<string>();
            _filestateslock = new object();
            BindingOperations.EnableCollectionSynchronization(_filestateslist, _filestateslock);
            _watchfolderqueue = new ObservableCollection<string>();
            _watchfolderqueue.CollectionChanged += _watchfolderqueue_CollectionChanged;
            _watchfolderqueuelock = new object();
            BindingOperations.EnableCollectionSynchronization(_watchfolderqueue, _watchfolderqueuelock);
            watchfolderqueueTimer = new DispatcherTimer();
            watchfolderqueueTimer.Interval = TimeSpan.FromSeconds(1);
            watchfolderqueueTimer.Tick += WatchfolderqueueTimer_Tick;
            processing = new List<string>();
            LoadSettings();
        }

        public void ResetAllPlaylist()
        {
            for (int i = 0; i < _filelist.Count; i++)
            {
                _filelist[i].ResetPlaylist();
            }
        }

        public ChannelSettingsItem GetCurrentChannelSettings(string channelid)
        {
            ChannelSettingsItem selected = null;
            for (int i = 1; i < _channelsettingslist.Count; i++)
            {
                if (_channelsettingslist[i].ChannelID == channelid)
                {
                    selected = _channelsettingslist[i];
                    break;
                }
            }

            if (selected == null)
            {
                return _channelsettingslist[0];
            }
            else
            {
                return selected;
            }
        }

        private void _watchfolderqueue_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!queueisprocessing)
            {
                watchfolderqueueTimer.Start();
            }
        }

        private async void WatchfolderqueueTimer_Tick(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                queueisprocessing = true;
                watchfolderqueueTimer.Stop();

                for (int g = 0; g < _watchfolderqueue.Count; g++)
                {
                    string[] temp = _watchfolderqueue[g].Split(';');
                    int i = 0;
                    bool found = false;
                    switch (temp[0])
                    {
                        case "created":
                            if (File.Exists(temp[1]))
                            {
                                if (DateTime.Now.Subtract(new FileInfo(temp[1]).LastWriteTime) > TimeSpan.FromSeconds(10))
                                {
                                    found = false;
                                    for (i = 0; i < _filelist.Count; i++)
                                    {
                                        if (_filelist[i].file.FullName == temp[1])
                                        {
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (found)
                                    {
                                        _filelist[i] = new FilelistItemClass() { file = new FileInfo(temp[1]), state = "new" };
                                    }
                                    else
                                    {
                                        _filelist.Add(new FilelistItemClass() { file = new FileInfo(temp[1]), state = "new" });
                                    }
                                    _watchfolderqueue.RemoveAt(g);
                                    g--;
                                }
                            }
                            else
                            {
                                _watchfolderqueue.RemoveAt(g);
                                g--;
                            }
                            break;
                        case "changed":
                            found = false;
                            for (i = 0; i < _filelist.Count; i++)
                            {
                                if (_filelist[i].file.FullName == temp[1])
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found)
                            {
                                _filelist.RemoveAt(i);
                            }

                            if (DateTime.Now.Subtract(new FileInfo(temp[1]).LastWriteTime) > TimeSpan.FromSeconds(10))
                            {
                                _filelist.Add(new FilelistItemClass() { file = new FileInfo(temp[1]), state = "new" });
                                _watchfolderqueue.RemoveAt(g);
                                g--;
                            }
                            break;
                        case "renamed":
                            found = false;
                            for (i = 0; i < _filelist.Count; i++)
                            {
                                if (_filelist[i].file.FullName == temp[1])
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found)
                            {
                                _filelist[i].file = new FileInfo(temp[2]);
                            }
                            _watchfolderqueue.RemoveAt(g);
                            g--;
                            break;
                        case "deleted":
                            found = false;
                            for (i = 0; i < _watchfolderqueue.Count; i++)
                            {
                                if (_watchfolderqueue[g].Split(';')[1] == _watchfolderqueue[i].Split(';')[1] &&
                                    _watchfolderqueue[i].Split(';')[0] == "created" && i < g)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                            {
                                _watchfolderqueue.RemoveAt(i);
                                _watchfolderqueue.RemoveAt(g);
                                g--;
                            }
                            else
                            {
                                for (i = 0; i < _filelist.Count; i++)
                                {
                                    if (_filelist[i].file.FullName == temp[1])
                                    {
                                        found = true;
                                        break;
                                    }
                                }

                                if (found)
                                {
                                    _filelist.RemoveAt(i);
                                }
                                _watchfolderqueue.RemoveAt(g);
                                g--;
                            }
                            break;
                        case "error":
                            FileSystemWatcher failedwatcher = null;
                            for (i = 0; i < _watchfolderlist.Count; i++)
                            {
                                if (_watchfolderlist[i].Path == temp[1])
                                {
                                    failedwatcher = _watchfolderlist[i];
                                }
                            }

                            if (failedwatcher != null)
                            {
                                try
                                {
                                    if (Directory.Exists(failedwatcher.Path))
                                    {
                                        failedwatcher.EnableRaisingEvents = false;
                                        failedwatcher.EnableRaisingEvents = true;
                                        DirectoryInfo failedfolder = new DirectoryInfo(temp[1]);
                                        FileInfo[] temparray = failedfolder.GetFiles("*", SearchOption.TopDirectoryOnly);
                                        for (i = 0; i < temparray.Length; i++)
                                        {
                                            int j;
                                            found = false;
                                            for (j = 0; j < _filelist.Count; j++)
                                            {
                                                if (temparray[i].FullName == _filelist[j].file.FullName)
                                                {
                                                    found = true;
                                                    break;
                                                }
                                            }
                                            if (found && temparray[i].LastWriteTime != _filelist[j].file.LastWriteTime)
                                            {
                                                _filelist[j] = (new FilelistItemClass() { file = temparray[i], state = "new" });
                                            }
                                            if (!found)
                                            {
                                                _filelist.Add(new FilelistItemClass() { file = temparray[i], state = "new" });
                                            }
                                        }
                                        for (i = 0; i < _filelist.Count; i++)
                                        {
                                            int j;
                                            bool foldermatch = false;
                                            found = false;
                                            for (j = 0; j < temparray.Length; j++)
                                            {
                                                if (_filelist[i].file.DirectoryName == failedfolder.FullName)
                                                {
                                                    foldermatch = true;
                                                    if (_filelist[i].file.FullName == temparray[j].FullName)
                                                    {
                                                        found = true;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    foldermatch = false;
                                                }
                                            }
                                            if (foldermatch && !found)
                                            {
                                                _filelist.RemoveAt(i);
                                                i--;
                                            }
                                        }
                                        _watchfolderqueue.RemoveAt(g);
                                        g--;
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }
                            else
                            {
                                _watchfolderqueue.RemoveAt(g);
                                g--;
                            }
                            break;
                        default:
                            break;
                    }
                }

                queueisprocessing = false;
                if (_watchfolderqueue.Count != 0)
                {
                    watchfolderqueueTimer.Start();
                }
            });
        }

        public ObservableCollection<DirectoryInfo> GetFolderlist()
        {
            return _folderlist;
        }

        public ObservableCollection<ChannelSettingsItem> GetSettingslist()
        {
            return _channelsettingslist;
        }

        public ObservableCollection<FilelistItemClass> GetFilelist()
        {
            return _filelist;
        }

        public void RemoveFolder(DirectoryInfo removedfolder)
        {
            _folderlist.Remove(removedfolder);
            DeregisterFolder(removedfolder);
        }

        private void DeregisterFolder(DirectoryInfo removedfolder)
        {
            int index;
            bool found = false;
            for (index = 0; index < _watchfolderlist.Count; index++)
            {
                if (_watchfolderlist[index].Path == removedfolder.FullName)
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                _watchfolderlist.RemoveAt(index);
            }

            List<FilelistItemClass> removelist = new List<FilelistItemClass>();
            for (int i = 0; i < _filelist.Count; i++)
            {
                if (_filelist[i].file.DirectoryName == removedfolder.FullName)
                {
                    removelist.Add(_filelist[i]);
                }
            }

            for (int i = 0; i < removelist.Count; i++)
            {
                _filelist.Remove(removelist[i]);
            }
        }

        public void AddFolder(DirectoryInfo newfolder)
        {
            bool isnewitem = true;

            for (int i = 0; i < _folderlist.Count; i++)
            {
                if (_folderlist[i].FullName == newfolder.FullName)
                {
                    isnewitem = false;
                    break;
                }
            }

            if (isnewitem)
            {
                _folderlist.Add(newfolder);
                RegisterFolder(newfolder);
            }
        }

        private void RegisterFolder(DirectoryInfo newfolder)
        {
            FileInfo[] temparray = newfolder.GetFiles("*", SearchOption.TopDirectoryOnly);
            List<string> toremove = new List<string>();

            for (int i = 0; i < temparray.Length; i++)
            {
                bool found = false;
                int statetoremove = 0;
                for (int j = 0; j < _filestateslist.Count; j++)
                {
                    string[] tempstring = _filestateslist[j].Split(';');
                    if (tempstring[0] == temparray[i].FullName && DateTime.FromBinary(Convert.ToInt64(tempstring[1])) == temparray[i].LastWriteTimeUtc)
                    {
                        found = true;
                        statetoremove = j;
                        break;
                    }
                }
                if (found)
                {
                    _filelist.Add(new FilelistItemClass() { file = temparray[i], state = _filestateslist[statetoremove].Split(';')[2] });
                    _filestateslist.RemoveAt(statetoremove);
                }
                else
                {
                    _filelist.Add(new FilelistItemClass() { file = temparray[i], state = "new" });
                }
            }

            CustomFileSystemWatcher watcher = new CustomFileSystemWatcher() { Path = newfolder.FullName, IncludeSubdirectories = false, EnableRaisingEvents = true };
            watcher.Created += (s, e) => { _watchfolderqueue.Add("created;" + e.FullPath); };
            watcher.Changed += (s, e) => { _watchfolderqueue.Add("changed;" + e.FullPath); };
            watcher.Renamed += (s, e) => { _watchfolderqueue.Add("renamed;" + e.OldFullPath + ";" + e.FullPath); };
            watcher.Deleted += (s, e) => { _watchfolderqueue.Add("deleted;" + e.FullPath); };
            watcher.Error += (s, e) => { _watchfolderqueue.Add("error;" + ((FileSystemWatcher)s).Path); };
            watcher.InternalBufferSize = 65536;
            _watchfolderlist.Add(watcher);
        }

        private async void LoadSettings()
        {
            await Task.Run(() =>
            {
                if (settingspath.Exists && channelsettingsfile.Exists)
                {
                    List<string> temp = ReadFile(channelsettingsfile, false);
                    for (int i = 0; i < temp.Count; i++)
                    {
                        _channelsettingslist.Add(new ChannelSettingsItem(temp[i]));
                    }
                }
                else
                {
                    GetDefaultChannelSettings();
                }
                if (settingspath.Exists && filelistfile.Exists)
                {
                    _filestateslist = ReadFile(filelistfile, false);
                }
                if (settingspath.Exists && folderlistfile.Exists)
                {
                    List<string> temp = ReadFile(folderlistfile, false);
                    for (int i = 0; i < temp.Count; i++)
                    {
                        DirectoryInfo tdir = new DirectoryInfo(temp[i]);

                        if (tdir.Exists)
                        {
                            AddFolder(tdir);
                        }
                    }
                }
                _loadingfinished = true;
            });
        }

        public void GetDefaultChannelSettings()
        {
            _channelsettingslist.Add(new ChannelSettingsItem("DEFAULT;25.00;true;true;true;true;true;true;true;true;false;true;true;720;12960;true;true"));
            _channelsettingslist.Add(new ChannelSettingsItem("13PL;25.00;true;true;true;true;true;true;true;true;false;true;true;720;12960;true;true"));
            _channelsettingslist.Add(new ChannelSettingsItem("DVRO;25.00;true;true;true;true;true;true;true;true;false;true;true;720;12960;true;true"));
            _channelsettingslist.Add(new ChannelSettingsItem("MOUK;25.00;true;true;true;true;true;true;true;true;false;true;true;720;14040;true;true"));
            _channelsettingslist.Add(new ChannelSettingsItem("MPUK;25.00;true;true;true;true;true;true;true;true;false;true;true;720;14040;true;true"));
            _channelsettingslist.Add(new ChannelSettingsItem("SUSA;25.00;true;true;true;true;true;true;true;false;true;true;true;720;12960;true;true"));
            _channelsettingslist.Add(new ChannelSettingsItem("TMAF;25.00;true;true;true;true;true;true;true;false;true;true;true;720;12960;true;true"));
            _channelsettingslist.Add(new ChannelSettingsItem("UNCZ;25.00;true;true;true;true;true;true;true;true;false;true;true;720;12480;true;true"));
            _channelsettingslist.Add(new ChannelSettingsItem("UNHU;25.00;true;true;true;true;true;true;true;true;false;true;true;720;12480;true;true"));
            _channelsettingslist.Add(new ChannelSettingsItem("UNIR;25.00;true;true;true;true;true;true;true;true;false;true;true;720;14400;true;true"));
            _channelsettingslist.Add(new ChannelSettingsItem("UNSA;25.00;true;true;true;true;true;true;true;false;true;true;true;720;12960;true;true"));
            _channelsettingslist.Add(new ChannelSettingsItem("UNUK;25.00;true;true;true;true;true;true;true;true;false;true;true;720;14400;true;true"));
        }

        public void SaveSettings()
        {
            if (_loadingfinished)
            {
                List<string> temp = new List<string>();
                ObservableCollection<DirectoryInfo> sortedfolderlist = new ObservableCollection<DirectoryInfo>(
                    _folderlist.OrderByDescending(item => item.FullName));
                for (int i = 0; i < sortedfolderlist.Count; i++)
                {
                    temp.Add(sortedfolderlist[i].FullName);
                }
                if (!settingspath.Exists)
                {
                    Directory.CreateDirectory(settingspath.FullName);
                }
                WriteFile(temp, folderlistfile);

                temp = new List<string>();
                ObservableCollection<FilelistItemClass> sortedfilelist = new ObservableCollection<FilelistItemClass>(
                    _filelist.OrderByDescending(item => item.file.DirectoryName).ThenByDescending(item => item.file.LastWriteTime));
                for (int i = 0; i < sortedfilelist.Count; i++)
                {
                    if (sortedfilelist[i].state != "new" || sortedfilelist[i].state != "processing")
                    {
                        temp.Add(sortedfilelist[i].file.FullName + ";" + sortedfilelist[i].file.LastWriteTimeUtc.ToBinary() + ";" + sortedfilelist[i].state);
                    }
                }
                if (!settingspath.Exists)
                {
                    Directory.CreateDirectory(settingspath.FullName);
                }
                WriteFile(temp, filelistfile);

                temp = new List<string>();
                temp.Add(_channelsettingslist[0].ToString());
                ObservableCollection<ChannelSettingsItem> sortedchannelsettingslist = new ObservableCollection<ChannelSettingsItem>(
                    _channelsettingslist.OrderBy(item => item.ChannelID));
                for (int i = 0; i < sortedchannelsettingslist.Count; i++)
                {
                    if (sortedchannelsettingslist[i].ChannelID != "DEFAULT")
                    {
                        temp.Add(sortedchannelsettingslist[i].ToString());
                    }
                }
                if (!settingspath.Exists)
                {
                    Directory.CreateDirectory(settingspath.FullName);
                }
                WriteFile(temp, channelsettingsfile);
            }
        }

        public void RunBatchChecksOnNew()
        {
            for (int i = 0; i < _filelist.Count; i++)
            {
                if (_filelist[i].state == "new")
                {
                    _filelist[i].state = "processing";
                    _filelist[i].GetPlaylist(true);
                }
            }
        }

        public void RunBatchChecksOnAll()
        {
            for (int i = 0; i < _filelist.Count; i++)
            {
                _filelist[i].state = "processing";
                _filelist[i].GetPlaylist(true);
            }
        }
    }
}
