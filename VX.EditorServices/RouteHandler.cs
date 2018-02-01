using Autofac;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;

namespace VX.EditorServices
{
    class RouteHandler : IRouteHandler
    {
        private readonly ILifetimeScope _container;

        public RouteHandler(ILifetimeScope container)
        {
            _container = container;
        }

        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return _container.Resolve<EditorServices.RequestHandler>();
        }
    }
}
