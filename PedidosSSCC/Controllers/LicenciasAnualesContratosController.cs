using AccesoDatos;
using AjaxControlToolkit;
using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using static PedidosSSCC.Comun.Constantes;

namespace PedidosSSCC.Controllers
{
    public class LicenciasAnualesContratosController : BaseController
    {
        public ActionResult LicenciasAnualesContratos() => View();

        [HttpGet]
        public ActionResult ObtenerContratos()
        {
            var dal = new DAL_ContratosLicenciasAnuales();
            var lista = dal.L(false, null)
                .Select(c => new
                {
                    c.CLA_Id,
                    c.CLA_PRV_Id,
                    ProveedorNombre = c.Proveedores.PRV_Nombre,
                    c.CLA_FechaInicio,
                    c.CLA_FechaFin
                })
                .ToList();

            return new LargeJsonResult
            {
                Data = lista,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpPost]
        public JsonResult GuardarContrato(ContratosLicenciasAnuales dto)
        {
            try
            {
                var dal = new DAL_ContratosLicenciasAnuales();
                string mensaje;
                bool ok = dal.GuardarContrato(dto, out mensaje);
                return Json(new { success = ok, message = mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarContrato(int idContrato)
        {
            try
            {
                var dal = new DAL_ContratosLicenciasAnuales();
                string mensaje;
                bool ok = dal.EliminarContrato(idContrato, out mensaje);
                return Json(new { success = ok, message = mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // — Tarifas del contrato —
        [HttpGet]
        public ActionResult ObtenerLicenciasProveedor(int idProveedor)
        {
            var dal = new DAL_LicenciasAnuales();
            var lista = dal.L(false, null)
                .Where(t => t.LAN_PRV_Id == idProveedor)
                .Select(t => new
                {
                    t.LAN_Id,
                    t.LAN_Nombre
                })
                .OrderBy(i => i.LAN_Nombre)
                .ToList();

            return new LargeJsonResult
            {
                Data = lista,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpGet]
        public ActionResult ObtenerTarifasContrato(int idContrato)
        {
            var dal = new DAL_ContratosLicenciasAnuales_Tarifas();
            var lista = dal.L(false, null)
                .Where(t => t.CLT_CLA_Id == idContrato)
                .Select(t => new
                {
                    t.CLT_CLA_Id,
                    t.CLT_LAN_Id,
                    t.CLT_ImporteAnual,
                    //t.CLT_NumLicencias,
                    LicenciaNombre = t.LicenciasAnuales.LAN_Nombre
                })
                .ToList();

            return new LargeJsonResult
            {
                Data = lista,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpPost]
        public JsonResult GuardarTarifaContrato(ContratosLicenciasAnuales_Tarifas dto)
        {
            try
            {
                var dal = new DAL_ContratosLicenciasAnuales_Tarifas();
                bool ok = dal.G(dto, Sesion.SPersonaId);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarTarifaContrato(int idContrato, int idLicencia)
        {
            try
            {
                var dal = new DAL_ContratosLicenciasAnuales_Tarifas();
                bool ok = dal.D(idContrato + "|" + idLicencia);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerContratosLicenciasAnuales()
        {
            var dal = new DAL_ContratosLicenciasAnuales();
            var lista = dal.L(false, null)
                .Select(c => new
                {
                    c.CLA_Id,
                    FechaInicio = c.CLA_FechaInicio,
                    FechaFin = c.CLA_FechaFin,
                    Proveedor = c.Proveedores != null
                                   ? c.Proveedores.PRV_Nombre
                                   : String.Empty
                })
                .ToList();

            // concatenamos en el servidor o lo dejamos para el cliente
            var dto = lista.Select(x => new
            {
                x.CLA_Id,
                Display = $"{x.Proveedor} | {x.FechaInicio:dd/MM/yyyy} → {x.FechaFin:dd/MM/yyyy)}"
            });

            return new LargeJsonResult
            {
                Data = dto,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpPost]
        public JsonResult PrevisualizarConceptosLicenciasAnuales()
        {
            try
            {
                var partesErrors = new List<string>();
                List<LicenciaAnualPreviewRow> previewConDetalle = new List<LicenciaAnualPreviewRow>();
                try
                {
                    var periodoFact = ObtenerPeriodoFacturacion();
                    previewConDetalle = BuildPreviewLicenciasAnuales(periodoFact.Year, periodoFact.Month, DateTime.Today);
                }
                catch (Exception ex)
                {
                    partesErrors.Add(ex.Message);
                }

                if (partesErrors.Any())
                {
                    var mensajeHtml = string.Join("<br/>", partesErrors);
                    return Json(new { success = false, mensaje = mensajeHtml });
                }

                return Json(new
                {
                    success = true,
                    partesPreview = previewConDetalle,
                    partesErrors = partesErrors
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GenerarConceptosLicenciasAnuales()
        {
            try
            {
                var resultados = EjecutarGenerarConceptosLicenciasAnuales();
                return Json(new { success = true, resultados });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        // ===============================
        // Refactor común
        // ===============================
        private sealed class DataCache
        {
            public int Anyo { get; set; }
            public int Mes { get; set; }
            public DateTime Hoy { get; set; }
            public string DescripcionConcepto { get; set; }

            public Dictionary<int, Tareas> DicTareas { get; set; }
            public Dictionary<int, Empresas> DicEmpresas { get; set; }
            public Dictionary<int, Entes> DicEntes { get; set; }
            public Dictionary<int, LicenciasAnuales> DicLicAnuales { get; set; }
            public Dictionary<int, Proveedores> DicProveedores { get; set; }

            public HashSet<(int emp, int tar)> TuplasTareaEmpresa { get; set; }
            public HashSet<(int emp, int tar)> ConceptosExistentes { get; set; }

            public List<Entes_LicenciasAnuales> EntesLic { get; set; }
            public List<Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales> TareasEntLic { get; set; }
            public HashSet<(int entId, int lanId)> ParesTaskeados { get; set; }

            public List<ContratosLicenciasAnuales> ContratosVigentes { get; set; }
            public List<ContratosLicenciasAnuales_Tarifas> Tarifas { get; set; }
        }

        // Carga todos los datos necesarios una sola vez
        private DataCache LoadData(int anyo, int mes, DateTime hoy)
        {
            string descripcion = "Generado Auto Licencias Anuales " + anyo + "-" + mes;

            var dicTareas = new DAL_Tareas().L(false, null).ToDictionary(e => e.TAR_Id);
            var dicEmpresas = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id);
            var dicEntes = new DAL_Entes().L(false, null).ToDictionary(e => e.ENT_Id);
            var dicLicAnuales = new DAL_LicenciasAnuales().L(false, null).ToDictionary(e => e.LAN_Id);
            var dicProveedores = new DAL_Proveedores().L(false, null).ToDictionary(p => p.PRV_Id);

            var tuplasTareaEmpresa = new DAL_Tareas_Empresas()
                .L(false, null)
                .Where(l => l.TEM_Anyo == anyo && l.TEM_EMP_Id != (int)EmpresaExcluyenteConceptos.EGC) 
                .Select(l => (emp: l.TEM_EMP_Id, tar: l.TEM_TAR_Id))
                .ToHashSet();

            var dalConc = new DAL_Tareas_Empresas_LineasEsfuerzo();
            var conceptosExistentes = new HashSet<(int emp, int tar)>(
                dalConc.L(false, null)
                    .Where(c => c.TLE_Anyo == anyo && c.TLE_Mes == mes && c.TLE_Descripcion == descripcion && 
                        c.TLE_EMP_Id != (int)EmpresaExcluyenteConceptos.EGC)
                    .Select(c => (c.TLE_EMP_Id, c.TLE_TAR_Id))
            );

            var dalEntesLic = new DAL_Entes_LicenciasAnuales();
            var entesLic = dalEntesLic.L(false, null).ToList();

            var dalTareasLic = new DAL_Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales();
            var tareasEntLic = dalTareasLic.L(false, null).ToList();

            var paresTaskeados = tareasEntLic
                .Select(t => (t.TCL_ENT_Id, t.TCL_LAN_Id))
                .ToHashSet();

            var dalContratos = new DAL_ContratosLicenciasAnuales();
            var contratosVigentes = dalContratos.L(false, null)
                .Where(c => c.CLA_FechaInicio <= hoy && (c.CLA_FechaFin == null || c.CLA_FechaFin >= hoy))
                // Excluir contratos cuyas vinculaciones ya estén taskeadas (ente, licencia)
                .Where(c =>
                {
                    var vinculaciones = entesLic.Where(e => e.ELA_CLA_Id == c.CLA_Id);
                    return !vinculaciones.Any(v => paresTaskeados.Contains((v.ELA_ENT_Id, v.ELA_LAN_Id)));
                })
                .ToList();

            var tarifas = new DAL_ContratosLicenciasAnuales_Tarifas()
                .L(false, null)
                .ToList();

            return new DataCache
            {
                Anyo = anyo,
                Mes = mes,
                Hoy = hoy,
                DescripcionConcepto = descripcion,
                DicTareas = dicTareas,
                DicEmpresas = dicEmpresas,
                DicEntes = dicEntes,
                DicLicAnuales = dicLicAnuales,
                DicProveedores = dicProveedores,
                TuplasTareaEmpresa = tuplasTareaEmpresa,
                ConceptosExistentes = conceptosExistentes,
                EntesLic = entesLic,
                TareasEntLic = tareasEntLic,
                ParesTaskeados = paresTaskeados,
                ContratosVigentes = contratosVigentes,
                Tarifas = tarifas
            };
        }

        // Construye el preview compartido por Previsualizar y Generar
        private List<LicenciaAnualPreviewRow> BuildPreviewLicenciasAnuales(int anyo, int mes, DateTime hoy)
        {
            var cache = LoadData(anyo, mes, hoy);

            // Acumulador por (Empresa, Tarea)
            var previews = new Dictionary<(int emp, int tar), LicenciaAnualPreviewRow>();

            LicenciaAnualPreviewRow GetOrCreatePreview(int emp, int tar)
            {
                var key = (emp, tar);
                if (!previews.TryGetValue(key, out var p))
                {
                    p = new LicenciaAnualPreviewRow
                    {
                        Anyo = anyo,
                        Mes = mes,
                        IdEmpresa = emp,
                        IdTarea = tar,
                        NombreEmpresa = cache.DicEmpresas.TryGetValue(emp, out var e) ? e.EMP_Nombre : $"Empresa {emp}",
                        ListaDetalle = new List<LicenciaAnualDetalle>(),
                        ListaErrores = new List<string>()
                    };
                    previews[key] = p;
                }
                return p;
            }

            // Recorremos contratos vigentes
            foreach (var contrato in cache.ContratosVigentes)
            {
                // Tarifas del contrato
                var tarifasContrato = cache.Tarifas
                    .Where(t => t.CLT_CLA_Id == contrato.CLA_Id)
                    .ToList();

                foreach (var tarifa in tarifasContrato)
                {
                    // Entes vinculados a esta licencia y contrato (por fechas)
                    var fechaFinContrato = contrato.CLA_FechaFin;

                    var entesVinculados = cache.EntesLic
                        .Where(e =>
                            e.ELA_LAN_Id == tarifa.CLT_LAN_Id &&
                            e.ELA_FechaInicio <= fechaFinContrato &&
                            (e.ELA_FechaFin == null || e.ELA_FechaFin >= contrato.CLA_FechaInicio) &&
                            (e.Entes?.ENT_EMP_Id.GetValueOrDefault() ?? 0) != (int)EmpresaExcluyenteConceptos.EGC)
                        .ToList();

                    if (!entesVinculados.Any())
                        continue;

                    foreach (var objEnte in entesVinculados)
                    {
                        // NOTA: asumiendo que Entes_LicenciasAnuales tiene navegación "Entes"
                        int idEnte = objEnte.Entes.ENT_Id;
                        int idEmpresa = objEnte.Entes.ENT_EMP_Id ?? 0;
                        if (idEmpresa == 0) continue;

                        // Proveedor -> tarea soporte
                        int idLic = tarifa.CLT_LAN_Id;
                        int idProveedor = cache.DicLicAnuales[idLic].LAN_PRV_Id;
                        var proveedor = cache.DicProveedores[idProveedor];
                        int tareaSoporte = proveedor.PRV_TAR_Id_Soporte ?? 0;

                        // Si ya existe un concepto de este mes/año/descr para (empresa,tarea), omitimos
                        if (cache.ConceptosExistentes.Contains((idEmpresa, tareaSoporte)))
                            continue;

                        var preview = GetOrCreatePreview(idEmpresa, tareaSoporte);

                        // ✅ Validación dura del par Empresa-Tarea
                        if (!cache.TuplasTareaEmpresa.Contains((idEmpresa, tareaSoporte)))
                        {
                            string nombreTarea = cache.DicTareas.TryGetValue(tareaSoporte, out var t)
                                ? t.TAR_Nombre
                                : $"Tarea {tareaSoporte}";
                            string msgPar = $"La empresa '{preview.NombreEmpresa}' no tiene asignada la tarea '{nombreTarea}' en Tareas_Empresas para el año {anyo}.";
                            if (!preview.ListaErrores.Contains(msgPar))
                                preview.ListaErrores.Add(msgPar);

                            // No agregamos detalle si falta el par
                            continue;
                        }

                        // Otras validaciones informativas
                        if (tareaSoporte == 0 || !cache.DicTareas.ContainsKey(tareaSoporte))
                        {
                            var msg = $"Proveedor '{proveedor.PRV_Nombre}' no tiene asignada tarea de soporte.";
                            if (!preview.ListaErrores.Contains(msg))
                                preview.ListaErrores.Add(msg);
                        }
                        if (!cache.DicEmpresas.ContainsKey(idEmpresa))
                        {
                            var msg = $"Empresa con ID {idEmpresa} no encontrada en el sistema.";
                            if (!preview.ListaErrores.Contains(msg))
                                preview.ListaErrores.Add(msg);
                        }

                        // Agregar detalle
                        decimal importeLic = tarifa.CLT_ImporteAnual;
                        preview.ListaDetalle.Add(new LicenciaAnualDetalle
                        {
                            IdLicencia = idLic,
                            NombreLicencia = cache.DicLicAnuales[idLic].LAN_Nombre,
                            IdEnte = idEnte,
                            NombreEnte = cache.DicEntes[idEnte].ENT_Nombre,
                            Importe = importeLic,
                            IdContrato = contrato.CLA_Id // ✅ imprescindible para la generación
                        });
                        preview.TotalImporte += importeLic;
                    }
                }
            }

            // Finalizar lista (solo filas con detalle o errores)
            return previews.Values
                .Where(p => p.ListaDetalle.Any() || p.ListaErrores.Any())
                .ToList();
        }

        // ===============================
        // Generación usando el mismo preview
        // ===============================
        private List<GenerarResultDto> EjecutarGenerarConceptosLicenciasAnuales()
        {
            var periodoFact = ObtenerPeriodoFacturacion();
            int anyo = periodoFact.Year;
            int mes = periodoFact.Month;
            DateTime hoy = DateTime.Today;

            var preview = BuildPreviewLicenciasAnuales(anyo, mes, hoy);

            var dalConc = new DAL_Tareas_Empresas_LineasEsfuerzo();
            var dalLicLine = new DAL_Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales();
            var dalEnteLicAn = new DAL_Entes_LicenciasAnuales();
            var dalTarifas = new DAL_ContratosLicenciasAnuales_Tarifas();

            var dicEmpresas = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id);

            // Revalidación de seguridad: par Empresa-Tarea para el año
            var tuplaTareaEmpresa = new DAL_Tareas_Empresas().L(false, null)
                .Where(l => l.TEM_Anyo == anyo && l.TEM_EMP_Id != (int)EmpresaExcluyenteConceptos.EGC)
                .Select(l => (emp: l.TEM_EMP_Id, tar: l.TEM_TAR_Id))
                .ToHashSet();

            var resumen = new Dictionary<int, int>();

            foreach (var row in preview)
            {
                // No generamos si hay errores en el preview
                if (row.ListaErrores.Any())
                    continue;

                // Revalidación para evitar reventar BD
                if (!tuplaTareaEmpresa.Contains((row.IdEmpresa, row.IdTarea)))
                    continue;

                // Crear cabecera del concepto
                var header = new Tareas_Empresas_LineasEsfuerzo
                {
                    TLE_TAR_Id = row.IdTarea,
                    TLE_EMP_Id = row.IdEmpresa,
                    TLE_Anyo = row.Anyo,
                    TLE_Mes = row.Mes,
                    TLE_Cantidad = row.ListaDetalle.Sum(d => d.Importe),
                    TLE_Descripcion = "Generado Auto Licencias Anuales " + anyo + "-" + mes,
                    TLE_ESO_Id = (int)Constantes.EstadosSolicitud.PendienteAprobacion,
                    TLE_ESO_Id_Original = (int)Constantes.EstadosSolicitud.SinSolicitar,
                    TLE_PER_Id_Aprobador = dicEmpresas[row.IdEmpresa].EMP_PER_Id_AprobadorDefault,
                    TLE_FechaAprobacion = hoy,
                    TLE_ComentarioAprobacion = string.Empty,
                    FechaAlta = hoy,
                    FechaModificacion = hoy,
                    PER_Id_Modificacion = Sesion.SPersonaId
                };
                dalConc.G(header, Sesion.SPersonaId);

                // Insertar líneas por cada licencia
                foreach (var det in row.ListaDetalle)
                {
                    var lineLic = new Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales
                    {
                        TCL_TLE_Id = header.TLE_Id,
                        TCL_LAN_Id = det.IdLicencia,
                        TCL_ENT_Id = det.IdEnte,
                        TCL_Importe = det.Importe
                    };
                    dalLicLine.G(lineLic, Sesion.SPersonaId);

                    // Marcar como facturada la relación Ente-Licencia
                    string mensaje;
                    bool ok = dalEnteLicAn.ActualizarFacturada(det.IdEnte, det.IdLicencia, det.IdContrato, true, out mensaje);
                    if (!ok)
                    {
                        // log suave; no interrumpimos toda la generación
                        Console.WriteLine("ActualizarFacturada: " + mensaje);
                    }

                    // Actualizar contador de licencias en la tarifa
                    string mensajeNum;
                    bool okNum = dalEnteLicAn.ActualizarNumLicencias(det.IdContrato, det.IdLicencia, 1, out mensajeNum);
                    if (!okNum)
                    {
                        Console.WriteLine("ActualizarNumLicencias: " + mensajeNum);
                    }

                    // (Opcional) si tuvieras que tocar entidad de Ente_Licencia directamente, hazlo mediante su DAL
                    // ...
                }

                if (resumen.ContainsKey(row.IdEmpresa))
                    resumen[row.IdEmpresa]++;
                else
                    resumen[row.IdEmpresa] = 1;
            }

            // DTO de salida
            return resumen.Select(kvp => new GenerarResultDto
            {
                EmpresaId = kvp.Key,
                EmpresaNombre = dicEmpresas[kvp.Key].EMP_Nombre,
                ConceptosCreados = kvp.Value
            }).ToList();
        }
    }

    public class LicenciaAnualDetalle
    {
        public int IdLicencia { get; set; }
        public string NombreLicencia { get; set; }
        public int IdEnte { get; set; }
        public string NombreEnte { get; set; }
        public decimal Importe { get; set; }
        public int IdContrato { get; set; }      // ✅ Necesario en la generación
    }

    public class LicenciaAnualPreviewRow
    {
        public int Anyo { get; set; }
        public int Mes { get; set; }
        public int IdEmpresa { get; set; }
        public int IdTarea { get; set; }
        public string NombreEmpresa { get; set; }
        public decimal TotalImporte { get; set; }
        public List<LicenciaAnualDetalle> ListaDetalle { get; set; }
        public List<string> ListaErrores { get; set; }
    }
}
