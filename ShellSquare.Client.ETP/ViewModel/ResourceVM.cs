using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellSquare.Client.ETP.ViewModel
{
    public class ResourceVM
    {
        public string Uri { get; set; }
        public string ContentType { get; set; }
        public string Name { get; set; }
        public bool ChannelSubscribable { get; set; }
        public string ResourceType { get; set; }
        public int HasChildren { get; set; }
        public string Uuid { get; set; }
        public long LastChanged { get; set; }
        public bool ObjectNotifiable { get; set; }
    }
}
