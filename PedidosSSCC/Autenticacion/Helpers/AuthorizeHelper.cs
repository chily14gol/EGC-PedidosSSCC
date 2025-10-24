using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace PedidosSSCC.Autenticacion.Helpers
{
    public class AuthorizeHelperAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var authorized = base.AuthorizeCore(httpContext);
            if (!authorized)
            {
                // The user is not authorized => no need to go any further
                // En caso de no ser autorizado, redirecciona al método HandleUnauthorizedRequest
                return false;
            }

            // We have an authenticated user, let's get his username
            //string authenticatedUser = httpContext.User.Identity.Name;

            return true;
        }

        // Función para el manejo de usuario no autorizado previamente por comprobación de rol
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            string username = filterContext.HttpContext.User.Identity.Name;

            if (string.IsNullOrEmpty(username))
            {
                if (!filterContext.IsChildAction)
                {
                    // Redirección a vista no autorizado
                    var routeValues = new RouteValueDictionary(new
                    {
                        controller = "Error",
                        action = "Unauthorized",
                    });

                    filterContext.Result = new RedirectToRouteResult(routeValues);
                }
            }
        }
    }
}