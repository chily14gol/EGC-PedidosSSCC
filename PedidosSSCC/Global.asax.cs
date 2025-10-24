using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace PedidosSSCC
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            string ruta = Server.MapPath("~/Logs/logErrores.txt");
            log4net.GlobalContext.Properties["LogFileName"] = ruta;
            log4net.Config.XmlConfigurator.Configure();
        }

        protected void Application_Error()
        {
            Exception exception = Server.GetLastError();
            Server.ClearError();

            // Evita bucles infinitos si ya estamos en la página de error
            if (HttpContext.Current.Request.Url.AbsolutePath.ToLower().Contains("/shared/error"))
            {
                return;
            }

            var httpException = exception as HttpException;

            if (httpException != null)
            {
                int errorCode = httpException.GetHttpCode();

                if (errorCode == 404)
                {
                    // Si es un error 404, redirige a la página de "No Encontrado"
                    Response.RedirectToRoute(new
                    {
                        controller = "Home",
                        action = "PaginaNoEncontrada",
                        url = HttpUtility.UrlEncode(Request.RawUrl) // Enviar la URL que causó el error
                    });
                    return;
                }
            }

            // Si es otro error, redirige a la página de error genérica
            Response.RedirectToRoute(new
            {
                controller = "Home",
                action = "Error",
                message = exception.Message,
                source = exception.Source,
                path = Request.Url.AbsoluteUri,
                status = Response.StatusCode 
            });
        }

    }
}
