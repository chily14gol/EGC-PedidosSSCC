using AccesoDatos;
using OfficeOpenXml;
using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using static PedidosSSCC.Comun.Constantes;

namespace PedidosSSCC.Controllers
{
    public class ProveedoresController : BaseController
    {
        public ActionResult Proveedores() => View();
        public ActionResult ProveedoresAsuntos() => View();

        [HttpGet]
        public ActionResult ObtenerProveedores()
        {
            DAL_Proveedores dal = new DAL_Proveedores();
            var lista = dal.L(false, null)
                .Select(p => new
                {
                    p.PRV_Id,
                    p.PRV_CIF,
                    p.PRV_Nombre,
                    p.PRV_Activo,
                    p.PRV_TAR_Id_Soporte,
                    p.PRV_PlantillaExcel
                })
                .OrderBy(i => i.PRV_Nombre)
                .ToList();

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerProveedoresSoporte()
        {
            DAL_Proveedores dal = new DAL_Proveedores();
            var lista = dal.L(false, null)
                .Select(p => new
                {
                    p.PRV_Id,
                    p.PRV_CIF,
                    p.PRV_Nombre,
                    p.PRV_Activo,
                    p.PRV_TAR_Id_Soporte
                })
                .Where(i => i.PRV_TAR_Id_Soporte != null)
                .OrderBy(i => i.PRV_Nombre)
                .ToList();

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerPeriodoFact()
        {
            DateTime fecha = ObtenerPeriodoFacturacion();

            var periodo = new
            {
                Mes = fecha.Month,
                Anyo = fecha.Year
            };

            return Json(periodo, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GuardarProveedor(Proveedores proveedor)
        {
            try
            {
                proveedor.FechaModificacion = DateTime.Now;
                proveedor.PER_Id_Modificacion = Sesion.SPersonaId;

                if (proveedor.PRV_Id == 0)
                {
                    proveedor.FechaAlta = DateTime.Now;
                }

                DAL_Proveedores dal = new DAL_Proveedores();
                bool ok = dal.G(proveedor, Sesion.SPersonaId);

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarProveedor(int idProveedor)
        {
            try
            {
                DAL_Proveedores dal = new DAL_Proveedores();
                dal.D(idProveedor);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerContratoSoporte()
        {
            try
            {
                var dal = new DAL_Proveedores_ContratosSoporte();
                var contratos = dal.L(false, null)
                    .Select(c => new
                    {
                        c.PVC_Id,
                        c.PVC_PRV_Id,
                        ProveedorNombre = c.Proveedores?.PRV_Nombre,
                        c.PVC_FechaInicio,
                        c.PVC_FechaFin,
                        c.PVC_PrecioHora,
                        c.PVC_HorasContratadas
                    }).ToList();

                return Json(contratos, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult GuardarContratoSoporte(ContratoSoporteDto contrato)
        {
            try
            {
                var dal = new DAL_Proveedores_ContratosSoporte();
                bool ok = dal.G(contrato.objContrato, Sesion.SPersonaId);

                DAL_Proveedores_ContratosSoporte_Reparto dalReparto = new DAL_Proveedores_ContratosSoporte_Reparto();
                dalReparto.EliminarPorContrato(contrato.objContrato.PVC_Id);

                if (contrato.Reparto != null)
                {
                    foreach (RepartoItemDto objReparto in contrato.Reparto)
                    {
                        Proveedores_ContratosSoporte_Reparto objNuevo = new Proveedores_ContratosSoporte_Reparto();
                        objNuevo.PVR_PVC_Id = contrato.objContrato.PVC_Id;
                        objNuevo.PVR_EMP_Id = objReparto.EMP_Id;
                        objNuevo.PVR_Porcentaje = objReparto.Porcentaje;
                        bool ok2 = dalReparto.G(objNuevo, Sesion.SPersonaId);
                    }
                }

                string mensaje = "";
                if (!ok)
                    mensaje = "Este contrato se solapa en fechas con otro contrato existente para este proveedor";

                return Json(new { success = ok, message = mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarContratoSoporte(int idContrato)
        {
            try
            {
                var dalReparto = new DAL_Proveedores_ContratosSoporte_Reparto();
                dalReparto.EliminarPorContrato(idContrato);

                var dal = new DAL_Proveedores_ContratosSoporte();
                bool resultado = dal.D(idContrato);

                return Json(new { success = resultado });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerAsuntos()
        {
            // 1) Obtengo todos los asuntos
            var dalAsuntos = new DAL_Proveedores_Asuntos();
            var listaAsuntos = dalAsuntos.L(false, null);

            // 2) Cargo todos los datos en un diccionario para busqueda rápida.
            var dalProv = new DAL_Proveedores();
            var listaProv = dalProv.L(false, null).ToDictionary(p => p.PRV_Id, p => p.PRV_Nombre);

            var dalTickets = new DAL_Tickets();
            var listaTickets = dalTickets.L(false, null).Where(t => t.TKC_Id_GLPI != null).ToDictionary(p => p.TKC_Id_GLPI, p => p.TKC_Titulo);

            var dalEntes = new DAL_Entes();
            var listaEntes = dalEntes.L(false, null).ToDictionary(p => p.ENT_Id, p => p.ENT_Nombre);

            var dalEmp = new DAL_Empresas();
            var listaEmpresas = dalEmp.L(false, null).ToDictionary(p => p.EMP_Id, p => p.EMP_Nombre);

            var dalT = new DAL_Tareas();
            var listaTareas = dalT.L(false, null).ToDictionary(t => t.TAR_Id, t => t.TAR_Nombre);

            // 3) Proyección con join manual
            var resultado = listaAsuntos
                .Select(p => new
                {
                    p.PAS_Id,
                    p.PAS_PRV_Id,
                    ProveedorNombre = listaProv.TryGetValue(p.PAS_PRV_Id, out var nombreProv) ? nombreProv : String.Empty,
                    p.PAS_Anyo,
                    p.PAS_Mes,
                    p.PAS_CodigoExterno,
                    p.PAS_Fecha,
                    p.PAS_TKC_Id_GLPI,
                    TicketTitulo = listaTickets.TryGetValue(p.PAS_TKC_Id_GLPI.HasValue ? p.PAS_TKC_Id_GLPI.Value : 0, out var titulo) ? titulo : String.Empty,
                    p.PAS_ENT_Id,
                    EntidadNombre = listaEntes.TryGetValue(p.PAS_ENT_Id.HasValue ? p.PAS_ENT_Id.Value : 0, out var nombreEnte) ? nombreEnte : String.Empty,
                    p.PAS_EMP_Id,
                    EmpresaNombre = listaEmpresas.TryGetValue(p.PAS_EMP_Id.HasValue ? p.PAS_EMP_Id.Value : 0, out var nombreEmpresa) ? nombreEmpresa : String.Empty,
                    p.PAS_TAR_Id,
                    TareaNombre = (p.PAS_TAR_Id.HasValue && listaTareas.TryGetValue(p.PAS_TAR_Id.Value, out var nomTar)) ? nomTar : String.Empty,
                    p.PAS_Descripcion,
                    p.PAS_Horas,
                    p.PAS_Importe,
                    p.PAS_NumFacturaP
                })
                .ToList();

            return new LargeJsonResult
            {
                Data = resultado,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpGet]
        public ActionResult ObtenerEmpresasCombo()
        {
            var dal = new DAL_Empresas();
            var lista = dal.L(false, null)
                .Select(e => new { e.EMP_Id, e.EMP_Nombre })
                .ToList();
            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerTicketsGLPICombo(string q)
        {
            var dal = new DAL_Tickets();
            var query = dal.L(false, t =>
                string.IsNullOrEmpty(q)
                || t.TKC_Titulo.Contains(q)
                || t.TKC_Id_GLPI.ToString().Contains(q)
            );

            var lista = query
                .OrderByDescending(t => t.TKC_Id_GLPI)
                .Select(t => new
                {
                    id = t.TKC_Id_GLPI,
                    text = $"#{t.TKC_Id_GLPI} – {t.TKC_Titulo}"
                })
                .Take(20)
                .ToList();

            return Json(new { results = lista }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GuardarAsunto(Proveedores_Asuntos dto)
        {
            try
            {
                dto.PAS_Fecha = dto.PAS_Fecha;
                bool ok = new DAL_Proveedores_Asuntos().G(dto, Sesion.SPersonaId);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarAsunto(int id)
        {
            try
            {
                bool ok = new DAL_Proveedores_Asuntos().D(id);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerFacturasPCombo(int proveedorId, int anyo, int mes)
        {
            var dal = new DAL_FacturasP();
            var lst = dal.L(false, null)
                         .Where(f => f.FAP_PRV_Id == proveedorId
                                  && f.FAP_Fecha.Year == anyo
                                  && f.FAP_Fecha.Month == mes
                                  && !f.FAP_Cerrada)
                         .Select(f => new { f.FAP_Id, f.FAP_NumFactura })
                         .ToList();

            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ImportarAsuntosExcel(int impProveedor, int impAnyo, int impMes, string impFactura, HttpPostedFileBase impFile)
        {
            if (impFile == null || impFile.ContentLength == 0)
                return Json(new { success = false, excepcion = new[] { "No se ha seleccionado ningún archivo." } });

            var listaErrores = new List<string>();
            int insertados = 0;

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var pkg = new ExcelPackage(impFile.InputStream))
                {
                    if (!pkg.Workbook.Worksheets.Any())
                        return Json(new { success = false, excepcion = new[] { "El Excel no tiene el formato correcto." } });

                    var ws = pkg.Workbook.Worksheets.First();

                    TipoImportAsunto tipoExcel = TipoImportAsunto.Generico;
                    DAL_Proveedores dalProv = new DAL_Proveedores();
                    Proveedores objProveedor = dalProv.L_PrimaryKey(impProveedor);
                    if (objProveedor != null)
                        tipoExcel = (TipoImportAsunto)objProveedor.PRV_PlantillaExcel;

                    DAL_Proveedores_Asuntos dalAsuntos = new DAL_Proveedores_Asuntos();
                    bool hayErroresBloqueantes = dalAsuntos.ImportarExcel(tipoExcel, ws, impFactura, dalAsuntos, listaErrores, 
                        ref insertados, impProveedor, impAnyo, impMes);

                    if (listaErrores.Any())
                    {
                        // 1️ Añadir encabezado "Errores" al final
                        int lastCol = ws.Cells
                            .Where(c => !string.IsNullOrWhiteSpace(c.Text))
                            .Max(c => c.End.Column) + 1;
                        //int lastCol = ws.Dimension.End.Column + 1;

                        int filaCabecera = 1;
                        switch (tipoExcel)
                        {
                            case TipoImportAsunto.Adur:
                                filaCabecera = 10;
                                break;
                            case TipoImportAsunto.Prodware:
                                filaCabecera = 1;
                                break;
                            case TipoImportAsunto.Optimize:
                                filaCabecera = 6;
                                break;
                            case TipoImportAsunto.Attest:
                                filaCabecera = 1;
                                break;
                            default:
                                filaCabecera = 1;
                                break;
                        }

                        ws.Cells[filaCabecera, lastCol].Value = "Errores";

                        // 2️ Mapear cada error a su fila
                        foreach (var err in listaErrores)
                        {
                            var m = Regex.Match(err, @"Fila\s+(\d+):\s*(.+)");
                            if (m.Success && int.TryParse(m.Groups[1].Value, out int row))
                                ws.Cells[row, lastCol].Value = m.Groups[2].Value;
                        }

                        // 3️ Stream para descarga
                        string rutaTemp = ConfigurationManager.AppSettings["PathTemporales"];
                        string nombreFichero = $"Errores_Importacion_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        string filePath = Path.Combine(rutaTemp, nombreFichero);

                        System.IO.File.WriteAllBytes(filePath, pkg.GetAsByteArray());
                        string fileUrl = Url.Action("DescargarErrores", "Portal", new { fileName = nombreFichero }, Request.Url.Scheme);

                        return Json(new { success = false, errores = listaErrores, hayErroresBloqueantes, insertados, fileUrl });
                    }
                    else
                    {
                        return Json(new { success = true, insertados });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    excepcion = new[] { "Error al procesar el archivo: " + ex.Message }
                });
            }
        }

        [HttpPost]
        public JsonResult PrevisulizarConceptosAsuntos()
        {
            // 1) Intentamos generar el preview; si falla, es un error fatal
            List<EmpresaPreviewRow> previewConDetalle;
            try
            {
                previewConDetalle = GenerarPreviewAsuntos();
            }
            catch (Exception ex)
            {
                // Error en la lógica de preview: devolvemos success=false
                var mensajeHtml = HttpUtility.HtmlEncode(ex.Message);
                return Json(new { success = false, mensaje = mensajeHtml });
            }

            // 2) Ahora detectamos los asuntos que no tienen ni GLPI, ni entidad, ni empresa
            var warnings = new List<string>();
            var periodo = ObtenerPeriodoFacturacion();
            var asuntosTodos = new DAL_Proveedores_Asuntos()
                .L(false, null)
                .Where(a => a.PAS_Anyo == periodo.Year && a.PAS_Mes == periodo.Month)
                .ToList();

            var invalidos = asuntosTodos
                .Where(a =>
                   // ni ticket GLPI
                   (!a.PAS_TKC_Id_GLPI.HasValue || a.PAS_TKC_Id_GLPI == 0)
                   // ni entidad
                   && (!a.PAS_ENT_Id.HasValue)
                   // ni empresa
                   && (!a.PAS_EMP_Id.HasValue)
                )
                .ToList();

            foreach (var a in invalidos)
            {
                warnings.Add(
                    $"Asunto '{a.PAS_Descripcion}' sin GLPI, Entidad ni Empresa asignada; no se generará concepto."
                );
            }

            // 3) Devolvemos success=true, el preview normal y las advertencias
            return Json(new
            {
                success = true,
                partesPreview = previewConDetalle,
                partesErrors = warnings    // la UI ya muestra partesErrors como alert-warning
            });
        }

        private List<EmpresaPreviewRow> GenerarPreviewAsuntos()
        {
            var periodoFact = ObtenerPeriodoFacturacion();
            int anyo = periodoFact.Year;
            int mes = periodoFact.Month;
            DateTime hoy = DateTime.Today;

            // 1) Tuplas (Empresa,Tarea) ya creadas
            var existentes = new DAL_Tareas_Empresas_LineasEsfuerzo()
                .L(false, null)
                .Where(l => l.TLE_Anyo == anyo && 
                    l.TLE_Mes == mes &&
                    (l.TLE_Descripcion?.Contains("Generado Auto Proveedores Asuntos") ?? false))
                .Select(l => (emp: l.TLE_EMP_Id, tar: l.TLE_TAR_Id))
                .ToHashSet();

            // 2) Proveedor → TAR_Id
            var dictProvATar = new DAL_Proveedores().L(false, null).ToDictionary(p => p.PRV_Id, p => p.PRV_TAR_Id_Soporte);

            // 3) Filtrado de asuntos que no tengan ya concepto (usando tarea del asunto o del proveedor)
            var asuntosPeriodo = new DAL_Proveedores_Asuntos().L(false, null)
                .Where(a => a.PAS_Anyo == anyo
                    && a.PAS_Mes == mes
                    && a.PAS_EMP_Id.HasValue
                    && a.PAS_EMP_Id.Value != (int)EmpresaExcluyenteConceptos.EGC)
                .ToList()
                .Where(a =>
                {
                    // tarea efectiva: la del asunto o la del proveedor
                    int tareaId = a.PAS_TAR_Id
                                  ?? (dictProvATar.ContainsKey(a.PAS_PRV_Id) ? dictProvATar[a.PAS_PRV_Id] ?? 0 : 0);

                    return tareaId != 0 && !existentes.Contains((a.PAS_EMP_Id.Value, tareaId));
                })
                .ToList();

            var dicEntidades = new DAL_Entes().L(false, null).ToDictionary(e => e.ENT_Id);
            var dicEmpresas = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id);
            var dicProveedores = new DAL_Proveedores().L(false, null).ToDictionary(p => p.PRV_Id);
            var tareasEmp = new DAL_Tareas_Empresas().L(false, null).ToList();

            return asuntosPeriodo
                .GroupBy(a => a.PAS_EMP_Id.Value)
                .Select(grpEmpresa =>
                {
                    int idEmpresa = grpEmpresa.Key;
                    var listaAsuntosEmpresa = grpEmpresa.ToList();

                    // —— Por cada proveedor, creamos su preview con errores propios ——
                    var proveedores = listaAsuntosEmpresa
                        .GroupBy(a => new { a.PAS_PRV_Id, a.PAS_TAR_Id })  
                        .Select(grpProv =>
                        {
                            int idProv = grpProv.Key.PAS_PRV_Id;
                            int idTarea = grpProv.Key.PAS_TAR_Id
                                          ?? (dictProvATar.ContainsKey(idProv) ? dictProvATar[idProv] ?? 0 : 0);

                            // 2) Validamos asignación TEM para esta empresa+esta tarea
                            var erroresProv = new List<string>();
                            if (!tareasEmp.Any(te =>
                                    te.TEM_EMP_Id == idEmpresa &&
                                    te.TEM_TAR_Id == idTarea &&
                                    te.TEM_Anyo == anyo))
                            {
                                var tarea = new DAL_Tareas()
                                               .L(false, null)
                                               .First(t => t.TAR_Id == idTarea);
                                erroresProv.Add(
                                    $"Sin asignación de '{tarea.TAR_Nombre}' para la empresa “" +
                                    $"{dicEmpresas[idEmpresa].EMP_Nombre}” en {anyo}."
                                );
                            }

                            // 3) Contrato vigente más reciente
                            var contrato = new DAL_Proveedores_ContratosSoporte()
                                .L(false, null)
                                .Where(c => c.PVC_PRV_Id == idProv
                                         && c.PVC_FechaInicio <= hoy
                                         && (c.PVC_FechaFin == null || c.PVC_FechaFin >= hoy))
                                .OrderByDescending(c => c.PVC_FechaInicio)
                                .FirstOrDefault();

                            decimal precioHora = contrato?.PVC_PrecioHora ?? 0m;
                            decimal horasContrat = contrato?.PVC_HorasContratadas ?? 0m;
                            int idContrato = contrato?.PVC_Id ?? 0;

                            // 4) Asuntos de este proveedor
                            var asuntosProv = grpProv.Select(a =>
                                new Asunto
                                {
                                    IdAsunto = a.PAS_Id,
                                    IdTicket = a.PAS_TKC_Id_GLPI ?? 0,
                                    IdEntidad = a.PAS_ENT_Id ?? 0,
                                    EntidadNombre = dicEntidades.TryGetValue(a.PAS_ENT_Id ?? 0, out var e)
                                                       ? e.ENT_Nombre
                                                       : String.Empty,
                                    Horas = a.PAS_Horas,
                                    Importe = a.PAS_Importe,
                                    Mensaje = (new DAL_Tareas_Empresas_LineasEsfuerzo_Asuntos()
                                        .L(false, null)
                                        .Where(d => d.TCA_PVC_Id == contrato?.PVC_Id)
                                        .Sum(d => d.TCA_Horas)
                                      + grpProv.Sum(x => x.PAS_Horas)
                                      > horasContrat)
                                        ? "Se sobrepasan las horas contratadas"
                                        : String.Empty,
                                    Fecha = a.PAS_Fecha ?? DateTime.MinValue,
                                    Descripcion = a.PAS_Descripcion
                                })
                                .ToList();

                            return new ProveedorPreview
                            {
                                IdProveedor = idProv,
                                NombreProveedor = dicProveedores[idProv].PRV_Nombre,
                                IdContrato = idContrato,
                                IdTarea = idTarea,
                                PrecioHoraContrato = precioHora,
                                HorasContratadas = horasContrat,
                                Asuntos = asuntosProv,
                                ListaErrores = erroresProv
                            };
                        })
                        .ToList();

                    // Totales al nivel empresa
                    decimal totalHoras = listaAsuntosEmpresa.Sum(a => a.PAS_Horas);
                    decimal totalImporte = listaAsuntosEmpresa.Sum(a =>
                    {
                        var pp = proveedores.First(p => p.IdProveedor == a.PAS_PRV_Id);
                        return pp.PrecioHoraContrato * a.PAS_Horas + a.PAS_Importe;
                    });

                    return new EmpresaPreviewRow
                    {
                        IdEmpresa = idEmpresa,
                        NombreEmpresa = dicEmpresas[idEmpresa].EMP_Nombre,
                        Anyo = anyo,
                        Mes = mes,
                        TotalHoras = totalHoras,
                        TotalImporte = totalImporte,
                        Proveedores = proveedores
                    };
                })
                .ToList();
        }

        [HttpPost]
        public JsonResult GenerarConceptosAsuntos()
        {
            try
            {
                var resultados = EjecutarGenerarConceptosAsuntos();

                return Json(new
                {
                    success = true,
                    resultados
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    mensaje = ex.Message
                });
            }
        }

        private List<GenerarResultDto> EjecutarGenerarConceptosAsuntos()
        {
            var periodoFact = ObtenerPeriodoFacturacion();
            int anyo = periodoFact.Year;
            int mes = periodoFact.Month;

            // 1) Obtenemos el preview agrupado
            var previewConDetalle = GenerarPreviewAsuntos();

            // 2) Filtramos sólo empresas sin errores de proveedor
            var aProcesar = previewConDetalle
                 .Where(emp => emp.Proveedores
                     .All(prov => prov.ListaErrores == null || !prov.ListaErrores.Any())
                     && emp.IdEmpresa is int empId && empId != (int)EmpresaExcluyenteConceptos.EGC)
                 .ToList();

            var dicEmpresas = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id);
            var dalConc = new DAL_Tareas_Empresas_LineasEsfuerzo();
            var dalDet = new DAL_Tareas_Empresas_LineasEsfuerzo_Asuntos();
            var resumen = new Dictionary<int, int>();
            DateTime hoy = DateTime.Today;

            foreach (var fila in aProcesar)
            {
                // 3) Agrupamos los proveedores de esta empresa por tarea
                var gruposPorTarea = fila.Proveedores.GroupBy(p => p.IdTarea);

                foreach (var grp in gruposPorTarea)
                {
                    int idTarea = grp.Key;

                    // 4) Aplanamos los asuntos junto con su contrato
                    var asuntosConContrato = grp
                        .SelectMany(prov => prov.Asuntos
                            .Select(a => new
                            {
                                ContratoId = prov.IdContrato,    // ← aquí sacamos el PVC_Id del proveedor
                                PrecioHora = prov.PrecioHoraContrato,
                                Asunto = a
                            })
                        )
                        .ToList();

                    // 5) Sumamos horas e importes para el concepto
                    decimal totalHoras = asuntosConContrato.Sum(x => x.Asunto.Horas * x.PrecioHora);
                    decimal totalImporte = asuntosConContrato.Sum(x => x.Asunto.Importe);

                    // Si no hay nada que facturar, saltamos
                    if (totalHoras <= 0 && totalImporte <= 0)
                        continue;

                    // 6) Creamos la cabecera de concepto para esta tarea
                    var concepto = new Tareas_Empresas_LineasEsfuerzo
                    {
                        TLE_TAR_Id = idTarea,
                        TLE_EMP_Id = fila.IdEmpresa,
                        TLE_Anyo = fila.Anyo,
                        TLE_Mes = fila.Mes,
                        TLE_Cantidad = totalHoras + totalImporte,
                        TLE_Descripcion = "Generado Auto Proveedores Asuntos " + anyo + "-" + mes,
                        TLE_ESO_Id = (int)Constantes.EstadosSolicitud.PendienteAprobacion,
                        TLE_ESO_Id_Original = (int)Constantes.EstadosSolicitud.SinSolicitar,
                        TLE_PER_Id_Aprobador = dicEmpresas[fila.IdEmpresa].EMP_PER_Id_AprobadorDefault,
                        TLE_FechaAprobacion = hoy,
                        TLE_ComentarioAprobacion = string.Empty,
                        FechaAlta = hoy,
                        FechaModificacion = hoy,
                        PER_Id_Modificacion = Sesion.SPersonaId
                    };
                    dalConc.G(concepto, Sesion.SPersonaId);

                    // 7) Insertamos cada detalle usando el ContratoId correspondiente
                    foreach (var item in asuntosConContrato)
                    {
                        var detalle = new Tareas_Empresas_LineasEsfuerzo_Asuntos
                        {
                            TCA_TLE_Id = concepto.TLE_Id,
                            TCA_PAS_Id = item.Asunto.IdAsunto,
                            TCA_PVC_Id = item.ContratoId,     // ← aquí asignamos el contrato del proveedor
                            TCA_Horas = item.Asunto.Horas,
                            TCA_Importe = item.Asunto.Importe + (item.Asunto.Horas * item.PrecioHora)
                        };
                        dalDet.G(detalle, Sesion.SPersonaId);
                    }

                    // 8) Contabilizamos un concepto para la empresa
                    if (resumen.ContainsKey(fila.IdEmpresa))
                        resumen[fila.IdEmpresa]++;
                    else
                        resumen[fila.IdEmpresa] = 1;
                }
            }

            // 9) Preparamos el DTO de salida
            return resumen
                .Select(kvp => new GenerarResultDto
                {
                    EmpresaId = kvp.Key,
                    EmpresaNombre = dicEmpresas[kvp.Key].EMP_Nombre,
                    ConceptosCreados = kvp.Value
                })
                .ToList();
        }

        [HttpGet]
        public ActionResult ObtenerTareasCombo()
        {
            var dal = new DAL_Tareas();
            //var listaTareas = dal.L(false, null)
            //                .Select(t => new { t.TAR_Id, t.TAR_Nombre })
            //                .OrderBy(t => t.TAR_Nombre)
            //                .ToList();
            var listaTareas = dal.GetTareasCombo(null);

            return new LargeJsonResult { Data = listaTareas, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
    }
}

public class EmpresaPreviewRow
{
    public int IdEmpresa { get; set; }
    public string NombreEmpresa { get; set; }
    public int Anyo { get; set; }
    public int Mes { get; set; }
    public decimal TotalHoras { get; set; }
    public decimal TotalImporte { get; set; }
    public List<ProveedorPreview> Proveedores { get; set; }
}

public class ProveedorPreview
{
    public int IdProveedor { get; set; }
    public string NombreProveedor { get; set; }
    public int IdTarea { get; set; }
    public int IdContrato { get; set; }
    public decimal PrecioHoraContrato { get; set; }
    public decimal HorasContratadas { get; set; }
    public List<Asunto> Asuntos { get; set; }
    public List<string> ListaErrores { get; set; }
}

public class Asunto
{
    public int IdAsunto { get; set; }
    public int IdTicket { get; set; }
    public int IdEntidad { get; set; }
    public string EntidadNombre { get; set; }
    public decimal Horas { get; set; }
    public decimal Importe { get; set; }
    public string Mensaje { get; set; }
    public DateTime Fecha { get; set; }
    public string Descripcion { get; set; }
}