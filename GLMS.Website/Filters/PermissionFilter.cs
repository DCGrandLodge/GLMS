using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using System.Web.Mvc;
using System.Web.Routing;
using GLMS.MVC.Extensions;
using GLMS.BLL.Entities;

namespace GLMS.Website.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public abstract class GLMSActionFilterAttribute : ActionFilterAttribute
    {
        public string FailArea { get; set; }
        public string FailController { get; set; }
        public string FailAction { get; set; }
        public AjaxRedirectReason FailReason { get; set; }
        public RedirectTarget RedirectTarget { get; set; }

        public GLMSActionFilterAttribute()
        {
        }

        protected void Redirect(ActionExecutingContext filterContext)
        {
            Redirect(filterContext, RedirectTarget);
        }

        protected void Redirect(ActionExecutingContext filterContext, RedirectTarget overrideTarget)
        {
            filterContext.Result = new RedirectException()
            {
                FailAction = FailAction,
                FailController = FailController,
                FailArea = FailArea,
                FailReason = FailReason,
                Target = overrideTarget
            }.GetRedirect(filterContext.RequestContext);
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AuthenticatedPermissionFilterAttribute : GLMSActionFilterAttribute
    {
        public AuthenticatedPermissionFilterAttribute()
        {
            RedirectTarget = RedirectTarget.LogOn;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            CurrentUser currentUser = DependencyResolver.Current.GetService<CurrentUser>();
            if (currentUser == null)
            {
                Redirect(filterContext);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class AccessLevelFilterAttribute : GLMSActionFilterAttribute
    {
        public AccessLevel AccessLevel { get; set; }
        [DefaultValue(null)]
        public string NoPermissionView { get; set; }

        public AccessLevelFilterAttribute(AccessLevel AccessLevel)
        {
            this.AccessLevel = AccessLevel;
            RedirectTarget = RedirectTarget.NoPermission;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            CurrentUser currentUser = DependencyResolver.Current.GetService<CurrentUser>();
            if (currentUser == null)
            {
                Redirect(filterContext, RedirectTarget.LogOn);
            }
            else if (!currentUser.hasPermission(AccessLevel))
            {
                NoPermission(filterContext);
            }
        }

        private void NoPermission(ActionExecutingContext filterContext)
        {
            if (String.IsNullOrWhiteSpace(NoPermissionView))
            {
                Redirect(filterContext);
            }
            else
            {
                filterContext.Result = new ViewResult()
                {
                    MasterName = "",
                    ViewName = NoPermissionView
                };
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PublicPermissionFilterAttribute : GLMSActionFilterAttribute
    {
        [DefaultValue(true)]
        public bool AllowAuthenticated { get; set; }

        public PublicPermissionFilterAttribute()
        {
            RedirectTarget = RedirectTarget.Home;
            AllowAuthenticated = true;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!AllowAuthenticated)
            {
                CurrentUser currentUser = DependencyResolver.Current.GetService<CurrentUser>();
                // Logged in, attempt to access anonymous-only page (e.g. Forgot Password)
                if (currentUser != null)
                {
                    Redirect(filterContext);
                }
            }
        }
    }
}