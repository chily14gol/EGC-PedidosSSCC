using System;
using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;

namespace PedidosSSCC.Comun
{
    public static class Sesion
	{
		private static HttpSessionState Session { get { return HttpContext.Current.Session; } }

        #region Datos usuario/persona conectado
        public static int SPersonaId
        {
            get
            {
                if (Session["SPersonaId"] != null)
                    return Convert.ToInt32(Session["SPersonaId"]);
                else 
                    return int.MinValue;
            }
            set
            {
                if (value != int.MinValue)
                    Session["SPersonaId"] = value;
                else 
                    Session.Remove("SPersonaId");
            }
        }
        #endregion

        #region Seguridad
        public static Dictionary<string, bool> SOpcionesAcceso
        {
            get
            {
                if (Session["SOpcionesAcceso"] == null)
                    return new Dictionary<string, bool>();
                else return (Dictionary<string, bool>)Session["SOpcionesAcceso"];
            }
            set
            {

                Session["SOpcionesAcceso"] = value;
            }
        }

        public static List<int> SSeccionesAcceso
        {
            get
            {
                if (Session["SSeccionesAcceso"] == null)
                    return new List<int>();
                else
                    return (List<int>)Session["SSeccionesAcceso"];
            }
            set
            {

                Session["SSeccionesAcceso"] = value;
            }
        }

        public static List<int> SEmpresasAprobador
        {
            get
            {
                if (Session["SEmpresasAprobador"] == null)
                    return new List<int>();
                else return (List<int>)Session["SEmpresasAprobador"];
            }
            set
            {

                Session["SEmpresasAprobador"] = value;
            }
        }

        public static bool SVerTodo
        {
            get
            {
                if (Session["SVerTodo"] != null)
                {
                    return (bool)Session["SVerTodo"];
                }
                return false;
            }
            set
            {
                Session["SVerTodo"] = value;
            }
        }
        #endregion
    }
}
