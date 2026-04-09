using DAL;
using EmailHandling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Wikimedia
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            var culture = new CultureInfo("fr-FR");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            Clean_Data();
        }

        // Avoid null references caused by bad foreign keys
        //
        // this method should be removed before final release
        //
        private void Clean_Data()
        {
            Debug.Write("Start cleaning data");
            foreach (var login in DB.Logins.ToList().Copy())
            {
                if (login.User == null) DB.Logins.Delete(login.Id);
            }
            foreach (var uvEmail in DB.UnverifiedEmails.ToList().Copy())
            {
                if (uvEmail.User == null) DB.UnverifiedEmails.Delete(uvEmail.Id);
            }
            foreach (RenewPasswordCommand renewPC in DB.RenewPasswordCommands.ToList().Copy())
            {
                if (renewPC.User == null) DB.RenewPasswordCommands.Delete(renewPC.Id);
            }
            foreach (Models.Event @event in DB.Events.ToList().Copy())
            {
                if (@event.User == null) DB.RenewPasswordCommands.Delete(@event.Id);
            }
            foreach (var notification in DB.Notifications.ToList().Copy())
            {
                if (notification.User == null || notification.User == null) DB.Notifications.Delete(notification.Id);
            }
            foreach (Models.Media media in DB.Medias.ToList().Copy())
            {
                if (media.Owner == null) DB.Medias.Delete(media.Id);
            }
            Debug.Write("End of data cleaning");
        }
        protected void Session_Start()
        {
            // do session intialisations

        }
        protected void Session_End()
        {
            var connectedUser = Models.User.ConnectedUser;
            if (connectedUser != null)
                connectedUser.Online = false;
        }
    }
}
