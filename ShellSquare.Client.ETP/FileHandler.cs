using Energistics.Datatypes.ChannelData;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellSquare.Client.ETP
{
    internal class FileHandler
    {
        private static System.Timers.Timer m_Timer;
        private Dictionary<long, string> m_FilePaths = new Dictionary<long, string>();
        private Dictionary<long, List<Packet>> m_Data = new Dictionary<long, List<Packet>>();
        public bool SaveToCSVFile { get; set; }
        public bool CombinedFile { get; set; }
        private string CombinedTimeFileName { get; set; }
        private string CombinedDepthFileName { get; set; }
        private string m_Folder;
        private DataTable m_TimeDataTableFile;
        private DataTable m_DepthDataTableFile;

        public FileHandler()
        {
            m_Timer = new System.Timers.Timer();
            m_Timer.Interval = 3000;
            m_Timer.Elapsed += Timer_Elapsed;
            m_Timer.Enabled = true;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                m_Timer.Enabled = false;
                Flush();
            }
            catch
            {

            }
            finally
            {
                m_Timer.Enabled = true;
            }
        }

        public void SetFolder(string folder)
        {
            m_Folder = folder;
            if(!new DirectoryInfo(m_Folder).Exists)
            new DirectoryInfo(m_Folder).Create();
        }

        public void CloneTables(DataTable depthDataTable, DataTable timeDataTable)
        {
            if (SaveToCSVFile && CombinedFile)
            {
                string header = string.Empty;
                if (depthDataTable.Columns.Count > 1)
                {
                    m_DepthDataTableFile = depthDataTable.Clone();
                    m_DepthDataTableFile.Columns[0].DataType = typeof(string);
                    CombinedDepthFileName = Path.Combine(m_Folder, $"Depth_{DateTime.Now.ToString("yyyyMMddhhmmss")}.csv");
                    header = string.Join(",", m_DepthDataTableFile.Columns.OfType<DataColumn>().Select(x => string.Join(",", x.Caption)));

                    using (FileStream fs = new FileStream(CombinedDepthFileName, FileMode.Append, FileAccess.Write, FileShare.Write))
                    using (StreamWriter streamWriter = new StreamWriter(fs))
                    {
                        streamWriter.WriteLine(header);
                        streamWriter.Flush();
                    }
                }
                else
                {
                    m_DepthDataTableFile = null;
                }
                if (timeDataTable.Columns.Count > 1)
                {
                    m_TimeDataTableFile = timeDataTable.Clone();
                    m_TimeDataTableFile.Columns[0].DataType = typeof(string);
                    CombinedTimeFileName = Path.Combine(m_Folder, $"Time_{DateTime.Now.ToString("yyyyMMddhhmmss")}.csv");
                    header = string.Join(",", m_TimeDataTableFile.Columns.OfType<DataColumn>().Select(x => string.Join(",", x.Caption)));

                    using (FileStream fs = new FileStream(CombinedTimeFileName, FileMode.Append, FileAccess.Write, FileShare.Write))
                    using (StreamWriter streamWriter = new StreamWriter(fs))
                    {
                        streamWriter.WriteLine(header);
                        streamWriter.Flush();
                    }
                }
                else
                {
                    m_TimeDataTableFile = null;
                }
            }
        }

        public void SetHeder(string indexName, string name, string uom, long channelid)
        {
            if (SaveToCSVFile && ! CombinedFile)
            {
                lock (m_FilePaths)
                {
                    if (!m_FilePaths.ContainsKey(channelid))
                    {
                        string path = Path.Combine(m_Folder, $"{name} {channelid}.csv");
                        m_FilePaths.Add(channelid, path);

                        m_Data.Add(channelid, new List<Packet>());

                        using (FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Write))
                        using (StreamWriter streamWriter = new StreamWriter(fs))
                        {
                            streamWriter.WriteLine($"{indexName},{name} ({uom}) : {channelid}");
                            streamWriter.Flush();
                        }
                    }
                }
            }
        }

        public void WriteToFile(long channelid, string index, string value, ChannelIndexTypes indexType)
        {
            if (SaveToCSVFile)
            {
                if (CombinedFile)
                {
                    if(ChannelIndexTypes.Time.Equals(indexType))
                    {
                        lock (m_TimeDataTableFile)
                        {
                            var result = m_TimeDataTableFile.AsEnumerable().Where(dr => dr.Field<string>("Time") == index);
                            if (result.Count() == 0)
                            {
                                DataRow row = m_TimeDataTableFile.NewRow();
                                row[0] = index;

                                row[channelid.ToString()] = value;
                                m_TimeDataTableFile.Rows.Add(row);
                            }
                            else
                            {
                                foreach (var row in result)
                                {
                                    row[channelid.ToString()] = value;
                                }
                            }
                        }
                    }
                    else if (ChannelIndexTypes.Depth.Equals(indexType))
                    {
                        lock (m_DepthDataTableFile)
                        {
                            var result = m_DepthDataTableFile.AsEnumerable().Where(dr => dr.Field<string>("Depth") == index);
                            if (result.Count() == 0)
                            {
                                DataRow row = m_DepthDataTableFile.NewRow();
                                row[0] = index;

                                row[channelid.ToString()] = value;
                                m_DepthDataTableFile.Rows.Add(row);
                            }
                            else
                            {
                                foreach (var row in result)
                                {
                                    row[channelid.ToString()] = value;
                                }
                            }
                        }
                    }
                }
                else
                {
                    lock (m_Data)
                    {
                        m_Data[channelid].Add(new Packet()
                        {
                            Index = index,
                            Data = value
                        });
                    }
                }
            }
        }

        public void Flush()
        {
            if (CombinedFile)
            {
                string data = string.Empty;
                if (m_TimeDataTableFile != null)
                {
                    lock (m_TimeDataTableFile)
                    {
                        data = string.Join(Environment.NewLine, m_TimeDataTableFile.Rows.OfType<DataRow>().Select(x => string.Join(",", x.ItemArray)));
                        m_TimeDataTableFile.Clear();
                    }

                    if (!string.IsNullOrWhiteSpace(data))
                    {
                        using (FileStream fs = new FileStream(CombinedTimeFileName, FileMode.Append, FileAccess.Write, FileShare.Write))
                        using (StreamWriter streamWriter = new StreamWriter(fs))
                        {
                            streamWriter.WriteLine(data);
                            streamWriter.Flush();
                        }
                    }
                }

                if (m_DepthDataTableFile != null)
                {
                    lock (m_DepthDataTableFile)
                    {
                        data = string.Join(Environment.NewLine, m_DepthDataTableFile.Rows.OfType<DataRow>().Select(x => string.Join(",", x.ItemArray)));
                        m_DepthDataTableFile.Clear();
                    }

                    if (!string.IsNullOrWhiteSpace(data))
                    {
                        using (FileStream fs = new FileStream(CombinedDepthFileName, FileMode.Append, FileAccess.Write, FileShare.Write))
                        using (StreamWriter streamWriter = new StreamWriter(fs))
                        {
                            streamWriter.WriteLine(data);
                            streamWriter.Flush();
                        }
                    }
                }

            }
            else
            {
                Dictionary<long, List<Packet>> dataPackets = new Dictionary<long, List<Packet>>();

                lock (m_Data)
                {
                    foreach (var item in m_Data)
                    {
                        List<Packet> packets = new List<Packet>(item.Value);
                        dataPackets.Add(item.Key, packets);
                        item.Value.Clear();
                    }
                }

                lock (m_FilePaths)
                {
                    foreach (var item in dataPackets)
                    {
                        if (item.Value.Count > 0)
                        {
                            if (m_FilePaths.TryGetValue(item.Key, out string m_FilePath))
                            {
                                using (FileStream fs = new FileStream(m_FilePath, FileMode.Append, FileAccess.Write, FileShare.Write))
                                {
                                    using (StreamWriter streamWriter = new StreamWriter(fs))
                                    {
                                        foreach (Packet packet in item.Value)
                                        {
                                            streamWriter.WriteLine(packet.ToString());
                                        }
                                        streamWriter.Flush();
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }

        internal void Clear()
        {
            lock (m_FilePaths)
            {

                m_FilePaths.Clear();
                m_Data.Clear();

                DirectoryInfo di = new DirectoryInfo(m_Folder);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }

                
            }
        }
    }
}
