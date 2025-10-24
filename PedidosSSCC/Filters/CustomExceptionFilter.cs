using PedidosSSCC.Controllers;
using System.Configuration;
using System.Web;
using System.Web.Mvc;

namespace PedidosSSCC.Filters
{
    public class CustomExceptionFilter : FilterAttribute, IExceptionFilter
    {
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(ConfigurationManager.AppSettings["NOMBRE_LOGGER"]);
        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled) 
                return;

            var exception = filterContext.Exception;
            filterContext.ExceptionHandled = true;

            // Verificar si la excepción es un HttpException y si el código de estado es 404
            if (exception is HttpException httpException && httpException.GetHttpCode() == 404)
            {
                filterContext.Result = new RedirectToRouteResult(new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Home" },
                        { "action", "PaginaNoEncontrada" }, // Redirigir a la vista 404
                        { "url", filterContext.HttpContext.Request.Url.AbsoluteUri } // Pasar la URL que causó el error
                    });

                return;
            }

            // Detectar si la petición es AJAX
            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                filterContext.Result = new JsonResult
                {
                    Data = new { 
                        success = false, 
                        error_servidor = true, 
                        message = exception.Message, 
                        status = filterContext.HttpContext.Response.StatusCode 
                    },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            else
            {
                string errorDetalle = $"{filterContext.HttpContext.Request.Url.AbsoluteUri} | {exception.Message}";
                log.Error(errorDetalle);

                // Si NO es AJAX, redirigir a la página de error
                filterContext.Result = new RedirectToRouteResult(new System.Web.Routing.RouteValueDictionary
                {
                    { "controller", "Home" },
                    { "action", "Error" },
                    { "message", exception.Message },
                    { "source", exception.Source },
                    { "path", filterContext.HttpContext.Request.Url.AbsoluteUri },
                    { "status", filterContext.HttpContext.Response.StatusCode }
                });
            }
        }
    }
}
