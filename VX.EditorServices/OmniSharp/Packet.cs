using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VX.EditorServices.OmniSharp
{
    public class Packet
    {
        private readonly string _type;

        public Packet(string type)
        {
            _type = type;
        }

        public static Packet Parse(string json)
        {
            var obj = JObject.Parse(json);
            var result = obj.ToObject<Packet>();

            if (result.Seq <= 0)
            {
                throw new ArgumentException("invalid seq value");
            }
            
            return result;
        }

        public int Seq { get; set; }

        public string Type { get { return _type; } }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
