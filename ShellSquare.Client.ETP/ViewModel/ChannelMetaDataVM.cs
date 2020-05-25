using Energistics.Datatypes.ChannelData;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellSquare.Client.ETP.ViewModel
{
    public class ChannelMetaDataVM
    {
        public string Uuid { get; set; }
        public string MeasureClass { get; set; }
        public string Source { get; set; }
        public string ContentType { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ChannelStatuses Status { get; set; }
        public string Description { get; set; }
        public IndexData_VM StartIndex { get; set; }
        public IndexData_VM EndIndex { get; set; }
        public string Uom { get; set; }
        public string DataType { get; set; }
        public string ChannelName { get; set; }
        public IList<IndexMetadataRecordVM> Indexes { get; set; }
        public long ChannelId { get; set; }
        public string ChannelUri { get; set; }
        public DataObjectVM DomainObject { get; set; }
    }
}
