using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VX.EditorServices.OmniSharp
{
    internal class CustObjectTimestamp
    {
        public DateTime? LastModifiedDateTime;
        public Dictionary<string, bool> Files = new Dictionary<string, bool>();
    }
}
