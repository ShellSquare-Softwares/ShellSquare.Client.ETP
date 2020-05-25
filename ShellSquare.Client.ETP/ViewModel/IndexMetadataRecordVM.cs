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
    public class IndexMetadataRecordVM
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ChannelIndexTypes IndexType { get; set; }
        public string Uom { get; set; }
        public string DepthDatum { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public IndexDirections Direction { get; set; }
        public string Mnemonic { get; set; }
        public string Description { get; set; }
        public string Uri { get; set; }
        public int Scale { get; set; }
        public string TimeDatum { get; set; }
    }
}
