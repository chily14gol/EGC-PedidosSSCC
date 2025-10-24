using AccesoDatos;
using OfficeOpenXml;
using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Linq;
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
    public class TareasController : BaseController
    {
        [HttpGet]
        public ActionResult ObtenerTareaEmpresas(int idTarea)
        {
            DAL_Tareas_Empresas dal = new DAL_Tareas_Empresas();
            Expression<Func<Tareas_Empresas, bool>> filtroTarea = t => t.TEM_TAR_Id == idTarea;
            List<Tareas_Empresas> listaEmpresas = dal.L(false, filtroTarea);

            var empresasFiltro = listaEmpresas
                .Select(i => new
                {
                    i.TEM_TAR_Id,
                    i.TEM_EMP_Id,
                    i.EmpresaNombre,
                    i.TEM_Anyo,
                    i.TEM_Elementos,
                    i.TEM_Presupuesto
                })
                .OrderByDescending(i => i.TEM_Anyo)
                .ToList();

            return new LargeJsonResult { Data = empresasFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerTareaEmpresa(int idTarea, int idEmpresa, int anio)
        {
            DAL_Tareas_Empresas dal = new DAL_Tareas_Empresas();
            Tareas_Empresas objTareaEmpresa = dal.L_PrimaryKey(idTarea + "|" + idEmpresa + "|" + anio);

            if (objTareaEmpresa == null)
            {
                return new LargeJsonResult { Data = null, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }

            var empresaFiltro = new
            {
                objTareaEmpresa.TEM_EMP_Id,
                objTareaEmpresa.TEM_Anyo,
                objTareaEmpresa.TEM_Elementos,
                objTareaEmpresa.TEM_Presupuesto,
                objTareaEmpresa.TEM_PER_Id_Aprobador
            };

            return new LargeJsonResult { Data = empresaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult CrearTareaEmpresa(int idTarea, int idEmpresa, int anio, int idAprobador,
            decimal unidades, decimal presupuesto)
        {
            try
            {
                DAL_Tareas_Empresas dal = new DAL_Tareas_Empresas();
                Tareas_Empresas objEmpresa = new Tareas_Empresas
                {
                    TEM_TAR_Id = idTarea,
                    TEM_EMP_Id = idEmpresa,
                    TEM_Anyo = anio,
                    TEM_Elementos = unidades,
                    TEM_Presupuesto = presupuesto,
                    TEM_Vigente = true,
                    TEM_ESO_Id = (int)Constantes.EstadosSolicitud.Aprobado,
                    TEM_PER_Id_Aprobador = idAprobador,
                    TEM_FechaAprobacion = null,
                    TEM_ComentarioAprobacion = String.Empty
                };

                // Si no es editable, devolvemos mensaje explícito
                if (!objEmpresa.Editable)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No se puede editar esta tarea-empresa en el estado actual."
                    });
                }

                // Si es editable, intentamos guardar
                bool guardarOK = dal.G(objEmpresa, Sesion.SPersonaId);
                LimpiarCache(TipoCache.Conceptos);
                if (!guardarOK)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Ha ocurrido un error al guardar."
                    });
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarTareaEmpresa(int idTarea, int idEmpresa, int anio)
        {
            try
            {
                DAL_Tareas_Empresas dal = new DAL_Tareas_Empresas();
                bool deleteOK = dal.Eliminar(idTarea, idEmpresa, anio);

                return Json(new { success = deleteOK });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerTareaDetalle(int idTarea)
        {
            DAL_Tareas dal = new DAL_Tareas();
            Tareas objTarea = dal.L_PrimaryKey(idTarea, false);

            if (objTarea == null)
            {
                return new LargeJsonResult { Data = null, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }

            var tareaFiltro = new
            {
                objTarea.TAR_Id,
                objTarea.TAR_Nombre,
                objTarea.SeccionNombre,
                objTarea.TAR_SEC_Id,
                objTarea.TipoTarea,
                objTarea.TAR_TTA_Id,
                objTarea.TAR_ImporteUnitario,
                objTarea.TAR_PR3_Id,
                objTarea.TAR_IN3_Id,
                objTarea.TAR_TipoIva,
                objTarea.DescripcionUnidad,
                objTarea.TAR_Activo,
                objTarea.TAR_UTA_Id
            };

            return new LargeJsonResult { Data = tareaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerTareas()
        {
            DAL_Tareas dal = new DAL_Tareas();
            ObjectCache cache = MemoryCache.Default;

            List<Tareas> lista = dal.L(false, null);

            // Obtener la última fecha de modificación
            DateTime? ultimaModificacion = lista.Max(t => (DateTime?)t.FechaModificacion);

            string cacheKey = $"Tareas_{ultimaModificacion:yyyyMMddHHmmss}";

            if (cache.Contains(cacheKey))
            {
                var cached = cache.Get(cacheKey);
                return new LargeJsonResult
                {
                    Data = cached,
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }

            var tareasFiltro = lista.Select(i => new
            {
                i.TAR_Id,
                i.TAR_Nombre,
                TAR_Seccion = i.SeccionNombre,
                TAR_Tipo = i.TipoTarea,
                i.TAR_ImporteUnitario,
                i.ProductosD365.PR3_Nombre,
                i.ItemNumbersD365.IN3_Nombre,
                i.TAR_Activo,
                Anios = i.Tareas_Empresas
                    .Select(te => te.TEM_Anyo)
                    .Distinct()
                    .OrderBy(y => y)
                    .ToList()
            });

            // Guardar en caché
            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(15)
            };
            cache.Set(cacheKey, tareasFiltro, policy);

            return new LargeJsonResult
            {
                Data = tareasFiltro,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpPost]
        public JsonResult EliminarTarea(int idTarea)
        {
            try
            {
                DAL_Tareas dal = new DAL_Tareas();
                bool ok = dal.D(idTarea);

                if (ok)
                {
                    LimpiarCache(TipoCache.Tareas);
                }

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerComboTareaEmpresa(bool verTodas)
        {
            DAL_Tareas_Empresas dal = new DAL_Tareas_Empresas();

            DAL_Configuraciones dalConfig = new DAL_Configuraciones();
            Configuraciones objConfig = dalConfig.L_PrimaryKey((int)Constantes.Configuracion.AnioConcepto);
            int anioConcepto = (objConfig != null ? Convert.ToInt32(objConfig.CFG_Valor) : DateTime.Now.Year);

            List<Tareas_Empresas> listaTareasEmpresa;
            if (verTodas)
                listaTareasEmpresa = dal.L(true, null);
            else
            {
                Expression<Func<Tareas_Empresas, bool>> filtroAprobados = p => p.TEM_Vigente
                    && p.TEM_ESO_Id.Equals((int)Constantes.EstadosSolicitud.Aprobado)
                    && p.TEM_Anyo == anioConcepto;

                listaTareasEmpresa = dal.L(false, filtroAprobados);
            }

            var tareasEmpresaFiltro = listaTareasEmpresa
                .Select(i => new
                {
                    i.TEM_TAR_Id,
                    i.TEM_EMP_Id,
                    i.TEM_Anyo,
                    EmpresaNombre = i.Tareas.TAR_Nombre + "-" + i.EmpresaNombre
                })
                .ToList();

            return new LargeJsonResult { Data = tareasEmpresaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarTarea(Tareas objTarea)
        {
            try
            {
                DAL_Tareas dal = new DAL_Tareas();
                bool ok = dal.G(objTarea, Sesion.SPersonaId);

                if (ok)
                {
                    LimpiarCache(TipoCache.Tareas);
                }

                return Json(new { success = ok, idNuevo = objTarea.TAR_Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ImportarExcelTareas(HttpPostedFileBase archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.ContentLength == 0)
            {
                return Json(new { success = false, message = "No se ha seleccionado un archivo." });
            }

            try
            {
                var registros = new List<Tareas_Empresas>();
                List<RegistroExcelRetorno> lstErroresExcel = new List<RegistroExcelRetorno>();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var excelPackage = new ExcelPackage(archivoExcel.InputStream))
                {
                    ExcelWorksheet wsConceptos = excelPackage.Workbook.Worksheets.First();

                    // **Validación del formato del Excel**
                    if (wsConceptos.Dimension == null || wsConceptos.Dimension.End.Column < 3)
                    {
                        return Json(new { success = false, excelErroneo = true, message = "El archivo no tiene el formato esperado.", mensajeExcelErroneo = "Ha ocurrido un error al importar el fichero" });
                    }

                    // **Validar los nombres de las cabeceras**
                    string colA = wsConceptos.Cells[1, (int)Columnas.A].Text.Trim();
                    string colB = wsConceptos.Cells[1, (int)Columnas.B].Text.Trim();
                    string colC = wsConceptos.Cells[1, (int)Columnas.C].Text.Trim();

                    if (colA.ToLower() != "tarea" || colB.ToLower() != "empresa" || colC.ToLower() != "presupuesto - unidades")
                    {
                        return Json(new { success = false, excelErroneo = true, message = "Las cabeceras del archivo no coinciden con el formato esperado.", mensajeExcelErroneo = "El archivo Excel no tiene el formato correcto." });
                    }

                    DAL_Tareas_Empresas dalTEM = new DAL_Tareas_Empresas();
                    DAL_Tareas dalTAR = new DAL_Tareas();
                    DAL_Empresas dalEMP = new DAL_Empresas();

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
                        Tareas_Empresas tareaEmpresa = new Tareas_Empresas();

                        string lstrTarea = wsConceptos.Cells[2 + registro, 1].Value != null ? wsConceptos.Cells[2 + registro, 1].Value.ToString() : String.Empty;
                        if (String.IsNullOrEmpty(lstrTarea))
                            lbolHayDatos = false;
                        else
                        {
                            StringBuilder sbErrores = new StringBuilder();
                            dalTAR.ModoConsultaClaveExterna = DAL_Tareas.TipoClaveExterna.Nombre;
                            Tareas tarea = dalTAR.L_ClaveExterna(lstrTarea).FirstOrDefault();

                            if (tarea != null)
                            {
                                string lstrEmpresa = wsConceptos.Cells[2 + registro, 2].Value != null ? wsConceptos.Cells[2 + registro, 2].Value.ToString() : String.Empty;
                                dalEMP.ModoConsultaClaveExterna = DAL_Empresas.TipoClaveExterna.Nombre;
                                Empresas empresa = dalEMP.L_ClaveExterna(lstrEmpresa).FirstOrDefault();

                                if (empresa != null)
                                {
                                    tareaEmpresa.TEM_EMP_Id = empresa.EMP_Id;
                                    tareaEmpresa.TEM_TAR_Id = tarea.TAR_Id;
                                    tareaEmpresa.TEM_Anyo = Parametros.AnyoConcepto;
                                    tareaEmpresa.TEM_PER_Id_Aprobador = empresa.EMP_PER_Id_AprobadorDefault;
                                    tareaEmpresa.TEM_Vigente = true;
                                    tareaEmpresa.TEM_ESO_Id = (int)Constantes.EstadosSolicitud.Aprobado;

                                    Tareas_Empresas tareaEmpresaExistente = dalTEM.L_PrimaryKey(
                                        $"{tareaEmpresa.TEM_TAR_Id}{Constantes.SeparadorPK}{tareaEmpresa.TEM_EMP_Id}{Constantes.SeparadorPK}{tareaEmpresa.TEM_Anyo}",
                                        sinFiltrar: true
                                    );

                                    if (tareaEmpresaExistente != null)
                                        sbErrores.AppendLine($"Ya existe en la tarea datos para la empresa {lstrEmpresa} en el año {Parametros.AnyoConcepto}");
                                }
                                else
                                    sbErrores.AppendLine("Error al localizar la empresa " + lstrEmpresa);

                                decimal? ldecValor = wsConceptos.Cells[2 + registro, 3].Value.ToDecimal();
                                if (ldecValor.HasValue)
                                {
                                    switch (tarea.TAR_TTA_Id)
                                    {
                                        case (int)Constantes.TipoTarea.PorHoras:
                                        case (int)Constantes.TipoTarea.PorUnidades:
                                            tareaEmpresa.TEM_Elementos = ldecValor.Value;
                                            tareaEmpresa.TEM_Presupuesto = tareaEmpresa.TEM_Elementos * tarea.TAR_ImporteUnitario.Value; // Cantidad x Precio unitario
                                            break;
                                        case (int)Constantes.TipoTarea.CantidadFija:
                                        default:
                                            tareaEmpresa.TEM_Elementos = 1;
                                            tareaEmpresa.TEM_Presupuesto = ldecValor.Value;
                                            break;
                                    }
                                }
                                else
                                    sbErrores.AppendLine("Nº de elementos incorrecto: " + wsConceptos.Cells[2 + registro, 3].Value);
                            }
                            else
                                sbErrores.AppendLine("Error al localizar la tarea " + lstrTarea);

                            if (sbErrores.Length == 0)
                            {
                                //Se guarda la tarea
                                if (!dalTEM.G(tareaEmpresa, Sesion.SPersonaId))
                                {
                                    if (!String.IsNullOrEmpty(dalTEM.MensajeErrorEspecifico))
                                        sbErrores.AppendLine(dalTEM.MensajeErrorEspecifico);
                                    else
                                        sbErrores.AppendLine("Ha ocurrido un error al dar de alta el registro");
                                }
                            }

                            // Escribir los errores en la única columna de errores
                            wsConceptos.Cells[2 + registro, columnaErrores].Value = sbErrores.Length > 0 ? sbErrores.ToString() : "OK";

                            if (sbErrores.Length > 0)
                            {
                                RegistroExcelRetorno error = new RegistroExcelRetorno
                                {
                                    Tarea = wsConceptos.Cells[2 + registro, 1].Value,
                                    Empresa = wsConceptos.Cells[2 + registro, 2].Value,
                                    Elementos = wsConceptos.Cells[2 + registro, 3].Value,
                                    Errores = sbErrores.ToString()
                                };
                                lstErroresExcel.Add(error);
                            }

                            registro++;
                        }
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

        public ActionResult DescargarPlantillaTareasEmpresas()
        {
            string rutaPlantilla = Server.MapPath("~/Plantillas/Plantilla_Tareas_Empresas.xlsx");

            if (!System.IO.File.Exists(rutaPlantilla))
            {
                return HttpNotFound("El archivo no existe.");
            }

            return File(rutaPlantilla, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plantilla_Tareas_Empresas.xlsx");
        }

        [HttpGet]
        public ActionResult ObtenerTareaEmpresaDetalle(int tareaId, int empresaId, int anyo)
        {
            var tareasEmpresasDal = new DAL_Tareas_Empresas();
            var tareaDal = new DAL_Tareas();

            var tareaEmpresa = tareasEmpresasDal
                .L_ClaveExterna(tareaId)
                .FirstOrDefault(te => te.TEM_EMP_Id == empresaId && te.TEM_Anyo == anyo);

            if (tareaEmpresa == null)
                return HttpNotFound("No se encontró la relación tarea-empresa.");

            var tarea = tareaDal.L_PrimaryKey(tareaId, false);
            if (tarea != null)
            {
                tareaEmpresa.TEM_PresupuestoConsumido = tareasEmpresasDal
                    .PresupuestoConsumido(tareaId, empresaId, anyo)
                    .ToDouble();

                if (tarea.TAR_ImporteUnitario.HasValue)
                    tareaEmpresa.TAR_ImporteUnitario = tarea.TAR_ImporteUnitario.Value;
            }

            var resultado = new
            {
                tareaEmpresa.TEM_TAR_Id,
                tareaEmpresa.TEM_EMP_Id,
                tareaEmpresa.TEM_Anyo,
                tareaEmpresa.TEM_Presupuesto,
                tareaEmpresa.TEM_PresupuestoConsumido,
                tareaEmpresa.TAR_ImporteUnitario
            };

            return new LargeJsonResult
            {
                Data = resultado,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }
    }
}