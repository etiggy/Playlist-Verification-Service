using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Playlist_Verification_Service
{
    class PlaylistChecksClass
    {
        private ObservableCollection<PlaylistItemClass> _playlist;
        private ObservableCollection<ErrorItemClass> _errorlist;
        private ChannelSettingsItem CurrentSettings;
        private DateTime _date;
        private bool _isadvanced;
        private bool _haserrors;
        public bool HasErrors
        {
            get
            {
                return _haserrors;
            }
        }

        public PlaylistChecksClass(ObservableCollection<PlaylistItemClass> playlist, ObservableCollection<ErrorItemClass> errorlist, ChannelSettingsItem settings, DateTime playlistdate, string filename)
        {
            _playlist = playlist;
            _errorlist = errorlist;
            CurrentSettings = settings;
            _isadvanced = (filename.ToLower().Contains("adv") || filename.ToLower().Contains("test"));
            _date = playlistdate;

            AdjustPlaylist(); 

            if (CurrentSettings.FilenameCheck && CurrentSettings.ChannelID != "DEFAULT")
            {
                PlaylistHeaderMatchesFilename(filename);
            }
            
            if (_isadvanced)
            {
                if (CurrentSettings.TxIDCheck)
                {
                    NoTxIdAdvance();
                }
                if (CurrentSettings.ZeroDurationCheck)
                {
                    NoZeroOrEmptyDurationAdvance();
                }
                if (CurrentSettings.MaterialDurationCheck)
                {
                    DurationMatchesMaterialDurationAdvance();
                } 
            }
            else
            {                
                if (CurrentSettings.TxIDCheck)
                {
                    NoTxId();
                }
                if (CurrentSettings.ZeroDurationCheck)
                {
                    NoZeroOrEmptyDuration();
                }
                if (CurrentSettings.MaterialDurationCheck)
                {
                    DurationMatchesMaterialDuration();
                }
                if (CurrentSettings.ProgrammePartOrderCheck)
                {
                    ProgrammePartsInOrder(); 
                }
                if (CurrentSettings.FixedEventCheck)
                {
                    NoFixedEvents(); 
                }
                if (CurrentSettings.GapAndOverRunCheck)
                {
                    NoGapsOrUnderruns(); 
                }                
                if (CurrentSettings.BugCheck)
                {                    
                    BugCheck();
                }
                if (CurrentSettings.CertBugCheck)
                {
                    CertBugCheck();
                }
                if (CurrentSettings.SecondaryEventCheck)
                {
                    SecondaryEventLongerThanPrimary(); 
                }
                if (CurrentSettings.CommercialOverrunCheck)
                {
                    NoCommercialOverruns(); 
                }
                if (CurrentSettings.OptsignalPairscheck)
                {
                    OptSignalPairs(); 
                }
                if (CurrentSettings.DuplicateEventInBreakCheck)
                {
                    DuplicateEventInBreak();
                }
            }
        }

        private string TimeSpanStringWithFrames(TimeSpan time)
        {
            return time.Hours.ToString().PadLeft(2, '0') + ':' + time.Minutes.ToString().PadLeft(2, '0') + ':' + time.Seconds.ToString().PadLeft(2, '0') + '.' + (Convert.ToInt32(time.Milliseconds / (1000 / CurrentSettings.Framerate))).ToString().PadLeft(2, '0');
        }

        private void AdjustPlaylist()
        {
            List<string> prevlineids = new List<string>();
            List<int> indexestoremove = new List<int>();
			bool aftermidnight = false;
            for (int i = 0; i < _playlist.Count; i++)
            {
                if (prevlineids.Contains(_playlist[i].lineid))
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (_playlist[j].lineid == _playlist[i].lineid)
                        {
                            _playlist[j].duration = _playlist[j].duration.Add(_playlist[i].duration);
                            _playlist[j].timeout = _playlist[j].timeout.Add(_playlist[i].matduration);
                            _playlist[j].segmentcount = _playlist[j].segmentcount + _playlist[i].segmentcount;
                            indexestoremove.Add(i);
                            break;
                        }
                    }
                }
                else
                {
                    prevlineids.Add(_playlist[i].lineid);
                }

                if (i > 0 && !aftermidnight && _playlist[i].starttime < _playlist[i-1].starttime)
                {
                    aftermidnight = true;
                }
				
				if (aftermidnight)
				{
					_playlist[i].starttime = _playlist[i].starttime.Add(new TimeSpan(24, 0, 0));
				}
            }
            for (int i = 0; i < indexestoremove.Count; i++)
            {
                _playlist.RemoveAt(indexestoremove[i] - i);
            }
        }

        private void PlaylistHeaderMatchesFilename(string filename)
        {
            string checkname = "Filename check";
            string checkmessage = "Incorrect playlist filename, should be ";
            string checkseverity = "high";
            bool checkfounderror = false;

            string temp = CurrentSettings.ChannelID + _date.Day.ToString().PadLeft(2, '0') + _date.Month.ToString().PadLeft(2, '0') + _date.Year.ToString();

            if (!filename.Contains(temp))
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage + temp, severity = checkseverity, noindexitem = true });
                checkfounderror = true;
            }

            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }

        private void DuplicateEventInBreak()
        {
            string checkname = "Duplicate items check";
            string checkmessage = "Item is present more than once in same break";
            string checkseverity = "low";
            bool checkfounderror = false;

            List<string> templist = new List<string>();
            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].subtype == "Programme" || _playlist[i].subtype == "Bug" || _playlist[i].type == "EOF")
                {
                    templist.Clear();
                }
                else if (_playlist[i].subtype == "Promotion" || _playlist[i].subtype == "Commercial")
                {
                    if (templist.Contains(_playlist[i].txid))
                    {
                        _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage, severity = checkseverity, index = i });
                        checkfounderror = true;
                    }
                    else
                    {
                        templist.Add(_playlist[i].txid);
                    }
                }
            }
            
            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }

        private void NoTxId()
        {
            string checkname = "Missing TX IDs";
            string checkmessage = "Playlist item has no TX ID";
            string checkseverity = "high";
            bool checkfounderror = false;

            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].txid == string.Empty)
                {
                    if (!(CurrentSettings.ChannelID == "13PL" && _playlist[i].subtype.Contains("Opt")))
                    {
                        if (!(_playlist[i].subtype == "Programme" && _playlist[i].partnum == 0) && !(_playlist[i].type == "EOF"))
                        {
                            _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage, severity = checkseverity, index = i });
                            checkfounderror = true;
                        }
                    }
                }
            }

            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }

        private void NoTxIdAdvance()
        {
            string checkname = "Missing TX IDs";
            string checkmessage = "Playlist item has no TX ID";
            string checkseverity = "high";
            bool checkfounderror = false;

            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].txid == string.Empty && _playlist[i].subtype == "Programme")
                {
                    if (!(CurrentSettings.ChannelID == "13PL" && _playlist[i].subtype.Contains("Opt")))
                    {
                        if (!(_playlist[i].partnum == 0) && !(_playlist[i].type == "EOF"))
                        {
                            _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage, severity = checkseverity, index = i });
                            checkfounderror = true;
                        }
                    }
                }
            }

            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }

        private void NoZeroOrEmptyDuration()
        {
            string checkname = "Zero duration items";
            string checkmessage = "Event has zero duration";
            string checkseverity = "medium";
            bool checkfounderror = false;

            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].type == "Primary" || (_playlist[i].type == "Secondary" && !(_playlist[i].subtype == "Bug" || _playlist[i].subtype == "Opt In" || _playlist[i].subtype == "Opt Out")))
                {
                    if (_playlist[i].duration == TimeSpan.Zero)
                    {
                        _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage, severity = checkseverity, index = i });
                        checkfounderror = true;
                    }
                }
            }

            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }

        private void NoZeroOrEmptyDurationAdvance()
        {
            string checkname = "Zero duration items";
            string checkmessage = "Event has zero duration";
            string checkseverity = "medium";
            bool checkfounderror = false;

            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].subtype == "Programme")
                {
                    if (_playlist[i].duration == TimeSpan.Zero)
                    {
                        _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage, severity = checkseverity, index = i });
                        checkfounderror = true;
                    }
                }
            }

            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }

        private void DurationMatchesMaterialDuration()
        {
            string checkname = "Material duration mismatches";
            string checkmessage = "Item's duration does not match it's material duration";
            string checkseverity = "medium";
            bool checkfounderror = false;

            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].duration != _playlist[i].matduration && !(_playlist[i].title.ToLower().Contains("timing device") || _playlist[i].title.ToLower().Contains("variable") || _playlist[i].title.ToLower().Contains("end of day")) && ((_playlist[i].type == "Primary" && _playlist[i].subtype != "Commercial") || (_playlist[i].type == "Secondary" && (_playlist[i].subtype == "Voice Over" || _playlist[i].subtype == "DVE"))))
                {
                    _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage, severity = checkseverity, index = i });
                    checkfounderror = true;
                }
            }

            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }

        private void DurationMatchesMaterialDurationAdvance()
        {
            string checkname = "Material duration mismatches";
            string checkmessage = "Item's duration does not match it's material duration";
            string checkseverity = "medium";
            bool checkfounderror = false;

            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].duration != _playlist[i].matduration && _playlist[i].subtype == "Programme")
                {
                    _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage, severity = checkseverity, index = i });
                    checkfounderror = true;
                }
            }

            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }

        private void ProgrammePartsInOrder()
        {
            string checkname = "Programme part order discrepancies";
            string checkmessage = "Programme parts order mismatch or immediate repeat";
            string checkseverity = "medium";
            bool checkfounderror = false;

            List<PlaylistItemClass> templist = new List<PlaylistItemClass>();
            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].subtype == "Programme" || _playlist[i].type == "EOF")
                {
                    templist.Add(_playlist[i]);
                }
            }

            for (int i = 0; i < templist.Count; i++)
            {
                if (templist[i].partnum != 0)
                {
                    if (i > 0)
                    {
                        if (templist[i].txid == templist[i - 1].txid)
                        {
                            if (templist[i].partnum != templist[i - 1].partnum + templist[i - 1].segmentcount)
                            {
                                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage, severity = checkseverity, index = _playlist.IndexOf(templist[i]) });
                                checkfounderror = true;
                            }
                        }
                        else if ((templist[i].title.ToLower().Contains("mini") || templist[i].title.ToLower().Contains("news")) && i + 1 < templist.Count)
                        {
                            if (!(templist[i + 1].txid == templist[i].txid && templist[i + 1].partnum == templist[i].partnum + templist[i].segmentcount))
                            {
                                if (!(templist[i + 1].txid == templist[i - 1].txid && templist[i + 1].partnum == templist[i - 1].partnum + templist[i - 1].segmentcount))
                                {
                                    _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage, severity = checkseverity, index = _playlist.IndexOf(templist[i]) });
                                    checkfounderror = true;
                                }
                            }
                        }
                    }
                }
                else if (templist[i].type != "EOF")
                {
                    _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "No programme part information (placeholder?)", severity = "low", index = _playlist.IndexOf(templist[i]) });
                    checkfounderror = true;
                }
            }

            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }

        private void NoFixedEvents()
        {
            string checkname = "Fixed events";
            string checkmessage1 = "First playlist item is not fixed";
            string checkmessage2 = "Playlist item is fixed";
            string checkseverity = "low";
            bool checkfounderror = false;

            if (!_playlist[0].isfixed)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage1, severity = checkseverity, index = 0 });
                checkfounderror = true;
            }

            for (int i = 1; i < _playlist.Count; i++)
            {
                if (_playlist[i].isfixed)
                {
                    _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage2, severity = checkseverity, index = i });
                    checkfounderror = true;
                }
            }

            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }

        private void NoGapsOrUnderruns()
        {
            string checkname = "Gaps and overruns";
            string checkmessage = "Item's start time indicates timing mismatch";
            string checkseverity = "high";
            bool checkfounderror = false;

            List<PlaylistItemClass> templist = new List<PlaylistItemClass>();

            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].type == "Primary" || _playlist[i].type == "EOF")
                {
                    templist.Add(_playlist[i]);
                }
            }

            for (int i = 1; i < templist.Count; i++)
            {
                if (templist[i].starttime != (templist[i - 1].starttime + templist[i - 1].duration))
                {
                    _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage, severity = checkseverity, index = _playlist.IndexOf(templist[i]) });
					checkfounderror = true;
                }
            }

            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }

        private void BugCheck()
        {
            string checkname = "Channel bugs";
            string checkseverity = "low";
            bool checkfounderror = false;

            List<string> templist = new List<string>();

            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].subtype == "Programme")
                {
                    int numofbugs = 0;
                    for (int j = 0; j < _playlist.Count; j++)
                    {
                        if (_playlist[j].subtype == "Bug" && _playlist[i].lineid == _playlist[j].pointer)
                        {
                            numofbugs++;
                        }
                    }
                    templist.Add(i + ";" + numofbugs);
                }
            }

            for (int i = 0; i < templist.Count; i++)
            {
                if (Convert.ToInt32(templist[i].Split(';')[1]) < 1)
                {
                    _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Programme part has no bugs attached", severity = checkseverity, index = Convert.ToInt32(templist[i].Split(';')[0]) });
                    checkfounderror = true;
                }
                else if (Convert.ToInt32(templist[i].Split(';')[1]) > 1)
                {
                    _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Programme part has more bugs attached than required", severity = checkseverity, index = Convert.ToInt32(templist[i].Split(';')[0]) });
                    checkfounderror = true;
                }
            }

            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].subtype == "Bug")
                {
                    for (int j = 0; j < _playlist.Count; j++)
                    {
                        if (_playlist[i].pointer == _playlist[j].lineid && _playlist[j].subtype != "Programme")
                        {
                            _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Bug item is attached to a non-programme type", severity = checkseverity, index = i });
                            checkfounderror = true;
                        }
                    }
                }
            }

            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }

        private void CertBugCheck()
        {
            string checkname = "Channel bugs";
            string checkseverity = "low";
            bool checkfounderror = false;

            List<string> templist = new List<string>();
            bool blockstart = false;
            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].subtype == "Programme")
                {
                    int needbugs;
                    if (!blockstart)
                    {
                        blockstart = true;
                        needbugs = 2;
                    }
                    else
                    {
                        needbugs = 1;
                    }
                    int numofbugs = 0;
                    for (int j = 0; j < _playlist.Count; j++)
                    {
                        if (_playlist[j].subtype == "Bug" && _playlist[i].lineid == _playlist[j].pointer)
                        {
                            numofbugs++;
                        }
                    }
                    templist.Add(i + ";" + numofbugs + ";" + needbugs);
                }
                else if (_playlist[i].type == "Primary")
                {
                    blockstart = false;
                }
            }

            for (int i = 0; i < templist.Count; i++)
            {
                if (Convert.ToInt32(templist[i].Split(';')[1]) == 0)
                {
                    _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Programme part has no bugs attached", severity = checkseverity, index = Convert.ToInt32(templist[i].Split(';')[0]) });
                    checkfounderror = true;
                }
                else if (Convert.ToInt32(templist[i].Split(';')[1]) < Convert.ToInt32(templist[i].Split(';')[2]))
                {
                    _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Programme part is missing a bug (cert bug?)", severity = checkseverity, index = Convert.ToInt32(templist[i].Split(';')[0]) });
                    checkfounderror = true;
                }
                else if (Convert.ToInt32(templist[i].Split(';')[1]) > Convert.ToInt32(templist[i].Split(';')[2]))
                {
                    _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Programme part has more bugs attached than required", severity = checkseverity, index = Convert.ToInt32(templist[i].Split(';')[0]) });
                    checkfounderror = true;
                }
            }

            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].subtype == "Bug")
                {
                    for (int j = 0; j < _playlist.Count; j++)
                    {
                        if (_playlist[i].pointer == _playlist[j].lineid && _playlist[j].subtype != "Programme")
                        {
                            _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Bug item is attached to a non-programme type", severity = checkseverity, index = i });
                            checkfounderror = true;
                        }
                    }
                }
            }

            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }

        private void SecondaryEventLongerThanPrimary()
        {
            string checkname = "Secondary event timing mismatches";
            string checkmessage1 = "Secondary event starts before it's primary event";
            string checkmessage2 = "Secondary event ends after it's primary event";
            string checkseverity = "medium";
            bool checkfounderror = false;

            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].type == "Secondary")
                {
                    bool found = false;
                    for (int j = 0; j < _playlist.Count; j++)
                    {
                        if (_playlist[j].type == "Primary")
                        {
                            if (_playlist[i].pointer == _playlist[j].lineid)
                            {
                                found = true;
                                _playlist[i].BindingTo = _playlist[j].subtype +" at "+ _playlist[j].starttime_wframes + "\n" + _playlist[j].title;
                                if ((_playlist[i].starttime) < _playlist[j].starttime)
                                {
                                    _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage1, severity = checkseverity, index = i });
                                    checkfounderror = true;
                                }
                                if ((_playlist[i].starttime + _playlist[i].duration) > (_playlist[j].starttime + _playlist[j].duration))
                                {
                                    _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage2, severity = checkseverity, index = i });
                                    checkfounderror = true;
                                }
                                break;
                            }
                        }
                    }
                    if (!found)
                    {
                        _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Attached primary event could not be found", severity = checkseverity, index = i });
                        checkfounderror = true;
                    }
                }
            }

            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }

        private void NoCommercialOverruns()
        {
            string checkname = "Commercial overruns";
            string checkmessage = "Overrun detected";
            string checkseverity = "high";
            bool checkfounderror = false;

            List<CommCheckClass> templist = new List<CommCheckClass>();
            for (int i = 0; i < 24; i++)
            {
                templist.Add(new CommCheckClass() { hour = i, commperhour = new TimeSpan() });
            }

            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].subtype == "Commercial")
                {
                    if (_playlist[i].starttime.Hours == (_playlist[i].starttime + _playlist[i].duration).Hours)
                    {
                        templist[_playlist[i].starttime.Hours].commperhour += _playlist[i].duration;
                    }
                    else
                    {                        
						templist[_playlist[i].starttime.Hours].commperhour += new TimeSpan(((int)(_playlist[i].starttime + _playlist[i].duration).TotalHours), 0, 0) - _playlist[i].starttime;
                        templist[(_playlist[i].starttime + _playlist[i].duration).Hours].commperhour += (_playlist[i].starttime + _playlist[i].duration) - new TimeSpan((int)((_playlist[i].starttime + _playlist[i].duration).TotalHours), 0, 0);
                    }
                }
            }

            TimeSpan commperday = new TimeSpan();
            for (int i = 0; i < templist.Count; i++)
            {
                if (templist[i].commperhour > CurrentSettings.MaxComsPerHour)
                {
                    _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage + " in hour " + templist[i].hour.ToString().PadLeft(2, '0') + ": " + TimeSpanStringWithFrames(templist[i].commperhour).Substring(3, 8) + " (max. allowed: " + TimeSpanStringWithFrames(CurrentSettings.MaxComsPerHour).Substring(3, 8) + ")", severity = checkseverity, noindexitem = true });
                    checkfounderror = true;
                }
                commperday += templist[i].commperhour;
            }
            if (commperday > CurrentSettings.MaxComsPerDay)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = checkmessage + " in day: " + TimeSpanStringWithFrames(commperday) + " (max. allowed: " + TimeSpanStringWithFrames(CurrentSettings.MaxComsPerDay) + ")", severity = checkseverity, noindexitem = true });
                checkfounderror = true;
            }

            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }

        private void OptSignalPairs()
        {
            string checkname = "Opt out signal checks";
            string checkseverity = "medium";
            bool checkfounderror = false;

            List<int> numofsignals = new List<int>();
            for (int i = 0; i < _playlist.Count; i++)
            {
                if (_playlist[i].subtype.Contains("Opt"))
                {
                    numofsignals.Add(i);
                }

                if (_playlist[i].subtype == "Programme" || _playlist[i].type == "EOF")
                {
                    if (numofsignals.Count != 0)
                    {
                        if (numofsignals.Count % 2 == 0)
                        {
                            for (int j = 0; j < numofsignals.Count; j++)
                            {
                                if (j % 2 == 0)
                                {
                                    if (_playlist[numofsignals[j]].subtype != "Opt Out")
                                    {
                                        _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Opt Out signal type expected", severity = checkseverity, index = numofsignals[j] });
                                        checkfounderror = true;
                                    }
                                }
                                else
                                {
                                    if (_playlist[numofsignals[j]].subtype != "Opt In")
                                    {
                                        _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Opt In signal type expected", severity = checkseverity, index = numofsignals[j] });
                                        checkfounderror = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Opt signal pair mismatch (No of signals: " + numofsignals.Count + ")", severity = checkseverity, index = numofsignals[numofsignals.Count - 1] });
                            checkfounderror = true;
                        }
                        numofsignals.Clear();
                    }
                }
            }

            if (!checkfounderror)
            {
                _errorlist.Add(new ErrorItemClass() { check = checkname.ToUpper(), message = "Check passed successfully", severity = "ok", noindexitem = true });
            }
            else
            {
                _haserrors = true;
            }
        }
    }
}
