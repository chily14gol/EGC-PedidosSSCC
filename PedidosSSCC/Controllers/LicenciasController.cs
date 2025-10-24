using AccesoDatos;
using Microsoft.Extensions.Primitives;
using OfficeOpenXml;
using PedidosSSCC.Comun;
using PedidosSSCC.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using static PedidosSSCC.Comun.Constantes;

namespace PedidosSSCC.Controllers
{
    public class LicenciasController : BaseController
    {
        private readonly DAL_Licencias dalLicencias = new DAL_Licencias();
        private readonly DAL_Licencias_Tarifas _dalTarifas = new DAL_Licencias_Tarifas();
        private readonly DAL_Licencias_Excepciones _dalExcepciones = new DAL_Licencias_Excepciones();

        [HttpGet]
        public JsonResult ObtenerLicencias()
        {
            var todas = dalLicencias.L(); // Cargar todas las licencias una vez
            var lista = todas.Select(x => new
            {
                x.LIC_Id,
                x.LIC_Nombre,
                x.LIC_NombreMS,
                x.LIC_Gestionado,
                x.LIC_MaximoGrupo,
                x.LIC_LIC_Id_Padre,
                NombrePadre = x.LIC_LIC_Id_Padre.HasValue ? ObtenerNombre(x.LIC_LIC_Id_Padre.Value) : null,
                x.LIC_TAR_Id_Antivirus,
                x.LIC_TAR_Id_Backup,
                x.LIC_TAR_Id_SW,
                TieneHijas = todas.Any(l => l.LIC_LIC_Id_Padre == x.LIC_Id) // Nueva propiedad
            });

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarLicencia(Licencias licencia)
        {
            try
            {
                bool ok = dalLicencias.G(licencia, Sesion.SPersonaId);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarLicencia(Licencias licencia)
        {
            try
            {
                dalLicencias.Eliminar(licencia);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerTarifas(int licenciaId)
        {
            var lista = _dalTarifas.ObtenerTarifasPorLicencia(licenciaId);
            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarTarifa(Licencias_Tarifas tarifa)
        {
            bool esAlta = true;
            bool resultado = _dalTarifas.GuardarTarifa(tarifa, esAlta, Sesion.SPersonaId);
            return Json(new { success = resultado });
        }

        [HttpPost]
        public JsonResult EliminarTarifa(int idLicencia, DateTime fechaInicio)
        {
            bool resultado = _dalTarifas.EliminarTarifa(idLicencia, fechaInicio);
            return Json(new { success = resultado });
        }

        [HttpGet]
        public JsonResult ObtenerExcepciones(int licenciaId)
        {
            var lista = _dalExcepciones.ObtenerExcepcionesPorLicencia(licenciaId);
            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarExcepcion(Licencias_Excepciones excepcion)
        {
            bool resultado = _dalExcepciones.G(excepcion, Sesion.SPersonaId);
            return Json(new { success = resultado });
        }

        [HttpPost]
        public JsonResult EliminarExcepcion(int idLicencia, int idEmpresa)
        {
            bool resultado = _dalExcepciones.EliminarExcepcion(idLicencia, idEmpresa);
            return Json(new { success = resultado });
        }

        [HttpGet]
        public JsonResult ObtenerPedidosCombo()
        {
            DAL_Facturas dal = new DAL_Facturas();
            var lista = dal.L(false, null);

            var resultado = lista.Select(f => new
            {
                f.FAC_Id,
                f.FAC_NumFactura
            }).ToList();

            return new LargeJsonResult { Data = resultado, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public ActionResult ProcesarLicenciasDesdeExcel(HttpPostedFileBase archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.ContentLength == 0)
                return Json(new { success = false, message = "No se ha seleccionado un archivo." });

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var excelPackage = new ExcelPackage(archivoExcel.InputStream))
                {
                    var ws = excelPackage.Workbook.Worksheets.FirstOrDefault()
                             ?? throw new Exception("No se encontró ninguna hoja en el Excel.");

                    var dal = new DAL_LicenciasExcel();
                    var resultados = dal.ProcesarLicencias(ws);

                    // 1) detectamos bloqueantes
                    var bloqueantes = resultados
                        .Where(r =>
                            r.Resultado == "Entidad no encontrada" ||
                            r.Resultado == "Licencia no encontrada" ||
                            r.Resultado == "Licencias incompatibles" ||
                            r.Resultado == "Licencias incompatibles (hermanas)"
                        )
                        .ToList();

                    // 2) detectamos no bloqueantes (todo resultado distinto de OK que no esté en bloqueantes)
                    var noBloqueantes = resultados
                        .Where(r => !bloqueantes.Contains(r))
                        .ToList();

                    if (bloqueantes.Any())
                    {
                        // devolvemos fichero y marcamos como bloqueante
                        Response.AddHeader("X-Type", "bloqueante");
                        var stream = GenerarExcelResultados(bloqueantes);
                        const string nombreArchivo = "Errores_Bloqueantes.xlsx";
                        return File(
                            stream,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            nombreArchivo
                        );
                    }
                    else if (noBloqueantes.Any())
                    {
                        // devolvemos fichero y marcamos como warning
                        Response.AddHeader("X-Type", "warning");
                        var stream = GenerarExcelResultados(noBloqueantes);
                        const string nombreArchivo = "Advertencias_No_Bloqueantes.xlsx";
                        return File(
                            stream,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            nombreArchivo
                        );
                    }

                    // 3) todo ok
                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new
                {
                    success = false,
                    excelErroneo = true,
                    message = ex.Message,
                    mensajeExcelErroneo = "Ha ocurrido un error al importar el fichero"
                });
            }
        }

        private MemoryStream GenerarExcelResultados(List<ResultadoProcesamiento> resultados)
        {
            var stream = new MemoryStream();
            using (var package = new ExcelPackage(stream))
            {
                var ws = package.Workbook.Worksheets.Add("Resultado");

                // CABECERAS
                string[] headers = { "email", "nombre", "licencia", "resultado" };
                for (int col = 0; col < headers.Length; col++)
                {
                    var cell = ws.Cells[1, col + 1];
                    cell.Value = headers[col];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCoral);
                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }

                // CONTENIDO
                for (int i = 0; i < resultados.Count; i++)
                {
                    ws.Cells[i + 2, 1].Value = resultados[i].Email;
                    ws.Cells[i + 2, 2].Value = resultados[i].Nombre;
                    ws.Cells[i + 2, 3].Value = resultados[i].Licencia;
                    ws.Cells[i + 2, 4].Value = resultados[i].Resultado;
                }

                // BORDES Y AUTOAJUSTE
                var tableRange = ws.Cells[1, 1, resultados.Count + 1, 4];
                tableRange.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                tableRange.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                tableRange.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                tableRange.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                package.Save();
            }

            stream.Position = 0;
            return stream;
        }

        [HttpGet]
        public JsonResult ObtenerLicenciasCombo()
        {
            var lista = dalLicencias.L().Select(x => new
            {
                x.LIC_Id,
                x.LIC_Nombre
            }).OrderBy(i => i.LIC_Nombre);
            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ObtenerComboTiposEnte(int idLicencia)
        {
            var dalTipos = new DAL_TiposEnte();
            var dalAsociaciones = new DAL_Licencias_TiposEnte();

            // Obtener todos los tipos de ente
            var todos = dalTipos.L(false, null)
                .Select(t => new
                {
                    id = t.TEN_Id,
                    text = t.TEN_Nombre
                }).ToList();

            // Obtener IDs ya asociados a esta licencia
            var asociados = dalAsociaciones.L(false, null)
                .Where(l => l.LTE_LIC_Id == idLicencia)
                .Select(l => l.LTE_TEN_Id)
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
        public JsonResult ObtenerLicenciasIncompatiblesCombo(int idLicencia)
        {
            var dalLicencias = new DAL_Licencias();
            var dalIncompatibles = new DAL_Licencias_Incompatibles();

            // 1) todos los candidatos
            var todos = dalLicencias.L(false, null)
                .Select(x => new
                {
                    id = x.LIC_Id,
                    text = x.LIC_Nombre
                })
                .ToList();

            // 2) los que ya están marcados como incompatibles con esta licencia
            //    (ten en cuenta que la tabla puede guardar pares en un solo sentido o en ambos)
            var asociados = dalIncompatibles.L(false, null)
                .Where(r => r.LIL_LIC_Id1 == idLicencia)
                .Select(r => r.LIL_LIC_Id2)
                // si guardas también la inversa, descomenta esta unión:
                //.Union(dalIncompatibles.L(false, null)
                //    .Where(r => r.LIL_LIC_Id2 == idLicencia)
                //    .Select(r => r.LIL_LIC_Id1))
                .ToHashSet();

            // 3) marcar selected
            var resultado = todos
                .Select(x => new
                {
                    x.id,
                    x.text,
                    selected = asociados.Contains(x.id)
                })
                .OrderBy(x => x.text)
                .ToList();

            return Json(resultado, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ObtenerComboLicenciasReemplazo(int idLicencia, int idEmpresa)
        {
            var dalLicencias = new DAL_Licencias(); // Asumo que esta devuelve nombres de licencias
            var dalReemplazos = new DAL_Licencias_Excepciones_LicenciasReemplazo();

            // Todas las licencias disponibles como posibles reemplazos (excepto la misma)
            var todas = dalLicencias.L(false, null)
                .Where(l => l.LIC_Id != idLicencia)
                .Select(l => new
                {
                    id = l.LIC_Id,
                    text = l.LIC_Nombre
                }).ToList();

            // Reemplazos ya asociados
            var seleccionadas = dalReemplazos.L(false, null)
                .Where(r => r.LEL_LIE_LIC_Id == idLicencia && r.LEL_LIE_EMP_Id == idEmpresa)
                .Select(r => r.LEL_LIC_Id_Reemplazo)
                .ToHashSet();

            // Marcar como selected las asociadas
            var resultado = todas.Select(l => new
            {
                id = l.id,
                text = l.text,
                selected = seleccionadas.Contains(l.id)
            }).OrderBy(l => l.text).ToList();

            return Json(resultado, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ObtenerIncompatibilidades(int idLicencia)
        {
            var dal = new DAL_Licencias_Incompatibles();
            var todas = dal.L(false, null);

            var incompatibles = todas
                .Where(x => x.LIL_LIC_Id1 == idLicencia || x.LIL_LIC_Id2 == idLicencia)
                .Select(x => x.LIL_LIC_Id1 == idLicencia ? x.LIL_LIC_Id2 : x.LIL_LIC_Id1)
                .Distinct()
                .ToList();

            var dalLicencias = new DAL_Licencias();
            var licencias = dalLicencias.L(false, null)
                .Where(l => incompatibles.Contains(l.LIC_Id))
                .Select(l => new
                {
                    LIC_Id = l.LIC_Id,
                    NombreLicenciaIncompatible = l.LIC_Nombre
                })
                .ToList();

            return Json(licencias, JsonRequestBehavior.AllowGet);
        }

        public string ObtenerNombre(int id)
        {
            DAL_Licencias dalLicencias = new DAL_Licencias();
            return dalLicencias.L().Where(x => x.LIC_Id == id).Select(x => x.LIC_Nombre).FirstOrDefault();
        }

        [HttpGet]
        public JsonResult ObtenerMinimos(int licenciaId)
        {
            // Instanciar el DAL correspondiente (lo crearemos abajo)
            var dalMinimos = new DAL_Licencias_Minimos();

            // Obtener lista de mínimos para esa licencia, junto con el nombre de la empresa
            var lista = dalMinimos.ObtenerMinimosPorLicencia(licenciaId);

            // Devolver como JSON (LargeJsonResult si esperas muchos registros)
            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarMinimo(Licencias_Minimos dto)
        {
            try
            {
                // dto contiene: LEM_LIC_Id, LEM_EMP_Id, LEM_MinimoFacturar
                var dalMinimos = new DAL_Licencias_Minimos();
                bool ok = dalMinimos.GuardarOModificar(dto, Sesion.SPersonaId);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarMinimo(int idLicencia, int idEmpresa)
        {
            try
            {
                var dalMinimos = new DAL_Licencias_Minimos();
                bool ok = dalMinimos.Eliminar(idLicencia, idEmpresa);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Calcula los detalles de facturación por licencia agrupando por empresa y tarea.
        /// Aplica tarifas, reemplazos, excepciones y mínimos de facturación.
        /// 
        /// Regla de negocio:
        /// - Si el padre NO tiene hijas: usa sus propias excepciones y reemplazos.
        /// - Si el padre SÍ tiene hijas: usa las excepciones y reemplazos de cada hija.
        /// - Los mínimos se aplican SIEMPRE al padre.
        /// </summary>
        private (Dictionary<(int empId, int tId), List<(int licId, int entId, decimal importe)>> Detalles,
            Dictionary<int, Licencias> LicAll, Dictionary<int, int> PadreMap, Dictionary<int, Empresas> EmpDict,
            Dictionary<int, Entes> EntesDict, Dictionary<int, string> TareasDict)
            CalcularDetallesPorLicencia(DateTime periodo)
        {
            var hoy = DateTime.Today;

            // === 1️⃣ Carga de datos base ===
            var entesLic = new DAL_Entes_Licencias().L(false, null);
            var entesDict = new DAL_Entes().L(false, null).ToDictionary(e => e.ENT_Id);
            var empDict = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id);
            var tareasDict = new DAL_Tareas().L(false, null).ToDictionary(t => t.TAR_Id, t => t.TAR_Nombre);
            var licAll = new DAL_Licencias().L(false, null).ToDictionary(l => l.LIC_Id);
            var tarifas = new DAL_Licencias_Tarifas().L(false, null);
            var excs = new DAL_Licencias_Excepciones().L(false, null);
            var repl = new DAL_Licencias_Excepciones_LicenciasReemplazo().L(false, null);

            // === 2️⃣ Licencias ya facturadas ===
            var daoDetLic = new DAL_Tareas_Empresas_LineasEsfuerzo_LicenciasMS();
            var licYaFacturadasIds = daoDetLic.L(true, null)
                .Where(d =>
                    d.Tareas_Empresas_LineasEsfuerzo.TLE_Anyo == periodo.Year &&
                    d.Tareas_Empresas_LineasEsfuerzo.TLE_Mes == periodo.Month)
                .Select(d => d.TCL_LIC_Id)
                .ToHashSet();

            // === 3️⃣ Filtrado de Entes/Licencias válidos ===
            var entesLicPendientes = entesLic
                .Where(el => !licYaFacturadasIds.Contains(el.ENL_LIC_Id))
                .Where(el => entesDict.ContainsKey(el.ENL_ENT_Id) && entesDict[el.ENL_ENT_Id].ENT_EMP_Id.HasValue)
                .Where(el => entesDict[el.ENL_ENT_Id].ENT_EMP_Id.Value != (int)EmpresaExcluyenteConceptos.EGC)
                .ToList();

            // === 4️⃣ Mapa hija → padre ===
            var padreMap = licAll
                .Where(kv => kv.Value.LIC_LIC_Id_Padre.HasValue)
                .ToDictionary(kv => kv.Key, kv => kv.Value.LIC_LIC_Id_Padre.Value);

            // === 5️⃣ Contenedor acumulador ===
            var detallesPorConcepto = new Dictionary<(int empId, int tId), List<(int licId, int entId, decimal importe)>>();

            // === 6️⃣ Agrupar por (licPadre, empresa) ===
            var agrupados = entesLicPendientes
                .Select(el => new
                {
                    LIC_Id = el.ENL_LIC_Id,
                    EMP_Id = entesDict[el.ENL_ENT_Id].ENT_EMP_Id.Value,
                    ENT_Id = el.ENL_ENT_Id
                })
                .GroupBy(x => new
                {
                    Padre = padreMap.TryGetValue(x.LIC_Id, out var p) ? p : x.LIC_Id,
                    x.EMP_Id
                });

            // === 7️⃣ Procesamiento principal por grupo ===
            foreach (var grp in agrupados)
            {
                int licPadre = grp.Key.Padre;
                int empId = grp.Key.EMP_Id;

                // Determinar si el padre tiene hijas
                bool padreTieneHijas = padreMap.Values.Contains(licPadre);

                // Si el padre no tiene hijas, puede tener su propia excepción
                var excPadre = !padreTieneHijas
                    ? excs.FirstOrDefault(e => e.LIE_LIC_Id == licPadre && e.LIE_EMP_Id == empId)
                    : null;

                int ajustePadre = excPadre?.LIE_CorreccionFacturacion ?? 0;
                if (excPadre != null && ajustePadre == 0)
                    continue; // excepción de no facturar

                // Agrupar hijas (si las hay)
                var porHija = grp
                    .GroupBy(x => x.LIC_Id)
                    .Select(g => new
                    {
                        LicHija = g.Key,
                        Ents = g.Select(v => v.ENT_Id).Distinct().ToList(),
                        Cant = g.Count()
                    });

                foreach (var h in porHija)
                {
                    int actualLic = h.LicHija;
                    int qtyAsign = h.Cant;
                    if (qtyAsign <= 0) continue;

                    // Excepción o corrección: si hay hijas, se usa la suya; si no, la del padre
                    var exc = padreTieneHijas
                        ? excs.FirstOrDefault(e => e.LIE_LIC_Id == actualLic && e.LIE_EMP_Id == empId)
                        : excPadre;
                    int ajuste = exc?.LIE_CorreccionFacturacion ?? 0;

                    // Reemplazos: igual que las excepciones (depende si el padre tiene hijas)
                    var reemplazos = repl
                        .Where(r => r.LEL_LIE_EMP_Id == empId &&
                                   (padreTieneHijas
                                       ? r.LEL_LIE_LIC_Id == actualLic
                                       : r.LEL_LIE_LIC_Id == licPadre))
                        .ToList();

                    // === CASO CON REEMPLAZOS ===
                    if (reemplazos.Any())
                    {
                        foreach (var r in reemplazos)
                        {
                            int licReemplazo = r.LEL_LIC_Id_Reemplazo;
                            if (licReemplazo == 0) continue;

                            if (!licAll.TryGetValue(licReemplazo, out var licRep) || licRep.LIC_Gestionado != true)
                                continue;

                            int licParaTarifa = padreMap.TryGetValue(licReemplazo, out var padreTarifa)
                                ? padreTarifa : licReemplazo;

                            var objTarifa = tarifas.FirstOrDefault(t =>
                                t.LIT_LIC_Id == licParaTarifa &&
                                t.LIT_FechaInicio <= hoy &&
                                (t.LIT_FechaFin == null || t.LIT_FechaFin > hoy));

                            if (objTarifa == null) continue;

                            void AddDet(int tareaId, decimal precioUnit)
                            {
                                if (tareaId == 0 || precioUnit <= 0) return;
                                var key = (empId, tareaId);
                                if (!detallesPorConcepto.TryGetValue(key, out var list))
                                {
                                    list = new List<(int licId, int entId, decimal importe)>();
                                    detallesPorConcepto[key] = list;
                                }
                                foreach (var entId in h.Ents.Where(entesDict.ContainsKey))
                                    list.Add((licReemplazo, entId, precioUnit));
                            }

                            if (licRep.LIC_TAR_Id_Antivirus.HasValue)
                                AddDet(licRep.LIC_TAR_Id_Antivirus.Value, objTarifa.LIT_PrecioUnitarioAntivirus ?? 0m);
                            if (licRep.LIC_TAR_Id_Backup.HasValue)
                                AddDet(licRep.LIC_TAR_Id_Backup.Value, objTarifa.LIT_PrecioUnitarioBackup ?? 0m);
                            if (licRep.LIC_TAR_Id_SW.HasValue)
                                AddDet(licRep.LIC_TAR_Id_SW.Value, objTarifa.LIT_PrecioUnitarioSW);
                        }
                        continue;
                    }

                    // === CASO SIN REEMPLAZOS ===
                    if (!licAll.TryGetValue(actualLic, out var lic) || lic.LIC_Gestionado != true)
                        continue;

                    int licParaTarifa2 = padreMap.TryGetValue(actualLic, out var padreTarifa2)
                        ? padreTarifa2 : actualLic;

                    var tarifa = tarifas.FirstOrDefault(t =>
                        t.LIT_LIC_Id == licParaTarifa2 &&
                        t.LIT_FechaInicio <= hoy &&
                        (t.LIT_FechaFin == null || t.LIT_FechaFin > hoy));

                    if (tarifa == null) continue;

                    void AddDetNormal(int tareaId, decimal precioUnit)
                    {
                        if (tareaId == 0 || precioUnit <= 0) return;
                        var key = (empId, tareaId);
                        if (!detallesPorConcepto.TryGetValue(key, out var list))
                        {
                            list = new List<(int licId, int entId, decimal importe)>();
                            detallesPorConcepto[key] = list;
                        }

                        foreach (var entId in h.Ents.Where(eid => entesDict.ContainsKey(eid)))
                            list.Add((actualLic, entId, precioUnit));
                    }

                    if (lic.LIC_TAR_Id_Antivirus.HasValue)
                        AddDetNormal(lic.LIC_TAR_Id_Antivirus.Value, tarifa.LIT_PrecioUnitarioAntivirus ?? 0m);
                    if (lic.LIC_TAR_Id_Backup.HasValue)
                        AddDetNormal(lic.LIC_TAR_Id_Backup.Value, tarifa.LIT_PrecioUnitarioBackup ?? 0m);
                    if (lic.LIC_TAR_Id_SW.HasValue)
                        AddDetNormal(lic.LIC_TAR_Id_SW.Value, tarifa.LIT_PrecioUnitarioSW);

                    // === Ajuste (corrección) si lo hay ===
                    if (ajuste != 0)
                    {
                        var licParaAjuste = padreMap.TryGetValue(actualLic, out var pAdj) ? pAdj : actualLic;
                        var tarifaAjuste = tarifas.FirstOrDefault(t =>
                            t.LIT_LIC_Id == licParaAjuste &&
                            t.LIT_FechaInicio <= hoy &&
                            (t.LIT_FechaFin == null || t.LIT_FechaFin > hoy));

                        if (tarifaAjuste != null)
                        {
                            void AddAjuste(int tareaId, decimal precioUnit)
                            {
                                if (tareaId == 0 || precioUnit <= 0) return;
                                var key = (empId, tareaId);
                                if (!detallesPorConcepto.TryGetValue(key, out var list))
                                {
                                    list = new List<(int licId, int entId, decimal importe)>();
                                    detallesPorConcepto[key] = list;
                                }

                                decimal importeAjuste = precioUnit * ajuste;
                                list.Add((licParaAjuste, 0, importeAjuste)); // entId = 0 → línea global
                            }

                            var licBase = licAll[licParaAjuste];
                            if (licBase.LIC_TAR_Id_SW.HasValue)
                                AddAjuste(licBase.LIC_TAR_Id_SW.Value, tarifaAjuste.LIT_PrecioUnitarioSW);
                            if (licBase.LIC_TAR_Id_Antivirus.HasValue)
                                AddAjuste(licBase.LIC_TAR_Id_Antivirus.Value, tarifaAjuste.LIT_PrecioUnitarioAntivirus ?? 0m);
                            if (licBase.LIC_TAR_Id_Backup.HasValue)
                                AddAjuste(licBase.LIC_TAR_Id_Backup.Value, tarifaAjuste.LIT_PrecioUnitarioBackup ?? 0m);
                        }
                    }
                }

                // === Aplicar mínimos al padre ===
                AplicarMinimoFacturar(detallesPorConcepto, periodo);
            }

            // === 8️⃣ Retorno completo ===
            return (detallesPorConcepto, licAll, padreMap, empDict, entesDict, tareasDict);
        }

        /// <summary>
        /// Aplica los mínimos de facturación a las licencias.
        /// - Si el consumo real (entes asociados) es menor que el mínimo almacenado,
        ///   añade una única línea de ajuste con el importe total necesario para alcanzar el mínimo.
        /// - Si el consumo real supera el mínimo, actualiza el valor en BD.
        /// 
        /// Reglas:
        ///   • Los mínimos se aplican siempre al PADRE.
        ///   • No se añaden líneas por unidad (solo una global por déficit).
        /// </summary>
        private void AplicarMinimoFacturar(Dictionary<(int empId, int tId), 
            List<(int licId, int entId, decimal importe)>> detallesPorConcepto, DateTime periodo)
        {
            int año = periodo.Year;
            int mesActual = periodo.Month;
            var hoy = DateTime.Today;

            // -------------------------------------------------
            // 1️⃣ Cargar mínimos almacenados
            // -------------------------------------------------
            var dalMinimos = new DAL_Licencias_Minimos();
            var listaMinimos = dalMinimos.L(false, null)
                .Select(m => new
                {
                    m.LEM_LIC_Id,   // Id de licencia padre
                    m.LEM_EMP_Id,   // Id de empresa
                    m.LEM_MinimoFacturar
                })
                .ToList();

            // Diccionario para búsqueda rápida: (empresa, licPadre) -> mínimo
            var dictMinimos = listaMinimos
                .ToDictionary(x => (x.LEM_EMP_Id, x.LEM_LIC_Id), x => x.LEM_MinimoFacturar);

            // -------------------------------------------------
            // 2️⃣ Cargar datos auxiliares
            // -------------------------------------------------
            var licAll = new DAL_Licencias().L(false, null).ToDictionary(l => l.LIC_Id);
            var tarifas = new DAL_Licencias_Tarifas().L(false, null);

            // -------------------------------------------------
            // 3️⃣ Recorrer cada grupo (empresa, tarea)
            // -------------------------------------------------
            foreach (var clave in detallesPorConcepto.Keys.ToList())
            {
                int empId = clave.empId;
                int tId = clave.tId;
                var listaDetalle = detallesPorConcepto[clave];

                // 3.1️⃣ Contar licencias reales (entId != 0), agrupadas por su padre
                var conteoPorPadre = listaDetalle
                    .Where(d => d.entId != 0)
                    .GroupBy(d =>
                    {
                        // Determinar licencia padre
                        var lic = licAll[d.licId];
                        return lic.LIC_LIC_Id_Padre ?? d.licId;
                    })
                    .ToDictionary(g => g.Key, g => g.Count());

                // 3.2️⃣ Aplicar mínimo a cada licencia padre detectada
                foreach (var kvLic in conteoPorPadre)
                {
                    int licPadre = kvLic.Key;
                    int actualCount = kvLic.Value;

                    // Buscar mínimo actual almacenado
                    dictMinimos.TryGetValue((empId, licPadre), out int minimoGuardado);

                    // 3.2.1️⃣ Si se ha consumido más que el mínimo, actualizarlo en BD
                    if (actualCount > minimoGuardado)
                    {
                        var entidadMin = new Licencias_Minimos
                        {
                            LEM_LIC_Id = licPadre,
                            LEM_EMP_Id = empId,
                            LEM_MinimoFacturar = actualCount
                        };

                        dalMinimos.GuardarOModificar(entidadMin, Sesion.SPersonaId);

                        minimoGuardado = actualCount;
                        dictMinimos[(empId, licPadre)] = minimoGuardado;
                    }

                    // 3.2.2️⃣ Si se ha consumido menos, calcular déficit y generar una sola línea de ajuste
                    if (minimoGuardado > actualCount)
                    {
                        int deficit = minimoGuardado - actualCount;

                        // Obtener licencia padre
                        if (!licAll.TryGetValue(licPadre, out var lic))
                            continue;

                        // Tarifa vigente
                        var tarifa = tarifas.FirstOrDefault(t =>
                            t.LIT_LIC_Id == licPadre &&
                            t.LIT_FechaInicio <= hoy &&
                            (t.LIT_FechaFin == null || t.LIT_FechaFin > hoy));

                        if (tarifa == null)
                            continue;

                        // Determinar precio unitario según la tarea
                        decimal precioUnit = 0;
                        if (lic.LIC_TAR_Id_Antivirus.HasValue && tId == lic.LIC_TAR_Id_Antivirus.Value)
                            precioUnit = tarifa.LIT_PrecioUnitarioAntivirus ?? 0;
                        else if (lic.LIC_TAR_Id_Backup.HasValue && tId == lic.LIC_TAR_Id_Backup.Value)
                            precioUnit = tarifa.LIT_PrecioUnitarioBackup ?? 0;
                        else if (lic.LIC_TAR_Id_SW.HasValue && tId == lic.LIC_TAR_Id_SW.Value)
                            precioUnit = tarifa.LIT_PrecioUnitarioSW;

                        if (precioUnit <= 0)
                            continue;

                        // 3.2.3️⃣ Calcular importe total de déficit (una sola línea)
                        decimal importeDeficit = precioUnit * deficit;

                        // Agregar una línea global de ajuste
                        listaDetalle.Add((licPadre, 0, importeDeficit));
                    }
                }
            }
        }

        [HttpPost]
        public JsonResult PrevisualizarConceptos()
        {
            try
            {
                // Año/mes según configuración
                DAL_Configuraciones dalConfig = new DAL_Configuraciones();
                Configuraciones objConfig = dalConfig.L_PrimaryKey((int)Constantes.Configuracion.AnioConcepto);
                var hoy = DateTime.Today;
                int anioConcepto = objConfig != null ? Convert.ToInt32(objConfig.CFG_Valor) : DateTime.Now.Year;
                int mesConcepto = anioConcepto < hoy.Year ? 12 : hoy.Month;
                var periodo = new DateTime(anioConcepto, mesConcepto, 1);

                // Núcleo común
                var core = CalcularDetallesPorLicencia(periodo);
                var detallesPorConcepto = core.Detalles;
                var licAll = core.LicAll;
                var padreMap = core.PadreMap;
                var empDict = core.EmpDict;
                var tareasDict = core.TareasDict;

                // Datos para validaciones/duplicados
                var tareasEmp = new DAL_Tareas_Empresas().L(false, null);
                var dalMS = new DAL_Tareas_Empresas_LineasEsfuerzo_LicenciasMS();
                var todasLineasMS = dalMS.L(false, null).Select(ms => ms.TCL_TLE_Id).ToHashSet();
                var todosConc = new DAL_Tareas_Empresas_LineasEsfuerzo().L(true, null).ToList();

                var erroresPorEmpresa = new List<string>();
                var preview = new List<ConceptoPreviewRow>();

                foreach (var kv in detallesPorConcepto)
                {
                    var key = kv.Key;
                    var lista = kv.Value;

                    // 1) Si NO hay asignación TEM para el año → añadir a errores de preview (NO paramos)
                    bool tieneTEM = tareasEmp.Any(te =>
                        te.TEM_EMP_Id == key.empId &&
                        te.TEM_TAR_Id == key.tId &&
                        te.TEM_Anyo == periodo.Year);

                    if (!tieneTEM)
                    {
                        var nombreEmp = empDict.TryGetValue(key.empId, out var e) ? e.EMP_Nombre : $"EMP {key.empId}";
                        var nombreTar = tareasDict.TryGetValue(key.tId, out var t) ? t : $"TAR {key.tId}";
                        erroresPorEmpresa.Add($"Sin asignación de tarea “{nombreTar}” para la empresa “{nombreEmp}” en {periodo.Year}.");
                        continue;
                    }

                    // 2) Evitar duplicar conceptos que ya tienen líneas MS
                    //var conceptoExistente = todosConc.FirstOrDefault(c =>
                    //    c.TLE_EMP_Id == key.empId &&
                    //    c.TLE_TAR_Id == key.tId &&
                    //    c.TLE_Anyo == periodo.Year &&
                    //    c.TLE_Mes == periodo.Month);

                    //if (conceptoExistente != null && todasLineasMS.Contains(conceptoExistente.TLE_Id))
                    //    continue;

                    // 3) Cálculo de importe total y “licencias incluidas”
                    decimal impTot = lista.Sum(x => x.importe);

                    var groupTexto = lista.GroupBy(d =>
                    {
                        int padreId = padreMap.TryGetValue(d.licId, out var p) ? p : d.licId;
                        var nombre = licAll[padreId].LIC_Nombre;
                        bool isAdj = d.entId == 0;
                        return (nombre, isAdj);
                    })
                    .Select(g =>
                    {
                        var (nombre, isAdj) = g.Key;
                        int count = g.Count();
                        return isAdj ? $"{count}-{nombre} (ajuste)" : $"{count}-{nombre}";
                    })
                    .ToList();

                    var tarea = new DAL_Tareas().L(false, null).First(i => i.TAR_Id == key.tId);
                    int totalCantidad = lista.Count(x => x.entId != 0); // entId = 0 son ajustes, los ignoramos

                    preview.Add(new ConceptoPreviewRow
                    {
                        TAR_Id = key.tId,
                        TAR_Nombre = tarea.TAR_Nombre,
                        EmpresaId = key.empId,
                        EmpresaNombre = empDict[key.empId].EMP_Nombre,
                        Anyo = periodo.Year,
                        Mes = periodo.Month,
                        ImporteTotal = impTot,
                        LicenciasIncluidas = string.Join(", ", groupTexto),
                        Cantidad = totalCantidad   // ✅ nuevo campo
                    });
                }

                // (Extra) Entes sin empresa -> error informativo en preview (como tenías)
                var entesLic = new DAL_Entes_Licencias().L(false, null);
                var entesDict2 = new DAL_Entes().L(false, null).ToDictionary(e => e.ENT_Id);
                var invalidEntes = entesLic
                    .Where(el => !entesDict2.ContainsKey(el.ENL_ENT_Id) || !entesDict2[el.ENL_ENT_Id].ENT_EMP_Id.HasValue)
                    .Select(el => entesDict2.ContainsKey(el.ENL_ENT_Id)
                        ? entesDict2[el.ENL_ENT_Id].ENT_Nombre
                        : $"Id:{el.ENL_ENT_Id}")
                    .Distinct()
                    .ToList();

                if (invalidEntes.Any())
                    erroresPorEmpresa.Add("Entidades sin empresa asociada: " + string.Join(", ", invalidEntes));

                return Json(new { success = true, licencias = preview, licenciasErrors = erroresPorEmpresa });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GenerarTodosConceptos()
        {
            try
            {
                // Año/mes según configuración
                DAL_Configuraciones dalConfig = new DAL_Configuraciones();
                Configuraciones objConfig = dalConfig.L_PrimaryKey((int)Constantes.Configuracion.AnioConcepto);
                var hoy = DateTime.Today;
                int anioConcepto = objConfig != null ? Convert.ToInt32(objConfig.CFG_Valor) : DateTime.Now.Year;
                int mesConcepto = anioConcepto < hoy.Year ? 12 : hoy.Month;
                var periodo = new DateTime(anioConcepto, mesConcepto, 1);

                // Núcleo común
                var core = CalcularDetallesPorLicencia(periodo);
                var detallesPorConcepto = core.Detalles;
                var licAll = core.LicAll;
                var padreMap = core.PadreMap;
                var empDict = core.EmpDict;
                var entesDict = core.EntesDict;

                // Datos auxiliares
                var tareasEmp = new DAL_Tareas_Empresas().L(false, null);
                var dalConc = new DAL_Tareas_Empresas_LineasEsfuerzo();
                var todosConc = dalConc.L(true, null).ToList();
                var dalMS = new DAL_Tareas_Empresas_LineasEsfuerzo_LicenciasMS();
                var todasLineasMS = dalMS.L(false, null).Select(ms => ms.TCL_TLE_Id).ToHashSet();

                var errores = new List<string>();
                var resumen = new Dictionary<int, int>(); // EMP_Id -> num conceptos creados
                var conceptosGen = new Dictionary<(int empId, int tId), int>(); // key -> TLE_Id

                foreach (var kv in detallesPorConcepto)
                {
                    var key = kv.Key;
                    var lista = kv.Value;

                    // Validación TEM
                    bool tieneTEM = tareasEmp.Any(te =>
                        te.TEM_EMP_Id == key.empId &&
                        te.TEM_TAR_Id == key.tId &&
                        te.TEM_Anyo == periodo.Year);

                    if (!tieneTEM)
                        continue;

                    decimal sumaImportes = lista.Sum(d => d.importe);
                    if (sumaImportes <= 0) continue;

                    // Evitar duplicados con MS existentes
                    //var conceptoExistente = todosConc.FirstOrDefault(c =>
                    //    c.TLE_EMP_Id == key.empId &&
                    //    c.TLE_TAR_Id == key.tId &&
                    //    c.TLE_Anyo == periodo.Year &&
                    //    c.TLE_Mes == periodo.Month);

                    //if (conceptoExistente != null && todasLineasMS.Contains(conceptoExistente.TLE_Id))
                    //    continue;

                    // Texto “Licencias incluidas”
                    var grupoTexto = lista.GroupBy(d =>
                    {
                        int padreId = padreMap.TryGetValue(d.licId, out var p) ? p : d.licId;
                        var nombre = licAll[padreId].LIC_Nombre;
                        bool isAdj = d.entId == 0;
                        return (nombre, isAdj);
                    })
                    .Select(g =>
                    {
                        var (nombre, isAdj) = g.Key;
                        int count = g.Count();
                        return isAdj ? $"{count}-{nombre} (ajuste)" : $"{count}-{nombre}";
                    });

                    StringBuilder sbLic = new StringBuilder();
                    sbLic.Append($"Licencias incluidas {periodo.Year}-{periodo.Month}:");
                    foreach (var s in grupoTexto) sbLic.Append(s);

                    // Crear concepto si no existe
                    int tleId;
                    if (!conceptosGen.TryGetValue(key, out tleId))
                    {
                        var nuevo = new Tareas_Empresas_LineasEsfuerzo
                        {
                            TLE_TAR_Id = key.tId,
                            TLE_EMP_Id = key.empId,
                            TLE_Anyo = periodo.Year,
                            TLE_Mes = periodo.Month,
                            TLE_Cantidad = sumaImportes,
                            TLE_Descripcion = sbLic.ToString(),
                            TLE_ESO_Id = (int)Constantes.EstadosSolicitud.PendienteAprobacion,
                            TLE_ESO_Id_Original = (int)Constantes.EstadosSolicitud.SinSolicitar,
                            TLE_PER_Id_Aprobador = empDict[key.empId].EMP_PER_Id_AprobadorDefault,
                            TLE_FechaAprobacion = DateTime.Now,
                            TLE_ComentarioAprobacion = Resources.Resource.AprobadoAutomatico,
                            FechaAlta = DateTime.Now,
                            FechaModificacion = DateTime.Now,
                            PER_Id_Modificacion = Sesion.SPersonaId,
                            TLE_Inversion = false
                        };
                        dalConc.G(nuevo, Sesion.SPersonaId);
                        tleId = nuevo.TLE_Id;
                        resumen[key.empId] = resumen.TryGetValue(key.empId, out var val) ? val + 1 : 1;

                        conceptosGen[key] = tleId;   // registrar el concepto ya creado
                    }

                    foreach (var g in lista.GroupBy(x => new { x.licId, x.entId }))
                    {
                        var totalImporte = g.Sum(x => x.importe);

                        var ms = new Tareas_Empresas_LineasEsfuerzo_LicenciasMS
                        {
                            TCL_TLE_Id = tleId,
                            TCL_LIC_Id = g.Key.licId,
                            TCL_ENT_Id = g.Key.entId,
                            TCL_Importe = totalImporte
                        };

                        dalMS.G(ms, Sesion.SPersonaId);
                    }
                }

                if (errores.Any())
                    throw new ApplicationException(string.Join("\n", errores));

                LimpiarCache(TipoCache.Tareas, TipoCache.Conceptos, TipoCache.Pedidos);

                var result = resumen.Select(r => new GenerarResultDto
                {
                    EmpresaId = r.Key,
                    EmpresaNombre = empDict[r.Key].EMP_Nombre,
                    ConceptosCreados = r.Value
                }).ToList();

                return Json(new { success = true, licencias = result });
            }
            catch (ApplicationException ex)
            {
                var errores = ex.Message.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                return Json(new { success = false, errors = errores });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, errors = new[] { "Error inesperado generando conceptos: " + ex.Message } });
            }
        }
    }

    public class IncompatibilidadDTO
    {
        public int LIC_Id { get; set; }
        public List<int> Incompatibles { get; set; }
    }
}