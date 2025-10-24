using AccesoDatos;
using Comun;
using Newtonsoft.Json;
using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Caching;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;

namespace PedidosSSCC.Controllers
{
    public static class Sesion
    {
        private static HttpSessionState Session { get { return HttpContext.Current.Session; } }

        #region Datos usuario/persona conectado
        public static string SUsuarioId
        {
            get
            {
                if (Session["SUsuarioId"] != null)
                {
                    return Session["SUsuarioId"].ToString();
                }
                return String.Empty;
            }
            set
            {
                if (value != String.Empty)
                    Session["SUsuarioId"] = value;
                else Session.Remove("SUsuarioId");
            }
        }

        public static int SPersonaId
        {
            get
            {
                if (Session["SPersonaId"] != null)
                    return Convert.ToInt32(Session["SPersonaId"]);
                else return int.MinValue;
            }
            set
            {
                if (value != int.MinValue)
                    Session["SPersonaId"] = value;
                else Session.Remove("SPersonaId");
            }
        }

        public static string SNombrePersona
        {
            get
            {
                if (Session["SNombrePersona"] != null)
                {
                    return Session["SNombrePersona"].ToString();
                }
                return String.Empty;
            }
            set
            {
                if (value != String.Empty)
                    Session["SNombrePersona"] = value;
                else Session.Remove("SNombrePersona");
            }
        }
        #endregion

        #region Seguridad
        public static bool SUsuarioIdentificado
        {
            get
            {
                if (Session["SUsuarioIdentificado"] != null)
                {
                    return (bool)Session["SUsuarioIdentificado"];
                }
                return false;
            }
            set
            {
                Session["SUsuarioIdentificado"] = value;
            }
        }

        public static Dictionary<string, bool> SOpcionesAcceso
        {
            get
            {
                if (Session["SOpcionesAcceso"] == null)
                    return new Dictionary<string, bool>();
                else
                    return (Dictionary<string, bool>)Session["SOpcionesAcceso"];
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
                else
                    return (List<int>)Session["SEmpresasAprobador"];
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

        public static int SDepartamentoId
        {
            get
            {
                if (Session["SDepartamentoId"] != null)
                    return Convert.ToInt32(Session["SDepartamentoId"]);
                else return int.MinValue;
            }
            set
            {
                if (value != int.MinValue)
                    Session["SDepartamentoId"] = value;
                else Session.Remove("SDepartamentoId");
            }
        }
    }

    public class BaseController : Controller
    {
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(ConfigurationManager.AppSettings["NOMBRE_LOGGER"]);

        public BaseController()
        {
            ViewBag.TiempoInactividad = Convert.ToInt32(ConfigurationManager.AppSettings["TiempoInactividadMinutos"]);
        }

        public void CargarDatosUsuario(string userName)
        {
            Sesion.SUsuarioIdentificado = true;

#if DEBUG
            //userName = @"PRINCIPAL\eleon";
#endif
            Usuarios objUsuario = DAL_Usuarios.ObtenerUsuario(userName);

            if (objUsuario != null)
            {
                Sesion.SUsuarioId = objUsuario.USU_Id;
                Sesion.SPersonaId = objUsuario.USU_PER_Id;
                Sesion.SNombrePersona = objUsuario.NombrePersona;
                Sesion.SOpcionesAcceso = objUsuario.OpcionesAcceso;
                Sesion.SVerTodo = objUsuario.USU_VerTodo;
                Sesion.SDepartamentoId = objUsuario.PersonaUsuario.PER_DEP_Id.HasValue ? objUsuario.PersonaUsuario.PER_DEP_Id.Value : 0;

                if (objUsuario.USU_VerTodo)
                {
                    //Obtenemos todas las secciones
                    DAL_Secciones dalSEC = new DAL_Secciones();
                    Sesion.SSeccionesAcceso = dalSEC.L(sinFiltrar: true).Select(r => r.SEC_Id).ToList();
                }
                else
                {
                    //Obtenemos las secciones del trabajador
                    DAL_Personas dalPER = new DAL_Personas();
                    Sesion.SSeccionesAcceso = dalPER.ObtenerSeccionesTrabajador(objUsuario.USU_PER_Id);
                }

                //Obtenemos las empresas de las que es Aprobador
                DAL_Empresas_Aprobadores dalEmpApr = new DAL_Empresas_Aprobadores();
                dalEmpApr.ModoConsultaClaveExterna = DAL_Empresas_Aprobadores.TipoClaveExterna.Persona;
                Sesion.SEmpresasAprobador = dalEmpApr.L_ClaveExterna(objUsuario.USU_PER_Id).Select(r => r.EMA_EMP_Id).ToList();

                //Obtenemos las empresas de las que es Responsable (EMP_PER_Id_AprobadorDefault)
                DAL_Empresas dal = new DAL_Empresas();
                Expression<Func<Empresas, bool>> filtroResponsable = t => t.EMP_PER_Id_AprobadorDefault == objUsuario.USU_PER_Id;
                Sesion.SEmpresasAprobador.AddRange(dal.L(false, filtroResponsable).Select(r => r.EMP_Id).ToList());
            }
        }

        public static ResultadoEnvioMail EnviarMail_SolicitudConcepto(Tareas_Empresas_LineasEsfuerzo objConcepto, string emailAprobador, string nombreSolicitante)
        {
            try
            {
                bool emailOK = false;

                string mailEmisor = ConfigurationManager.AppSettings["EmailEmisor"];
                string nombreEmisor = ConfigurationManager.AppSettings["EmailEmisorAlias"];
                string asuntoMail = Resources.Resource.msgAsuntoMailSolicitudLineaEsfuerzo;
                string mensajeMail = String.Empty;

                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string urlPlantilla = Path.Combine(basePath, "Plantillas", "NotificacionSolicitudLineaEsfuerzo.html");

                if (!string.IsNullOrEmpty(mailEmisor) && !string.IsNullOrEmpty(emailAprobador))
                {
                    mensajeMail = ObtenerTextoPlantilla(urlPlantilla);
                    mensajeMail = mensajeMail.Replace("[Tarea]", objConcepto.TareaNombre);
                    mensajeMail = mensajeMail.Replace("[Empresa]", objConcepto.EmpresaNombre);
                    mensajeMail = mensajeMail.Replace("[Cantidad]", objConcepto.CantidadNombre);
                    mensajeMail = mensajeMail.Replace("[ImporteTotal]", objConcepto.ImporteTotal.ToEuro());
                    mensajeMail = mensajeMail.Replace("[Descripcion]", System.Web.HttpUtility.HtmlEncode(objConcepto.TLE_Descripcion));
                    mensajeMail = mensajeMail.Replace("[Solicitante]", nombreSolicitante);
                    mensajeMail = mensajeMail.Replace("[FechaSolicitud]", objConcepto.FechaModificacion.ToString("dd/MM/yyyy"));

                    mensajeMail = mensajeMail.Replace("[URL_Aplicacion]", ConfigurationManager.AppSettings["PUBLIC_SITE_URL"]);

                    emailOK = Utilidades.EnviarEmail(mailEmisor, nombreEmisor, emailAprobador, asuntoMail, mensajeMail);
                }

                return new ResultadoEnvioMail
                {
                    Success = emailOK,
                    Message = emailOK ? "Correo enviado correctamente." : "No se pudo enviar el correo."
                };
            }
            catch (Exception ex)
            {
                return new ResultadoEnvioMail
                {
                    Success = false,
                    Message = "Error al enviar el correo: " + ex.Message
                };
            }
        }

        public static ResultadoEnvioMail EnviarMail_ConceptoRechazado(Tareas_Empresas_LineasEsfuerzo objConcepto, string emailAprobador, string nombreSolicitante)
        {
            try
            {
                bool emailOK = false;

                string mailEmisor = ConfigurationManager.AppSettings["EmailEmisor"];
                string nombreEmisor = ConfigurationManager.AppSettings["EmailEmisorAlias"];
                string asuntoMail = Resources.Resource.msgAsuntoMailSolicitudLineaEsfuerzo;
                string mensajeMail = String.Empty;

                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string urlPlantilla = Path.Combine(basePath, "Plantillas", "NotificacionRechazoLineaEsfuerzo.html");

                if (!string.IsNullOrEmpty(mailEmisor) && !string.IsNullOrEmpty(emailAprobador))
                {
                    mensajeMail = ObtenerTextoPlantilla(urlPlantilla);
                    mensajeMail = mensajeMail.Replace("[Tarea]", objConcepto.TareaNombre);
                    mensajeMail = mensajeMail.Replace("[Empresa]", objConcepto.EmpresaNombre);
                    mensajeMail = mensajeMail.Replace("[Cantidad]", objConcepto.CantidadNombre);
                    mensajeMail = mensajeMail.Replace("[ImporteTotal]", objConcepto.ImporteTotal.ToEuro());
                    mensajeMail = mensajeMail.Replace("[Descripcion]", System.Web.HttpUtility.HtmlEncode(objConcepto.TLE_Descripcion));

                    mensajeMail = mensajeMail.Replace("[URL_Aplicacion]", ConfigurationManager.AppSettings["PUBLIC_SITE_URL"]);

                    emailOK = Utilidades.EnviarEmail(mailEmisor, nombreEmisor, emailAprobador, asuntoMail, mensajeMail);
                }

                return new ResultadoEnvioMail
                {
                    Success = emailOK,
                    Message = emailOK ? "Correo enviado correctamente." : "No se pudo enviar el correo."
                };
            }
            catch (Exception ex)
            {
                return new ResultadoEnvioMail
                {
                    Success = false,
                    Message = "Error al enviar el correo: " + ex.Message
                };
            }
        }

        /// <summary>
        /// Envía un correo notificando que existe un Entes con un email distinto
        /// al que acaba de llegar del Directorio Activo, para que se revise manualmente.
        /// </summary>
        /// <param name="nombreEnBD">Nombre del Ente que estaba en BD</param>
        /// <param name="emailEnBD">Email que figuraba en la base de datos</param>
        /// <param name="emailEnAD">Email recién leído del Active Directory</param>
        /// <param name="emailDestino">A quién enviar la notificación (ej. soporte TI)</param>
        /// <returns>Objeto con éxito o mensaje de error</returns>
        public static ResultadoEnvioMail EnviarMail_DiscrepanciaEmail(string nombreEnBD, string emailEnBD, string emailEnAD, string emailDestino)
        {
            try
            {
                bool emailOK = false;

                string mailEmisor = ConfigurationManager.AppSettings["EmailEmisor"];
                string nombreEmisor = ConfigurationManager.AppSettings["EmailEmisorAlias"];
                string asuntoMail = "Discrepancia de correo en registro de Entes";
                string mensajeMail = String.Empty;

                // Ruta a la plantilla en disco
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string urlPlantilla = Path.Combine(basePath, "Plantillas", "DiscrepanciaEmail.html");

                if (!string.IsNullOrEmpty(mailEmisor) && !string.IsNullOrEmpty(emailDestino))
                {
                    // Leemos la plantilla completa
                    mensajeMail = ObtenerTextoPlantilla(urlPlantilla);

                    // Reemplazamos los marcadores por los valores reales:
                    mensajeMail = mensajeMail.Replace("[NOMBRE_ENTE]", HttpUtility.HtmlEncode(nombreEnBD));
                    mensajeMail = mensajeMail.Replace("[EMAIL_ANTERIOR]", HttpUtility.HtmlEncode(emailEnBD));
                    mensajeMail = mensajeMail.Replace("[EMAIL_NUEVO]", HttpUtility.HtmlEncode(emailEnAD));
                    mensajeMail = mensajeMail.Replace("[ENLACE_GESTION]", HttpUtility.HtmlEncode(ConfigurationManager.AppSettings["URL_GestionEntes"]));
                    mensajeMail = mensajeMail.Replace("[FECHA_HORA]", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

                    // Enviamos el correo (utilizando tu método de envío de email genérico)
                    emailOK = Utilidades.EnviarEmail(mailEmisor, nombreEmisor, emailDestino, asuntoMail, mensajeMail);
                }

                return new ResultadoEnvioMail
                {
                    Success = emailOK,
                    Message = emailOK ? "Correo de discrepancia enviado correctamente."
                                      : "No se pudo enviar el correo de discrepancia."
                };
            }
            catch (Exception ex)
            {
                return new ResultadoEnvioMail
                {
                    Success = false,
                    Message = "Error al enviar el correo de discrepancia: " + ex.Message
                };
            }
        }

        public static string ObtenerTextoPlantilla(string urlPlantilla)
        {
            System.IO.TextReader trPlantilla = new System.IO.StreamReader(urlPlantilla, true);
            string lstrTextoPlantilla = trPlantilla.ReadToEnd();
            trPlantilla.Close();
            return lstrTextoPlantilla;
        }

        private static List<int> _cacheAños;
        private static readonly object _lockAños = new object();

        public List<int> ObtenerAños()
        {
            if (_cacheAños != null)
                return _cacheAños;

            lock (_lockAños)
            {
                if (_cacheAños == null)
                {
                    DAL_Tareas_Empresas dal = new DAL_Tareas_Empresas();
                    _cacheAños = dal.ObtenerAnyos().OrderByDescending(a => a).ToList();
                }
            }
            return _cacheAños;
        }

        public void LimpiarCache(params string[] nombresCache)
        {
            ObjectCache cache = MemoryCache.Default;

            var keysToRemove = cache
                .Where(kvp => nombresCache.Any(prefix => kvp.Key.StartsWith(prefix)))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                cache.Remove(key);
            }
        }

        public DateTime ObtenerPeriodoFacturacion()
        {
            DAL_Configuraciones dalConfig = new DAL_Configuraciones();
            Configuraciones objConfig = dalConfig.L_PrimaryKey((int)Constantes.Configuracion.AnioConcepto);

            // Si el año es anterior al actual → diciembre (12), si es el año actual → mes corriente
            var hoy = DateTime.Today;
            int anioConcepto = objConfig != null ? Convert.ToInt32(objConfig.CFG_Valor) : DateTime.Now.Year;
            int mesConcepto = anioConcepto < hoy.Year ? 12 : hoy.Month;
            var periodo = new DateTime(anioConcepto, mesConcepto, 1);

            return periodo;
        }

        public class ResultadoEnvioMail
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }

        public class LargeJsonResult : JsonResult
        {
            const string JsonRequest_GetNotAllowed = "This request has been blocked because sensitive information could be disclosed to third party web sites when this is used in a GET request. To allow GET requests, set JsonRequestBehavior to AllowGet.";
            public LargeJsonResult()
            {
                MaxJsonLength = 10024000;
                RecursionLimit = 10000;
            }

            public new int MaxJsonLength { get; set; }
            public new int RecursionLimit { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                context.HttpContext.Response.ContentType = "application/json";
                context.HttpContext.Response.AddHeader("Content-Encoding", "gzip");

                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                using (var gzip = new GZipStream(context.HttpContext.Response.OutputStream, CompressionMode.Compress))
                using (var writer = new StreamWriter(gzip))
                {
                    string json = JsonConvert.SerializeObject(Data, Formatting.None, settings);
                    writer.Write(json);
                    writer.Flush(); // 👈 asegúrate de forzar el volcado del buffer
                }
            }
        }
    }
}