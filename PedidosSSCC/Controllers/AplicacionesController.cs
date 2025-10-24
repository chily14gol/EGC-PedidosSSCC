using AccesoDatos;
using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using static PedidosSSCC.Comun.Constantes;

namespace PedidosSSCC.Controllers
{
    public class AplicacionesController : BaseController
    {
        public ActionResult Aplicaciones() => View();

        [HttpGet]
        public ActionResult ObtenerAplicaciones()
        {
            var dal = new DAL_Aplicaciones();
            var lista = dal.L(false, null)
                .Select(x => new
                {
                    x.APP_Id,
                    x.APP_Nombre,
                    x.APP_TAR_Id,
                    NombreTarea = x.Tareas != null ? x.Tareas.TAR_Nombre : ""
                })
                .ToList();

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarAplicacion(Aplicaciones obj)
        {
            try
            {
                var dal = new DAL_Aplicaciones();
                bool ok = dal.G(obj, Sesion.SPersonaId);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarAplicacion(int idAplicacion)
        {
            try
            {
                var dal = new DAL_Aplicaciones();
                bool ok = dal.Eliminar(idAplicacion);
                return Json(new { success = ok, mensaje = dal.MensajeErrorEspecifico });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        // ——————————————
        // Aplicaciones -> Tipos Ente
        // ——————————————

        [HttpGet]
        public ActionResult ObtenerAplicacionesTiposEnte(int idAplicacion)
        {
            var dalTipos = new DAL_TiposEnte();
            var dalAsociaciones = new DAL_Aplicaciones_TiposEnte();

            // Obtener todos los tipos de ente
            var todos = dalTipos.L(false, null)
                .Select(t => new
                {
                    id = t.TEN_Id,
                    text = t.TEN_Nombre
                }).ToList();

            // Obtener IDs ya asociados a esta licencia
            var asociados = dalAsociaciones.L(false, null)
                .Where(l => l.ATE_APP_Id == idAplicacion)
                .Select(l => l.ATE_TEN_Id)
                .ToHashSet(); // para búsqueda rápida

            // Combinar: añadir propiedad 'selected' si está asociado
            var resultado = todos.Select(t => new
            {
                id = t.id,
                text = t.text,
                selected = asociados.Contains(t.id)
            }).OrderBy(t => t.text).ToList();

            return Json(resultado, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult ObtenerEntidadesComboPorTipo(List<int> idTiposEntidad)
        {
            if (idTiposEntidad == null || !idTiposEntidad.Any())
                return new LargeJsonResult { Data = new List<object>(), JsonRequestBehavior = JsonRequestBehavior.AllowGet };

            var dal = new DAL_Entes();
            var lista = dal.L(false, null)
                .Where(e => e.ENT_TEN_Id.HasValue && idTiposEntidad.Contains(e.ENT_TEN_Id.Value))
                .Select(e => new
                {
                    ENT_Id = e.ENT_Id,
                    Text = (e.ENT_Nombre ?? "")
                           + " — "
                           + (e.Empresas != null ? e.Empresas.EMP_Nombre : "Sin empresa")
                })
                .OrderBy(i => i.Text)
                .ToList();

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        // ——————————————
        // Aplicaciones -> Tarifas
        // ——————————————

        [HttpGet]
        public JsonResult ObtenerAplicacionesTarifas(int idAplicacion)
        {
            var dal = new DAL_Aplicaciones_Tarifas();
            var lista = dal.L(false, null)
                           .Where(x => x.APT_APP_Id == idAplicacion)
                           .Select(x => new
                           {
                               x.APT_APP_Id,
                               x.APT_FechaInicio,
                               x.APT_FechaFin,
                               x.APT_PrecioUnitario
                           })
                           .ToList();
            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarAplicacionTarifa(Aplicaciones_Tarifas dto)
        {
            try
            {
                var dal = new DAL_Aplicaciones_Tarifas();
                bool ok = dal.G(dto, Sesion.SPersonaId);

                string mensaje = "";
                if (!ok)
                    mensaje = "La tarifa se solapa en fechas con otra tarifa.";

                return Json(new { success = ok, message = mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarAplicacionTarifa(int idAplicacion, DateTime fechaInicio)
        {
            try
            {
                var dal = new DAL_Aplicaciones_Tarifas();
                // El DAL espera la PK compuesta en formato "APPId|FechaInicio"
                string pk = idAplicacion + "|" + fechaInicio;
                bool ok = dal.D(pk);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ——————————————
        // Aplicaciones -> Módulos
        // ——————————————

        [HttpGet]
        public JsonResult ObtenerAplicacionesModulos(int idAplicacion)
        {
            var dal = new DAL_Aplicaciones_Modulos();
            var lista = dal.L(false, null)
                           .Where(m => m.APM_APP_Id == idAplicacion)
                           .Select(m => new
                           {
                               m.APM_Id,
                               m.APM_Nombre,
                               m.APM_TAR_Id,
                               TareaNombre = m.Tareas.TAR_Nombre
                           })
                           .ToList();
            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public JsonResult ObtenerDetalleModulo(int idModulo)
        {
            var dalEmp = new DAL_Aplicaciones_Modulos_Empresas();
            var dalTar = new DAL_Aplicaciones_Modulos_Tarifas();

            // 1) Empresas asignadas a este módulo
            var empresas = dalEmp.L(false, null)
                .Where(e => e.AME_APM_Id == idModulo)
                .Select(e => new
                {
                    e.AME_Id,
                    Nombre = e.Empresas.EMP_Nombre,
                    FechaInicio = e.AME_FechaInicio,
                    FechaFin = e.AME_FechaFin,
                    Importe = e.AME_ImporteMensual,
                    Descripcion = e.AME_DescripcionConcepto
                })
                .ToList();

            // 2) Tarifas de este módulo
            var tarifas = dalTar.L(false, null)
                .Where(t => t.AMT_APM_Id == idModulo)
                .Select(t => new
                {
                    t.AMT_Id,
                    FechaInicio = t.AMT_FechaInicio,
                    FechaFin = t.AMT_FechaFin,
                    ImporteMensual = t.AMT_ImporteMensualReparto,
                    Porcentaje = t.AMT_ImporteMensualRepartoPorcentajes
                })
                .ToList();

            var dto = new
            {
                Empresas = empresas,
                Tarifas = tarifas
            };

            return new LargeJsonResult
            {
                Data = dto,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        // ——————————————
        // Aplicaciones -> Entidades
        // ——————————————

        [HttpGet]
        public JsonResult ObtenerEntesAplicaciones(int idAplicacion)
        {
            var dal = new DAL_Entes_Aplicaciones();
            var lista = dal.L(false, null)
                            .Where(e => e.ENL_APP_Id == idAplicacion)
                            .Select(e => new
                            {
                                e.ENL_ENT_Id,
                                e.ENL_APP_Id,
                                e.ENL_FechaInicio,
                                e.ENL_FechaFin,
                                NombreEntidad = e.Entes != null ? e.Entes.ENT_Nombre : "",
                                EMP_Nombre = e.Entes?.Empresas?.EMP_Nombre ?? ""
                            })
                            .ToList();
            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public JsonResult ObtenerAplicacionesPorEntidad(int idEnte)
        {
            var dal = new DAL_Entes_Aplicaciones();
            var lista = dal.L(false, null)
                            .Where(e => e.ENL_ENT_Id == idEnte)
                            .Select(e => new
                            {
                                e.ENL_ENT_Id,
                                e.ENL_APP_Id,
                                e.ENL_FechaInicio,
                                e.ENL_FechaFin,
                                NombreAplicacion = e.Aplicaciones != null ? e.Aplicaciones.APP_Nombre : ""
                            })
                            .ToList();
            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarAplicacionEnte(Entes_Aplicaciones obj)
        {
            try
            {
                var dal = new DAL_Entes_Aplicaciones();

                // Normalizamos la fecha fin (si viene sin valor)
                DateTime? fechaFin = (obj.ENL_FechaFin == default(DateTime)) ? (DateTime?)null : obj.ENL_FechaFin;

                // 1) Mismo APP, misma ENTIDAD y MISMAS FECHAS -> error claro
                var yaExisteMismasFechas = dal.L(false, null).Any(e =>
                    e.ENL_APP_Id == obj.ENL_APP_Id &&
                    e.ENL_ENT_Id == obj.ENL_ENT_Id &&
                    e.ENL_FechaInicio.Date == obj.ENL_FechaInicio.Date &&
                    (
                        (e.ENL_FechaFin == null && fechaFin == null) ||
                        (e.ENL_FechaFin.HasValue && fechaFin.HasValue && e.ENL_FechaFin.Value.Date == fechaFin.Value.Date)
                    )
                );

                if (yaExisteMismasFechas)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Esta entidad ya está asignada a la aplicación con las mismas fechas."
                    });
                }

                // evitar solapes de rangos
                var haySolape = dal.L(false, null).Any(e =>
                    e.ENL_APP_Id == obj.ENL_APP_Id &&
                    e.ENL_ENT_Id == obj.ENL_ENT_Id &&
                    // Solape de rangos: [inicioA, finA] vs [inicioB, finB]
                    e.ENL_FechaInicio <= (fechaFin ?? DateTime.MaxValue) &&
                    (e.ENL_FechaFin ?? DateTime.MaxValue) >= obj.ENL_FechaInicio
                );
                if (haySolape)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Ya existe una asignación de esta entidad para la aplicación que se solapa en fechas."
                    });
                }
               
                // 2) Guardamos
                bool ok = dal.G(obj, Sesion.SPersonaId);
                if (!ok)
                {
                    return Json(new
                    {
                        success = false,
                        message = dal.MensajeErrorEspecifico ?? "No se pudo guardar la asignación."
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
        public JsonResult EliminarAplicacionEnte(int idAplicacion, int idEnte, DateTime fechaInicio)
        {
            try
            {
                var dal = new DAL_Entes_Aplicaciones();
                string valorPK = idEnte + "|" + idAplicacion + "|" + fechaInicio;
                bool ok = dal.D(valorPK);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ——————————————
        // Generar Conceptos
        // ——————————————
        [HttpPost]
        public JsonResult PrevisulizarConceptosAplicaciones()
        {
            try
            {
                var partesErrors = new List<string>();
                List<AplicacionPreviewRow> previewConDetalle = new List<AplicacionPreviewRow>();
                try
                {
                    previewConDetalle = GenerarPreviewAplicaciones();
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

        private List<AplicacionPreviewRow> GenerarPreviewAplicaciones()
        {
            var periodoFact = ObtenerPeriodoFacturacion();
            int anyoConceptos = periodoFact.Year;
            int mesConceptos = periodoFact.Month;
            var fechaActual = DateTime.Now;

            // Traigo todo en memoria
            var listaApps = new DAL_Aplicaciones().L(false, null).ToList();
            var listaAppsTarifas = new DAL_Aplicaciones_Tarifas().L(false, null).ToList();
            var listaModulos = new DAL_Aplicaciones_Modulos().L(false, null).ToList();
            var listaModEmpresas = new DAL_Aplicaciones_Modulos_Empresas().L(false, null).ToList();
            var listaModTarifas = new DAL_Aplicaciones_Modulos_Tarifas().L(false, null).ToList();
            var listaEntesApps = new DAL_Entes_Aplicaciones().L(false, null).ToList();
            var listaEntes = new DAL_Entes().L(false, null).ToList();
            var listaEmpresas = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id);
            var listaTareas = new DAL_Tareas().L(false, null).ToDictionary(e => e.TAR_Id);

            var existentes = new DAL_Tareas_Empresas_LineasEsfuerzo()
                .L(false, null)
                .Where(l => l.TLE_Anyo == anyoConceptos && l.TLE_Mes == mesConceptos)
                .Select(l => (emp: l.TLE_EMP_Id, tar: l.TLE_TAR_Id))
                .ToHashSet();

            // 1) pares directos módulo–empres
            var pairsModuloEmpresa =
                from app in listaApps
                from mod in listaModulos.Where(m => m.APM_APP_Id == app.APP_Id)
                from me in listaModEmpresas.Where(x => x.AME_APM_Id == mod.APM_Id)
                let par = (me.AME_EMP_Id, mod.APM_TAR_Id)
                where me.AME_EMP_Id != (int)EmpresaExcluyenteConceptos.EGC
                    && !existentes.Contains(par)
                select new
                {
                    EmpresaId = me.AME_EMP_Id,
                    TareaId = mod.APM_TAR_Id
                };

            var pairsEntesAplicacion =
                from ea in listaEntesApps
                join ent in listaEntes on ea.ENL_ENT_Id equals ent.ENT_Id
                where ent.ENT_EMP_Id != null
                from mod in listaModulos.Where(m => m.APM_APP_Id == ea.ENL_APP_Id)
                    // exigir que la empresa tenga asignado ese módulo
                join me in listaModEmpresas
                    on new { Mod = mod.APM_Id, Emp = ent.ENT_EMP_Id.Value }
                    equals new { Mod = me.AME_APM_Id, Emp = me.AME_EMP_Id }
                let par = (ent.ENT_EMP_Id.Value, mod.APM_TAR_Id)
                where ent.ENT_EMP_Id.Value != (int)EmpresaExcluyenteConceptos.EGC
                    && !existentes.Contains(par)
                select new { EmpresaId = ent.ENT_EMP_Id.Value, TareaId = mod.APM_TAR_Id };

            var agrupado = pairsModuloEmpresa
                .Concat(pairsEntesAplicacion)
                .GroupBy(x => new { x.EmpresaId, x.TareaId })
                .Select(g => new { g.Key.EmpresaId, g.Key.TareaId })
                .ToList();

            var resultado = new List<AplicacionPreviewRow>();
            foreach (var grp in agrupado)
            {
                int idEmpresa = grp.EmpresaId;
                int idTarea = grp.TareaId;

                decimal ImporteTotal = 0;

                List<AplicacionPreviewDetalleLicencia> listaLicencias = new List<AplicacionPreviewDetalleLicencia>();
                List<AplicacionPreviewDetalleLicenciaDetalle> listaLicenciasDetalle = new List<AplicacionPreviewDetalleLicenciaDetalle>();
                List<AplicacionPreviewDetalleImporteFijo> listaImporteFijo = new List<AplicacionPreviewDetalleImporteFijo>();
                List<AplicacionPreviewDetalleImporteReparto> listaImporteReparto = new List<AplicacionPreviewDetalleImporteReparto>();

                var erroresPorEmpresa = new List<string>();
                var tareasEmp = new DAL_Tareas_Empresas().L(false, null);

                // Validar asignación TEM
                if (!tareasEmp.Any(te =>
                        te.TEM_EMP_Id == idEmpresa &&
                        te.TEM_TAR_Id == idTarea &&
                        te.TEM_Anyo == anyoConceptos))
                {
                    var objTarea = new DAL_Tareas().L(false, null).First(i => i.TAR_Id == idTarea);
                    erroresPorEmpresa.Add(
                        $"Sin asignación de '{objTarea.TAR_Nombre}' para la empresa “{listaEmpresas[idEmpresa].EMP_Nombre}” en {anyoConceptos}."
                    );
                }

                #region Importe licencia
                // 0) Diccionario de AppId → NombreApp
                var dictApps = listaApps.ToDictionary(a => a.APP_Id, a => a.APP_Nombre);

                // 1) Extraigo los appIds de los módulos que corresponden a la tarea actual
                var appsIds = listaModulos
                    .Where(m => m.APM_TAR_Id == idTarea)
                    .Select(m => m.APM_APP_Id)
                    .Distinct();

                foreach (var appId in appsIds)
                {
                    // 2) Obtengo la tarifa vigente para esa aplicación
                    var tarifa = listaAppsTarifas
                        .FirstOrDefault(t =>
                            t.APT_APP_Id == appId
                         && t.APT_FechaInicio <= fechaActual
                         && (t.APT_FechaFin == null || t.APT_FechaFin >= fechaActual)
                        );
                    if (tarifa == null)
                        continue;

                    // 3) Cuento cuántos entes de esta empresa están asociados a esa app
                    int entesCount = listaEntesApps
                        .Count(ea =>
                            ea.ENL_APP_Id == appId
                         && listaEntes.Any(ent =>
                                ent.ENT_Id == ea.ENL_ENT_Id
                             && ent.ENT_EMP_Id == idEmpresa
                         )
                        );
                    if (entesCount == 0)
                        continue;

                    // 4) Calculo el importe de licencia: PrecioUnitario × número de entes
                    decimal importeLicencia = tarifa.APT_PrecioUnitario * entesCount;

                    // 5) Recupero el nombre de la aplicación
                    string nombreApp = dictApps.TryGetValue(appId, out var n) ? n : $"App {appId}";

                    // 6) Creo el detalle y lo sumo al total
                    var objLic = new AplicacionPreviewDetalleLicencia
                    {
                        Origen = $"Importe licencias",
                        IdAplicacion = appId,
                        Aplicacion = nombreApp,
                        Importe = importeLicencia
                    };
                    listaLicencias.Add(objLic);
                    ImporteTotal += importeLicencia;
                }
                #endregion

                #region Importe licencia Por Entidad
                foreach (var appId in appsIds)
                {
                    // 2) Obtengo la tarifa vigente para esa aplicación
                    var tarifa = listaAppsTarifas
                        .FirstOrDefault(t =>
                            t.APT_APP_Id == appId
                         && t.APT_FechaInicio <= fechaActual
                         && (t.APT_FechaFin == null || t.APT_FechaFin >= fechaActual)
                        );
                    if (tarifa == null)
                        continue;

                    // 3) Obtengo la lista de ENT_Id de los entes de esta empresa para esa app
                    var entesIds = (
                        from ea in listaEntesApps
                        join ent in listaEntes on ea.ENL_ENT_Id equals ent.ENT_Id
                        where ea.ENL_APP_Id == appId
                          && ent.ENT_EMP_Id == idEmpresa
                        select ent.ENT_Id
                    )
                    .Distinct()
                    .ToList();

                    if (!entesIds.Any())
                        continue;

                    // 4) Por cada entidad hago un detalle independiente (y sumo precio unitario)
                    string nombreApp = dictApps.TryGetValue(appId, out var n) ? n : $"App {appId}";
                    foreach (var entId in entesIds)
                    {
                        decimal importeLicencia = tarifa.APT_PrecioUnitario;

                        var objLic = new AplicacionPreviewDetalleLicenciaDetalle
                        {
                            Origen = $"Importe licencias",
                            IdAplicacion = appId,
                            Aplicacion = nombreApp,
                            IdEntidad = entId,
                            Importe = importeLicencia
                        };
                        listaLicenciasDetalle.Add(objLic);
                    }
                }
                #endregion

                #region Importe Fijo
                // 1) Preparamos la lista plana con todos los datos que necesitamos
                var detalles =
                    (from me in listaModEmpresas
                     join m in listaModulos
                         on me.AME_APM_Id equals m.APM_Id
                     where me.AME_EMP_Id == idEmpresa
                        && m.APM_TAR_Id == idTarea
                       // filtro para que la tarifa esté vigente en el período
                       && me.AME_FechaInicio <= fechaActual
                       && (me.AME_FechaFin == null || me.AME_FechaFin >= fechaActual)
                     select new
                     {
                         IdModuloEmpresa = me.AME_Id,
                         NombreModulo = m.APM_Nombre,
                         ImporteMensual = me.AME_ImporteMensual
                     })
                    .ToList();

                // 2) Iteramos y creamos un AplicacionPreviewDetalleImporteFijo por cada uno
                foreach (var d in detalles)
                {
                    var obj = new AplicacionPreviewDetalleImporteFijo
                    {
                        // Pones el nombre de módulo en el origen:
                        Origen = $"Importe fijo '{d.NombreModulo}'",
                        // El id módulo-empresa como string
                        IdModuloEmpresa = d.IdModuloEmpresa,
                        // Si fuera nullable, puedes castear null → 0:
                        Importe = d.ImporteMensual
                    };
                    ImporteTotal += d.ImporteMensual;
                    listaImporteFijo.Add(obj);
                }
                #endregion

                #region Reparto Fijo
                // 1) Obtengo sólo las tarifas de los módulos de la tarea actual
                //    y que además estén asociadas a la empresa (idEmpresa)
                var detallesTarifa =
                    (from taf in listaModTarifas
                     join m in listaModulos
                         on taf.AMT_APM_Id equals m.APM_Id
                     // filtro de tarea y vigencia
                     where m.APM_TAR_Id == idTarea
                       && taf.AMT_FechaInicio <= fechaActual
                       && (taf.AMT_FechaFin == null || taf.AMT_FechaFin >= fechaActual)
                     // filtro para que la empresa tenga ese módulo
                     join meEmp in listaModEmpresas
                         .Where(me => me.AME_EMP_Id == idEmpresa)
                       on taf.AMT_APM_Id equals meEmp.AME_APM_Id
                     select new
                     {
                         taf.AMT_Id,
                         taf.AMT_APM_Id,
                         NombreModulo = m.APM_Nombre,
                         ImporteMensualReparto = taf.AMT_ImporteMensualReparto
                     })
                    .ToList();

                foreach (var d in detallesTarifa)
                {
                    // 2) Ahora cuento TODAS las empresas que comparten ese módulo
                    int empresasCount = listaModEmpresas
                        .Count(me => me.AME_APM_Id == d.AMT_APM_Id);
                    if (empresasCount == 0)
                        continue;

                    // 3) Reparto equitativo entre todas ellas
                    decimal porcentaje = 1m / empresasCount;
                    decimal importePorEmpresa = d.ImporteMensualReparto / empresasCount;

                    // 4) Creo el detalle de reparto
                    var objReparto = new AplicacionPreviewDetalleImporteReparto
                    {
                        Origen = $"Importe reparto fijo '{d.NombreModulo}'",
                        IdModuloTarifa = d.AMT_Id,
                        Equitativo = true,
                        ImporteTotal = d.ImporteMensualReparto,
                        Porcentaje = porcentaje,
                        Importe = importePorEmpresa
                    };

                    listaImporteReparto.Add(objReparto);
                    ImporteTotal += importePorEmpresa;
                }
                #endregion

                #region Reparto Porcentaje
                var detallesTarifaPorcentaje =
                    (from taf in listaModTarifas
                     join m in listaModulos on taf.AMT_APM_Id equals m.APM_Id
                     where m.APM_TAR_Id == idTarea
                        && taf.AMT_FechaInicio <= fechaActual
                        && (taf.AMT_FechaFin == null || taf.AMT_FechaFin >= fechaActual)
                     select new
                     {
                         taf.AMT_Id,
                         taf.AMT_APM_Id,
                         NombreModulo = m.APM_Nombre,
                         ImporteMensualRepartoPorcentajes = taf.AMT_ImporteMensualRepartoPorcentajes
                     })
                    .ToList();

                const decimal EPS = 0.0001m; // tolerancia por redondeos

                foreach (var grupo in detallesTarifaPorcentaje.GroupBy(d => new { d.AMT_APM_Id, d.NombreModulo }))
                {
                    // Total a repartir por % para este módulo
                    decimal totalModulo = grupo.First().ImporteMensualRepartoPorcentajes;

                    // Si no hay nada que repartir, NO exigimos 100%
                    if (Math.Abs(totalModulo) <= EPS)
                        continue;

                    decimal sumaPorcentajes = listaModEmpresas
                        .Where(me => me.AME_APM_Id == grupo.Key.AMT_APM_Id)
                        .Sum(me => me.AME_PorcentajeReparto ?? 0m);

                    if (Math.Abs(sumaPorcentajes - 100m) > EPS)
                    {
                        erroresPorEmpresa.Add(
                            $"Reparto porcentaje incorrecto para módulo '{grupo.Key.NombreModulo}': suma={sumaPorcentajes.ToString("F2")}% (debe ser 100% cuando hay importe a repartir)."
                        );
                    }
                }

                if (!erroresPorEmpresa.Any())
                {
                    foreach (var d in detallesTarifaPorcentaje)
                    {
                        decimal totalModulo = d.ImporteMensualRepartoPorcentajes;

                        // Si el total es 0, no generamos líneas (o puedes generarlas a 0 si quieres trazabilidad)
                        if (Math.Abs(totalModulo) <= EPS)
                            continue;

                        var me = listaModEmpresas.FirstOrDefault(x =>
                            x.AME_APM_Id == d.AMT_APM_Id && x.AME_EMP_Id == idEmpresa);

                        if (me == null || !(me.AME_PorcentajeReparto.HasValue))
                            continue;

                        decimal porcentaje = me.AME_PorcentajeReparto.Value; // 0..100
                        decimal importePorEmpresa = (totalModulo * porcentaje) / 100m;

                        var objReparto = new AplicacionPreviewDetalleImporteReparto
                        {
                            Origen = $"Importe reparto porcentaje '{d.NombreModulo}'",
                            IdModuloTarifa = d.AMT_Id,
                            Equitativo = false,
                            ImporteTotal = totalModulo,
                            Porcentaje = porcentaje / 100m, // en 0..1 para tu DTO
                            Importe = importePorEmpresa
                        };

                        listaImporteReparto.Add(objReparto);
                        ImporteTotal += importePorEmpresa;
                    }
                }
                #endregion

                resultado.Add(new AplicacionPreviewRow
                {
                    IdEmpresa = idEmpresa,
                    NombreEmpresa = listaEmpresas.ContainsKey(idEmpresa) ? listaEmpresas[idEmpresa].EMP_Nombre : String.Empty,
                    IdTarea = idTarea,
                    NombreTarea = listaTareas.ContainsKey(idTarea) ? listaTareas[idTarea].TAR_Nombre : String.Empty,
                    Anyo = anyoConceptos,
                    Mes = mesConceptos,
                    ImporteTotal = ImporteTotal,
                    ListaDetallesLicencia = listaLicencias,
                    ListaDetallesLicenciaPorEntidad = listaLicenciasDetalle,
                    ListaDetallesImporteFijo = listaImporteFijo,
                    ListaDetallesImporteReparto = listaImporteReparto,
                    ListaErrores = erroresPorEmpresa
                });
            }

            return resultado;
        }

        [HttpPost]
        public JsonResult GenerarConceptosAplicaciones()
        {
            try
            {
                var periodoFact = ObtenerPeriodoFacturacion();
                int anyoConceptos = periodoFact.Year;
                int mesConceptos = periodoFact.Month;

                // 1) Obtienes el preview como ya lo haces
                var previewRows = GenerarPreviewAplicaciones();
                int lineasCreadas = 0;
                int detallesApp = 0, detallesMod = 0, detallesRep = 0;

                var dalLineas = new DAL_Tareas_Empresas_LineasEsfuerzo();
                var dalApps = new DAL_Tareas_Empresas_LineasEsfuerzo_Aplicaciones();
                var dalMods = new DAL_Tareas_Empresas_LineasEsfuerzo_Modulos();
                var dalReps = new DAL_Tareas_Empresas_LineasEsfuerzo_ModulosReparto();
                var empresasDict = new DAL_Empresas().L(false, null).ToDictionary(e => e.EMP_Id);

                // Recorres cada fila preview
                foreach (var row in previewRows)
                {
                    if (row.ListaErrores != null && row.ListaErrores.Any())
                        continue;
                    else
                    {
                        // 3) Inserto cabecera en Tareas_Empresas_LineasEsfuerzo
                        var nuevo = new Tareas_Empresas_LineasEsfuerzo
                        {
                            TLE_TAR_Id = row.IdTarea,
                            TLE_EMP_Id = row.IdEmpresa,
                            TLE_Anyo = row.Anyo,
                            TLE_Mes = row.Mes,
                            TLE_Cantidad = row.ImporteTotal,
                            TLE_Descripcion = "Generado Auto Aplicaciones " + anyoConceptos + "-" + mesConceptos,
                            TLE_ESO_Id = (int)Constantes.EstadosSolicitud.PendienteAprobacion,
                            TLE_ESO_Id_Original = (int)Constantes.EstadosSolicitud.SinSolicitar,
                            TLE_PER_Id_Aprobador = empresasDict[row.IdEmpresa].EMP_PER_Id_AprobadorDefault,
                            TLE_FechaAprobacion = DateTime.Now,
                            TLE_ComentarioAprobacion = Resources.Resource.AprobadoAutomatico,
                            FechaAlta = DateTime.Now,
                            FechaModificacion = DateTime.Now,
                            PER_Id_Modificacion = Sesion.SPersonaId,
                            TLE_Inversion = false
                        };
                        new DAL_Tareas_Empresas_LineasEsfuerzo().G(nuevo, Sesion.SPersonaId);

                        lineasCreadas++;
                        int nuevaLineaId = nuevo.TLE_Id;

                        // 4) Detalle de licencias → Tareas_Empresas_LineasEsfuerzo_Aplicaciones
                        foreach (var det in row.ListaDetallesLicenciaPorEntidad)
                        {
                            var detalleApp = new Tareas_Empresas_LineasEsfuerzo_Aplicaciones
                            {
                                TCA_TLE_Id = nuevaLineaId,
                                TCA_APP_Id = det.IdAplicacion,
                                TCA_ENT_Id = det.IdEntidad,
                                TCA_Importe = det.Importe
                            };
                            dalApps.G(detalleApp, Sesion.SPersonaId);

                            detallesApp++;
                        }

                        // 5) Detalle Importe Fijo → Tareas_Empresas_LineasEsfuerzo_Modulos
                        foreach (var det in row.ListaDetallesImporteFijo)
                        {
                            var detalleMod = new Tareas_Empresas_LineasEsfuerzo_Modulos
                            {
                                TCM_TLE_Id = nuevaLineaId,
                                TCM_AME_Id = det.IdModuloEmpresa,
                                TCM_Importe = det.Importe
                            };
                            dalMods.G(detalleMod, Sesion.SPersonaId);

                            detallesMod++;
                        }

                        // 6) Detalle Reparto → Tareas_Empresas_LineasEsfuerzo_ModulosReparto
                        foreach (var det in row.ListaDetallesImporteReparto)
                        {
                            var detalleRep = new Tareas_Empresas_LineasEsfuerzo_ModulosReparto
                            {
                                TCR_TLE_Id = nuevaLineaId,
                                TCR_AMT_Id = det.IdModuloTarifa,
                                TCR_Equitativo = det.Equitativo,
                                TCR_ImporteTotal = det.ImporteTotal,
                                TCR_Porcentaje = det.Porcentaje,
                                TCR_Importe = det.Importe,
                            };
                            dalReps.G(detalleRep, Sesion.SPersonaId);

                            detallesRep++;
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    lineasCreadas,
                    detallesApp,
                    detallesMod,
                    detallesRep
                });
            }
            catch (Exception ex)
            {
                // loguea ex
                return Json(new { success = false, mensaje = ex.Message });
            }
        }
    }
}

public class AplicacionPreviewRow
{
    public int IdEmpresa { get; set; }
    public string NombreEmpresa { get; set; }
    public int IdTarea { get; set; }
    public string NombreTarea { get; set; }
    public int Anyo { get; set; }
    public int Mes { get; set; }
    public decimal ImporteTotal { get; set; }
    public List<AplicacionPreviewDetalleLicencia> ListaDetallesLicencia { get; set; }
    public List<AplicacionPreviewDetalleLicenciaDetalle> ListaDetallesLicenciaPorEntidad { get; set; }
    public List<AplicacionPreviewDetalleImporteFijo> ListaDetallesImporteFijo { get; set; }
    public List<AplicacionPreviewDetalleImporteReparto> ListaDetallesImporteReparto { get; set; }
    public List<string> ListaErrores { get; set; }
}

public class AplicacionPreviewDetalleLicencia
{
    public string Origen { get; set; }
    public int IdAplicacion { get; set; }
    public string Aplicacion { get; set; }
    public decimal Importe { get; set; }
}

public class AplicacionPreviewDetalleLicenciaDetalle
{
    public string Origen { get; set; }
    public int IdAplicacion { get; set; }
    public string Aplicacion { get; set; }
    public int IdEntidad { get; set; }
    public decimal Importe { get; set; }
}

public class AplicacionPreviewDetalleImporteFijo
{
    public string Origen { get; set; }
    public int IdModuloEmpresa { get; set; }
    public decimal Importe { get; set; }
}

public class AplicacionPreviewDetalleImporteReparto
{
    public string Origen { get; set; }
    public int IdModuloTarifa { get; set; }
    public bool Equitativo { get; set; }
    public decimal ImporteTotal { get; set; }
    public decimal Porcentaje { get; set; }
    public decimal Importe { get; set; }
}