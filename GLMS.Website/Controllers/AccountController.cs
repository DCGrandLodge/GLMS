using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DotNetOpenAuth.AspNet;
using Microsoft.Web.WebPages.OAuth;
using WebMatrix.WebData;
using GLMS.Website.Filters;
using GLMS.Website.Models;
using GLMS.BLL;
using GLMS.BLL.Entities;
using GLMS.Mail;
using System.Web.Routing;
using System.Collections.Specialized;
using GLMS.MVC.Extensions;

namespace GLMS.Website.Controllers
{
    [Authorize]
    public class AccountController : GLMSController
    {

        public AccountController(IGLMSContext context) : base(context) { }

        #region Login / Logoff
        //
        // GET: /Account/Login

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            var loginResult = CheckLogin(model);
            switch (loginResult)
            {
                case LoginResult.Success:
                    return LoginRedirect();
                case LoginResult.TempPasswordExpired:
                    ModelState.AddModelError("", "Your temporary password has expired; a new temporary password has been generated and sent to you.");
                    break;
                case LoginResult.ChangePassword:
                    TempData["__ErrorMessage"] = "You must change your password";
                    return RedirectToAction("ChangePassword", new { id = context.Users.Where(x => x.Username == model.UserName).Select(x => x.UserID).FirstOrDefault() });
                case LoginResult.Inactive:
                    ModelState.AddModelError("", "Your account is inactive.");
                    break;
                default:
                    ModelState.AddModelError("", "The Email Address or Password provided is incorrect.");
                    break;
            }
            return View(model);
        }

        //
        // POST: /Account/LogOff

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            CurrentUser.SignOut();
            return Home();
        }
        #endregion

        #region Change Password
        [AllowAnonymous]
        public ActionResult RequestPasswordReset(string username)
        {
            var account = context.Users.FirstOrDefault(x => x.Username == username);
            if (account == null)
            {
                return Json("An account with that usernamecould not be found.", JsonRequestBehavior.AllowGet);
            }
            else
            {
                try
                {
                    GenerateTemporaryPassword(account);
                    return Json("An email has been sent with a temporary password.", JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    return Json(String.Format("Error {0} occured while trying to generate a temporary password.", ex.Log()), JsonRequestBehavior.AllowGet);
                }
            }
        }

        [AllowAnonymous]
        public ActionResult ChangePassword(Guid id)
        {
            var model = new ChangePasswordModel() { UserID = id };
            if (CurrentUser.IsAuthenticated)
            {
                model.UserID = CurrentUser.UserID;
            }
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (CurrentUser.IsAuthenticated)
            {
                model.UserID = CurrentUser.UserID;
            }
            if (ModelState.IsValid)
            {
                User user = context.Users.FirstOrDefault(x => x.UserID == model.UserID);
                if (user == null)
                {
                    TempData["__ErrorMessage"] = "Account could not be found.";
                    if (CurrentUser.IsAuthenticated)
                    {
                        return Home();
                    }
                    else
                    {
                        return RedirectToAction("Login");
                    }
                }
                else
                {
                    if (model.OldPassword.CompareToEncrypted(user.Password.Encrypted) ||
                        model.OldPassword.CompareToEncrypted(user.Password.Temporary))
                    {
                        user.Password.PlainTextPassword = model.Password;
                        user.Password.Temporary = null;
                        user.Password.TempExpiration = null;
                        user.Password.ForceChange = false;
                        context.SaveChanges();
                        if (CurrentUser.IsAuthenticated)
                        {
                            TempData["__Success"] = "Password changed";
                            return Home();
                        }
                        else
                        {
                            TempData["__Success"] = "Password changed.  Please log in using your new password.";
                            return RedirectToAction("Login");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Password", "The password you entered does not match your current password.");
                    }
                }
            }
            return View(model);
        }
        #endregion

        #region Helpers
        private enum LoginResult { Failure, Success, Inactive, ChangePassword, TempPasswordExpired }
        private LoginResult CheckLogin(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                User user = context.Users
                    .Where(x => x.Username == model.UserName)
                    .FirstOrDefault();
                if (user != null)
                {
                    if (model.Password.CompareToEncrypted(user.Password.Encrypted) || 
                        model.Password.CompareToEncrypted(user.Password.Temporary))
                    {
                        if (!user.Active)
                        {
                            return LoginResult.Inactive;
                        }
                        user.LastLogin = DateTime.Now;
                        if (model.Password.CompareToEncrypted(user.Password.Temporary))
                        {
                            if (user.Password.TempExpiration < DateTime.Now)
                            {
                                GenerateTemporaryPassword(user);
                                return LoginResult.TempPasswordExpired;
                            }
                            user.Password.ForceChange = true;
                        }
                        else if (user.Password.Temporary != null)
                        {
                            user.Password.Temporary = null;
                            user.Password.TempExpiration = null;
                        }
                        context.SaveChanges();
                        if (user.Password.ForceChange)
                        {
                            return LoginResult.ChangePassword;
                        }
                        CurrentUser.SignIn(user);
                        return LoginResult.Success;
                    }
                }
            }
            return LoginResult.Failure;
        }

        private ActionResult LoginRedirect()
        {
            object oRouteValues = Session["LoginRedirect.RouteValues"];
            Session.Remove("LoginRedirect.RouteValues");
            if (oRouteValues != null)
            {
                RouteValueDictionary routeValues = new RouteValueDictionary((IDictionary<string, object>)oRouteValues);
                NameValueCollection queryString = (NameValueCollection)Session["LoginRedirect.QueryString"];
                Session.Remove("LoginRedirect.QueryString");
                foreach (string key in queryString.AllKeys.Where(x => !String.IsNullOrWhiteSpace(x)))
                {
                    routeValues.Add(key, queryString[key]);
                }
                return new RedirectToRouteResult(routeValues);
            }
            else
            {
                return Home();
            }
        }

        private void GenerateTemporaryPassword(User user)
        {
            var password = Password.CreateRandomPassword(8);
            user.Password.PlainTextTemporary = password;
            context.SaveChanges();
            var mailService = IoC.GetInstance<IMailService>();
            // TODO - Send Email
        }

        #endregion
    }
}
