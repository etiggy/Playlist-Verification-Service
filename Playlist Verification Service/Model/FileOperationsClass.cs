using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;

namespace Playlist_Verification_Service
{
    public class FileOperationsClass
    {
        public List<string> ReadFile(FileInfo file, bool playlist)
        {
            List<string> rawdata = new List<string>();
            if (file.Length < (5 * 1024 * 1024))
            {
                try
                {
                    var lines = File.ReadAllLines(file.FullName, Encoding.UTF8);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        rawdata.Add(lines[i]);
                    }
                }
                catch (Exception excp)
                {
                    rawdata.Clear();
                    if (!playlist)
                    {
                        MessageBox.Show("Error: " + excp.Message);
                    }
                }
            }

            if (playlist)
            {
                List<string> processeddata = new List<string>();
                if (rawdata.Count != 0)
                {
                    string temp = string.Empty;
                    for (int i = 0; i < rawdata.Count; i++)
                    {
                        if (rawdata[i].Length < 1815 && i != 0 && i != rawdata.Count - 1)
                        {
                            if (temp.Length < 1815)
                            {
                                temp = temp + rawdata[i];
                            }
                            if (temp.Length >= 1815)
                            {
                                processeddata.Add(temp);
                                temp = string.Empty;
                            }
                        }
                        else
                        {
                            processeddata.Add(rawdata[i]);
                            temp = string.Empty;
                        }
                    }
                }
                return processeddata;
            }
            else
            {
                return rawdata;
            }
        }

        public void WriteFile(List<string> rawdata, FileInfo file)
        {
            try
            {
                File.WriteAllLines(file.FullName, rawdata.ToArray(), Encoding.UTF8);
            }
            catch (Exception excp)
            {
                MessageBox.Show("Error: Settings cannot be saved\n" + file.Name);
            }
        }
    }
}