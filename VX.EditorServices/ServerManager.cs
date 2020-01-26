using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PX.Data;
using PX.SM;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Xml;
using System.Xml.Linq;
using VX.EditorServices.OmniSharp;

namespace VX.EditorServices
{
    internal class ServerManager
    {
        #region Static variables to manage singleton
        private static object _syncRoot = new object();
        public static bool Initialized { get; private set; }
        public static ServerManager Current { get; private set; }
        #endregion

        private readonly AutoResetEvent _custObjectChanged;
        private ConcurrentDictionary<Guid, StdioServerWrapper> _servers;

        private const int ServerStartWaitTimeout = 10;
        private const int ServerInactivityTimeout = 14400; //For demo, we want it to be fully loaded

        public ServerManager()
        {
            System.Diagnostics.Trace.WriteLine("VX.EditorServices: Initializing ServerManager");
            _custObjectChanged = new AutoResetEvent(false);
            _servers = new ConcurrentDictionary<Guid, StdioServerWrapper>();
            
            PXDatabase.Subscribe(typeof(CustObject), () => _custObjectChanged.Set());
        }

        public static void Initialize()
        {
            lock (_syncRoot)
            {
                if (Initialized) throw new Exception("ServerManager is already initialized.");
                Initialized = true;
            }

            using (new PXImpersonationContext("admin"))
            {
                ServerManager serverManager = new ServerManager();
                Current = serverManager;
                Thread runner = new Thread(serverManager.Watch);
                runner.IsBackground = true;
                runner.Start(System.Security.Principal.WindowsIdentity.GetCurrent());
            }
        }

        public StdioServerWrapper GetServer(Guid customizationProjectId)
        {
            var server = _servers.GetOrAdd(customizationProjectId, g =>
            {
                string startFolder = String.Empty;

                if (g == Guid.Empty)
                {
                    startFolder = CustomizationProjectUtils.GetOmniSharpFilePath("Console");
                }
                else
                {
                    var custProject = (CustProject)PXSelect<CustProject, Where<CustProject.projID, Equal<Required<CustProject.projID>>>>.Select(new PXGraph(), customizationProjectId);
                    if (custProject == null) throw new Exception($"Customization project {customizationProjectId} not found.");
                    startFolder = CustomizationProjectUtils.GetOmniSharpFilePath(custProject.Name);
                }

                return new StdioServerWrapper(customizationProjectId, startFolder);
            });

            if (!server.IsActive)
            {
                lock (server)
                {
                    if (!server.IsActive)
                    {                        
                        CustomizationProjectUtils.SaveCustomizationFilesToFolder(server.CustomizationProjectId, (IFileSystemNotifier) server);
                        
                        server.Start();
                        server.WaitForProjectAddedEvent(TimeSpan.FromSeconds(ServerStartWaitTimeout));
                        System.Diagnostics.Trace.WriteLine("VX.EditorServices: Server started successfully.");
                    }
                }
            }

            return server;
        }

        private void Watch(object identity)
        {
            System.Security.Principal.WindowsImpersonationContext context = ((System.Security.Principal.WindowsIdentity)identity).Impersonate();
            
            try
            {
                while(true)
                {
                    if(this._custObjectChanged.WaitOne(TimeSpan.FromMinutes(1)))
                    {
                        System.Diagnostics.Trace.WriteLine("VX.EditorServices: CustObject Changed!");
                        RescanActiveProjects();
                    }

                    List<Guid> removals = new List<Guid>();
                    foreach (var kv in _servers)
                    {
                        StdioServerWrapper server = kv.Value;
                        if((DateTime.Now - server.LastCommandTimeStamp).TotalSeconds >= ServerInactivityTimeout)
                        {
                            //Potential race condition if we stop the server while a request is received by a different thread. Impact is likely minimal (request failing and editor ignoring it silently)
                            System.Diagnostics.Trace.WriteLine($"VX.EditorServices: Server for project {kv.Key} wil be stopped due to inactivity.");
                            server.Stop();
                            removals.Add(kv.Key);
                        }
                    }

                    foreach(Guid key in removals)
                    {
                        _servers.TryRemove(key, out var server);
                    }
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"VX.EditorServices: Exception in server manager Watch() thread: " + ex.ToString());
            }
            finally
            {
                context.Undo();
            }
        }
        
        private void RescanActiveProjects()
        {
            //We have no way to know which project got updated, so we rescan all of them.
            //We could theoritically use the LastModifiedDateTime in CustObject to scan for modified project IDs,
            //however we would miss file deletions. I don't think this is a big problem for now since the
            //number of active servers should remain low, and they clean-up after inactivity.
            foreach(var kv in _servers)
            {
                if(kv.Value.IsActive)
                { 
                    CustomizationProjectUtils.SaveCustomizationFilesToFolder(kv.Value.CustomizationProjectId, kv.Value);
                }
            }
        }
    }
}
