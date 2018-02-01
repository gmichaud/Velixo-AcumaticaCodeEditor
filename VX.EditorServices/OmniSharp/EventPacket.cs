using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VX.EditorServices.OmniSharp
{
    public class EventPacket : Packet
    {
        public string Event { get; set; }

        public MemoryStream BodyStream { get; set; }

        public EventPacket() : base("event")
        {

        }

        public static new EventPacket Parse(string json)
        {
            var obj = JObject.Parse(json);
            var result = obj.ToObject<EventPacket>();

            if (result.Seq <= 0)
            {
                throw new ArgumentException("invalid seq value");
            }

            if (obj.TryGetValue("body", StringComparison.OrdinalIgnoreCase, out var body))
            {
                result.BodyStream = new MemoryStream(Encoding.UTF8.GetBytes(body.ToString()));
            }
            else
            {
                result.BodyStream = null;
            }

            return result;
        }
    }
}
