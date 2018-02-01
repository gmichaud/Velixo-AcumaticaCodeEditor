using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VX.EditorServices.OmniSharp
{
    public class StdioServerWrapper : IFileSystemNotifier, IDisposable
    {
        private Guid _customizationProjectId;
        private ProcessStartInfo _startInfo;
        private Process _process;
        private CancellationTokenSource _outputCancellationToken;
        private ConcurrentDictionary<int, ResponsePacket> _queuedResponses;
        private int _requestSeq = 0;

        private string _startFolder;

        public StdioServerWrapper(Guid customizationProjectId, string startFolder)
        {
            _customizationProjectId = customizationProjectId;
            _startFolder = startFolder;
            string omnisharpArgs = $"-s \"{startFolder}\" -hpid {Process.GetCurrentProcess().Id}";
            string omnisharpPath = Path.GetTempPath() + $"OmniSharp {Properties.Resources.OmniSharpVersion}\\OmniSharp.exe";
         
            if(!File.Exists(omnisharpPath))
            {
                ExtractOmniSharpBinaries(Path.GetDirectoryName(omnisharpPath));
            }
            
            _startInfo = new ProcessStartInfo();
            _startInfo.UseShellExecute = false;
            _startInfo.RedirectStandardInput = true;
            _startInfo.RedirectStandardError = true;
            _startInfo.RedirectStandardOutput = true;
            _startInfo.FileName = omnisharpPath;
            _startInfo.Arguments = omnisharpArgs;
        }

        private void ExtractOmniSharpBinaries(string targetPath)
        {
            string tempFile = Path.GetTempFileName();
            File.WriteAllBytes(tempFile, Properties.Resources.OmniSharpBinariesZip);
            if(!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }
            System.IO.Compression.ZipFile.ExtractToDirectory(tempFile, targetPath);
            File.Delete(tempFile);
        }

        public Guid CustomizationProjectId
        {
            get => _customizationProjectId;
        }

        public bool IsActive
        {
            get
            {
                if (_process == null) return false;

                try
                {
                    return  !_process.HasExited;
                }
                catch(System.InvalidOperationException)
                {
                    return false; //Will happen if Start has failed
                }
            }
        }
        
        public void Start()
        {
            if (_process != null) throw new Exception("Process is already initialized.");
            _outputCancellationToken = new CancellationTokenSource();
            _queuedResponses = new ConcurrentDictionary<int, ResponsePacket>();
            LastCommandTimeStamp = DateTime.Now;
            
            _process = new Process();
            _process.EnableRaisingEvents = true;
            _process.StartInfo = _startInfo;
            _process.Exited += ProcessExited;
            _process.Start();
            
            var token = _outputCancellationToken.Token;
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    string json = await _process.StandardOutput.ReadLineAsync();
                    if (String.IsNullOrEmpty(json)) continue; //When process closes, ReadLineAsync() will return null.
                    var packet = Packet.Parse(json);

                    switch (packet.Type)
                    {
                        case "event":
#if DEBUG
                            System.Diagnostics.Trace.WriteLine("VX.EditorServices: Event " + json);
#endif
                            var eventPacket = EventPacket.Parse(json);
                            if(eventPacket.Event == "ProjectAdded")
                            {
                                var wh = GetServerProjectAddedWaitHandle(_process);
                                wh.Set();
                            }
                            break;
                        case "response":
                            var responsePacket = ResponsePacket.Parse(json);

                            //Make response available and notify threads waiting on response for Request_seq
                            if(_queuedResponses.TryAdd(responsePacket.Request_seq, responsePacket))
                            {
                                var wh = GetRequestWaitHandle(_process, responsePacket.Request_seq);
                                wh.Set();
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(false, "Failed to add response to queued responses.");
                            }

                            break;
                        default:
                            System.Diagnostics.Debug.Assert(false, $"Unknown packet type: {packet.Type}");
                            break;
                    }
                }
            }, token);
        }

        private void ProcessExited(object sender, EventArgs e)
        {
            var process = (Process)sender;
            System.Diagnostics.Trace.WriteLine($"VX.EditorServices: OmniSharp process has exited with exit code {process.ExitCode}.");
            _outputCancellationToken.Cancel();
            _process = null;
        }

        public void Notify(string path, FileChangeType type)
        {
            if (!IsActive) return; //Project file will be saved initially before we start server, no need to invoke this yet

            if (string.Equals(Path.GetExtension(path), ".cs", StringComparison.CurrentCultureIgnoreCase) && File.Exists(path))
            {
                SendRequest(new RequestPacket()
                {
                    Seq = GetNextRequestSeq(),
                    Command = "/updateBuffer",
                    Arguments = new { FileName = path, FromDisk = true }
                });
            }

            SendRequest(new RequestPacket()
            {
                Seq = GetNextRequestSeq(),
                Command = "/filesChanged",
                Arguments = new object[] { new { FileName = path, ChangeType = type } }
            });
        }

        public int GetNextRequestSeq()
        {
            lock (this)
            {
                _requestSeq++;
                return _requestSeq;
            }
        }

        public void Stop()
        {
            if (_process != null)
            {
                SendRequest(new RequestPacket()
                {
                    Seq = int.MaxValue,
                    Command = "/stopserver"
                });

                if (!_process.WaitForExit(5000))
                {
                    System.Diagnostics.Trace.WriteLine("VX.EditorServices: Server failed to stop gracefully. Calling Kill().");
                    _process.Kill();
                }

                _outputCancellationToken.Cancel(); //Signal task that's watching StandardOutput that it's time to stop
                _process = null;
            }
        }

        public DateTime LastCommandTimeStamp { get; private set; }

        public void WaitForProjectAddedEvent(TimeSpan timeout)
        {
            var wh = GetServerProjectAddedWaitHandle(_process);
            if (!wh.WaitOne(timeout))
            {
                throw new TimeoutException($"Timeout expired. The timeout period elapsed prior to receiving the ProjectAdded event.");
            }
        }

        public void SendRequest(RequestPacket request)
        {
            LastCommandTimeStamp = DateTime.Now;
            _process.StandardInput.WriteLine(request.ToString());
        }

        public async Task<ResponsePacket> SendRequestAndWaitForResponseAsync(RequestPacket request, TimeSpan timeout)
        {
            SendRequest(request);

            try
            {
                var wh = GetRequestWaitHandle(_process, request.Seq);
                await wh.WaitHandleAsTask(timeout);

                if (_queuedResponses.TryRemove(request.Seq, out var response))
                {
                    return response;
                }
                else
                {
                    throw new Exception("EventWaitHandle was signaled, but response not in queued responses.");
                }
            }
            catch(TaskCanceledException)
            {
                throw new TimeoutException($"Timeout expired. The timeout period elapsed prior to receiving a response for request {request.Seq}.");
            }
        }

        private EventWaitHandle GetRequestWaitHandle(Process process, int sequence)
        {
            return new EventWaitHandle(false, EventResetMode.AutoReset, $"Request_seq${process.Id}${sequence}");
        }

        private EventWaitHandle GetServerProjectAddedWaitHandle(Process process)
        {
            return new EventWaitHandle(false, EventResetMode.AutoReset, $"Started{process.Id}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
                _process.Dispose();
            }
        }
    }
}
