using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VX.EditorServices.OmniSharp
{
    interface IFileSystemNotifier
    {
        void Notify(string path, FileChangeType type);
    }
}
