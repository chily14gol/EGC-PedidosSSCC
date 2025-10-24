using AccesoDatos;
using OfficeOpenXml;
using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Caching;
using System.Text;
using System.Web;
using System.Web.Mvc;
using static PedidosSSCC.Comun.Constantes;

namespace PedidosSSCC.Controllers
{
    [Serializable]
    class RegistroExcelRetorno
    {
        public object Tarea;
        public object Empresa;
        public object Elementos;
        public string Errores;
        public object TLE_Anyo;
        public object TLE_Mes;
        public object TLE_Cantidad;
        public object TLE_Descripcion;
        public object TLE_Inversion;
    }

    public class LineaIVAEnlace
    {
        public int TAR_TipoIva;
        public decimal ImporteTotalSinIVA;
    }

    public class BusquedaConceptosViewModel
    {
        public int AnioSeleccionado { get; set; }
        public List<int> AniosDisponibles { get; set; }
    }

    public class PortalController : BaseController
    {
        public ActionResult Inicio()
        {
            return View();
        }

        public ActionResult BusquedaTareas()
        {
            var añosDisponibles = ObtenerAños();
            return View(añosDisponibles);
        }

        public ActionResult TareaDetalle(int id)
        {
            ViewBag.IdTarea = id;
            return View();
        }

        public ActionResult TareaNueva()
        {
            return View();
        }

        public ActionResult BusquedaConceptos()
        {
            var añosDisponibles = ObtenerAños();

            DAL_Configuraciones dalConfig = new DAL_Configuraciones();
            Configuraciones objConfig = dalConfig.L_PrimaryKey((int)Constantes.Configuracion.AnioConcepto);
            int anioConcepto = (objConfig != null ? Convert.ToInt32(objConfig.CFG_Valor) : DateTime.Now.Year);

            var modelo = new BusquedaConceptosViewModel
            {
                AnioSeleccionado = anioConcepto,
                AniosDisponibles = añosDisponibles
            };

            return View(modelo);
        }

        public ActionResult BusquedaFacturas()
        {
            return View();
        }

        public ActionResult BusquedaPedidos()
        {
            return View();
        }

        public ActionResult BusquedaEnlaces()
        {
            return View();
        }

        public ActionResult PedidoDetalle(int id)
        {
            ViewBag.IdPedido = id;
            return View();
        }

        public ActionResult PedidoNuevo()
        {
            return View();
        }

        public ActionResult ConceptoNuevo()
        {
            return View();
        }

        public ActionResult ConceptoDetalle(int id)
        {
            ViewBag.IdConcepto = id;
            return View();
        }

        public ActionResult VistaPrueba()
        {
            return View();
        }

        [HttpGet]
        public ActionResult ObtenerAprobadoresEmpresas(int idEmpresa)
        {
            DAL_Empresas_Aprobadores dal = new DAL_Empresas_Aprobadores();
            Expression<Func<Empresas_Aprobadores, bool>> filtroEmpresa = t => t.EMA_EMP_Id == idEmpresa;
            List<Empresas_Aprobadores> listaAprobadores = dal.L(false, filtroEmpresa);

            // Obtener idPersona y nombre del aprobador por defecto
            Empresas objEmpresa = new DAL_Empresas().L_PrimaryKey(idEmpresa);
            int idPersona = objEmpresa?.EMP_PER_Id_AprobadorDefault ?? 0;

            string nombre = idPersona > 0 ? new DAL_Personas().L_PrimaryKey(idPersona)?.ApellidosNombre : "Desconocido";

            // Construir lista final con la persona por defecto incluida
            var empresasFiltro = listaAprobadores
                .Select(i => new { i.EMA_PER_Id, i.NombrePersona })
                .ToList();

            // Agregar el aprobador por defecto si no está ya en la lista
            if (!empresasFiltro.Any(e => e.EMA_PER_Id == idPersona) && idPersona > 0)
            {
                empresasFiltro.Add(new { EMA_PER_Id = idPersona, NombrePersona = nombre });
            }

            return new LargeJsonResult { Data = empresasFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerPedidoDetalle(int idPedido)
        {
            DAL_Facturas dal = new DAL_Facturas();
            Expression<Func<Facturas, bool>> filtroId = (t => t.FAC_Id == idPedido && t.FAC_Pedido == true);
            List<Facturas> listaPedidos = dal.L(false, filtroId);

            var lista = listaPedidos
                .Select(i => new
                {
                    i.FAC_Id,
                    i.Empresas.EMP_Id,
                    i.EmpresaNombre,
                    i.FAC_EMP_Id_Facturar,
                    i.EmpresaFacturarNombre,
                    i.FAC_NumFactura,
                    FAC_FechaEmision = i.FAC_FechaEmision.HasValue ? i.FAC_FechaEmision.Value.ToString("dd/MM/yyyy") : "",
                    i.FAC_ImporteTotal,
                    i.EstadoNombre,
                    FechaEnlace = i.FechaEnlace.HasValue ? i.FechaEnlace.Value.ToString("dd/MM/yyyy") : "",
                    i.FAC_Expediente,
                    FAC_Direccion = !string.IsNullOrEmpty(i.FAC_Direccion) ? i.FAC_Direccion : i.Empresas.EMP_Direccion,
                    i.FAC_RequiereAprobacion,
                    i.FAC_Contacto,
                    FAC_Documento = i.FAC_Documento ?? "",
                    FAC_DocumentoBytes = i.FAC_DocumentoBytes != null ? "data:application/pdf;base64," + Convert.ToBase64String(i.FAC_DocumentoBytes.ToArray()) : "",
                    FAC_FechaAprobacion = i.FAC_FechaAprobacion.HasValue ? i.FAC_FechaAprobacion.Value.ToString("dd/MM/yyyy") : "",
                    PersonaAprobador = i.PersonaAprobador != null ? i.PersonaAprobador.ApellidosNombre : "",
                    i.FAC_ComentarioAprobacion
                })
                .ToList();

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerConceptosFacturacion()
        {
            DAL_Tareas_Empresas_LineasEsfuerzo dal = new DAL_Tareas_Empresas_LineasEsfuerzo();
            ObjectCache cache = MemoryCache.Default;
            DateTime ldtFechaExportar = DateTime.Today.AddMonths(-1);

            // Variables de sesión para control de acceso
            var verTodo = Sesion.SVerTodo;
            var empresasAprobador = Sesion.SEmpresasAprobador ?? new List<int>();
            var personaId = Sesion.SPersonaId;
            var seccionesAcceso = Sesion.SSeccionesAcceso ?? new List<int>();

            Expression<Func<Tareas_Empresas_LineasEsfuerzo, bool>> filtroAcceso;

            //¿Tiene acceso al registro?
            if (verTodo)
            {
                filtroAcceso = t => true; // acceso total
            }
            else
            {
                filtroAcceso = t =>
                    empresasAprobador.Contains(t.TLE_EMP_Id)
                    || t.TLE_PER_Id_Aprobador == personaId
                    || seccionesAcceso.Contains(t.Tareas_Empresas.Tareas.TAR_SEC_Id);
            }

            var query = dal.Leer(filtroAcceso);

            //Obtener la última fecha de modificación
            DateTime? ultimaModificacion = query.Max(t => (DateTime?)t.FechaModificacion);

            string cacheKey = $"ConceptosFacturacion_{ultimaModificacion:yyyyMMddHHmmss}";

            if (cache.Contains(cacheKey))
            {
                var cached = cache.Get(cacheKey);
                return new LargeJsonResult
                {
                    Data = cached,
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }

            int anioExportar = ldtFechaExportar.Year;
            int mesExportar = ldtFechaExportar.Month;

            var lista = query
                .Select(i => new
                {
                    i.TLE_TAR_Id,
                    i.TLE_Id,
                    i.TareaNombre,
                    i.EmpresaNombre,
                    i.TLE_Anyo,
                    i.TLE_Mes,
                    i.CantidadNombre,
                    i.TLE_Descripcion,
                    EstadoNombre = i.EstadosSolicitud.Nombre,
                    i.ImporteTotal,
                    i.TLE_Inversion,
                    i.TLE_ESO_Id,
                    AnioExportar = anioExportar,
                    MesExportar = mesExportar
                });

            // Guardar en caché
            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(15)
            };
            cache.Set(cacheKey, lista, policy);

            return new LargeJsonResult
            {
                Data = lista,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpGet]
        public ActionResult ObtenerConceptosPorPedido(int idPedido, int idEmpresa, bool verSeleccionables)
        {
            DAL_Tareas_Empresas_LineasEsfuerzo dalConceptos = new DAL_Tareas_Empresas_LineasEsfuerzo();
            List<Tareas_Empresas_LineasEsfuerzo> listaConceptos;

            if (verSeleccionables)
            {
                string cadena = idEmpresa + "|" + idPedido + "|";
                dalConceptos.ModoConsultaClaveExterna = DAL_Tareas_Empresas_LineasEsfuerzo.TipoClaveExterna.SeleccionablesFactura;
                listaConceptos = dalConceptos.L_ClaveExterna(cadena, null);
            }
            else
            {
                dalConceptos.ModoConsultaClaveExterna = DAL_Tareas_Empresas_LineasEsfuerzo.TipoClaveExterna.SeleccionadosFactura;
                listaConceptos = dalConceptos.L_ClaveExterna(idPedido, null);
            }

            //La tabla Facturas_Tareas_LineasEsfuerzo es la que une un pedido con sus conceptos de facturación.
            DAL_Facturas_Tareas_LineasEsfuerzo dal = new DAL_Facturas_Tareas_LineasEsfuerzo();
            Expression<Func<Facturas_Tareas_LineasEsfuerzo, bool>> filtro = (i => i.FLE_FAC_Id == idPedido);
            List<int> listaFiltro = dal.L(false, filtro).Select(i => i.FLE_TLE_Id).ToList();

            var conceptosFiltro = listaConceptos
                .Select(i => new
                {
                    i.TLE_Id,
                    i.TareaNombre,
                    i.EmpresaNombre,
                    i.TLE_Anyo,
                    i.TLE_Mes,
                    i.CantidadNombre,
                    i.TLE_Descripcion,
                    i.EstadoNombre,
                    i.ImporteTotal,
                    i.TLE_ESO_Id,
                    Activo = listaFiltro.Contains(i.TLE_Id) // Devuelve true si está en la listaFiltro
                })
                .ToList();

            return new LargeJsonResult { Data = conceptosFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerConceptosFacturacionSeleccionables(int idPedido, int idEmpresa)
        {
            DAL_Tareas_Empresas_LineasEsfuerzo dal = new DAL_Tareas_Empresas_LineasEsfuerzo();

            string cadena = idEmpresa + "|" + idPedido + "|";
            dal.ModoConsultaClaveExterna = DAL_Tareas_Empresas_LineasEsfuerzo.TipoClaveExterna.SeleccionablesFactura;

            List<Tareas_Empresas_LineasEsfuerzo> lista = dal.L_ClaveExterna(cadena, null);

            var tareasFiltro = lista
                .Select(i => new
                {
                    i.TLE_Id,
                    i.TareaNombre,
                    i.EmpresaNombre,
                    i.TLE_Anyo,
                    i.TLE_Mes,
                    i.CantidadNombre,
                    i.TLE_Descripcion,
                    i.EstadoNombre,
                    i.ImporteTotal
                })
                .ToList();

            return new LargeJsonResult { Data = tareasFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerConceptosPendienteAprobacion()
        {
            DAL_Tareas_Empresas_LineasEsfuerzo dalConceptos = new DAL_Tareas_Empresas_LineasEsfuerzo();

            Expression<Func<Tareas_Empresas_LineasEsfuerzo, bool>> filtroPendiente = t => t.TLE_ESO_Id == (int)Constantes.EstadosSolicitud.PendienteAprobacion
                && t.TLE_PER_Id_Aprobador == Sesion.SPersonaId;
            List<Tareas_Empresas_LineasEsfuerzo> listaConceptos = dalConceptos.L(false, filtroPendiente);

            var conceptosFiltro = listaConceptos
                .Select(i => new
                {
                    i.TLE_Id,
                    i.TLE_TAR_Id,
                    TareaNombre = i.Tareas.TAR_Nombre,
                    i.TLE_EMP_Id,
                    EmpresaNombre = i.Empresas?.EMP_Nombre ?? "(sin empresa)",
                    i.TLE_Anyo,
                    i.TLE_Mes,
                    CantidadNombre = i.TLE_Cantidad,
                    i.TLE_Descripcion,
                    EstadoNombre = i.EstadosSolicitud.Nombre,
                    i.ImporteTotal,
                    i.TLE_ESO_Id
                })
                .ToList();

            return new LargeJsonResult { Data = conceptosFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult EliminarConcepto(int idConcepto)
        {
            try
            {
                DAL_Tareas_Empresas_LineasEsfuerzo dal = new DAL_Tareas_Empresas_LineasEsfuerzo();
                bool ok = dal.D(idConcepto);

                if (ok)
                {
                    LimpiarCache(TipoCache.Conceptos);
                }

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarPedido(int idPedido)
        {
            try
            {
                DAL_Facturas dal = new DAL_Facturas();
                bool ok = dal.D(idPedido);

                if (ok)
                {
                    LimpiarCache(TipoCache.Pedidos);
                }

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerComboUnidades()
        {
            DAL_UnidadesTarea_Idioma dalUnidades = new DAL_UnidadesTarea_Idioma();
            List<UnidadesTarea_Idioma> unidades = dalUnidades.L(true);

            var unidadesFiltro = unidades
                .Select(i => new
                {
                    i.UTI_UTA_Id,
                    i.UTI_Nombre
                })
                .ToList();

            return new LargeJsonResult { Data = unidadesFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerComboProductosD365()
        {
            DAL_ProductosD365 dalProductosD365 = new DAL_ProductosD365();
            List<ProductosD365> productos = dalProductosD365.ObtenerCombo(null, registroVacio: false, valorSeleccionado: null).OrderBy(r => r.PR3_Nombre).ToList();

            var productosFiltro = productos
                .Select(i => new
                {
                    i.PR3_Id,
                    i.PR3_Nombre,
                })
                .ToList();

            return new LargeJsonResult { Data = productosFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerComboItemNumber()
        {
            DAL_ItemNumbersD365 dalItemNumbersD365 = new DAL_ItemNumbersD365();
            List<ItemNumbersD365> itemsNumber = dalItemNumbersD365.ObtenerCombo(null, registroVacio: false, valorSeleccionado: null).OrderBy(r => r.IN3_Nombre).ToList();

            var itemsNumberFiltro = itemsNumber
                .Select(i => new
                {
                    i.IN3_Id,
                    i.IN3_Nombre
                })
                .ToList();

            return new LargeJsonResult { Data = itemsNumberFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerComboTipos()
        {
            DAL_TiposTarea dalTipos = new DAL_TiposTarea();
            List<TiposTarea> tipos = dalTipos.L(true);

            var tiposFiltro = tipos
                .Select(i => new
                {
                    i.TTA_Id,
                    i.TTA_Nombre
                })
                .ToList();

            return new LargeJsonResult { Data = tiposFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerComboSecciones()
        {
            DAL_Secciones dalSecciones = new DAL_Secciones();
            List<Secciones> tipos = dalSecciones.L(true);

            var tiposFiltro = tipos
                .Select(i => new
                {
                    i.SEC_Id,
                    SEC_Nombre = i.Departamentos.DEP_Nombre + " - " + i.SEC_Nombre
                })
                .OrderBy(i => i.SEC_Nombre)
                .ToList();

            return new LargeJsonResult { Data = tiposFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerComboEmpresas(int idEmpresa)
        {
            DAL_Empresas dalEmpresas = new DAL_Empresas();
            List<Empresas> empresas = dalEmpresas.ObtenerCombo(null, registroVacio: false);

            var listaFiltro = empresas
                .Where(r => r.EMP_EmpresaFacturar || r.EMP_Id == empresas.FirstOrDefault(i => i.EMP_Id == idEmpresa)?.EMP_EmpresaFacturar_Id)
                .Select(i => new { i.EMP_Id, i.EMP_Nombre })
                .ToList();

            return new LargeJsonResult { Data = listaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarConcepto(Tareas_Empresas_LineasEsfuerzo objConcepto)
        {
            try
            {
                DAL_Tareas_Empresas_LineasEsfuerzo dal = new DAL_Tareas_Empresas_LineasEsfuerzo();
                bool ok_Guardar = dal.G(objConcepto, Sesion.SPersonaId);

                if (ok_Guardar)
                {
                    LimpiarCache(TipoCache.Conceptos);
                }

                Tareas_Empresas_LineasEsfuerzo objConceptoCompleto = dal.L_PrimaryKey(objConcepto.TLE_Id);

                ResultadoEnvioMail respuestaEmail = new ResultadoEnvioMail();
                string mensaje_email = "";

                if (objConcepto.TLE_ESO_Id == (int)Constantes.EstadosSolicitud.PendienteAprobacion)
                {
                    DAL_Personas dalPer = new DAL_Personas();
                    Personas objPer = dalPer.L_PrimaryKey(objConceptoCompleto.TLE_PER_Id_Aprobador, false);

                    if (objPer != null)
                    {
                        respuestaEmail = EnviarMail_SolicitudConcepto(objConceptoCompleto, objPer.PER_Email, Sesion.SNombrePersona);

                        if (!respuestaEmail.Success)
                            mensaje_email = respuestaEmail.Message;
                    }
                    else
                        mensaje_email = Properties.Resources.Toast_EmailNoEncontrado;
                }

                return Json(new { success = ok_Guardar, ok_email = respuestaEmail.Success, mensaje_email });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult AprobarRechazarConceptos(List<int> ids, string comentarios, int idEstado)
        {
            Constantes.EstadosSolicitud estado = Constantes.EstadosSolicitud.Aprobado;

            try
            {
                if (ids == null || !ids.Any())
                {
                    return Json(new { success = false, message = "No se recibieron conceptos para aprobar." });
                }

                switch (idEstado)
                {
                    case 3:
                        estado = Constantes.EstadosSolicitud.Aprobado;
                        break;
                    case 4:
                        estado = Constantes.EstadosSolicitud.Rechazado;
                        break;
                }

                bool ok_AprobarRechazar = false;
                ResultadoEnvioMail respuestaEmail = new ResultadoEnvioMail();
                string mensaje_email = "";

                DAL_Tareas_Empresas_LineasEsfuerzo dal = new DAL_Tareas_Empresas_LineasEsfuerzo();
                foreach (int idConcepto in ids)
                {
                    ok_AprobarRechazar = dal.AprobarConcepto(idConcepto, estado, comentarios);

                    if (estado == Constantes.EstadosSolicitud.Rechazado)
                    {
                        DAL_Tareas_Empresas_LineasEsfuerzo dalConc = new DAL_Tareas_Empresas_LineasEsfuerzo();
                        Tareas_Empresas_LineasEsfuerzo objConcepto = dalConc.L_PrimaryKey(idConcepto);

                        DAL_Personas dalPer = new DAL_Personas();
                        Personas objPer = dalPer.L_PrimaryKey(objConcepto.TLE_PER_Id_Aprobador, false);

                        if (objPer != null)
                        {
                            respuestaEmail = EnviarMail_ConceptoRechazado(objConcepto, objPer.PER_Email, Sesion.SNombrePersona);

                            if (!respuestaEmail.Success)
                                mensaje_email = respuestaEmail.Message;
                        }
                        else
                            mensaje_email = Properties.Resources.Toast_EmailNoEncontrado;
                    }
                }

                LimpiarCache(TipoCache.Conceptos);

                return Json(new { success = ok_AprobarRechazar, ok_email = respuestaEmail.Success, mensaje_email });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GuardarNuevoPedido(Facturas objPedido)
        {
            try
            {
                DAL_Facturas dalFAC = new DAL_Facturas();
                objPedido.FAC_Pedido = true;
                objPedido.FAC_ESO_Id = (int)Constantes.EstadosSolicitud.SinSolicitar;
                objPedido.FAC_ImporteTotal = 0;
                bool ok = dalFAC.G(objPedido, Sesion.SPersonaId);

                if (ok)
                {
                    LimpiarCache(TipoCache.Pedidos);
                }

                return Json(new { success = ok, pedidoId = objPedido.FAC_Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EditarPedido(Facturas objPedido, List<int> ids)
        {
            try
            {
                DAL_Facturas dal = new DAL_Facturas();
                bool ok = dal.EditarPedido(objPedido, Sesion.SPersonaId);

                if (ok)
                {
                    LimpiarCache(TipoCache.Pedidos);
                }

                DAL_Facturas_Tareas_LineasEsfuerzo dalFacConceptos = new DAL_Facturas_Tareas_LineasEsfuerzo();
                dalFacConceptos.EliminarPedidoConceptos(objPedido.FAC_Id);

                if (ids != null)
                {
                    foreach (int idConcepto in ids)
                    {
                        Facturas_Tareas_LineasEsfuerzo objTareaConcepto = new Facturas_Tareas_LineasEsfuerzo
                        {
                            FLE_FAC_Id = objPedido.FAC_Id,
                            FLE_TLE_Id = idConcepto
                        };
                        dalFacConceptos.G(objTareaConcepto, Sesion.SPersonaId);
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EmitirPedido(Facturas objPedido, List<int> listaIdConceptos)
        {
            try
            {
                DAL_Facturas dalFAC = new DAL_Facturas();

                objPedido.FAC_ESO_Id = (int)Constantes.EstadosSolicitud.Aprobado;
                objPedido.FAC_PER_Id_Aprobador = Sesion.SPersonaId;
                objPedido.FAC_FechaAprobacion = DateTime.Now;
                objPedido.FAC_ComentarioAprobacion = "";

                //Solo generamos el nº de pedido cuando el importe es positivo
                if (objPedido.FAC_ImporteTotal > 0 && objPedido.FAC_Pedido)
                {
                    objPedido.FAC_NumFactura = dalFAC.GenerarNumPedido(objPedido.FAC_FechaEmision.Value);
                }

                bool ok = dalFAC.G(objPedido, Sesion.SPersonaId);

                if (ok)
                {
                    LimpiarCache(TipoCache.Pedidos);
                }

                DAL_Facturas_Tareas_LineasEsfuerzo dal = new DAL_Facturas_Tareas_LineasEsfuerzo();
                foreach (int idConcepto in listaIdConceptos)
                {
                    Facturas_Tareas_LineasEsfuerzo objTareaConcepto = new Facturas_Tareas_LineasEsfuerzo
                    {
                        FLE_FAC_Id = objPedido.FAC_Id,
                        FLE_TLE_Id = idConcepto
                    };
                    dal.G(objTareaConcepto, Sesion.SPersonaId);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult PrevisualizarPedidosAutomaticamente()
        {
            try
            {
                // Obtener conceptos aprobados
                DAL_Tareas_Empresas_LineasEsfuerzo dalConceptos = new DAL_Tareas_Empresas_LineasEsfuerzo();
                Expression<Func<Tareas_Empresas_LineasEsfuerzo, bool>> filtroAprobados = c => c.TLE_ESO_Id == (int)Constantes.EstadosSolicitud.Aprobado;
                var listaConceptos = dalConceptos.L(false, filtroAprobados);

                // Obtener los conceptos ya facturados
                DAL_Facturas_Tareas_LineasEsfuerzo dalFacConceptos = new DAL_Facturas_Tareas_LineasEsfuerzo();
                var listaFacturas = dalFacConceptos.L(false, null);

                var conceptosSinFactura = listaConceptos
                    .Where(c => !listaFacturas.Any(f => f.FLE_TLE_Id == c.TLE_Id))
                    .ToList();

                var empDict = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id);

                // Cargar tareas
                DAL_Tareas dalTareas = new DAL_Tareas();
                var listaTareas = dalTareas.L(false, null);
                var dicTareas = listaTareas.ToDictionary(t => t.TAR_Id);

                // Leemos de appSettings la lista de TAR_Id adicionales
                var cfg = ConfigurationManager.AppSettings["TareasPorHoras"] ?? "";
                // resultado: ["1","5","23"]  ó [""]
                var tareasExtras = cfg
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => {
                        int x;
                        return int.TryParse(s, out x) ? x : -1;
                    })
                    .Where(x => x > 0)
                    .ToArray();

                // Agrupar por empresa y aplicar la lógica de grupos
                var agrupadoPorEmpresa = conceptosSinFactura
                    .GroupBy(c => c.TLE_EMP_Id)
                    .Select(grupo =>
                    {
                        Empresas empresa = empDict.ContainsKey(grupo.Key) ? empDict[grupo.Key] : null;

                        // NUEVA LÓGICA: TAR por horas (TAR_TTA_Id == PorHoras) 
                        // o TAR_Id está en la lista de config
                        var grupo1 = grupo.Where(c =>
                            empresa != null
                            && empresa.EMP_EmpresaFacturar_Id != null
                            && dicTareas.ContainsKey(c.TLE_TAR_Id)
                            && (
                                 // tareas “por horas” originales
                                 dicTareas[c.TLE_TAR_Id].TAR_TTA_Id
                                     == (int)Constantes.TipoTarea.PorHoras
                                 // o bien / además, está en la lista de IDs extra
                                 || tareasExtras.Contains(c.TLE_TAR_Id)
                               )
                        ).ToList();

                        var grupo2 = grupo.Where(c => c.TLE_Inversion).ToList();

                        var grupo3 = grupo.Except(grupo1).Except(grupo2).ToList();

                        var empresaFacturar = (empresa != null && empresa.EMP_EmpresaFacturar_Id.HasValue &&
                            empDict.ContainsKey(empresa.EMP_EmpresaFacturar_Id.Value))
                            ? empDict[empresa.EMP_EmpresaFacturar_Id.Value] : null;

                        return new
                        {
                            EmpresaId = grupo.Key,
                            EmpresaNombre = empresa?.EMP_Nombre ?? "Desconocida",
                            EmpresaFacturarNombre = empresaFacturar?.EMP_Nombre ?? "-",
                            Grupo1 = grupo1.Select(c => new
                            {
                                c.TareaNombre,
                                c.TLE_Anyo,
                                c.TLE_Mes,
                                Importe = c.ImporteTotal
                            }).ToList(),
                            Grupo2 = grupo2.Select(c => new
                            {
                                c.TareaNombre,
                                c.TLE_Anyo,
                                c.TLE_Mes,
                                Importe = c.ImporteTotal
                            }).ToList(),
                            Grupo3 = grupo3.Select(c => new
                            {
                                c.TareaNombre,
                                c.TLE_Anyo,
                                c.TLE_Mes,
                                Importe = c.ImporteTotal
                            }).ToList()
                        };
                    })
                    .ToList();

                return Json(new { success = true, resumen = agrupadoPorEmpresa });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GenerarPedidosAutomaticamente()
        {
            try
            {
                DAL_Tareas_Empresas_LineasEsfuerzo dalConceptos = new DAL_Tareas_Empresas_LineasEsfuerzo();

                // Filtrar los conceptos aprobados directamente en la consulta
                Expression<Func<Tareas_Empresas_LineasEsfuerzo, bool>> filtroAprobados = c => c.TLE_ESO_Id == (int)Constantes.EstadosSolicitud.Aprobado;
                List<Tareas_Empresas_LineasEsfuerzo> listaConceptos = dalConceptos.L(false, filtroAprobados);

                DAL_Facturas_Tareas_LineasEsfuerzo dalFacConceptos = new DAL_Facturas_Tareas_LineasEsfuerzo();
                List<Facturas_Tareas_LineasEsfuerzo> listaFacturas = dalFacConceptos.L(false, null);

                // Obtener conceptos aprobados que no están en facturas
                var conceptosSinFactura = listaConceptos
                    .Where(c => !listaFacturas.Any(f => f.FLE_TLE_Id == c.TLE_Id))
                    .ToList();

                var empDict = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id);

                // Obtener la lista de tareas
                DAL_Tareas dalTareas = new DAL_Tareas();
                List<Tareas> listaTareas = dalTareas.L(false, null);

                // Convertir en diccionario para acceso rápido por ID
                var dicTareas = listaTareas.ToDictionary(t => t.TAR_Id);

                // Lista de resultados para cada empresa
                var resultadoPedidos = new List<object>();

                // Leemos de appSettings la lista de TAR_Id adicionales
                var cfg = ConfigurationManager.AppSettings["TareasPorHoras"] ?? "";
                // resultado: ["1","5","23"]  ó [""]
                var tareasExtras = cfg
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => {
                        int x;
                        return int.TryParse(s, out x) ? x : -1;
                    })
                    .Where(x => x > 0)
                    .ToArray();

                // Agrupamos por empresa y separamos en grupos
                var agrupadoPorEmpresa = conceptosSinFactura
                    .GroupBy(c => c.TLE_EMP_Id)
                    .Select(grupo =>
                    {
                        Empresas empresa = empDict.ContainsKey(grupo.Key) ? empDict[grupo.Key] : null;

                        // TAR por horas (TAR_TTA_Id == PorHoras) o TAR_Id está en la lista de config
                        var grupo1 = grupo.Where(c =>
                            empresa != null
                            && empresa.EMP_EmpresaFacturar_Id != null
                            && dicTareas.ContainsKey(c.TLE_TAR_Id)
                            && (
                                 // tareas “por horas” originales
                                 dicTareas[c.TLE_TAR_Id].TAR_TTA_Id
                                     == (int)Constantes.TipoTarea.PorHoras
                                 // o bien / además, está en la lista de IDs extra
                                 || tareasExtras.Contains(c.TLE_TAR_Id)
                               )
                        ).ToList();

                        var grupo2 = grupo
                            .Where(c => c.TLE_Inversion)
                            .ToList();

                        var grupo3 = grupo
                            .Except(grupo1)
                            .Except(grupo2)
                            .ToList();

                        return new
                        {
                            EmpresaId = grupo.Key,
                            EmpresaNombre = empresa?.EMP_Nombre ?? "Desconocida",
                            EmpresaFactruraId = empresa?.EMP_EmpresaFacturar_Id,
                            Grupo1 = grupo1,
                            Grupo2 = grupo2,
                            Grupo3 = grupo3
                        };
                    })
                    .ToList();

                foreach (var empresa in agrupadoPorEmpresa)
                {
                    Console.WriteLine($"Empresa ID: {empresa.EmpresaId}");

                    DAL_Facturas dalFAC = new DAL_Facturas();

                    int pedidosGenerados = 0;

                    // Procesar cada grupo de conceptos y contar los pedidos generados
                    pedidosGenerados += ProcesarGrupoConceptos(empresa.EmpresaId, empresa.EmpresaFactruraId, empresa.Grupo1, dalFAC, "Grupo 1");
                    pedidosGenerados += ProcesarGrupoConceptos(empresa.EmpresaId, null, empresa.Grupo2, dalFAC, "Grupo 2");
                    pedidosGenerados += ProcesarGrupoConceptos(empresa.EmpresaId, null, empresa.Grupo3, dalFAC, "Grupo 3");

                    // Agregar el resultado a la lista
                    resultadoPedidos.Add(new
                    {
                        empresa.EmpresaId,
                        empresa.EmpresaNombre,
                        PedidosGenerados = pedidosGenerados
                    });
                }

                LimpiarCache(TipoCache.Pedidos);

                return Json(new { success = true, resultado = resultadoPedidos });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private int ProcesarGrupoConceptos(int idEmpresa, int? idEmpresaFacturar, List<Tareas_Empresas_LineasEsfuerzo> grupo, DAL_Facturas dalFAC, string grupoNombre)
        {
            if (grupo.Count == 0)
                return 0;

            Console.WriteLine(grupoNombre);
            decimal importeTotal = grupo.Sum(c => c.ImporteTotal);

            Facturas objPedido = new Facturas
            {
                FAC_EMP_Id = idEmpresa,
                FAC_Pedido = true,
                FAC_ESO_Id = (int)Constantes.EstadosSolicitud.Aprobado,
                FAC_Expediente = "",
                FAC_ImporteTotal = importeTotal,
                FAC_FechaEmision = DateTime.Today
            };

            if (idEmpresaFacturar.HasValue)
            {
                objPedido.FAC_EMP_Id_Facturar = idEmpresaFacturar.Value;
            }

            // Solo generamos el número de pedido cuando el importe es positivo (no en abonos)
            if (objPedido.FAC_ImporteTotal > 0)
            {
                objPedido.FAC_NumFactura = dalFAC.GenerarNumPedido(objPedido.FAC_FechaEmision.Value);
            }

            dalFAC.G(objPedido, Sesion.SPersonaId);
            int idFactura = objPedido.FAC_Id;

            DAL_Facturas_Tareas_LineasEsfuerzo dalFacConceptos = new DAL_Facturas_Tareas_LineasEsfuerzo();
            foreach (var concepto in grupo)
            {
                Facturas_Tareas_LineasEsfuerzo objTareaConcepto = new Facturas_Tareas_LineasEsfuerzo
                {
                    FLE_FAC_Id = objPedido.FAC_Id,
                    FLE_TLE_Id = concepto.TLE_Id
                };
                dalFacConceptos.G(objTareaConcepto, Sesion.SPersonaId);
            }

            dalFAC.AprobarRechazar(objPedido, Constantes.EstadosSolicitud.Aprobado, "", Sesion.SPersonaId);

            return 1;
        }

        [HttpGet]
        public ActionResult ObtenerConceptoDetalle(int idConcepto)
        {
            DAL_Tareas_Empresas_LineasEsfuerzo dalConceptos = new DAL_Tareas_Empresas_LineasEsfuerzo();
            Tareas_Empresas_LineasEsfuerzo objConceptos = dalConceptos.L_PrimaryKey(idConcepto);

            if (objConceptos != null)
            {
                DAL_Tareas_Empresas dalTEM = new DAL_Tareas_Empresas();

                var conceptosFiltro = new
                {
                    objConceptos.TLE_Id,
                    objConceptos.TLE_TAR_Id,
                    objConceptos.TareaNombre,
                    objConceptos.EmpresaNombre,
                    objConceptos.TLE_EMP_Id,
                    objConceptos.TLE_Anyo,
                    objConceptos.TLE_Mes,
                    objConceptos.TLE_Cantidad,
                    objConceptos.TLE_Descripcion,
                    objConceptos.TLE_Inversion,
                    TLE_FechaAprobacion = objConceptos.TLE_FechaAprobacion.HasValue ? objConceptos.TLE_FechaAprobacion.Value.ToString("dd/MM/yyyy") : "",
                    TLE_ComentarioAprobacion = objConceptos.TLE_ComentarioAprobacion ?? "",
                    PersonaAprobador = objConceptos.PersonaAprobador?.ApellidosNombre ?? "",
                    objConceptos.EstadoNombre,
                    objConceptos.TLE_ESO_Id,
                    objConceptos.ImporteTotal,
                    TEM_Presupuesto = objConceptos.Tareas_Empresas?.TEM_Presupuesto ?? 0,
                    TEM_PresupuestoConsumido = dalTEM.PresupuestoConsumido(
                        objConceptos.Tareas_Empresas?.TEM_TAR_Id ?? 0,
                        objConceptos.Tareas_Empresas?.TEM_EMP_Id ?? 0,
                        objConceptos.Tareas_Empresas?.TEM_Anyo ?? 0
                    ).ToDouble(),
                    TAR_ImporteUnitario = objConceptos?.Tareas_Empresas?.Tareas?.TAR_ImporteUnitario ?? 0,
                    TAR_TTA_Id = objConceptos?.Tareas_Empresas?.Tareas?.TAR_TTA_Id ?? 0,
                    Solicitante = objConceptos.PersonaModificacion.ApellidosNombre,
                    FechaModificacion = objConceptos.FechaModificacion.ToString("dd/MM/yyyy")
                };

                return new LargeJsonResult { Data = conceptosFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }

            return Json(new { success = false });
        }

        [HttpGet]
        public ActionResult ObtenerPedidos()
        {
            DAL_Facturas dal = new DAL_Facturas();
            ObjectCache cache = MemoryCache.Default;

            // Generar clave de caché basada en la última fecha de modificación
            Expression<Func<Facturas, bool>> filtroPedido = t => t.FAC_Pedido == true;
            List<Facturas> listaPedidos = dal.L(false, filtroPedido);

            DateTime? ultimaModificacion = listaPedidos.Max(t => (DateTime?)t.FechaAlta); // o FechaModificacion si existe
            string cacheKey = $"Pedidos_{ultimaModificacion:yyyyMMddHHmmss}";

            if (cache.Contains(cacheKey))
            {
                var cached = cache.Get(cacheKey);
                return new LargeJsonResult
                {
                    Data = cached,
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }

            // Crear el contenido que se va a devolver y almacenar
            var pedidosFiltro = listaPedidos.AsQueryable()
                .Select(i => new
                {
                    i.FAC_Id,
                    i.EmpresaNombre,
                    i.EmpresaFacturarNombre,
                    i.FAC_NumFactura,
                    FAC_FechaEmision = i.FAC_FechaEmision.HasValue ? i.FAC_FechaEmision.Value.ToString("dd/MM/yyyy") : "",
                    i.FAC_ImporteTotal,
                    i.FAC_ESO_Id,
                    i.EstadoNombre,
                    FechaEnlace = i.FechaEnlace.HasValue ? i.FechaEnlace.Value.ToString("dd/MM/yyyy") : "",
                    i.FechaAlta
                })
                .OrderByDescending(i => i.FechaAlta)
                .ToList();

            // Guardar en caché
            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(15)
            };
            cache.Set(cacheKey, pedidosFiltro, policy);

            return new LargeJsonResult
            {
                Data = pedidosFiltro,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpGet]
        public ActionResult ObtenerEnlaces()
        {
            DAL_EnlacesContables dal = new DAL_EnlacesContables();
            List<EnlacesContables> conceptos = dal.L();

            var conceptosFiltro = conceptos
                .Select(i => new
                {
                    i.ECO_Id,
                    ECO_Fecha = i.ECO_Fecha.ToString("dd/MM/yyyy"),
                    FechaAlta = i.FechaAlta.ToString("dd/MM/yyyy HH:mmmm:ss"),
                    ECO_PER_Id = i.Personas.ApellidosNombre
                })
                .ToList();

            return new LargeJsonResult { Data = conceptosFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult NuevoEnlace(string fechaEnlace)
        {
            try
            {
                DateTime fechaEnlaceDT = Convert.ToDateTime(fechaEnlace);

                DAL_Facturas dalFAC = new DAL_Facturas();
                List<Facturas> lstPedidos = dalFAC.L_EnlaceContable(fechaEnlaceDT);

                if (lstPedidos == null || lstPedidos.Count == 0)
                {
                    return Json(new { success = false, noPedidos = true, message = "No existen pedidos que cumplan los criterios indicandos para realizar el enlace contable." });
                }

                DAL_EnlacesContables dal = new DAL_EnlacesContables();

                EnlacesContables objEnlace = new EnlacesContables
                {
                    ECO_Fecha = fechaEnlaceDT,
                    ECO_PER_Id = Sesion.SPersonaId,
                    ECO_Documento = null,
                    ECO_DocumentoBytes = null,
                    FechaAlta = DateTime.Now
                };

                objEnlace.ECO_Documento = ConfigurationManager.AppSettings["D365_Activo"] == "1"
                    ? $"ECO_{fechaEnlaceDT:yyyy_MM}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                    : $"ECO_{fechaEnlaceDT:yyyy_MM}_{DateTime.Now:yyyyMMdd_HHmm}.txt";

                string lstrError = String.Empty;

                byte[] contenidoEnlaceContable = GenerarFicheroEnlaceContable(lstPedidos, out lstrError);
                objEnlace.ECO_DocumentoBytes = contenidoEnlaceContable;

                bool ok = dal.G(objEnlace, Sesion.SPersonaId);
                bool okAsignacion = dal.AsignarEnlaceContablaAPedidos(objEnlace, lstPedidos, Sesion.SPersonaId);

                if (ok && okAsignacion)
                {
                    LimpiarCache(TipoCache.Pedidos);
                }

                return Json(new { success = ok && okAsignacion });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult CargarConfiguracionInicial()
        {
            int AnioDefecto = Parametros.AnyoConcepto;
            int MesDefecto = Parametros.AnyoConcepto.Equals(DateTime.Today.Year) ? DateTime.Today.Month : 12;

            var resultado = new
            {
                AnioDefecto,
                MesDefecto
            };

            return new LargeJsonResult { Data = resultado, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult DescargarEnlace(int idEnlace)
        {
            // Recogemos los datos del enlace a descargar
            DAL_EnlacesContables dal = new DAL_EnlacesContables();
            EnlacesContables enlace = dal.L_PrimaryKey(idEnlace);

            if (enlace != null && enlace.ECO_DocumentoBytes != null)
            {
                try
                {
                    string nombreDocumentoMostrar = enlace.ECO_Documento; // Nombre del archivo
                    byte[] binData = enlace.ECO_DocumentoBytes.ToArray(); // Datos binarios del archivo

                    // Retornar el archivo como un FileResult
                    return File(binData, "text/plain", nombreDocumentoMostrar);
                }
                catch (Exception ex)
                {
                    // Manejar el error adecuadamente
                    Console.WriteLine($"Error al enviar el archivo: {ex.Message}");
                    return new HttpStatusCodeResult(500, $"Error al procesar el archivo: {ex.Message}");
                }
            }

            // Si no hay archivo, devolver un error
            return new HttpStatusCodeResult(404, "El archivo no existe o no está disponible.");
        }

        [HttpPost]
        public JsonResult ImportarExcelConceptos(HttpPostedFileBase archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.ContentLength == 0)
            {
                return Json(new { success = false, message = "No se ha seleccionado un archivo." });
            }

            try
            {
                int anioConceptosConfig = 0;

                try
                {
                    anioConceptosConfig = Parametros.AnyoConcepto;
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, errorFormato = true, message = "Debe de configurar el Año del concepto" });
                }

                int lintMes = Parametros.AnyoConcepto.Equals(DateTime.Today.Year) ? DateTime.Today.Month : 12;

                var registros = new List<Tareas_Empresas>();
                List<RegistroExcelRetorno> lstErroresExcel = new List<RegistroExcelRetorno>();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (ExcelPackage excelPackage = new ExcelPackage(archivoExcel.InputStream))
                {
                    ExcelWorksheet wsConceptos = excelPackage.Workbook.Worksheets.First();

                    // **Validación del formato del Excel**
                    if (wsConceptos.Dimension == null || wsConceptos.Dimension.End.Column < 7)
                    {
                        return Json(new { success = false, errorFormato = true, message = "El archivo no tiene el formato esperado." });
                    }

                    // **Validar los nombres de las cabeceras**
                    string colA = wsConceptos.Cells[1, 1].Text.Trim();
                    string colB = wsConceptos.Cells[1, 2].Text.Trim();
                    string colC = wsConceptos.Cells[1, 3].Text.Trim();
                    string colD = wsConceptos.Cells[1, 4].Text.Trim();
                    string colE = wsConceptos.Cells[1, 5].Text.Trim();
                    string colF = wsConceptos.Cells[1, 6].Text.Trim();
                    string colG = wsConceptos.Cells[1, 7].Text.Trim();

                    if (colA.ToLower() != "tarea" || colB.ToLower() != "empresa" || colC.ToLower() != "año del concepto" ||
                        colD.ToLower() != "mes del concepto" || colE.ToLower() != "cantidad" ||
                        colF.ToLower() != "descripción" || colG.ToLower() != "inversión (si/no)")
                    {
                        return Json(new { success = false, errorFormato = true, message = "Las cabeceras del archivo no coinciden con el formato esperado." });
                    }

                    DAL_Tareas dalTAR = new DAL_Tareas
                    {
                        ModoConsultaClaveExterna = DAL_Tareas.TipoClaveExterna.Nombre
                    };

                    DAL_Empresas dalEMP = new DAL_Empresas
                    {
                        ModoConsultaClaveExterna = DAL_Empresas.TipoClaveExterna.Nombre
                    };

                    DAL_Tareas_Empresas dalTAE = new DAL_Tareas_Empresas();
                    DAL_Tareas_Empresas_LineasEsfuerzo dalTLE = new DAL_Tareas_Empresas_LineasEsfuerzo
                    {
                        ModoConsultaClaveExterna = DAL_Tareas_Empresas_LineasEsfuerzo.TipoClaveExterna.TareaEmpresaAnyoMes
                    };

                    int registro = 0;
                    bool lbolHayDatos = true;

                    int ultimaColumna = wsConceptos.Dimension.End.Column;
                    int primeraFila = wsConceptos.Dimension.Start.Row;
                    var ultimaCeldaCabecera = wsConceptos.Cells[primeraFila, ultimaColumna];

                    // Determinar la columna de error (solo una vez)
                    int columnaErrores = wsConceptos.Dimension.End.Column + 1;
                    wsConceptos.Cells[1, columnaErrores].StyleID = ultimaCeldaCabecera.StyleID;
                    wsConceptos.Cells[1, columnaErrores].Value = "Errores"; // Encabezado de la columna de errores

                    while (lbolHayDatos)
                    {
                        Tareas_Empresas_LineasEsfuerzo objConcepto = new Tareas_Empresas_LineasEsfuerzo();

                        StringBuilder sbErrores = new StringBuilder();

                        bool filaVacia = true;
                        for (int col = 1; col <= 7; col++) // Recorre las columnas esperadas (ajusta el número si cambia la cantidad de columnas)
                        {
                            if (wsConceptos.Cells[2 + registro, col].Value != null && !string.IsNullOrWhiteSpace(wsConceptos.Cells[2 + registro, col].Value.ToString()))
                            {
                                filaVacia = false;
                                break; // Si encuentra un dato válido, la fila no está vacía
                            }
                        }

                        // Si la fila está completamente vacía, terminamos el bucle
                        if (filaVacia)
                        {
                            break;
                        }

                        string lstrTareaNombre = wsConceptos.Cells[2 + registro, 1].Value.ToStringSeguro();
                        string lstrEmpresaNombre = wsConceptos.Cells[2 + registro, 2].Value.ToStringSeguro();

                        if (!String.IsNullOrEmpty(lstrTareaNombre))
                        {
                            List<Tareas> lstTareas = dalTAR.L_ClaveExterna(lstrTareaNombre);
                            if (lstTareas.Count != 1)
                                sbErrores.AppendLine("Error al localizar la tarea " + lstrTareaNombre + "; ");
                            else
                                objConcepto.TLE_TAR_Id = lstTareas[0].TAR_Id;
                        }
                        else
                            sbErrores.AppendLine("Error al localizar la tarea " + lstrTareaNombre + "; ");

                        if (!String.IsNullOrEmpty(lstrTareaNombre))
                        {
                            List<Empresas> lstEmpresas = dalEMP.L_ClaveExterna(lstrEmpresaNombre);
                            if (lstEmpresas.Count != 1)
                                sbErrores.AppendLine("Error al localizar la empresa " + lstrEmpresaNombre + "; ");
                            else
                                objConcepto.TLE_EMP_Id = lstEmpresas[0].EMP_Id;
                        }
                        else
                            sbErrores.AppendLine("Error al localizar la empresa " + lstrEmpresaNombre + "; ");

                        int? lintAnyoCelda = wsConceptos.Cells[2 + registro, 3].Value.ToInt();
                        if (lintAnyoCelda.HasValue && lintAnyoCelda == anioConceptosConfig)
                            objConcepto.TLE_Anyo = lintAnyoCelda.Value;
                        else
                            sbErrores.AppendLine("Año incorrecto: " + wsConceptos.Cells[2 + registro, 3].Value + "; ");

                        Tareas_Empresas tareaEmpresa = null;
                        if (objConcepto.TLE_TAR_Id > 0 && objConcepto.TLE_EMP_Id > 0 && objConcepto.TLE_Anyo > 0)
                        {
                            tareaEmpresa = dalTAE.L_PrimaryKey($"{objConcepto.TLE_TAR_Id}{Constantes.SeparadorPK}{objConcepto.TLE_EMP_Id}{Constantes.SeparadorPK}{objConcepto.TLE_Anyo}");
                            if (tareaEmpresa == null)
                                sbErrores.AppendLine($"No existe la combinación {lstrTareaNombre}-{lstrEmpresaNombre}-{lintAnyoCelda}; ");
                        }

                        int? lintMesCelda = wsConceptos.Cells[2 + registro, 4].Value.ToInt();
                        if (lintMesCelda.HasValue && lintMesCelda == lintMes)
                            objConcepto.TLE_Mes = lintMesCelda.Value;
                        else
                            sbErrores.AppendLine("Mes incorrecto: " + wsConceptos.Cells[2 + registro, 4].Value + "; ");

                        decimal? ldecValor = wsConceptos.Cells[2 + registro, 5].Value.ToDecimal();
                        if (ldecValor.HasValue)
                            objConcepto.TLE_Cantidad = ldecValor.Value;
                        else
                            sbErrores.AppendLine("Cantidad incorrecta: " + wsConceptos.Cells[2 + registro, 5].Value + "; ");

                        objConcepto.TLE_Descripcion = wsConceptos.Cells[2 + registro, 6].Value.ToStringSeguro();

                        object celdaInversion = wsConceptos.Cells[2 + registro, 7].Value;

                        if (celdaInversion == null || string.IsNullOrWhiteSpace(celdaInversion.ToString()))
                        {
                            sbErrores.AppendLine("Campo Inversión obligatorio; ");
                        }
                        else
                        {
                            string inversion = celdaInversion.ToString().Trim().ToUpper();

                            if (inversion == "TRUE" || inversion == "SI" || inversion == "SÍ" || inversion == "VERDADERO" || inversion == "1" || inversion == "S")
                            {
                                objConcepto.TLE_Inversion = true;
                            }
                            else if (inversion == "FALSE" || inversion == "NO" || inversion == "FALSO" || inversion == "0" || inversion == "N")
                            {
                                objConcepto.TLE_Inversion = false;
                            }
                            else
                            {
                                sbErrores.AppendLine("Valor incorrecto en Inversión: " + inversion + "; ");
                            }
                        }

                        if (sbErrores.Length == 0)
                        {
                            objConcepto.TLE_PER_Id_Aprobador = tareaEmpresa.TEM_PER_Id_Aprobador;
                            //Para que considere el objeto editable
                            objConcepto.TLE_ESO_Id_Original = (int)Constantes.EstadosSolicitud.PendienteAprobacion;
                            objConcepto.TLE_ESO_Id = (int)Constantes.EstadosSolicitud.PendienteAprobacion;

                            //Se guarda el concepto
                            if (!dalTLE.G(objConcepto, Sesion.SPersonaId))
                            {
                                sbErrores.AppendLine(string.IsNullOrEmpty(objConcepto.MensajeErrorEspecifico)
                                    ? "Ha ocurrido un error al dar de alta el registro"
                                    : objConcepto.MensajeErrorEspecifico);
                            }
                            else if (!string.IsNullOrEmpty(objConcepto.MensajeErrorEspecifico))
                            {
                                sbErrores.AppendLine(objConcepto.MensajeErrorEspecifico);
                            }
                        }

                        if (sbErrores.Length > 0)
                        {
                            // Escribir los errores en la única columna de errores
                            wsConceptos.Cells[2 + registro, columnaErrores].Value = sbErrores.ToString();

                            // Agregar a la lista de errores (como ya lo haces)
                            RegistroExcelRetorno error = new RegistroExcelRetorno
                            {
                                Tarea = wsConceptos.Cells[2 + registro, 1].Value,
                                Empresa = wsConceptos.Cells[2 + registro, 2].Value,
                                TLE_Anyo = wsConceptos.Cells[2 + registro, 3].Value,
                                TLE_Mes = wsConceptos.Cells[2 + registro, 4].Value,
                                TLE_Cantidad = wsConceptos.Cells[2 + registro, 5].Value,
                                TLE_Descripcion = wsConceptos.Cells[2 + registro, 6].Value,
                                TLE_Inversion = wsConceptos.Cells[2 + registro, 7].Value,
                                Errores = sbErrores.ToString()
                            };
                            lstErroresExcel.Add(error);
                        }
                        else
                        {
                            // Eliminar la fila completa si no hay errores
                            wsConceptos.DeleteRow(2 + registro);
                            registro--; // ¡Importante! Porque el siguiente registro ocupará la misma fila
                        }

                        if (sbErrores.Length > 0)
                        {
                            RegistroExcelRetorno error = new RegistroExcelRetorno
                            {
                                Tarea = wsConceptos.Cells[2 + registro, 1].Value,
                                Empresa = wsConceptos.Cells[2 + registro, 2].Value,
                                TLE_Anyo = wsConceptos.Cells[2 + registro, 3].Value,
                                TLE_Mes = wsConceptos.Cells[2 + registro, 4].Value,
                                TLE_Cantidad = wsConceptos.Cells[2 + registro, 5].Value,
                                TLE_Descripcion = wsConceptos.Cells[2 + registro, 6].Value,
                                TLE_Inversion = wsConceptos.Cells[2 + registro, 7].Value,
                                Errores = sbErrores.ToString()
                            };
                            lstErroresExcel.Add(error);
                        }

                        registro++;
                    }

                    excelPackage.Save();

                    if (lstErroresExcel.Count > 0)
                    {
                        string rutaTemp = ConfigurationManager.AppSettings["PathTemporales"];
                        string nombreFichero = $"Errores_Importacion_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        string filePath = Path.Combine(rutaTemp, nombreFichero);

                        System.IO.File.WriteAllBytes(filePath, excelPackage.GetAsByteArray());
                        string fileUrl = Url.Action("DescargarErrores", "Portal", new { fileName = nombreFichero }, Request.Url.Scheme);

                        return Json(new { success = false, erroresExcel = lstErroresExcel, fileUrl });
                    }

                    return Json(new { success = true, message = "Importación completada correctamente." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, excelErroneo = true, message = ex.Message, mensajeExcelErroneo = "Ha ocurrido un error al importar el fichero" });
            }
        }

        [HttpPost]
        public ActionResult ExportarConceptos(int anio, int mes, int? idTarea)
        {
            string nombreFichero = String.Format("Conceptos_{0}_{1}_{2}.xlsx", anio, mes, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            byte[] fileBytes = GenerarExcelConceptos(anio, mes, idTarea);

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreFichero);
        }

        [HttpGet]
        public JsonResult ObtenerLicenciasMs(int idConcepto)
        {
            using (var ctx = new FacturacionInternaDataContext())
            {
                var resultado = (from ms in ctx.Tareas_Empresas_LineasEsfuerzo_LicenciasMS
                                 join lic in ctx.Licencias
                                    on ms.TCL_LIC_Id equals lic.LIC_Id
                                 join ent in ctx.Entes
                                    on ms.TCL_ENT_Id equals ent.ENT_Id
                                 where ms.TCL_TLE_Id == idConcepto
                                 select new
                                 {
                                     Licencia = lic.LIC_Nombre,
                                     Entidad = ent.ENT_Nombre,
                                     Importe = ms.TCL_Importe
                                 })
                                .ToList();

                return Json(resultado, JsonRequestBehavior.AllowGet);
            }
        }

        private List<TicketConceptoDto> ObtenerTicketsPorConcepto(int idConcepto)
        {
            var daoDetickets = new DAL_Tareas_Empresas_LineasEsfuerzo_Tickets();

            return daoDetickets.L(true, null)
                .Where(d => d.TCT_TLE_Id == idConcepto)
                .Select(d => new TicketConceptoDto
                {
                    // --- Campos principales (visibles en tabla) ---
                    TicketId = d.Tickets?.TKC_Id_GLPI ?? 0,
                    DescripcionTicket = d.Tickets?.TKC_Titulo ?? "",
                    Entidad = d.Tickets?.Entes?.ENT_Nombre ?? "(sin entidad)",
                    Importe = d.TCT_Importe,

                    // --- Campos adicionales (para Excel / detalle completo) ---
                    TKC_Id = d.Tickets?.TKC_Id ?? 0,
                    TKC_Id_GLPI = d.Tickets?.TKC_Id_GLPI ?? 0,
                    TKC_Titulo = d.Tickets?.TKC_Titulo,
                    TKC_GrupoAsignado = d.Tickets?.TKC_GrupoAsignado,
                    TKC_Categoria = d.Tickets?.TKC_Categoria,
                    TKC_CTK_Id = d.Tickets?.CategoriasTicket?.CTK_Nombre,
                    TKC_Ubicacion = d.Tickets?.TKC_Ubicacion,
                    TKC_Duracion = d.Tickets?.TKC_Duracion,
                    TKC_Descripcion = d.Tickets?.TKC_Descripcion,
                    TKC_ETK_Id = d.Tickets?.EstadosTicket?.ETK_Nombre ?? "",
                    TKC_TTK_Id = d.Tickets?.TiposTicket?.TTK_Nombre ?? "",
                    TKC_OTK_Id = d.Tickets?.OrigenesTicket?.OTK_Nombre ?? "",
                    TKC_VTK_Id = d.Tickets?.ValidacionesTicket?.VTK_Nombre ?? "",
                    TKC_ENT_Id_Solicitante = d.Tickets?.Entes?.ENT_Nombre ?? "",
                    TKC_ProveedorAsignado = d.Tickets?.TKC_ProveedorAsignado,
                    TKC_GrupoCargo = d.Tickets?.TKC_GrupoCargo,
                    TKC_FechaApertura = d.Tickets?.TKC_FechaApertura,
                    TKC_FechaResolucion = d.Tickets?.TKC_FechaResolucion
                })
                .ToList<TicketConceptoDto>();
        }

        [HttpGet]
        public ActionResult ObtenerTicketsConcepto(int idConcepto)
        {
            var resultado = ObtenerTicketsPorConcepto(idConcepto);
            return Json(resultado, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ObtenerPartesConcepto(int idConcepto)
        {
            var daoPartes = new DAL_Tareas_Empresas_LineasEsfuerzo_Partes();
            var listaPartes = daoPartes.L(true, null)
                .Where(x => x.TCP_TLE_Id == idConcepto)
                .ToList();

            var resultado = listaPartes
                .Select(x => new
                {
                    x.Proyectos_Partes.PPA_PEP_Anyo,
                    x.Proyectos_Partes.PPA_PEP_Mes,
                    ProyectoNombre = x.Proyectos_Partes.Proyectos.PRY_Nombre,
                    Actividad = x.Proyectos_Partes.PPA_Descripcion,
                    Persona = x.Proyectos_Partes.Personas.ApellidosNombre,
                    x.TCP_Fecha,
                    x.TCP_Horas
                })
                .OrderByDescending(x => x.TCP_Fecha)
                .ToList();

            return Json(resultado, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ObtenerAplicacionesPorConcepto(int idConcepto)
        {
            var lista = new DAL_Tareas_Empresas_LineasEsfuerzo_Aplicaciones()
                .L(true, x => x.TCA_TLE_Id == idConcepto)
                .Select(x => new {
                    NombreAplicacion = x.Aplicaciones.APP_Nombre,
                    NombreEntidad = x.Entes.ENT_Nombre,
                    x.TCA_Importe
                }).ToList();
            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ObtenerModulosPorConcepto(int idConcepto)
        {
            var modDict = new DAL_Aplicaciones_Modulos().L(false, null).ToDictionary(e => e.APM_Id, m => m.APM_Nombre);

            var lista = new DAL_Tareas_Empresas_LineasEsfuerzo_Modulos()
                .L(true, x => x.TCM_TLE_Id == idConcepto)
                .Select(x => new {
                    NombreModulo = modDict[x.Aplicaciones_Modulos_Empresas.AME_APM_Id],
                    x.TCM_Importe
                }).ToList();

            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ObtenerModulosRepartoPorConcepto(int idConcepto)
        {
            var modDict = new DAL_Aplicaciones_Modulos().L(false, null).ToDictionary(e => e.APM_Id, m => m.APM_Nombre);

            var lista = new DAL_Tareas_Empresas_LineasEsfuerzo_ModulosReparto()
                .L(true, x => x.TCR_TLE_Id == idConcepto)
                .Select(x => new {
                    NombreModulo = modDict[x.Aplicaciones_Modulos_Tarifas.AMT_APM_Id],
                    x.TCR_ImporteTotal,
                    TCR_Porcentaje = (x.TCR_Porcentaje * 100),
                    x.TCR_Importe
                }).ToList();

            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ObtenerProveedoresAsuntosPorConcepto(int idConcepto)
        {
            var lista = ObtenerProveedoresAsuntosPorConcepto_Interno(idConcepto);

            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        private dynamic ObtenerProveedoresAsuntosPorConcepto_Interno(int idConcepto)
        {
            var asuDict = new DAL_Proveedores_Asuntos().L(false, null).ToDictionary(e => e.PAS_Id, m => m.PAS_Descripcion);
            var prvDict = new DAL_Proveedores_ContratosSoporte().L(false, null).ToDictionary(e => e.PVC_Id, m => $"Horas: {m.PVC_HorasContratadas:F2} / Precio: {m.PVC_PrecioHora:F2} €");

            var lista = new DAL_Tareas_Empresas_LineasEsfuerzo_Asuntos()
                .L(true, x => x.TCA_TLE_Id == idConcepto)
                .Select(x => new {
                    NombreAsunto = asuDict[x.TCA_PAS_Id],
                    NombreContrato = prvDict[x.TCA_PVC_Id],
                    x.TCA_Horas,
                    x.TCA_Importe,
                    Entidad = x.Proveedores_Asuntos != null && x.Proveedores_Asuntos.Entes != null ? x.Proveedores_Asuntos.Entes.ENT_Nombre : String.Empty,
                    Departamento = x.Proveedores_Asuntos != null && x.Proveedores_Asuntos.Entes != null && x.Proveedores_Asuntos.Entes.Empresas_Departamentos != null ? x.Proveedores_Asuntos.Entes.Empresas_Departamentos.EDE_Nombre : String.Empty
                }).ToList();

            return lista;
        }

        [HttpGet]
        public JsonResult ObtenerLicenciasAnualesPorConcepto(int idConcepto)
        {
            var licDict = new DAL_LicenciasAnuales().L(false, null).ToDictionary(e => e.LAN_Id, m => m.LAN_Nombre);
            var entDict = new DAL_Entes().L(false, null).ToDictionary(e => e.ENT_Id, m => m.ENT_Nombre);

            var lista = new DAL_Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales()
                .L(true, x => x.TCL_TLE_Id == idConcepto)
                .Select(x => new {
                    NombreLicenciaAnual = licDict[x.TCL_LAN_Id],
                    NombreEntidad = entDict[x.TCL_ENT_Id],
                    x.TCL_Importe
                }).ToList();

            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DescargarErrores(string fileName)
        {
            string rutaTemp = ConfigurationManager.AppSettings["PathTemporales"];
            string filePath = Path.Combine(rutaTemp, fileName);

            if (System.IO.File.Exists(filePath))
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            else
            {
                return HttpNotFound("El archivo de errores no existe.");
            }
        }

        public static byte[] GenerarFicheroEnlaceContable(List<Facturas> lstFacturas, out string pstrError)
        {
            pstrError = String.Empty;

            if (ConfigurationManager.AppSettings["D365_Activo"] == "1")
            {
                MemoryStream excelMS = new MemoryStream();

                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string urlPlantilla = Path.Combine(basePath, "Plantillas", "Plantilla_D365.xlsx");

                FileInfo fi = new FileInfo(urlPlantilla);

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (ExcelPackage excelPackage = new ExcelPackage(fi))
                {
                    //Get a WorkSheet by index. Note that EPPlus indexes are base 1, not base 0!
                    ExcelWorksheet wsCabecera = excelPackage.Workbook.Worksheets[0];
                    ExcelWorksheet wsLineas = excelPackage.Workbook.Worksheets[1];

                    int lintContadorLineasFactura = 0;
                    for (int registro = 0; registro < lstFacturas.Count; registro++)
                    {
                        //Copia la fila (con el formato) 
                        wsCabecera.Cells[2, 1, 2, 5].Copy(wsCabecera.Cells[2 + registro, 1]);

                        Empresas empresaFacturar = lstFacturas[registro].EmpresaFacturar ?? lstFacturas[registro].Empresas;

                        //SALESORDERNUMBER
                        wsCabecera.Cells[2 + registro, 1].Value = lstFacturas[registro].FAC_NumFactura; //Nº de factura. Ejemplo fichero: PVE06-100001                                                                                                        
                        //CURRENCYCODE
                        wsCabecera.Cells[2 + registro, 2].Value = "EUR";
                        //CUSTOMERPAYMENTMETHODNAME
                        wsCabecera.Cells[2 + registro, 3].Value = empresaFacturar.EMP_FPA_D365; //Codigo D365 de la forma de pago. Ejemplo fichero: GIRO                                                                                                
                        //ORDERINGCUSTOMERACCOUNTNUMBER
                        wsCabecera.Cells[2 + registro, 4].Value = empresaFacturar.EMP_CodigoD365; //Codigo D365 de la empresa. Ejemplo fichero: 0001481MC                                                                                                  
                        //LANGUAGEID
                        wsCabecera.Cells[2 + registro, 5].Value = "es";

                        foreach (Facturas_Tareas_LineasEsfuerzo linea in lstFacturas[registro].Facturas_Tareas_LineasEsfuerzo)
                        {
                            // Copia la fila (con el formato)
                            wsLineas.Cells[2, 1, 2, 10].Copy(wsLineas.Cells[2 + lintContadorLineasFactura, 1]);

                            bool lbolCantidadFija = false;
                            int? tipoTarea = linea?.Tareas_Empresas_LineasEsfuerzo?.Tareas_Empresas?.Tareas?.TAR_TTA_Id;

                            if (tipoTarea.HasValue && tipoTarea == (int)Constantes.TipoTarea.CantidadFija)
                            {
                                lbolCantidadFija = true;
                            }

                            // Variables de acceso seguro
                            var tareas = linea?.Tareas_Empresas_LineasEsfuerzo?.Tareas_Empresas?.Tareas;
                            var descripcionUnidad = tareas?.DescripcionUnidad ?? "";
                            var departamento = tareas?.Secciones?.Departamentos?.DEP_CodigoD365 ?? "";
                            var producto = tareas?.ProductosD365?.PR3_Nombre ?? "";
                            var itemNumber = tareas?.ItemNumbersD365?.IN3_Nombre ?? "";
                            var descripcion = linea?.Tareas_Empresas_LineasEsfuerzo?.TLE_Descripcion ?? "";
                            var cantidad = linea?.Tareas_Empresas_LineasEsfuerzo?.TLE_Cantidad ?? 0;
                            var importeTotal = linea?.Tareas_Empresas_LineasEsfuerzo?.ImporteTotal ?? 0;
                            var importeUnitario = tareas?.TAR_ImporteUnitario ?? 0;

                            // Celdas
                            wsLineas.Cells[2 + lintContadorLineasFactura, 1].Value = "EUR";
                            wsLineas.Cells[2 + lintContadorLineasFactura, 2].Value = $"SSCC-SSCC-{empresaFacturar.EMP_EGrupoD365}--{departamento}-{producto}";
                            wsLineas.Cells[2 + lintContadorLineasFactura, 3].Value = itemNumber;
                            wsLineas.Cells[2 + lintContadorLineasFactura, 4].Value = importeTotal;
                            wsLineas.Cells[2 + lintContadorLineasFactura, 5].Value = descripcion;
                            wsLineas.Cells[2 + lintContadorLineasFactura, 6].Value = lbolCantidadFija ? 1 : cantidad;

                            // Unidad
                            string lstrUnidad = "";
                            switch (tipoTarea)
                            {
                                case (int)Constantes.TipoTarea.PorHoras:
                                    lstrUnidad = "h";
                                    break;

                                case (int)Constantes.TipoTarea.PorUnidades:
                                    lstrUnidad = descripcionUnidad;
                                    break;

                                default:
                                    lstrUnidad = "";
                                    break;
                            }

                            wsLineas.Cells[2 + lintContadorLineasFactura, 7].Value = lstrUnidad;

                            // Nº de factura
                            wsLineas.Cells[2 + lintContadorLineasFactura, 8].Value = lstFacturas[registro].FAC_NumFactura;

                            // Precio unitario
                            wsLineas.Cells[2 + lintContadorLineasFactura, 9].Value = lbolCantidadFija ? cantidad : importeUnitario;

                            // Unidad final (igual que columna 7)
                            wsLineas.Cells[2 + lintContadorLineasFactura, 10].Value = lstrUnidad;

                            lintContadorLineasFactura++;
                        }
                    }

                    //Autoajustamos anchuras:
                    wsCabecera.Cells[wsCabecera.Dimension.Address].AutoFitColumns(0, 50);
                    wsLineas.Cells[wsLineas.Dimension.Address].AutoFitColumns(0, 50);

                    //Save your file
                    excelPackage.SaveAs(excelMS);
                }

                return excelMS.ToArray();
            }
            else
            {
                StringBuilder sbFichero = new StringBuilder();

                foreach (Facturas factura in lstFacturas)
                {
                    Empresas empresaFacturar = factura.EmpresaFacturar ?? factura.Empresas;

                    string l_strTipoFacturaApika = "F", l_strNumFacturaApika = String.Empty;
                    if (factura.FAC_NumFactura.Length == 13) //EAAAAMMXXXXXX (Ej. E202101000001)
                    {
                        l_strNumFacturaApika = factura.FAC_NumFactura.Substring(5, 2) + factura.FAC_NumFactura.Substring(8, 5); //Los 2 caracteres del mes y los ultimos 5 caracteres (Nos saltamos uno porque Transkal solo admite 7 caracteres)
                    }
                    else
                    {
                        //MostrarMensaje(Resources.Resource.litError, "Existen clientes con números de factura erróneos.", Serikat.Comun.TipoMensaje.Error);
                        pstrError = "Existen clientes con números de factura erróneos.";
                        return null;
                    }

                    //DATOS CABECERA DE FACTURA

                    //Prefijo de factura (F "normal", R "rectificativas) + Numero de factura. 
                    sbFichero.AppendLine(String.Format("FA0+{0}+{1}", l_strTipoFacturaApika, l_strNumFacturaApika));
                    //Fecha de factura
                    sbFichero.AppendLine(String.Format("FA5+{0}", factura.FAC_FechaEmision.Value.ToString("dd-MM-yyyy")));
                    //Codigo de cliente a facturar
                    sbFichero.AppendLine(String.Format("FA8+{0}", empresaFacturar.EMP_CodigoAPIKA));
                    //Codigo de forma de pago
                    //Ejemplo: Forma de pago-> Transferencia bancaria correpondencia en TRANSKAl 10(Forma de pago FOR_COD_APIKA)
                    //l_strTextoFicheroApika = "FA9+10"
                    //sbFichero.AppendLine(String.Format("FA9+{0}", factura.FormasPago.FPA_CodigoAPIKA)); //Si creamos la tabla y lo asociamos a la factura
                    sbFichero.AppendLine(String.Format("FA9+{0}", empresaFacturar.EMP_FPA_CodigoAPIKA)); //Si lo guardamos en la empresa cliente y en la factura no se elige
                                                                                                         //Centro de iva
                    sbFichero.AppendLine(String.Format("FA11+{0}", "38"));
                    //Serie de iva
                    sbFichero.AppendLine("FA12+1");
                    //Tipo de iva: 0 es 21%
                    switch (empresaFacturar.EMP_TipoCliente)
                    {
                        case (int)Constantes.TipoClienteEnlace.Peninsular:
                            sbFichero.AppendLine("FA13+0");
                            break;
                        case (int)Constantes.TipoClienteEnlace.Extranjero:
                            sbFichero.AppendLine("FA13+4");
                            break;
                    }
                    //Codigo de moneda+nombre de moneda
                    sbFichero.AppendLine(String.Format("FA48+{0}", "EUR"));
                    //Cambio
                    sbFichero.AppendLine("FA49+1");

                    // FA56: Tipo de Factura
                    if (l_strTipoFacturaApika == "F")
                    {
                        sbFichero.AppendLine("FA56+F1");
                    }
                    else
                    {
                        sbFichero.AppendLine("FA56+R1");
                    }

                    // FA57: Tipo de Factura rectificativa
                    //sbFichero.AppendLine("FA57+I");

                    string valorFA58 = null, valorFA59 = "FA59+", valorFA62 = null, valorFA63 = null;
                    switch (empresaFacturar.EMP_TipoCliente)
                    {
                        case (int)Constantes.TipoClienteEnlace.Peninsular:
                            valorFA58 = "FA58+01";
                            valorFA62 = "FA62+S1";
                            break;
                        case (int)Constantes.TipoClienteEnlace.Extranjero:
                            valorFA58 = "FA58+09";
                            valorFA63 = "FA63+N2";
                            break;
                    }

                    //valorFA59 += factura.LineaActividadFormacion ? Resources.Resource.litActividadFormacion : Resources.Resource.litActividadGeneral;
                    valorFA59 += Resources.Resource.litActividadGeneral;

                    if (valorFA58 != null) sbFichero.AppendLine(valorFA58);
                    sbFichero.AppendLine(valorFA59);
                    if (valorFA62 != null) sbFichero.AppendLine(valorFA62);
                    if (valorFA63 != null) sbFichero.AppendLine(valorFA63);
                    sbFichero.AppendLine("FA70+" + factura.FAC_NumFactura);

                    //DATOS DE VENCIMIENTO
                    //Fecha de vencimiento + importe
                    sbFichero.AppendLine(String.Format("VTO+{0}+{1}", factura.FAC_FechaVencimiento.Value.ToString("dd-MM-yyyy"), factura.ImporteTotalConIVA.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));

                    //DATOS DE CABECERA DE EXPTE
                    //Seccion+numero de dossier
                    //SECCION -> ‘38’
                    //Nº ALBARAN -> Nº correlativo usado en GICAP.Seguir norma aplicada en las facturas enviadas a SK
                    //INDICE -> 0
                    sbFichero.AppendLine(String.Format("EX0+{0}+{1}+0", "38", factura.FAC_Id.ToString().PadLeft(7, '0')));
                    //Codigo de cliente
                    sbFichero.AppendLine(String.Format("EX1+{0}", empresaFacturar.EMP_CodigoAPIKA));
                    sbFichero.AppendLine(String.Format("EX20+{0}", "."));
                    //Nº analítico
                    //Ejemplo: EX38+<<7 dig>>
                    //Debe de ser NUMERICO. De momento pasar valor ‘1’
                    //sbFichero.AppendLine(String.Format("EX38+{0}{1}{2}", l_strNumDel, l_strNumCodActividad, factura.FAC_nID_PRO.ToString().PadLeft(4, '0')));
                    sbFichero.AppendLine("EX38+0000001");
                    //Tipo de gestion tecnica
                    sbFichero.AppendLine("EXGT+UN");

                    IEnumerable<LineaIVAEnlace> lineasIVA;
                    if (empresaFacturar.EMP_TipoCliente == (int)Constantes.TipoClienteEnlace.Peninsular)
                    {
                        lineasIVA = factura.Facturas_Tareas_LineasEsfuerzo.GroupBy(reg => new
                        {
                            reg.Tareas_Empresas_LineasEsfuerzo.Tareas_Empresas.Tareas.TAR_TipoIva
                        })
                        .Select(reg => new LineaIVAEnlace
                        {
                            TAR_TipoIva = reg.Key.TAR_TipoIva,
                            ImporteTotalSinIVA = reg.Sum(tle => tle.Tareas_Empresas_LineasEsfuerzo.ImporteTotal)
                        });
                    }
                    else
                    {
                        lineasIVA = factura.Facturas_Tareas_LineasEsfuerzo.GroupBy(reg => new
                        {
                            reg.Tareas_Empresas_LineasEsfuerzo.Tareas_Empresas.Empresas.EMP_Id //Agrupamos por empresa para no hacerlo por el tipo de IVA (todos deben ser 0)
                        })
                        .Select(reg => new LineaIVAEnlace
                        {
                            TAR_TipoIva = 0,
                            ImporteTotalSinIVA = reg.Sum(tle => tle.Tareas_Empresas_LineasEsfuerzo.ImporteTotal)
                        });
                    }

                    foreach (LineaIVAEnlace linea in lineasIVA)
                    {
                        //DATOS DE CONCEPTOS DE FACTURACION
                        //Concepto + tipo de gestion tecnica
                        sbFichero.AppendLine("CO0+" + factura.ConceptosFacturacion.CFA_CodigoTK + "+UN+");

                        //Descripcion del concepto
                        sbFichero.AppendLine(String.Format("CO1+N/F.{0}", factura.FAC_NumFactura));
                        //%del iva
                        sbFichero.AppendLine(String.Format("CO2+{0}", linea.TAR_TipoIva));
                        //% de descuento
                        sbFichero.AppendLine("CO3+0");
                        //Importe
                        sbFichero.AppendLine(String.Format("CO4+{0}", linea.ImporteTotalSinIVA.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
                    }

                }

                return Encoding.UTF8.GetBytes(sbFichero.ToString());
            }
        }

        public static byte[] GenerarExcelConceptos(int anio, int mes, int? idTarea)
        {
            byte[] fileBytes = null;

            MemoryStream excelMS = new MemoryStream();

            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string urlPlantilla = Path.Combine(basePath, "Plantillas", "Plantilla_Conceptos.xlsx");

            FileInfo fi = new FileInfo(urlPlantilla);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (ExcelPackage excelPackage = new ExcelPackage(fi))
            {
                //Get a WorkSheet by index. Note that EPPlus indexes are base 1, not base 0!
                ExcelWorksheet wsConceptos = excelPackage.Workbook.Worksheets[0];

                DAL_Tareas_Empresas_LineasEsfuerzo dalTLE = new DAL_Tareas_Empresas_LineasEsfuerzo
                {
                    ModoConsultaClaveExterna = DAL_Tareas_Empresas_LineasEsfuerzo.TipoClaveExterna.AnyoMes
                };
                List<Tareas_Empresas_LineasEsfuerzo> lstConceptos = dalTLE.L_ClaveExterna(string.Format("{1}{0}{2}", Constantes.SeparadorPK, anio, mes));

                if (idTarea.HasValue)
                    lstConceptos = lstConceptos.Where(r => r.TLE_TAR_Id.Equals(idTarea.Value)).ToList();

                for (int registro = 0; registro < lstConceptos.Count; registro++)
                {
                    //Copia la fila (con el formato) 
                    wsConceptos.Cells[2, 1, 2, 8].Copy(wsConceptos.Cells[2 + registro, 1]);

                    wsConceptos.Cells[2 + registro, 1].Value = lstConceptos[registro].TareaNombre;
                    wsConceptos.Cells[2 + registro, 2].Value = lstConceptos[registro].EmpresaNombre;
                    wsConceptos.Cells[2 + registro, 3].Value = lstConceptos[registro].TLE_Anyo;
                    wsConceptos.Cells[2 + registro, 4].Value = lstConceptos[registro].TLE_Mes;
                    wsConceptos.Cells[2 + registro, 5].Value = lstConceptos[registro].TLE_Cantidad;
                    wsConceptos.Cells[2 + registro, 6].Value = lstConceptos[registro].TLE_Descripcion;
                    wsConceptos.Cells[2 + registro, 7].Value = (lstConceptos[registro].TLE_Inversion ? "SI" : "NO");
                }

                //Eliminamos la columna "Error"
                wsConceptos.DeleteColumn(8);

                //Autoajustamos anchuras:
                wsConceptos.Cells[wsConceptos.Dimension.Address].AutoFitColumns(0, 50);

                //Save your file
                excelPackage.SaveAs(excelMS);

                string nombreFichero = String.Format("Conceptos_{0}_{1}_{2}.xlsx", anio, mes, DateTime.Now.ToString("yyyyMMdd_HHmmss"));

                string rutaTemp = ConfigurationManager.AppSettings["PathTemporales"];
                string filePath = Path.Combine(rutaTemp, nombreFichero);
                System.IO.File.WriteAllBytes(filePath, excelPackage.GetAsByteArray());

                if (System.IO.File.Exists(filePath))
                {
                    fileBytes = System.IO.File.ReadAllBytes(filePath);
                }
            }

            return fileBytes;
        }

        public ActionResult DescargarPlantillaConceptos()
        {
            string rutaPlantilla = Server.MapPath("~/Plantillas/Plantilla_Conceptos.xlsx");

            if (!System.IO.File.Exists(rutaPlantilla))
            {
                return HttpNotFound("El archivo no existe.");
            }

            return File(rutaPlantilla, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plantilla_Conceptos.xlsx");
        }

        [HttpGet]
        public ActionResult ExportarTicketsConcepto(int idConcepto)
        {
            List<TicketConceptoDto> datos = ObtenerTicketsPorConcepto(idConcepto);

            // Generar Excel con EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Tickets");

                // Encabezados
                var headers = new[]
                {
                    "Ticket GLPI","Título","Entidad","Importe","Duración (min)",
                    "TKC_Id","Grupo Asignado","Categoría","Categoría Calculada",
                    "Ubicación","Descripción","Estado","Tipo","Origen","Validación",
                    "Solicitante (ENT)","Proveedor Asignado","Grupo Cargo",
                    "Fecha Apertura","Fecha Resolución"
                };
                for (int c = 0; c < headers.Length; c++)
                    ws.Cells[1, c + 1].Value = headers[c];

                // Filas
                int r = 2;
                foreach (var x in datos)
                {
                    ws.Cells[r, 1].Value = x.TicketId;
                    ws.Cells[r, 2].Value = x.DescripcionTicket;
                    ws.Cells[r, 3].Value = x.Entidad;
                    ws.Cells[r, 4].Value = x.Importe;
                    ws.Cells[r, 5].Value = x.TKC_Duracion;

                    ws.Cells[r, 6].Value = x.TKC_Id;
                    ws.Cells[r, 7].Value = x.TKC_GrupoAsignado;
                    ws.Cells[r, 8].Value = x.TKC_Categoria;
                    ws.Cells[r, 9].Value = x.TKC_CTK_Id;
                    ws.Cells[r, 10].Value = x.TKC_Ubicacion;
                    ws.Cells[r, 11].Value = x.TKC_Descripcion;
                    ws.Cells[r, 12].Value = x.TKC_ETK_Id;
                    ws.Cells[r, 13].Value = x.TKC_TTK_Id;
                    ws.Cells[r, 14].Value = x.TKC_OTK_Id;
                    ws.Cells[r, 15].Value = x.TKC_VTK_Id;
                    ws.Cells[r, 16].Value = x.TKC_ENT_Id_Solicitante;
                    ws.Cells[r, 17].Value = x.TKC_ProveedorAsignado;
                    ws.Cells[r, 18].Value = x.TKC_GrupoCargo;

                    if (x.TKC_FechaApertura.HasValue)
                    {
                        ws.Cells[r, 19].Value = x.TKC_FechaApertura.Value;
                        ws.Cells[r, 19].Style.Numberformat.Format = "dd/MM/yyyy";
                    }
                    if (x.TKC_FechaResolucion.HasValue)
                    {
                        ws.Cells[r, 20].Value = x.TKC_FechaResolucion.Value;
                        ws.Cells[r, 20].Style.Numberformat.Format = "dd/MM/yyyy";
                    }
                    r++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var bytes = pck.GetAsByteArray();
                var fileName = $"TicketsConcepto_{idConcepto}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [HttpGet]
        public ActionResult ExportarAsuntosConcepto(int idConcepto)
        {
            var datos = ObtenerProveedoresAsuntosPorConcepto_Interno(idConcepto);

            // Generar Excel con EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Tickets");

                // Encabezados
                var headers = new[]
                {
                    "Asunto","Contrato","Entidad", "Departamento","Horas","Importe"
                };
                for (int c = 0; c < headers.Length; c++)
                    ws.Cells[1, c + 1].Value = headers[c];

                // Filas
                int r = 2;
                foreach (var x in datos)
                {
                    ws.Cells[r, 1].Value = x.NombreAsunto;
                    ws.Cells[r, 2].Value = x.NombreContrato;
                    ws.Cells[r, 3].Value = x.Entidad;
                    ws.Cells[r, 4].Value = x.Departamento;
                    ws.Cells[r, 5].Value = x.TCA_Horas;
                    ws.Cells[r, 6].Value = x.TCA_Importe;
                    r++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var bytes = pck.GetAsByteArray();
                var fileName = $"AsuntosConcepto_{idConcepto}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [HttpGet]
        public ActionResult ExportarLicenciasMS(int idConcepto)
        {
            var dao = new DAL_Tareas_Empresas_LineasEsfuerzo_LicenciasMS();

            var lista = dao.L(true, null)
                .Where(d => d.TCL_TLE_Id == idConcepto)
                .ToList();

            var datos = lista.Select(d => new
            {
                Licencia = d.Licencias?.LIC_Nombre ?? "",
                LicenciaMS = d.Licencias?.LIC_NombreMS ?? "",
                Entidad = d.Entes?.ENT_Nombre ?? "",
                Importe = d.TCL_Importe,
                // Nombres de tareas asociadas (pueden ser null)
                TareaSW = d.Licencias?.Tareas1?.TAR_Nombre ?? "",
                TareaAntivirus = d.Licencias?.Tareas2?.TAR_Nombre ?? "",
                TareaBackup = d.Licencias?.Tareas3?.TAR_Nombre ?? ""
            }).ToList();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("LicenciasMS");

                // Encabezados
                var headers = new[]
                {
                    "Licencia","LicenciaMS","Entidad","Importe",
                    "Tarea SW","Tarea Antivirus","Tarea Backup"
                };
                for (int c = 0; c < headers.Length; c++)
                    ws.Cells[1, c + 1].Value = headers[c];

                // Filas
                int r = 2;
                foreach (var x in datos)
                {
                    ws.Cells[r, 1].Value = x.Licencia;
                    ws.Cells[r, 2].Value = x.LicenciaMS;
                    ws.Cells[r, 3].Value = x.Entidad;
                    ws.Cells[r, 4].Value = x.Importe;
                    ws.Cells[r, 5].Value = x.TareaSW;
                    ws.Cells[r, 6].Value = x.TareaAntivirus;
                    ws.Cells[r, 7].Value = x.TareaBackup;
                    r++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var bytes = pck.GetAsByteArray();
                var fileName = $"LicenciasMSConcepto_{idConcepto}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }

    public class TicketConceptoDto
    {
        public int TicketId { get; set; }
        public string DescripcionTicket { get; set; }
        public string Entidad { get; set; }
        public decimal? Importe { get; set; }
        public double? TKC_Duracion { get; set; }

        public int TKC_Id { get; set; }
        public int TKC_Id_GLPI { get; set; }
        public string TKC_Titulo { get; set; }
        public string TKC_GrupoAsignado { get; set; }
        public string TKC_Categoria { get; set; }
        public string TKC_CTK_Id { get; set; }
        public string TKC_Ubicacion { get; set; }
        public string TKC_Descripcion { get; set; }
        public string TKC_ETK_Id { get; set; }
        public string TKC_TTK_Id { get; set; }
        public string TKC_OTK_Id { get; set; }
        public string TKC_VTK_Id { get; set; }
        public string TKC_ENT_Id_Solicitante { get; set; }
        public string TKC_ProveedorAsignado { get; set; }
        public string TKC_GrupoCargo { get; set; }
        public DateTime? TKC_FechaApertura { get; set; }
        public DateTime? TKC_FechaResolucion { get; set; }
    }

}