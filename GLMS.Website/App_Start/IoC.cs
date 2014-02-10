using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using GLMS.BLL;
using GLMS.DAL;
using GLMS.Mail;
using GLMS.Website.Services;
using StructureMap;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(GLMS.Website.StructuremapMvc), "Start")]

namespace GLMS.Website
{
    public static class IoC
    {
        public static IContainer Initialize()
        {
            ObjectFactory.Initialize(x =>
            {
                x.Scan(scan =>
                {
                    scan.TheCallingAssembly();
                    scan.WithDefaultConventions();
                });
                x.For<IGLMSContext>().HybridHttpOrThreadLocalScoped().Use(() => new GLMSContext());
                x.For<String>().Use(CurrentUser.SessionKey).Named("CurrentUserKey");
                x.For<CurrentUser>().Use(() => CurrentUser.User);
                x.For<IEncrypt>().HybridHttpOrThreadLocalScoped().Use<DefaultEncryption>();
                // TODO - Change to MailQueuingService
                x.For<IMailService>().HybridHttpOrThreadLocalScoped().Use<MailDeliveryService>();
            });
            using (var context = new GLMSContext())
            {
                // TODO - Load configuration
                //Configuration.LoadConfiguration(context);
            }
            return ObjectFactory.Container;
        }
        public static T GetInstance<T>() where T : class
        {
            return ObjectFactory.GetInstance<T>();
        }
        public static T GetNamedInstance<T>(string name) where T : class
        {
            return ObjectFactory.GetNamedInstance<T>(name);
        }

        public static void ReleaseObjects()
        {
            ObjectFactory.ReleaseAndDisposeAllHttpScopedObjects();
        }
    }

    public static class StructuremapMvc
    {
        public static void Start()
        {
            GLMSContext.Migrate();
            var depResolver = new SMDependencyResolver((IContainer)IoC.Initialize());
            System.Web.Mvc.DependencyResolver.SetResolver(depResolver);
            GlobalConfiguration.Configuration.DependencyResolver = depResolver;
        }
    }
}