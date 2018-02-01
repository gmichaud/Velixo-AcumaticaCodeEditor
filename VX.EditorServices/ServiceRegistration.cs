using Autofac;
using PX.Data.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using PX.Export.Authentication;

namespace VX.EditorServices
{
    public class ServiceRegistration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RouteHandler>().SingleInstance();
            builder.RegisterType<RequestHandler>().SingleInstance();
            builder.ActivateOnApplicationStart<Startup>(e => e.Initialize());

            builder
                .RegisterInstance(new LocationSettings
                {
                    Path = "/" + Startup.EditorServicesRouteBase,
                    Providers =
                    {
                        new ProviderSettings
                        {
                            Name = "coockie",
                            Type = typeof (CoockieAuthenticationModule).AssemblyQualifiedName
                        },
                        new ProviderSettings
                        {
                            Name = "anonymous",
                            Type = typeof (AnonymousAuthenticationModule).AssemblyQualifiedName
                        }
                    }
                });
        }
    }
}
