using Autofac;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using VX.EditorServices.OmniSharp;

namespace VX.EditorServices
{
    class RequestHandler : HttpTaskAsyncHandler
    {
        private readonly ILifetimeScope _container;

        private const int ResponseWaitTimeout = 10;

        public RequestHandler(ILifetimeScope container)
        {
            _container = container;
        }

        public override bool IsReusable => true;

        public override async Task ProcessRequestAsync(HttpContext context)
        {
            bool isAcuShell = false;
            Guid workingProject;
            if(!Guid.TryParse(context.Request.Params["p"], out workingProject))
            {
                workingProject = Guid.Empty; //Acumatica Console
                isAcuShell = true;
            }

            var server = ServerManager.Current.GetServer(workingProject);

            var requestPacket = new RequestPacket();
            requestPacket.Seq = server.GetNextRequestSeq();
            requestPacket.Command = "/" + context.Request.Params["c"];
            using (var sr = new StreamReader(context.Request.InputStream))
            {
                var parsedJson = JObject.Parse(await sr.ReadToEndAsync());
    
                if (isAcuShell)
                {
                    ProcessRequestForAcuShell(parsedJson);
                }

                requestPacket.Arguments = parsedJson;
            }
            
            var responsePacket = await server.SendRequestAndWaitForResponseAsync(requestPacket, TimeSpan.FromSeconds(ResponseWaitTimeout));

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.Charset = "utf-8";

            string acceptEncoding = context.Request.Headers["Accept-Encoding"];
            if (!string.IsNullOrEmpty(acceptEncoding) && acceptEncoding.Contains("gzip"))
            {
                context.Response.AppendHeader("Content-encoding", "gzip");

                using (GZipStream gZipStream = new GZipStream(context.Response.OutputStream, CompressionMode.Compress, true))
                    await responsePacket.BodyStream.CopyToAsync(gZipStream);
            }
            else
            {
                await responsePacket.BodyStream.CopyToAsync(context.Response.OutputStream);
            }

            context.Response.Flush();
            context.Response.Close();
            context.Response.End();
        }

        private void ProcessRequestForAcuShell(JObject parsedJson)
        {
            parsedJson["FileName"] = Path.Combine(CustomizationProjectUtils.GetOmniSharpFilePath(CustomizationProjectUtils.AcuShellOmniSharpProjectName), CustomizationProjectUtils.AcuShellOmniSharpFileName);
            var template = @"using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Linq;
                using PX.Data;
                using PX.Common;
                using PX.Objects.GL;
                using PX.Objects.CM;
                using PX.Objects.CS;
                using PX.Objects.CR;
                using PX.Objects.TX;
                using PX.Objects.IN;
                using PX.Objects.EP;
                using PX.Objects.AP;
                using PX.TM;
                using PX.Objects;
                using PX.Objects.PO;
                using PX.Objects.SO;

                namespace PX.Objects
                {
                    public class Console
                    {
                        public void Method()
                        {
                            var Graph = new {GraphType}();
{Buffer} //Do not intend, and adjust line number in the request if this is moved.
                        }
                    }
                }";

            template = template.Replace("{GraphType}", (string) parsedJson["GraphType"]);
            template = template.Replace("{Buffer}", (string) parsedJson["Buffer"]);

            parsedJson["Buffer"] = template;
            parsedJson["Line"] = 27; 
        }
    }
}
