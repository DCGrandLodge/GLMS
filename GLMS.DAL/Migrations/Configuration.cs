namespace GLMS.DAL.Migrations
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Web.Mvc;
    using GLMS.BLL;
    using GLMS.DAL.Migrations.Seed;

    public sealed class Configuration : DbMigrationsConfiguration<GLMSContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(GLMSContext context)
        {
            DependencyResolver.SetResolver(new ConfigurationDependencyResolver());
            DegreeData.Seed(context);
            UserData.Seed(context);
        }

        private class ConfigurationDependencyResolver : IDependencyResolver
        {
            Dictionary<Type, Func<object>> ResolverMap = new Dictionary<Type, Func<object>>();
            public ConfigurationDependencyResolver()
            {
                ResolverMap.Add(typeof(IEncrypt), () => new DefaultEncryption());
            }

            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This method might throw exceptions whose type we cannot strongly link against; namely, ActivationException from common service locator")]
            public object GetService(Type serviceType)
            {
                // Our resolution
                if (ResolverMap.ContainsKey(serviceType))
                {
                    return ResolverMap[serviceType]();
                }
                // DefaultDependencyResolver resolution

                // Since attempting to create an instance of an interface or an abstract type results in an exception, immediately return null
                // to improve performance and the debugging experience with first-chance exceptions enabled.
                if (serviceType.IsInterface || serviceType.IsAbstract)
                {
                    return null;
                }

                try
                {
                    return Activator.CreateInstance(serviceType);
                }
                catch
                {
                    return null;
                }
            }

            public IEnumerable<object> GetServices(Type serviceType)
            {
                return Enumerable.Empty<object>();
            }
        }
    }

}
