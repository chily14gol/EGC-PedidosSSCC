using PedidosSSCC.Filters;
using System.Web;
using System.Web.Mvc;

namespace PedidosSSCC
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            //filters.Add(new HandleErrorAttribute()); // Manejo estándar de errores
            filters.Add(new CustomExceptionFilter()); // Agrega el nuevo filtro de excepciones
        }
    }
}
