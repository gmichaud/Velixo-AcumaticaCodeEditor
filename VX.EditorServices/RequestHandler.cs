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
            Guid workingProject;
            if(!Guid.TryParse(context.Request.Params["p"], out workingProject))
            {
                throw new Exception("Project Guid invalid or missing.");
            }

            var server = ServerManager.Current.GetServer(workingProject);

            var requestPacket = new RequestPacket();
            requestPacket.Seq = server.GetNextRequestSeq();
            requestPacket.Command = "/" + context.Request.Params["c"];
            using (var sr = new StreamReader(context.Request.InputStream))
            {
                var json = await sr.ReadToEndAsync();
                requestPacket.Arguments = JObject.Parse(json);
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
    }
}
