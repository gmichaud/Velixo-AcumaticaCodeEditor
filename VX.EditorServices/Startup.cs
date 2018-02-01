using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;

namespace VX.EditorServices
{
    public class Startup
    {
        private readonly ILifetimeScope _container;
        public const string EditorServicesRouteBase = "editor";

        public Startup(ILifetimeScope container)
        {
            _container = container;
        }

        public void Initialize()
        {
            RouteTable.Routes.Add(new Route($"{EditorServicesRouteBase}", _container.Resolve<EditorServices.RouteHandler>()));

            if(!ServerManager.Initialized)
            {
                ServerManager.Initialize();
            }
        }
    }

}
