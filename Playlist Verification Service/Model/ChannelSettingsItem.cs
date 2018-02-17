using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Playlist_Verification_Service
{
    public class ChannelSettingsItem : INotifyPropertyChanged, IComparable
    {
        private string _channelid;
        public string ChannelID
        {
            get
            {
                return _channelid.ToUpper();
            }
            set
            {
                if (_channelid != "DEFAULT")
                {
                    if (value == "DEFAULT")
                    {
                        _channelid = value.ToUpper();
                    }
                    else
                    {
                        if (value.Length > 4)
                        {
                            _channelid = value.Substring(0, 4).ToUpper();
                        }
                        else
                        {
                            _channelid = value.ToUpper();
                        }
                    }
                    NotifyPropertyChanged("ChannelID");
                }
            }
        }
        private double _framerate;
        public double Framerate
        {
            get
            {
                return _framerate;
            }
            set
            {
                if (value > 1)
                {
                    _framerate = value;
                }
            }
        }
        public bool FilenameCheck { get; set; }
        public bool TxIDCheck { get; set; }
        public bool ZeroDurationCheck { get; set; }
        public bool MaterialDurationCheck { get; set; }
        public bool ProgrammePartOrderCheck { get; set; }
        public bool FixedEventCheck { get; set; }
        public bool GapAndOverRunCheck { get; set; }
        private bool _bugcheck;
        public bool BugCheck
        {
            get
            {
                return _bugcheck;
            }
            set
            {
                if (value && _certbugcheck)
                {
                    _certbugcheck = false;
                    NotifyPropertyChanged("CertBugCheck");
                }
                _bugcheck = value;
            }
        }
        private bool _certbugcheck;
        public bool CertBugCheck
        {
            get
            {
                return _certbugcheck;
            }
            set
            {
                if (value && _bugcheck)
                {
                    _bugcheck = false;
                    NotifyPropertyChanged("BugCheck");
                }
                _certbugcheck = value;
            }
        }
        public bool SecondaryEventCheck { get; set; }
        private bool _commercialoverruncheck;
        public bool CommercialOverrunCheck
        {
            get
            {
                return _commercialoverruncheck;
            }
            set
            {
                _commercialoverruncheck = value;
                NotifyPropertyChanged("CommercialOverrunCheck");
            }
        }
        private TimeSpan _maxcomsperhour;
        public TimeSpan MaxComsPerHour
        {
            get
            {
                return _maxcomsperhour;
            }
            set
            {
                if (value > TimeSpan.Zero)
                {
                    _maxcomsperhour = value;
                }
            }
        }
        private TimeSpan _maxcomsperday;
        public TimeSpan MaxComsPerDay
        {
            get
            {
                return _maxcomsperday;
            }
            set
            {
                if (value > TimeSpan.Zero)
                {
                    _maxcomsperday = value;
                }
            }
        }
        public bool OptsignalPairscheck { get; set; }
        public bool DuplicateEventInBreakCheck { get; set; }

        public ChannelSettingsItem(string input)
        {
            string[] temp = input.Split(';');
            ChannelID = temp[0];
            Framerate = double.Parse(temp[1]);
            FilenameCheck = bool.Parse(temp[2]);
            TxIDCheck = bool.Parse(temp[3]);
            ZeroDurationCheck = bool.Parse(temp[4]);
            MaterialDurationCheck = bool.Parse(temp[5]);
            ProgrammePartOrderCheck = bool.Parse(temp[6]);
            FixedEventCheck = bool.Parse(temp[7]);
            GapAndOverRunCheck = bool.Parse(temp[8]);
            BugCheck = bool.Parse(temp[9]);
            CertBugCheck = bool.Parse(temp[10]);
            SecondaryEventCheck = bool.Parse(temp[11]);
            CommercialOverrunCheck = bool.Parse(temp[12]);
            MaxComsPerHour = TimeSpan.FromSeconds(double.Parse(temp[13]));
            MaxComsPerDay = TimeSpan.FromSeconds(double.Parse(temp[14]));
            OptsignalPairscheck = bool.Parse(temp[15]);
            DuplicateEventInBreakCheck = bool.Parse(temp[16]);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public override string ToString()
        {
            return ChannelID + ";" + Framerate.ToString() + ";" + FilenameCheck.ToString() + ";" + TxIDCheck.ToString() + ";" + ZeroDurationCheck.ToString() + ";" +
                MaterialDurationCheck.ToString() + ";" + ProgrammePartOrderCheck.ToString() + ";" + FixedEventCheck.ToString() + ";" +
                GapAndOverRunCheck.ToString() + ";" + BugCheck.ToString() + ";" + CertBugCheck.ToString() + ";" + SecondaryEventCheck.ToString() + ";" +
                CommercialOverrunCheck.ToString() + ";" + MaxComsPerHour.TotalSeconds + ";" + MaxComsPerDay.TotalSeconds + ";" +
                OptsignalPairscheck.ToString() + ";" + DuplicateEventInBreakCheck.ToString();
        }

        public int CompareTo(object obj)
        {
            return (this.ChannelID.CompareTo(((ChannelSettingsItem)obj).ChannelID));
        }
    }
}
