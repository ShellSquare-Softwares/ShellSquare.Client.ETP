using Avro.IO;
using Avro.Specific;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Core;
using Energistics.Protocol.Discovery;
using Newtonsoft.Json;
using ShellSquare.Client.ETP.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShellSquare.Client.ETP
{
    public class ETPHandler
    {
        private static object m_ConnectionLock = new object();
        private bool m_HasConnected;
        public bool HasConnected
        {
            get
            {
                lock (m_ConnectionLock)
                {
                    return m_HasConnected;
                }
            }
        }

        private bool m_HasDescribing;
        public bool HasDescribing
        {
            get
            {
                lock (m_ConnectionLock)
                {
                    return m_HasDescribing;
                }
            }

            set
            {
                lock (m_ConnectionLock)
                {
                    m_HasDescribing = value;
                }
            }
        }

        private List<ChannelStreamingInfo> m_ChannelStreamingInfo = new List<ChannelStreamingInfo>();
        private DateTime m_Time;
        public Action<string, double, TraceLevel> Message;
        public Action<ChannelItem, ChannelItem> ChannelChildrensReceived;
        public Action<ChannelItem> ChannelItemsReceived;
        public Action<IList<DataItem>> ChannelDataReceived;
        public Action<ChannelMetadata> ChannelInfoReceived;
        private List<byte> m_bytes = new List<byte>();
        private const int BufferSize = 4096;
        private ClientWebSocket m_Socket;
        const string SUBPROTOCOL = "energistics-tp";
        const string ETPENCODING = "etp-encoding";
        const string ENCODING = "binary";
        const string AUTHORIZATION = "Authorization";
        private Dictionary<long, RequestInformation> m_RequestInformation = new Dictionary<long, RequestInformation>();
        private readonly List<string> m_LogCurveEml = new List<string>();

        public async Task Connect(string url, string username, string password, int maxDataItems, int maxMessageRate, List<SupportedProtocol> protocols, CancellationToken token)
        {
            try
            {
                Uri uri = new Uri(url);

                string authInfo = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                authInfo = "Basic " + authInfo;

                m_Socket = new ClientWebSocket();
                m_Socket.Options.AddSubProtocol(SUBPROTOCOL);
                m_Socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(60);
                m_Socket.Options.SetBuffer(65536, 65536);
                m_Socket.Options.SetRequestHeader(AUTHORIZATION, authInfo);
                m_Socket.Options.SetRequestHeader(ETPENCODING, ENCODING);

                m_Socket.ConnectAsync(uri, token).Wait();

                CreateSession(protocols).Wait();
                await Task.Factory.StartNew(async () =>
                {
                    await ReceiveData(token);
                });

                await Start(maxDataItems, maxMessageRate);
            }
            catch (Exception ex)
            {

                if (ex.InnerException != null)
                {
                    throw new Exception($"{ex.Message} {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public void Disconnect()
        {
            m_Socket = null;
            lock (m_ConnectionLock)
            {
                m_HasConnected = false;
            }
        }




        public async Task Discover(string eml, ChannelItem channelItem)
        {
            MessageHeader header = new MessageHeader();
            header.Protocol = (int)Protocols.Discovery;
            header.MessageType = 1;
            header.MessageId = EtpHelper.NextMessageId;
            header.MessageFlags = 0;
            header.CorrelationId = 0;

            GetResources resource = new GetResources()
            {
                Uri = eml
            };

            var data = Encode(resource, header);
            var buffer = new ArraySegment<byte>(data, 0, data.Length);
            lock (m_RequestInformation)
            {
                RequestInformation info = new RequestInformation()
                {
                    ChannelItem = channelItem,
                    RequestTime = DateTime.UtcNow
                };

                m_RequestInformation.Add(header.MessageId, info);
            }
            await m_Socket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task Describe(List<string> uris)
        {
            lock (m_ChannelStreamingInfo)
            {
                m_ChannelStreamingInfo.Clear();

                m_LogCurveEml.Clear();
                m_LogCurveEml.AddRange(uris);
            }
            MessageHeader header = new MessageHeader();
            header.Protocol = (int)Protocols.ChannelStreaming;
            header.MessageType = 1;
            header.MessageId = EtpHelper.NextMessageId;
            header.MessageFlags = 0;
            header.CorrelationId = 0;

            var channelDescribe = new ChannelDescribe()
            {
                Uris = uris
            };

            var data = Encode(channelDescribe, header);
            var buffer = new ArraySegment<byte>(data, 0, data.Length);
            m_Time = DateTime.UtcNow;
            await m_Socket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
        }


        private async Task Start(int maxDataItems, int maxMessageRate)
        {
            MessageHeader header = new MessageHeader();
            header.Protocol = (int)Protocols.ChannelStreaming;
            header.MessageType = 0;
            header.MessageId = EtpHelper.NextMessageId;
            header.MessageFlags = 0;
            header.CorrelationId = 0;

            var start = new Start()
            {
                MaxDataItems = maxDataItems,
                MaxMessageRate = maxMessageRate
            };

            var data = Encode(start, header);
            var buffer = new ArraySegment<byte>(data, 0, data.Length);
            await m_Socket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task SendStreamRequest(List<ChannelStreamingInfo> lstChannels)
        {
            if (lstChannels.Count == 0)
                return;

            MessageHeader header = new MessageHeader();
            header.Protocol = (int)Protocols.ChannelStreaming;
            header.MessageType = 4;
            header.MessageId = EtpHelper.NextMessageId;
            header.MessageFlags = 0;
            header.CorrelationId = 0;
            var channelStreamingStart = new ChannelStreamingStart();
            channelStreamingStart.Channels = lstChannels;

            var data = Encode(channelStreamingStart, header);
            var buffer = new ArraySegment<byte>(data, 0, data.Length);
            await m_Socket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task StartStreaming(Settings settings, Dictionary<long, ChannelIndexTypes> channelTypes)
        {
            if (settings.ByIndexCount)
            {
                List<ChannelStreamingInfo> lstTimeChannels = new List<ChannelStreamingInfo>();
                List<ChannelStreamingInfo> lstDepthChannels = new List<ChannelStreamingInfo>();
                foreach (var item in m_ChannelStreamingInfo)
                {
                    item.StartIndex = new StreamingStartIndex()
                    {
                        Item = settings.IndexCount
                    };

                    if(channelTypes[item.ChannelId] == ChannelIndexTypes.Time)
                    {
                        lstTimeChannels.Add(item);
                    }
                    else
                    {
                        lstDepthChannels.Add(item);
                    }
                }

                await SendStreamRequest(lstTimeChannels);
                await SendStreamRequest(lstDepthChannels);
            }

            else
            {
                List<ChannelStreamingInfo> lstChannels = new List<ChannelStreamingInfo>();
                foreach (var item in m_ChannelStreamingInfo)
                {
                    if (settings.BetweenTimeIndex && channelTypes[item.ChannelId] == ChannelIndexTypes.Time)
                    {
                        item.StartIndex = new StreamingStartIndex()
                        {
                            Item = settings.StartTime
                        };
                        lstChannels.Add(item);
                    }
                    else if (settings.BetweenDepthIndex && channelTypes[item.ChannelId] == ChannelIndexTypes.Depth)
                    {
                        item.StartIndex = new StreamingStartIndex()
                        {
                            Item = settings.StartDepth
                        };
                        lstChannels.Add(item);
                    }
                }

                await SendStreamRequest(lstChannels);
            }
        }

        public async Task StopStreaming()
        {
            MessageHeader header = new MessageHeader();
            header.Protocol = (int)Protocols.ChannelStreaming;
            header.MessageType = 5;
            header.MessageId = EtpHelper.NextMessageId;
            header.MessageFlags = 0;
            header.CorrelationId = 0;

            var channelStreamingStop = new ChannelStreamingStop();

            lock (m_ChannelStreamingInfo)
            {
                if (m_ChannelStreamingInfo.Count == 0)
                {
                    return;
                }

                channelStreamingStop.Channels = m_ChannelStreamingInfo.Select(x => x.ChannelId).ToList();

                m_ChannelStreamingInfo.Clear();
            }

            var data = Encode(channelStreamingStop, header);
            var buffer = new ArraySegment<byte>(data, 0, data.Length);
            await m_Socket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
        }


        private async Task CreateSession(List<SupportedProtocol> protocols)
        {
            MessageHeader header = new MessageHeader();
            header.Protocol = (int)Protocols.Core;
            header.MessageType = 1;
            header.MessageId = EtpHelper.NextMessageId;
            header.MessageFlags = 0;
            header.CorrelationId = 0;

            string applicationName = "ShellSquare ETP Client";
            string applicationVersion = "1.4.1.1";

            List<SupportedProtocol> requestedProtocols = new List<SupportedProtocol>();
            requestedProtocols.AddRange(protocols);

            var requestSession = new RequestSession()
            {
                ApplicationName = applicationName,
                ApplicationVersion = applicationVersion,
                RequestedProtocols = requestedProtocols.Cast<SupportedProtocol>().ToList(),
                SupportedObjects = new List<string>()
            };
 
            
            var data = Encode(requestSession, header);
            var buffer = new ArraySegment<byte>(data, 0, data.Length);
            m_Time = DateTime.UtcNow;
            await m_Socket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
        }

        public static SupportedProtocol ToSupportedProtocol(Protocols protocol, string role)
        {
            SupportedProtocol supportedProtocol = new SupportedProtocol()
            {
                ProtocolVersion = new Energistics.Datatypes.Version()
                {
                    Major = 1,
                    Minor = 1,
                    Revision = 0,
                    Patch = 0
                },
                Protocol = (int)protocol,
                Role = role,
                ProtocolCapabilities = new Dictionary<string, DataValue>()
            };

            return supportedProtocol;
        }

        private byte[] Encode<T>(T body, MessageHeader header) where T : ISpecificRecord
        {
            using (var stream = new MemoryStream())
            {
                // create avro binary encoder to write to memory stream
                var headerEncoder = new BinaryEncoder(stream);
                var bodyEncoder = headerEncoder;

                var headerWriter = new SpecificWriter<MessageHeader>(header.Schema);
                headerWriter.Write(header, headerEncoder);

                var bodyWriter = new SpecificWriter<T>(body.Schema);
                bodyWriter.Write(body, bodyEncoder);

                return stream.ToArray();
            }
        }

        private async Task ReceiveData(CancellationToken token)
        {
            using (var stream = new MemoryStream())
            {
                while (m_Socket.State == WebSocketState.Open)
                {
                    var buffer = new ArraySegment<byte>(new byte[BufferSize]);
                    var result = await m_Socket.ReceiveAsync(buffer, token);

                    // transfer received data to MemoryStream
                    stream.Write(buffer.Array, 0, result.Count);

                    // do not process data until EndOfMessage received
                    if (!result.EndOfMessage || result.CloseStatus.HasValue)
                        continue;

                    // filter null bytes from data buffer
                    var bytes = stream.GetBuffer();

                    m_bytes.AddRange(bytes);

                    Decode(m_bytes.ToArray());

                    // Clearing
                    m_bytes.Clear();
                    var bufferc = stream.GetBuffer();
                    Array.Clear(bufferc, 0, bufferc.Length);
                    stream.Position = 0;
                    stream.SetLength(0);
                }
            }
        }

        protected void Decode(byte[] data)
        {
            var receivedTime = DateTime.UtcNow;
            using (var inputStream = new MemoryStream(data))
            {
                // create avro binary decoder to read from memory stream
                var decoder = new BinaryDecoder(inputStream);

                var record = Activator.CreateInstance<MessageHeader>();
                var reader = new SpecificReader<MessageHeader>(new EtpSpecificReader(record.Schema, record.Schema));
                MessageHeader header = reader.Read(record, decoder);


                // string message = Encoding.UTF8.GetString(inputStream.ToArray());

                if (header.Protocol == 0 && header.MessageType == 2)
                {
                    lock (m_ConnectionLock)
                    {
                        m_HasConnected = true;
                    }
                    var recordSession = Activator.CreateInstance<OpenSession>();
                    var readerSession = new SpecificReader<OpenSession>(new EtpSpecificReader(recordSession.Schema, recordSession.Schema));
                    readerSession.Read(recordSession, decoder);
                    string message = ToString(recordSession);
                    var timediff = receivedTime - m_Time;
                    Message?.Invoke(message, timediff.TotalMilliseconds, TraceLevel.Info);
                }
                else if (header.Protocol == 3 && header.MessageType == 2)
                {
                    var responce = Activator.CreateInstance<GetResourcesResponse>();
                    var bodyreader = new SpecificReader<GetResourcesResponse>(new EtpSpecificReader(responce.Schema, responce.Schema));
                    GetResourcesResponse bodyheader = bodyreader.Read(responce, decoder);

                    RequestInformation parent;
                    lock (m_RequestInformation)
                    {
                        parent = m_RequestInformation[header.CorrelationId];
                    }

                    var timediff = receivedTime - parent.RequestTime;
                    string message = ToString(responce);
                    Message?.Invoke(message, timediff.TotalMilliseconds, TraceLevel.Info);

                    if (parent.ChannelItem == null)
                    {
                        ChannelItem channelItem = new ChannelItem()
                        {
                            Name = responce.Resource.Name,
                            Uid = responce.Resource.Uuid,
                            Eml = responce.Resource.Uri,
                            Level = 0,
                            ChildrensCount = responce.Resource.HasChildren
                        };

                        ChannelItemsReceived?.Invoke(channelItem);
                    }
                    else
                    {
                        ChannelItem channelItem = new ChannelItem()
                        {
                            Name = responce.Resource.Name,
                            Uid = responce.Resource.Uuid,
                            Eml = responce.Resource.Uri,
                            Level = parent.ChannelItem.Level + 1,
                            ChildrensCount = responce.Resource.HasChildren
                        };
                        ChannelChildrensReceived?.Invoke(channelItem, parent.ChannelItem);
                    }

                }
                else if (header.Protocol == 1 && header.MessageType == 2)
                {
                    var timediff = receivedTime - m_Time;

                    string message = "Channels received: [";
                    var recordMetadata = Activator.CreateInstance<ChannelMetadata>();
                    var readerMetadata = new SpecificReader<ChannelMetadata>(new EtpSpecificReader(recordMetadata.Schema, recordMetadata.Schema));
                    readerMetadata.Read(recordMetadata, decoder);


                    ChannelMetadata metadata = new ChannelMetadata();
                    metadata.Channels = new List<ChannelMetadataRecord>();
                    lock (m_ChannelStreamingInfo)
                    {
                        foreach (var channel in recordMetadata.Channels)
                        {

                            if (m_LogCurveEml.Contains(channel.ChannelUri, StringComparer.InvariantCultureIgnoreCase))
                            {
                                metadata.Channels.Add(channel);
                                ChannelStreamingInfo channelStreamingInfo = new ChannelStreamingInfo()
                                {
                                    ChannelId = channel.ChannelId,
                                    StartIndex = new StreamingStartIndex()
                                    {
                                        Item = null
                                    },
                                    ReceiveChangeNotification = true
                                };

                                m_ChannelStreamingInfo.Add(channelStreamingInfo);
                                message = message + $"\n{channel.ChannelId} {channel.ChannelName}";

                                ChannelMetaDataVM channelMetaData_VM = ETPMapper.Instance().Map<ChannelMetaDataVM>(channel);
                                string json = JsonConvert.SerializeObject(channelMetaData_VM, Formatting.Indented);
                                Message?.Invoke(json, timediff.TotalMilliseconds, TraceLevel.Info);
                            }
                        }

                        ChannelInfoReceived?.Invoke(metadata);
                    }

                    message = message + "\n]";
                    Message?.Invoke(message, timediff.TotalMilliseconds, TraceLevel.Info);

                    HasDescribing = false;
                }
                else if (header.Protocol == 1 && header.MessageType == 3)
                {
                    var recordData = Activator.CreateInstance<ChannelData>();
                    var readerdata = new SpecificReader<ChannelData>(new EtpSpecificReader(recordData.Schema, recordData.Schema));
                    readerdata.Read(recordData, decoder);

                    ChannelDataReceived?.Invoke(recordData.Data);
                }
                else if (header.MessageType == 1000)
                {
                    var timediff = receivedTime - m_Time;
                    var bodyrecord = Activator.CreateInstance<ProtocolException>();
                    var bodyreader = new SpecificReader<ProtocolException>(new EtpSpecificReader(bodyrecord.Schema, bodyrecord.Schema));
                    ProtocolException bodyheader = bodyreader.Read(bodyrecord, decoder);
                    string message = $"Error Received ({bodyrecord.ErrorCode}): {bodyrecord.ErrorMessage}";

                    Message?.Invoke(message, timediff.TotalMilliseconds, TraceLevel.Error);
                    HasDescribing = false;
                }
                else
                {
                    HasDescribing = false;
                }
            }
        }

        private string ToString(OpenSession os)
        {
            string message = $"Connected to the application : {os.ApplicationName}, \n version : {os.ApplicationVersion},\n SessionId : {os.SessionId}";

            foreach (var s in os.SupportedProtocols)
            {
                message = message + $"\n   Protocol : {s.Protocol}";
          
                foreach (var c in s.ProtocolCapabilities)
                {
                    message = message + $"\n      {c.Key} : {c.Value.Item}";
                }

                message = message + $"\n   Role : {s.Role}\n";

            }

            return message;

        }

        private string ToString(GetResourcesResponse r)
        {
            string message = $"Record name {r.Resource.Name}, uri {r.Resource.Uri}";
            return message;

        }


        public async Task GetRange(Settings settings, Dictionary<long, ChannelIndexTypes> channelTypes)
        {
            List<long> channelIds = new List<long>();
            lock (m_ChannelStreamingInfo)
            {
                foreach (var item in m_ChannelStreamingInfo)
                {
                    if (settings.BetweenTimeIndex && channelTypes[item.ChannelId] == ChannelIndexTypes.Time)
                    {
                        channelIds.Add(item.ChannelId);
                    }
                    else if (settings.BetweenDepthIndex && channelTypes[item.ChannelId] == ChannelIndexTypes.Depth)
                    {
                        channelIds.Add(item.ChannelId);
                    }
                }
            }

            long startIndex;
            long endIndex;
            if (settings.BetweenTimeIndex)
            {
                startIndex = settings.StartTime;
                endIndex = settings.EndTime;

                await MakeRangeRequest(startIndex, endIndex, channelIds);

            }
            else if (settings.BetweenDepthIndex)
            {

                startIndex = settings.StartDepth;
                endIndex = settings.EndDepth;

                await MakeRangeRequest(startIndex, endIndex, channelIds);
            }
        }

        private async Task MakeRangeRequest(long startIndex, long endIndex, List<long> channelIds)
        {
            MessageHeader header = new MessageHeader();
            header.Protocol = (int)Protocols.ChannelStreaming;
            header.MessageType = 9;
            header.MessageId = EtpHelper.NextMessageId;
            header.MessageFlags = 0;
            header.CorrelationId = 0;

            ChannelRangeInfo info = new ChannelRangeInfo()
            {
                ChannelId = channelIds,
                StartIndex = startIndex,
                EndIndex = endIndex
            };

            ChannelRangeRequest requset = new ChannelRangeRequest();

            requset.ChannelRanges = new List<ChannelRangeInfo>();
            requset.ChannelRanges.Add(info);


            var data = Encode(requset, header);
            var buffer = new ArraySegment<byte>(data, 0, data.Length);
            await m_Socket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
        }

    }
}
