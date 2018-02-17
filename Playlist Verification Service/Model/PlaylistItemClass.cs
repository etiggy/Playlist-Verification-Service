using System;
using System.ComponentModel;

namespace Playlist_Verification_Service
{
    public class PlaylistItemClass : INotifyPropertyChanged
    {
        private double framerate;
        public TimeSpan starttime { get; set; }
        public TimeSpan duration { get; set; }
        public TimeSpan timein { get; set; }
        public TimeSpan timeout { get; set; }
        public TimeSpan offset { get; set; }
        public string starttime_wframes
        {
            get
            {
                return TimeSpanStringWithFrames(starttime);
            }
        }
        public string duration_wframes
        {
            get
            {
                return TimeSpanStringWithFrames(duration);
            }
        }
        public string matduration_wframes
        {
            get
            {
                return TimeSpanStringWithFrames(timeout.Subtract(timein));
            }
        }
        public TimeSpan matduration
        {
            get
            {
                return (timeout.Subtract(timein));
            }
        }
        public string timein_wframes
        {
            get
            {
                return TimeSpanStringWithFrames(timein);
            }
        }
        public string timeout_wframes
        {
            get
            {
                return TimeSpanStringWithFrames(timeout);
            }
        }
        public string offset_wframes
        {
            get
            {
                return TimeSpanStringWithFrames(offset);
            }
        }
        public bool isfixed { get; set; }
        public string type { get; set; }
        public string subtype { get; set; }
        public string txid { get; set; }
        private string _title;
        public string DynText { get; set; }
        public string title
        {
            get
            {
                if (subtype == "Programme")
                {
                    return _title + " - Part " + partnum.ToString().PadLeft(2, '0');
                }
                else
                {
                    return _title;
                }
            }
        }
        public int partnum { get; set; }
        public int segmentcount { get; set; }
        public string lineid { get; set; }
        public string pointer { get; set; }
        public bool slotboundary { get; set; }
        private string _bindingto;
        public event PropertyChangedEventHandler PropertyChanged;
        public string BindingTo
        {
            get
            {
                if (_bindingto == null)
                {
                    _bindingto = string.Empty;
                }
                return _bindingto;
            }
            set
            {
                _bindingto = value;
                NotifyPropertyChanged("tooltip");
            }
        }
        public string tooltip
        {
            get
            {
                string temp = string.Empty;
                if (type.Equals("Primary"))
                {
                    temp += "Material duration: " + matduration_wframes;
                    if (segmentcount != 1)
                    {
                        temp += "\nNo of sequences: " + segmentcount;
                    }
                    if (DynText != string.Empty)
                    {
                        temp += "\n\nDynamic Text:\n" + DynText;
                    }
                    return temp;
                }
                else if (type.Equals("Secondary"))
                {
                    if (!(subtype.Equals("Bug") || subtype.Equals("Opt In") || subtype.Equals("Opt Out")))
                    {
                        temp += "Material duration: " + matduration_wframes + "\n";
                    }
                    temp += "Offset: " + offset_wframes;                    
                    if (DynText != string.Empty)
                    {
                        temp += "\n\nDynamic Text:\n" + DynText;
                    }
                    if (BindingTo != string.Empty)
                    {
                        temp += "\n\nBinding to:\n" + BindingTo;
                    }
                    return temp;
                }
                else
                {
                    return null;
                }
            }
        }

        public PlaylistItemClass(string inputline, double playlistframerate)
        {
            framerate = playlistframerate;
            if (inputline.Contains("***") && inputline.Contains("EOF"))
            {
                starttime = new TimeSpan();
                duration = new TimeSpan();
                timein = new TimeSpan();
                timeout = new TimeSpan();
                offset = new TimeSpan();
                isfixed = false;
                slotboundary = false;
                txid = string.Empty;
                _title = "End of playlist";
                partnum = 0;
                segmentcount = 0;
                lineid = string.Empty;
                pointer = string.Empty;
                type = "EOF";
                subtype = string.Empty;
            }
            else
            {
                starttime = new TimeSpan(0, Convert.ToInt32(inputline.Substring(18, 2)), Convert.ToInt32(inputline.Substring(20, 2)), Convert.ToInt32(inputline.Substring(22, 2)), Convert.ToInt32(inputline.Substring(24, 2)) * Convert.ToInt32(1000 / framerate));
                duration = new TimeSpan(0, Convert.ToInt32(inputline.Substring(27, 2)), Convert.ToInt32(inputline.Substring(29, 2)), Convert.ToInt32(inputline.Substring(31, 2)), Convert.ToInt32(inputline.Substring(33, 2)) * Convert.ToInt32(1000 / framerate));
                timein = new TimeSpan(0, Convert.ToInt32(inputline.Substring(99, 2)), Convert.ToInt32(inputline.Substring(101, 2)), Convert.ToInt32(inputline.Substring(103, 2)), Convert.ToInt32(inputline.Substring(105, 2)) * Convert.ToInt32(1000 / framerate));
                timeout = new TimeSpan(0, Convert.ToInt32(inputline.Substring(107, 2)), Convert.ToInt32(inputline.Substring(109, 2)), Convert.ToInt32(inputline.Substring(111, 2)), Convert.ToInt32(inputline.Substring(113, 2)) * Convert.ToInt32(1000 / framerate));
                offset = new TimeSpan(0, Convert.ToInt32(inputline.Substring(188, 2)), Convert.ToInt32(inputline.Substring(190, 2)), Convert.ToInt32(inputline.Substring(192, 2)), Convert.ToInt32(inputline.Substring(194, 2)) * Convert.ToInt32(1000 / framerate));
                isfixed = (inputline.Substring(26, 1) == "T") ? true : false;
                slotboundary = (inputline.Substring(196, 1) == "Y") ? true : false;
                txid = (inputline.Substring(79, 20).Trim() != string.Empty) ? inputline.Substring(79, 20).Trim() : inputline.Substring(146, 30).Trim();
                _title = inputline.Substring(116, 30).Trim();
                if (_title[_title.Length - 1] == '/')
                {
                    _title = _title.Substring(0, _title.Length - 1);
                }
                partnum = Convert.ToInt32(inputline.Substring(197, 2));
                segmentcount = 1;
                lineid = inputline.Substring(213, 10).Trim();
                pointer = inputline.Substring(203, 10).Trim();

                if (inputline.Substring(223, 1530).Trim() != string.Empty)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        DynText += inputline.Substring((223 + i * 153), 153).Trim() + "\n";
                    }
                    DynText = DynText.Substring(0, DynText.Length - 2).Trim();
                }
                else
                {
                    DynText = string.Empty;
                }

                type = "Unknown";
                subtype = "Unknown";

                string[] tempids = new string[4];
                tempids[0] = inputline.Substring(0, 2).Trim().ToUpper();
                tempids[1] = inputline.Substring(176, 10).Trim().ToUpper();
                tempids[2] = inputline.Substring(186, 1).Trim().ToUpper();
                tempids[3] = inputline.Substring(187, 1).ToUpper();

                if (tempids[2] == "Y" && tempids[3] == "I")
                {
                    type = "Secondary";

                    if (tempids[1] == "BUG")
                    {
                        subtype = "Bug";
                    }
                    else if (tempids[1] == "OPT IN")
                    {
                        subtype = "Opt In";
                    }
                    else if (tempids[1] == "OPT OUT")
                    {
                        subtype = "Opt Out";
                    }
                    else if (tempids[1] == "VOICE OVER")
                    {
                        subtype = "Voice Over";
                    }
                    else if (tempids[1] == "ZIP")
                    {
                        subtype = "Zip";
                    }
                    else if (tempids[1] == "MENU")
                    {
                        subtype = "Dyn. Menu";
                    }
                    else if (tempids[1] == "ECS")
                    {
                        subtype = "ECS";
                    }
                    else if (tempids[1] == "ECS CLIP")
                    {
                        subtype = "DVE";
                    }
                }
                else
                {
                    type = "Primary";

                    if (tempids[0] == "P" && tempids[1] == string.Empty && tempids[2] == string.Empty && tempids[3] == "P")
                    {
                        subtype = "Programme";
                    }
                    else if (tempids[0] == "S" && tempids[1] == string.Empty && tempids[2] == string.Empty && tempids[3] == "S")
                    {
                        subtype = "Commercial";
                    }
                    else if (tempids[0] == "I" && tempids[1] == string.Empty && tempids[2] == string.Empty && tempids[3] == "I")
                    {
                        subtype = "Interstitial";
                    }
                    else if (tempids[0] == "I" && tempids[1] == string.Empty && tempids[2] == string.Empty && tempids[3] == "S")
                    {
                        subtype = "Promotion";
                    }
                }
            }
        }

        private string TimeSpanStringWithFrames(TimeSpan time)
        {
            return time.Hours.ToString().PadLeft(2, '0') + ':' + time.Minutes.ToString().PadLeft(2, '0') + ':' + time.Seconds.ToString().PadLeft(2, '0') + '.' + (Convert.ToInt32(time.Milliseconds / (1000 / framerate))).ToString().PadLeft(2, '0');
        }

        public override string ToString()
        {
            return (starttime_wframes + " " + duration_wframes + " " + title + " " + type + " " + subtype + " " + txid + (isfixed ? " Fixed" : " Follow on") + (slotboundary ? " Header" : ""));
        }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
