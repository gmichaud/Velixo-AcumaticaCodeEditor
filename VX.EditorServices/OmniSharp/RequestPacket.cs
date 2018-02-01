using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VX.EditorServices.OmniSharp
{
    public class RequestPacket : Packet
    {
        public string Command { get; set; }

        public object Arguments { get; set; }

        public RequestPacket() : base("request") { }
    }
}
