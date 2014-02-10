using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructureMap;

namespace GLMS.Website.Services
{
    public class SMDependencyResolver : 
        System.Web.Mvc.IDependencyResolver,               // For MVC
        System.Web.Http.Dependencies.IDependencyResolver  // For Web API
    {
        private readonly IContainer _container;

        public SMDependencyResolver(IContainer container)
        {
            _container = container;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == null) return null;
            try
            {
                return serviceType.IsAbstract || serviceType.IsInterface
                         ? _container.TryGetInstance(serviceType)
                         : _container.GetInstance(serviceType);
            }
            catch
            {

                return null;
            }
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _container.GetAllInstances(serviceType).Cast<object>();
        }

        System.Web.Http.Dependencies.IDependencyScope System.Web.Http.Dependencies.IDependencyResolver.BeginScope()
        {
            return this;
        }

        void IDisposable.Dispose()
        {
            // When BeginScope returns 'this', the Dispose method must be a no-op
        }
    }
}
