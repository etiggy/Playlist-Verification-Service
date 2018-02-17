using System.Windows;

namespace Playlist_Verification_Service
{
    public class ErrorItemClass
    {
        AppStateClass CurrentState = ((App)Application.Current).AppState;
        public string check { get; set; }
        public string message { get; set; }
        public string severity { get; set; }
        public string tooltip
        {
            get
            {
                string temp = string.Empty;
                if (severity != "ok")
                {
                    temp += "Severity: " + severity + "\n\n"; 
                }
                else
                {
                    temp += "Check passed successfully";
                }
                if (!noindexitem)
                {
                    temp += "Playlist item:\n" + (CurrentState.selectedfile.GetPlaylist(false))[_index].subtype +" at "+ (CurrentState.selectedfile.GetPlaylist(false))[_index].starttime_wframes +"\n"+ (CurrentState.selectedfile.GetPlaylist(false))[_index].title;
                }
                return temp.Trim();
            }
        }
        private int _index;
        public int index
        {
            get
            {
                if (noindexitem)
                {
                    return int.MaxValue;
                }
                else
                {
                    return _index;
                }
            }
            set
            {
                _index = value;
            }
        }
        public bool noindexitem { get; set; }

        public override string ToString()
        {
            return (check + " " + message + " " + severity + (severity!="ok" ? " error" : ""));
        }
    }
}
