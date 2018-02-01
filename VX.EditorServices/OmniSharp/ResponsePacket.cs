using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VX.EditorServices.OmniSharp
{
    public class ResponsePacket : Packet
    {
        public int Request_seq { get; set; }

        public string Command { get; set; }

        public bool Running { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }
        
        public MemoryStream BodyStream { get; set; }

        public ResponsePacket() : base("response")
        {
            
        }


        public static new ResponsePacket Parse(string json)
        {
            var obj = JObject.Parse(json);
            var result = obj.ToObject<ResponsePacket>();

            if (result.Seq <= 0)
            {
                throw new ArgumentException("invalid seq value");
            }

            if (result.Request_seq <= 0)
            {
                throw new ArgumentException("invalid request_seq value");
            }

            if (string.IsNullOrWhiteSpace(result.Command))
            {
                throw new ArgumentException("missing command");
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
