using KZ.Data;
using System.Collections.Generic;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

namespace KZ
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
        protected void Application_PostAuthorizeRequest()
        {
            // Anksčiau visais atvejais buvo "Required", bet buvo bėdų vienu metu kreipiantis į keletą servisų...
            // Lyg ir vienas po kito servisų funkcionalumai įvykdavo?
            // Susiję su: https://stackoverflow.com/questions/26736034/why-is-this-web-api-controller-not-concurrent/26737148
            // https://stackoverflow.com/questions/43813889/writing-to-a-session-variable-with-sessionstatebehavior-readonly/43957350
            string path = HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath;
            if (path.StartsWith("~/web-services/"))
            {
                if (path.Equals("~/web-services/user/login") || path.Equals("~/web-services/user/logout"))
                {
                    HttpContext.Current.SetSessionStateBehavior(System.Web.SessionState.SessionStateBehavior.Required);
                }
                else
                {
                    HttpContext.Current.SetSessionStateBehavior(System.Web.SessionState.SessionStateBehavior.ReadOnly);
                }
            }
        }
        protected void Application_AcquireRequestState()
        {
            IUserRepository userRepository = new UserRepository();
            if (HttpContext.Current.Session != null)
            {
                if (HttpContext.Current.Session["name"] != null && HttpContext.Current.Session["role"] != null && HttpContext.Current.Session["permissions"] != null)
                {
                    string email = null;
                    if (HttpContext.Current.Session["email"] != null)
                    {
                        email = HttpContext.Current.Session["email"].ToString();
                    }
                    HttpContext.Current.User = new CustomPrincipal(
                        HttpContext.Current.Session["name"].ToString(),
                        HttpContext.Current.Session["role"].ToString(),
                        (List<string>)HttpContext.Current.Session["permissions"],
                        email,
                        userRepository
                    );
                }
            }
            HttpContext.Current.Items["userRepository"] = userRepository; // FIXME! Kažin ar toks būdas geras perduoti info? Gal naudoti `Unity` ir `Dependency Injection`?
        }
    }
}