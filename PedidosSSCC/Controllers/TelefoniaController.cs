using AccesoDatos;
using OfficeOpenXml;
using PedidosSSCC.Comun;
using PedidosSSCC.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static PedidosSSCC.Comun.Constantes;

namespace PedidosSSCC.Controllers
{
    public class TelefoniaController : BaseController
    {
        private const string FormatoFecha = "dd/MM/yyyy";

        private static readonly DAL_Telefonia TelefoniaDAL = new DAL_Telefonia();

        public ActionResult Telefonia()
        {
            ViewBag.Title = "Gestión de Telefonía";
            return View();
        }

        public ActionResult TelefoniaGlobal()
        {
            ViewBag.Title = "Consumo de Datos por UN y Uso";
            return View();
        }

        public ActionResult TiposCuota() => View();

        [HttpGet]
        public JsonResult ObtenerConsumoGbPorUnYUso(int? anyo = null, int? mes = null, int? tipo = null)
        {
            var lista = TelefoniaDAL.L(false, null).AsQueryable();

            if (anyo.HasValue) lista = lista.Where(t => t.TFN_Anyo == anyo.Value).AsQueryable();
            if (mes.HasValue) lista = lista.Where(t => t.TFN_Mes == mes.Value).AsQueryable();
            if (tipo.HasValue) lista = lista.Where(t => t.TFN_Tipo == tipo.Value).AsQueryable();

            // UN = TFN_Planta_Uso ; Uso = TFN_TipoCuota
            var res = lista
                .GroupBy(t => new
                {
                    UN = (t.TFN_Planta_Uso ?? "").Trim(),
                    Uso = (t.TFN_TipoCuota ?? "").Trim()
                })
                .Select(g => new
                {
                    UN = g.Key.UN,
                    Uso = g.Key.Uso,
                    TotalGB = Math.Round(
                        g.Sum(x => Convert.ToDecimal(x.TFN_Bytes ?? 0)), 3),
                    Registros = g.Count()
                })
                .OrderByDescending(x => x.TotalGB)
                .ToList();

            return Json(res, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ObtenerTelefonias(DataTablesRequest request)
        {
            var query = TelefoniaDAL.L(false, null).AsQueryable();

            // Total sin filtros
            var recordsTotal = query.Count();

            // Filtro global
            if (!string.IsNullOrEmpty(request.Search?.Value))
            {
                string search = request.Search.Value.ToLower();
                query = query.Where(t =>
                    (t.TFN_Telefono ?? "").ToLower().Contains(search) ||
                    (t.Empresas.EMP_Nombre ?? "").ToLower().Contains(search) ||
                    (t.TFN_Categoria ?? "").ToLower().Contains(search) ||
                    (t.TFN_NumFactura ?? "").ToLower().Contains(search)
                );
            }

            // Total filtrado
            var recordsFiltered = query.Count();

            // Ordenación manual (sin EF)
            if (request.Order != null && request.Order.Count > 0)
            {
                var order = request.Order.First();
                var colName = request.Columns[order.Column].Data;

                switch (colName)
                {
                    case "TFN_Id":
                        query = order.Dir == "asc" ? query.OrderBy(t => t.TFN_Id) : query.OrderByDescending(t => t.TFN_Id);
                        break;
                    case "TFN_Anyo":
                        query = order.Dir == "asc" ? query.OrderBy(t => t.TFN_Anyo) : query.OrderByDescending(t => t.TFN_Anyo);
                        break;
                    case "TFN_Mes":
                        query = order.Dir == "asc" ? query.OrderBy(t => t.TFN_Mes) : query.OrderByDescending(t => t.TFN_Mes);
                        break;
                    case "EmpresaNombre":
                        query = order.Dir == "asc" ? query.OrderBy(t => t.Empresas.EMP_Nombre) : query.OrderByDescending(t => t.Empresas.EMP_Nombre);
                        break;
                    default:
                        query = query.OrderBy(t => t.TFN_Id);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(t => t.TFN_Id);
            }

            // Paginación
            var data = query
                .Skip(request.Start)
                .Take(request.Length)
                .Select(t => new
                {
                    t.TFN_Id,
                    t.TFN_Anyo,
                    t.TFN_Mes,
                    t.TFN_Tipo,
                    TipoNombre = ((TipoTelefonia)t.TFN_Tipo).ToString(),
                    t.TFN_EMP_Id,
                    EmpresaNombre = t.Empresas != null ? t.Empresas.EMP_Nombre : "",
                    t.TFN_Planta_EMP_Id,
                    t.TFN_Planta_Departamento,
                    t.TFN_Planta_Sede,
                    t.TFN_Planta_Uso,
                    t.TFN_Ciclo,
                    t.TFN_NumFactura,
                    t.TFN_NumCuenta,
                    t.TFN_Categoria,
                    t.TFN_Telefono,
                    t.TFN_Extension,
                    t.TFN_TipoCuota,
                    t.TFN_Bytes,
                    t.TFN_Importe,
                    TFN_FechaInicio = t.TFN_FechaInicio,
                    TFN_FechaFin = t.TFN_FechaFin
                })
                .ToList();

            return Json(new
            {
                draw = request.Draw,
                recordsTotal,
                recordsFiltered,
                data
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GuardarTelefonia(Telefonia obj)
        {
            try
            {
                bool ok = TelefoniaDAL.G(obj, Sesion.SPersonaId);
                return Json(new { success = ok, message = TelefoniaDAL.MensajeErrorEspecifico });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarTelefonia(int idTelefonia)
        {
            try
            {
                bool ok = TelefoniaDAL.D(idTelefonia);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> ImportarExcelTelefonia(HttpPostedFileBase archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.ContentLength == 0)
                return Json(new { success = false, message = "No se ha seleccionado un archivo." });

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var dalConfig = new DAL_Configuraciones();
                var objConfig = dalConfig.L_PrimaryKey((int)Constantes.Configuracion.AnioConcepto);
                var hoy = DateTime.Today;
                int anio = DateTime.Now.Year;
                string mes = DateTime.Now.Month.ToString("D2");

                // 1) Descargar Excel de Planta desde SharePoint
                var siteUrl = ConfigurationManager.AppSettings["siteUrl"];
                var folderPlanta = ConfigurationManager.AppSettings["folderPlanta"] + anio + "/" + anio + "-" + mes;
                // 2025/2025-09/

                string nombreExcel = $"Planta Vodafone {anio}-{mes}.xlsx";
                var ficheroPlanta = await SharePointFiles.ObtenerFicherosSharepoint(siteUrl, folderPlanta, nombreExcel);

                if (ficheroPlanta == null)
                    return Json(new { success = false, message = "No se encontró el fichero de planta en SharePoint." });

                var plantaPorTelefono = new Dictionary<string, PlantaRow>(); // clave: teléfono (solo dígitos)
                var plantaPorExtension = new Dictionary<string, PlantaRow>(StringComparer.OrdinalIgnoreCase); // clave: EXT.

                // Diccionarios empresa: alias (EMP_NombreDA) y nombre (EMP_Nombre)
                // Si ya los tienes por DAL, recupéralos aquí:
                var empresas = new DAL_Empresas().L(false, null).ToList();
                var dicEmpPorAlias = empresas
                    .Where(e => !string.IsNullOrWhiteSpace(e.EMP_NombreDA))
                    .GroupBy(e => e.EMP_NombreDA.Trim().ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First().EMP_Id);

                var dicEmpPorNombre = empresas
                    .Where(e => !string.IsNullOrWhiteSpace(e.EMP_Nombre))
                    .GroupBy(e => e.EMP_Nombre.Trim().ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First().EMP_Id);

                var dicEmpPorNumFactura = empresas
                    .Where(e => !string.IsNullOrWhiteSpace(e.EMP_NumFacturaVDF))
                    .GroupBy(e => e.EMP_NumFacturaVDF.Trim())
                    .ToDictionary(g => g.Key, g => g.First().EMP_Id);

                using (var pkgPlanta = new ExcelPackage(new FileInfo(ficheroPlanta)))
                {
                    var wsPlanta = pkgPlanta.Workbook.Worksheets["Planta"]
                                  ?? pkgPlanta.Workbook.Worksheets.FirstOrDefault(s => s.Name.IndexOf("planta", StringComparison.OrdinalIgnoreCase) >= 0)
                                  ?? pkgPlanta.Workbook.Worksheets.First();

                    if (wsPlanta?.Dimension != null)
                    {
                        int start = wsPlanta.Dimension.Start.Row + 1; // salta cabecera
                        int end = wsPlanta.Dimension.End.Row;

                        var range = wsPlanta.Cells[start, 1, end, 10].Value as object[,];
                        if (range != null)
                        {
                            for (int r = 0; r < range.GetLength(0); r++)
                            {
                                var tel = range[r, 0]?.ToString().Trim();
                                var ext = range[r, 1]?.ToString().Trim();
                                if (string.IsNullOrEmpty(tel) && string.IsNullOrEmpty(ext)) continue;

                                var row = new PlantaRow
                                {
                                    Telefono = tel,
                                    Extension = ext,
                                    Div = range[r, 5]?.ToString().Trim(),
                                    UN = range[r, 6]?.ToString().Trim(),
                                    Dpto = range[r, 7]?.ToString().Trim(),
                                    Sede = range[r, 9]?.ToString().Trim()
                                };

                                var telKey = SoloDigitos(row.Telefono);
                                if (!string.IsNullOrEmpty(telKey) && !plantaPorTelefono.ContainsKey(telKey))
                                    plantaPorTelefono[telKey] = row;

                                if (!string.IsNullOrEmpty(row.Extension) && !plantaPorExtension.ContainsKey(row.Extension))
                                    plantaPorExtension[row.Extension] = row;
                            }
                        }
                    }
                }

                // Cache mapeo UN/DIV -> EMP_Id para no recalcular
                var cacheEmp = new Dictionary<(string un, string div), int>();
                int MapearEmpresaId(string un, string div)
                {
                    var key = ((un ?? "").Trim().ToUpperInvariant(), (div ?? "").Trim().ToUpperInvariant());
                    if (cacheEmp.TryGetValue(key, out var empCached)) return empCached;

                    int emp;
                    if (!string.IsNullOrEmpty(key.Item1) &&
                        (dicEmpPorAlias.TryGetValue(key.Item1, out emp) || dicEmpPorNombre.TryGetValue(key.Item1, out emp)))
                    {
                        cacheEmp[key] = emp; return emp;
                    }
                    if (!string.IsNullOrEmpty(key.Item2) &&
                        (dicEmpPorAlias.TryGetValue(key.Item2, out emp) || dicEmpPorNombre.TryGetValue(key.Item2, out emp)))
                    {
                        cacheEmp[key] = emp; return emp;
                    }
                    cacheEmp[key] = 0; return 0;
                }

                // 2) Procesar el Excel subido "Detalle Cuotas"
                var errores = new List<RegistroExcelTelefoniaRetorno>();
                var dtTelefonia = CrearTablaTelefonia();

                using (var package = new ExcelPackage(archivoExcel.InputStream))
                {
                    var ws = package.Workbook.Worksheets["Detalle Cuotas"] ?? package.Workbook.Worksheets.FirstOrDefault();
                    if (ws == null || ws.Dimension == null || ws.Dimension.End.Column < (int)Columnas.L)
                    {
                        return Json(new
                        {
                            success = false,
                            excelErroneo = true,
                            mensajeExcelErroneo = "Formato incorrecto del Excel de detalle."
                        });
                    }

                    var startRow = 4; // según tu formato
                    var endRow = ws.Dimension.End.Row;

                    for (int fila = startRow; fila <= endRow; fila++)
                    {
                        // Corte por columna A vacía: fin de filas válidas
                        var colA = ws.Cells[fila, (int)Columnas.A].GetValue<string>()?.Trim();
                        if (string.IsNullOrEmpty(colA)) break;

                        var sb = new StringBuilder(64);

                        var ciclo = colA;
                        var facturaNum = ws.Cells[fila, (int)Columnas.B].GetValue<string>()?.Trim();
                        var cuentaNum = ws.Cells[fila, (int)Columnas.C].GetValue<string>()?.Trim();
                        var categoria = ws.Cells[fila, (int)Columnas.D].GetValue<string>()?.Trim();
                        var telefonoRaw = ws.Cells[fila, (int)Columnas.E].GetValue<string>()?.Trim();
                        var extension = ws.Cells[fila, (int)Columnas.F].GetValue<string>()?.Trim();
                        var tipoCuenta = ws.Cells[fila, (int)Columnas.G].GetValue<string>()?.Trim();

                        var importeVal = ws.Cells[fila, (int)Columnas.H].GetValue<decimal?>();
                        decimal importe = importeVal ?? ParseDecimalRapido(ws.Cells[fila, (int)Columnas.H].GetValue<string>());

                        var fechaIni = ws.Cells[fila, (int)Columnas.K].GetValue<DateTime?>();
                        var fechaFin = ws.Cells[fila, (int)Columnas.L].GetValue<DateTime?>();

                        if (!fechaIni.HasValue) sb.AppendLine("Fecha inicio inválida.");
                        if (!fechaFin.HasValue) sb.AppendLine("Fecha fin inválida.");

                        int? idEmpresa = null;
                        if (!string.IsNullOrEmpty(cuentaNum) && cuentaNum.Length >= 8)
                        {
                            var ultimos8 = cuentaNum.Substring(cuentaNum.Length - 8);
                            if (dicEmpPorNumFactura.TryGetValue(ultimos8, out var empId))
                                idEmpresa = empId;
                        }

                        // === NUEVA LÓGICA DE CRUCE ===
                        int? idPlantaEmpresa = null; 
                        string plantDepartamento = ""; string plantaSede = ""; string plantaUso = "";
                        string telefonoKey = SoloDigitos(telefonoRaw);

                        if (!string.IsNullOrEmpty(telefonoRaw))
                        {
                            if (telefonoRaw.StartsWith("2") && telefonoRaw.Length == 8)
                            {
                                // Caso 1: cuotas a nivel de cuenta
                                (plantDepartamento, plantaSede, plantaUso) = MaestroPorTipoCuenta(tipoCuenta);
                            }
                            else if (telefonoRaw.StartsWith("6"))
                            {
                                // Caso 2: móviles
                                if (plantaPorTelefono.TryGetValue(telefonoKey, out var rTel))
                                {
                                    idPlantaEmpresa = MapearEmpresaId(rTel.UN, rTel.Div);
                                    plantDepartamento = rTel.Dpto;
                                    plantaSede = rTel.Sede;
                                    plantaUso = rTel.UN;
                                }
                            }
                            else if (telefonoRaw.StartsWith("2") && telefonoRaw.Contains("-"))
                            {
                                // Caso 3: fijos
                                var partes = telefonoRaw.Split('-');
                                if (partes.Length == 2)
                                {
                                    var fijo = SoloDigitos(partes[1]);
                                    if (plantaPorTelefono.TryGetValue(fijo, out var rTel))
                                    {
                                        idPlantaEmpresa = MapearEmpresaId(rTel.UN, rTel.Div);
                                        plantDepartamento = rTel.Dpto;
                                        plantaSede = rTel.Sede;
                                        plantaUso = rTel.UN;
                                    }
                                }
                            }
                            else
                            {
                                // Fallback
                                if (plantaPorTelefono.TryGetValue(telefonoKey, out var rTel))
                                {
                                    idPlantaEmpresa = MapearEmpresaId(rTel.UN, rTel.Div);
                                    plantDepartamento = rTel.Dpto;
                                    plantaSede = rTel.Sede;
                                    plantaUso = rTel.UN;
                                }
                                else if (!string.IsNullOrEmpty(extension) && plantaPorExtension.TryGetValue(extension, out var rExt))
                                {
                                    idPlantaEmpresa = MapearEmpresaId(rExt.UN, rExt.Div);
                                    plantDepartamento = rExt.Dpto;
                                    plantaSede = rExt.Sede;
                                    plantaUso = rExt.UN;
                                }
                            }
                        }

                        // Validación adicional: al menos uno de los IDs debe estar localizado
                        if (idEmpresa == null && idPlantaEmpresa == null)
                        {
                            sb.AppendLine("No se ha podido localizar la empresa (ni por CUENTA_NO ni por TELEF).");
                        }

                        if (sb.Length == 0)
                        {
                            var row = dtTelefonia.NewRow();
                            row["TFN_Anyo"] = fechaIni.Value.Year;
                            row["TFN_Mes"] = fechaIni.Value.Month;
                            row["TFN_Tipo"] = (int)TipoTelefonia.Facturacion;
                            row["TFN_EMP_Id"] = idEmpresa.HasValue ? (object)idEmpresa.Value : DBNull.Value;
                            row["TFN_Planta_EMP_Id"] = (idPlantaEmpresa == null || idPlantaEmpresa == 0) ? (object)DBNull.Value : idPlantaEmpresa;
                            row["TFN_Planta_Departamento"] = Truncar(plantDepartamento, 50);
                            row["TFN_Planta_Sede"] = Truncar(plantaSede, 50);
                            row["TFN_Planta_Uso"] = Truncar(plantaUso, 50);
                            row["TFN_Ciclo"] = Truncar(ciclo, 50);
                            row["TFN_NumFactura"] = Truncar(facturaNum, 50);
                            row["TFN_NumCuenta"] = Truncar(cuentaNum, 50);
                            row["TFN_Categoria"] = Truncar(categoria, 50);
                            row["TFN_Telefono"] = Truncar(telefonoRaw, 50);
                            row["TFN_Extension"] = Truncar(extension, 50);
                            row["TFN_TipoCuota"] = Truncar(tipoCuenta, 50);
                            row["TFN_Bytes"] = 0L;
                            row["TFN_Importe"] = importe;
                            row["TFN_FechaInicio"] = fechaIni.Value;
                            row["TFN_FechaFin"] = fechaFin.Value;
                            dtTelefonia.Rows.Add(row);
                        }
                        else
                        {
                            errores.Add(new RegistroExcelTelefoniaRetorno
                            {
                                TFN_Anyo = fechaIni?.Year ?? 0,
                                TFN_Mes = fechaIni?.Month ?? 0,
                                TFN_Tipo = (int)TipoTelefonia.Facturacion,
                                TFN_EMP_Id = idEmpresa,
                                TFN_Planta_EMP_Id = idPlantaEmpresa,
                                Errores = sb.ToString(),
                                FilaExcel = fila
                            });
                        }
                    }
                }

                // 3) Si hay errores → devolver el mismo Excel pero solo con "Detalle Cuotas" y columna Errores
                if (errores.Count > 0)
                {
                    var rutaTemp = ConfigurationManager.AppSettings["PathTemporales"];
                    if (string.IsNullOrEmpty(rutaTemp))
                        rutaTemp = Server.MapPath("~/App_Data/Temp");

                    Directory.CreateDirectory(rutaTemp);
                    var extensionOriginal = Path.GetExtension(archivoExcel.FileName);
                    var nombreFichero = $"Errores_Telefonia_{DateTime.Now:yyyyMMdd_HHmmss}{extensionOriginal}";

                    var path = Path.Combine(rutaTemp, nombreFichero);

                    // Reabrimos el Excel original subido
                    using (var package = new ExcelPackage(archivoExcel.InputStream))
                    {
                        // Mantener solo "Detalle Cuotas"
                        var ws = package.Workbook.Worksheets["Detalle Cuotas"];
                        if (ws == null)
                            ws = package.Workbook.Worksheets.FirstOrDefault();

                        // Borrar todas las demás hojas
                        for (int i = package.Workbook.Worksheets.Count; i >= 1; i--)
                        {
                            var hoja = package.Workbook.Worksheets[i - 1];
                            if (hoja != ws)
                                package.Workbook.Worksheets.Delete(i - 1);
                        }

                        // Añadir cabecera "Errores" en la columna siguiente a la L
                        int colErrores = (int)Columnas.L + 1;
                        var celdaNueva = ws.Cells[3, colErrores];
                        var celdaBase = ws.Cells[3, (int)Columnas.L];

                        // Copiar estilo completo
                        celdaNueva.StyleID = celdaBase.StyleID;

                        // Poner el texto
                        celdaNueva.Value = "Errores";

                        // Copiar ancho de columna
                        ws.Column(colErrores).Width = ws.Column((int)Columnas.L).Width;

                        // Escribir errores fila a fila (usa FilaExcel que guardaste en el bucle)
                        foreach (var err in errores)
                        {
                            if (err.FilaExcel > 0)
                                ws.Cells[err.FilaExcel, colErrores].Value = err.Errores;
                        }

                        // Guardar a disco
                        System.IO.File.WriteAllBytes(path, package.GetAsByteArray());
                    }

                    var url = Url.Action("DescargarErrores", "Portal", new { fileName = nombreFichero }, Request.Url.Scheme);
                    return Json(new { success = false, erroresExcel = errores, fileUrl = url });
                }

                // 4) Sin errores → **bulk** en UNA operación (sin tocar SQL del servidor)
                //    Requiere que la tabla dbo.Telefonia exista con las columnas indicadas.
                var cs = ConfigurationManager.ConnectionStrings["AccesoDatos.Properties.Settings.FacturacionInternaConnectionString"].ConnectionString; // ajusta el nombre
                using (var conn = new SqlConnection(cs))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        BulkInsertTelefonia(dtTelefonia, conn, tran);
                        tran.Commit();
                    }
                }

                return Json(new { success = true, message = $"Importación correcta. Registros: {dtTelefonia.Rows.Count}" });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, message = ex.Message, excelErroneo = true });
            }
        }

        [HttpPost]
        public JsonResult ImportarExcelTelefoniaRoaming(HttpPostedFileBase archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.ContentLength == 0)
            {
                return Json(new { success = false, message = "No se ha seleccionado un archivo." });
            }

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var errores = new List<RegistroExcelTelefoniaRetorno>();
                var dtTelefonia = CrearTablaTelefonia();

                var empresas = new DAL_Empresas().L(false, null).ToList();
                var dicEmpPorNumFactura = empresas
                    .Where(e => !string.IsNullOrWhiteSpace(e.EMP_NumFacturaVDF))
                    .GroupBy(e => e.EMP_NumFacturaVDF.Trim())
                    .ToDictionary(g => g.Key, g => g.First().EMP_Id);

                using (var package = new ExcelPackage(archivoExcel.InputStream))
                {
                    var ws = package.Workbook.Worksheets["roaming datos Z1 y Nav marit"];
                    if (ws == null || ws.Dimension == null)
                    {
                        return Json(new { success = false, message = "No se encontró la hoja esperada." });
                    }

                    int fila = 2; // cabecera en fila 1
                    int erroresColumna = ws.Dimension.End.Column + 1;
                    ws.Cells[1, erroresColumna].Value = "Errores";

                    while (true)
                    {
                        if (string.IsNullOrEmpty(ws.Cells[fila, 2].Text?.Trim()))
                            break;

                        var sbErrores = new StringBuilder();

                        try
                        {
                            var tipo = (int)TipoTelefonia.Roaming;

                            int? idPlantaEmpresa = null;
                            string plantDepartamento = "";
                            string plantaSede = "";
                            string plantaUso = "";

                            // Columnas
                            var ciclo = ws.Cells[fila, (int)Columnas.A].GetValue<string>()?.Trim();
                            var facturaNum = ws.Cells[fila, (int)Columnas.B].GetValue<string>()?.Trim();
                            var cuentaNum = ws.Cells[fila, (int)Columnas.C].GetValue<string>()?.Trim();

                            // Buscar empresa por EMP_NumFacturaVDF
                            int? idEmpresa = null;
                            if (!string.IsNullOrEmpty(cuentaNum) && cuentaNum.Length >= 8)
                            {
                                var ultimos8 = cuentaNum.Substring(cuentaNum.Length - 8);
                                if (dicEmpPorNumFactura.TryGetValue(ultimos8, out var empId))
                                    idEmpresa = empId;
                            }

                            var categoria = ws.Cells[fila, (int)Columnas.D].GetValue<string>()?.Trim();
                            var telefono = ws.Cells[fila, (int)Columnas.F].GetValue<string>()?.Trim();
                            var extension = ws.Cells[fila, (int)Columnas.G].GetValue<string>()?.Trim();
                            var tipoCuota = ws.Cells[fila, (int)Columnas.J].GetValue<string>()?.Trim();
                            var destino = ws.Cells[fila, (int)Columnas.K].GetValue<string>()?.Trim();
                            var kb = ws.Cells[fila, (int)Columnas.L].GetValue<long>();
                            var mb = ws.Cells[fila, (int)Columnas.M].GetValue<decimal>();
                            var importe = ws.Cells[fila, (int)Columnas.O].GetValue<decimal>();
                            var fecha = ws.Cells[fila, (int)Columnas.H].GetValue<DateTime>();

                            // Si no hay errores -> agregar fila
                            var row = dtTelefonia.NewRow();
                            row["TFN_Anyo"] = fecha.Year;
                            row["TFN_Mes"] = fecha.Month;
                            row["TFN_Tipo"] = tipo;
                            row["TFN_EMP_Id"] = idEmpresa.HasValue ? (object)idEmpresa.Value : DBNull.Value;
                            row["TFN_Planta_EMP_Id"] = (idPlantaEmpresa == null || idPlantaEmpresa == 0) ? (object)DBNull.Value : idPlantaEmpresa;
                            row["TFN_Planta_Departamento"] = Truncar(plantDepartamento, 50);
                            row["TFN_Planta_Sede"] = Truncar(plantaSede, 50);
                            row["TFN_Planta_Uso"] = Truncar(plantaUso, 50);
                            row["TFN_Ciclo"] = Truncar(ciclo, 50);
                            row["TFN_NumFactura"] = Truncar(facturaNum, 50);
                            row["TFN_NumCuenta"] = Truncar(cuentaNum, 50);
                            row["TFN_Categoria"] = Truncar(categoria, 50);
                            row["TFN_Telefono"] = Truncar(telefono, 50);
                            row["TFN_Extension"] = Truncar(extension, 50);
                            row["TFN_TipoCuota"] = Truncar(tipoCuota, 50);
                            row["TFN_Bytes"] = kb; // o conversión de MB si aplica
                            row["TFN_Importe"] = importe;
                            row["TFN_FechaInicio"] = fecha;
                            row["TFN_FechaFin"] = fecha;
                            dtTelefonia.Rows.Add(row);
                        }
                        catch (Exception ex)
                        {
                            sbErrores.AppendLine(ex.Message);
                        }

                        ws.Cells[fila, erroresColumna].Value =
                            sbErrores.Length > 0 ? sbErrores.ToString() : "OK";

                        if (sbErrores.Length > 0)
                        {
                            errores.Add(new RegistroExcelTelefoniaRetorno
                            {
                                Errores = sbErrores.ToString(),
                                TFN_Mes = ws.Cells[fila, (int)Columnas.H].GetValue<DateTime>().Month
                            });
                        }

                        fila++;
                    }

                    // Errores -> Excel anotado
                    if (errores.Count > 0)
                    {
                        var rutaTemp = ConfigurationManager.AppSettings["PathTemporales"];
                        if (string.IsNullOrEmpty(rutaTemp))
                            rutaTemp = Server.MapPath("~/App_Data/Temp");

                        Directory.CreateDirectory(rutaTemp);
                        var extensionOriginal = Path.GetExtension(archivoExcel.FileName);
                        var nombreFichero = $"Errores_TelefoniaRoaming_{DateTime.Now:yyyyMMdd_HHmmss}{extensionOriginal}";
                        var path = Path.Combine(rutaTemp, nombreFichero);

                        System.IO.File.WriteAllBytes(path, package.GetAsByteArray());
                        var url = Url.Action("DescargarErrores", "Portal", new { fileName = nombreFichero }, Request.Url.Scheme);

                        return Json(new { success = false, erroresExcel = errores, fileUrl = url });
                    }

                    // Bulk insert
                    if (dtTelefonia.Rows.Count > 0)
                    {
                        var cs = ConfigurationManager.ConnectionStrings["AccesoDatos.Properties.Settings.FacturacionInternaConnectionString"].ConnectionString;
                        using (var conn = new SqlConnection(cs))
                        {
                            conn.Open();
                            using (var tran = conn.BeginTransaction())
                            {
                                BulkInsertTelefonia(dtTelefonia, conn, tran);
                                tran.Commit();
                            }
                        }
                    }

                    return Json(new { success = true, message = $"Importación de roaming correcta. Registros: {dtTelefonia.Rows.Count}" });
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
        }

        private bool ValidarFecha(string fechaStr, out DateTime fecha, StringBuilder errores, string campo)
        {
            fecha = default;

            // 1) Intentar intercambiar día/mes si el formato es “dd/MM/yyyy HH:mm”
            string fechaParaParseo = SwapDayMonth(fechaStr) ?? fechaStr;
            // — si SwapDayMonth devolvió null, usamos la cadena tal cual vino —

            // 2) Ahora probamos con el formato que espera “MM/dd/yyyy HH:mm”
            if (!DateTime.TryParseExact(
                    fechaParaParseo,
                    FormatoFecha,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out fecha))
            {
                errores.AppendLine($"Fecha de {campo} incorrecta (se esperaba formato “{FormatoFecha}”).");
                return false;
            }

            return true;
        }

        private string SwapDayMonth(string fechaStr)
        {
            if (string.IsNullOrWhiteSpace(fechaStr))
                return null;

            // Separa en “fecha” y “hora” (antes y después del espacio)
            var partes = fechaStr.Split(' ');
            if (partes.Length < 2)
                return null;

            string parteFecha = partes[0]; // ej. "21/2/2025" o "1/12/2025"
            string parteHora = partes[1]; // ej. "3:5" o "13:30" (sin segundos)

            // Procesar la parte de fecha: “día/mes/año”
            var elems = parteFecha.Split('/');
            if (elems.Length != 3)
                return null;

            // elems[0] = día, elems[1] = mes, elems[2] = año
            if (!int.TryParse(elems[1], out int diaOrig) ||
                !int.TryParse(elems[0], out int mesOrig) ||
                !int.TryParse(elems[2], out int anioInt))
            {
                return null; // Formato no numérico
            }

            // Formateamos con ceros a la izquierda:
            string dia = diaOrig.ToString("00");    // “02” en vez de “2”
            string mes = mesOrig.ToString("00");    // “09” en vez de “9”
            string anio = anioInt.ToString("2000"); // “2025” (4 dígitos)

            // Construimos la parte de fecha reordenada: “MM/dd/yyyy”
            string fechaReordenada = $"{dia}/{mes}/{anio}"; // ej. “02/21/2025”

            // Procesar la parte de hora: “HH:mm”
            var partesHora = parteHora.Split(':');
            if (partesHora.Length < 2)
                return null;

            if (!int.TryParse(partesHora[0], out int horaInt) ||
                !int.TryParse(partesHora[1], out int minInt))
            {
                return null;
            }

            // Dos dígitos para hora y minutos:
            string horaFormateada = horaInt.ToString("00"); // “03” en vez de “3”
            string minutoFormateado = minInt.ToString("00"); // “05” en vez de “5”

            string horaMin = $"{horaFormateada}:{minutoFormateado}"; // ej. “03:05”

            // Devolvemos “MM/dd/yyyy HH:mm”
            return $"{fechaReordenada} {horaMin}";
        }

        private static string SoloDigitos(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var arr = new char[s.Length]; int j = 0;
            for (int i = 0; i < s.Length; i++) if (char.IsDigit(s[i])) arr[j++] = s[i];
            return j == 0 ? string.Empty : new string(arr, 0, j);
        }

        private static decimal ParseDecimalRapido(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0m;
            raw = raw.Replace("€", "").Trim();
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.GetCultureInfo("es-ES"), out var d)) return d;
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
            return 0m;
        }

        private static int ExtraerEmpresaDesdeCuenta(string cuentaNum)
        {
            if (string.IsNullOrEmpty(cuentaNum) || cuentaNum.Length <= 8) return 0;
            if (int.TryParse(cuentaNum.Substring(cuentaNum.Length - 8), out var id)) return id;
            return 0;
        }

        private static DataTable CrearTablaTelefonia()
        {
            var dt = new DataTable();
            dt.Columns.Add("TFN_Anyo", typeof(int));
            dt.Columns.Add("TFN_Mes", typeof(int));
            dt.Columns.Add("TFN_Tipo", typeof(int));

            var colEmp = new DataColumn("TFN_EMP_Id", typeof(int)) { AllowDBNull = true };
            dt.Columns.Add(colEmp);

            var colPlantaEmp = new DataColumn("TFN_Planta_EMP_Id", typeof(int)) { AllowDBNull = true };
            dt.Columns.Add(colPlantaEmp);

            dt.Columns.Add("TFN_Planta_Departamento", typeof(string));
            dt.Columns.Add("TFN_Planta_Sede", typeof(string));
            dt.Columns.Add("TFN_Planta_Uso", typeof(string));
            dt.Columns.Add("TFN_Ciclo", typeof(string));
            dt.Columns.Add("TFN_NumFactura", typeof(string));
            dt.Columns.Add("TFN_NumCuenta", typeof(string));
            dt.Columns.Add("TFN_Categoria", typeof(string)).MaxLength = 50;
            dt.Columns.Add("TFN_Telefono", typeof(string));
            dt.Columns.Add("TFN_Extension", typeof(string));
            dt.Columns.Add("TFN_TipoCuota", typeof(string));
            dt.Columns.Add("TFN_Bytes", typeof(long));
            dt.Columns.Add("TFN_Importe", typeof(decimal));
            dt.Columns.Add("TFN_FechaInicio", typeof(DateTime));
            dt.Columns.Add("TFN_FechaFin", typeof(DateTime));
            return dt;
        }

        private static void BulkInsertTelefonia(DataTable dt, SqlConnection conn, SqlTransaction tran)
        {
            using (var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers, tran))
            {
                bulk.DestinationTableName = "dbo.Telefonia"; // <- ajusta si tu tabla está en otro esquema/nombre
                bulk.BatchSize = 1000;
                bulk.BulkCopyTimeout = 0; // sin límite

                foreach (DataColumn c in dt.Columns)
                    bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);

                bulk.WriteToServer(dt);
            }
        }

        private static (string, string, string) MaestroPorTipoCuenta(string tipoCuenta)
        {
            if (string.IsNullOrWhiteSpace(tipoCuenta))
                return ("", "", "");

            using (var dal = new DAL_TiposCuota())
            {
                // Normalizamos igual que antes
                var key = tipoCuenta.Trim().ToUpperInvariant();

                // Buscamos en la BD el registro con esa "cuota"
                var entidad = dal.L(false, t => t.TCU_Cuota.ToUpper() == key).FirstOrDefault();

                if (entidad != null)
                {
                    return (
                        entidad.TCU_Departamento ?? "",
                        entidad.TCU_Sede ?? "",
                        entidad.TCU_Uso ?? ""
                    );
                }
            }

            return ("", "", "");
        }

        private static string Truncar(string valor, int maxLen)
        {
            if (string.IsNullOrEmpty(valor)) return string.Empty;
            valor = valor.Trim();
            return valor.Length > maxLen ? valor.Substring(0, maxLen) : valor;
        }

        [HttpGet]
        public ActionResult ObtenerTiposCuota()
        {
            var dal = new DAL_TiposCuota();
            var lista = dal.L(false, null)
                .Select(t => new
                {
                    t.TCU_Id,
                    t.TCU_Cuota,
                    t.TCU_Departamento,
                    t.TCU_Sede,
                    t.TCU_Uso
                })
                .OrderBy(i => i.TCU_Cuota)
                .ToList();

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarTipoCuota(TiposCuota dto)
        {
            try
            {
                if (dto == null) return Json(new { success = false, message = "Datos inválidos." });

                var dal = new DAL_TiposCuota();
                bool ok = dal.G(dto, Sesion.SPersonaId);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarTipoCuota(int id)
        {
            try
            {
                var dal = new DAL_TiposCuota();
                bool ok = dal.D(id);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Abre un Excel "Detalle Cuotas" desde un Stream y ejecuta
        /// - validaciones
        /// - construcción de DataTable (CrearTablaTelefonia)
        /// - bulk insert (BulkInsertTelefonia)
        /// Devuelve true si todo fue OK.
        /// </summary>
        private bool ProcesarExcelDetalleCuotas(Stream excelStream,
            out List<RegistroExcelTelefoniaRetorno> errores, out int filasInsertadas)
        {
            errores = new List<RegistroExcelTelefoniaRetorno>();
            filasInsertadas = 0;

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                // === TODO: copia aquí la parte de tu método ImportarExcelTelefonia
                // === donde:
                //  - creas DataTable dtTelefonia = CrearTablaTelefonia();
                //  - abres el paquete: using (var package = new ExcelPackage(excelStream)) { ... }
                //  - seleccionas la hoja "Detalle Cuotas"
                //  - recorres las filas y vas agregando al DataTable
                //  - vas llenando 'errores' si corresponde
                //  - si errores.Count > 0 -> return false
                //  - si OK -> BulkInsertTelefonia(dtTelefonia, conn, tran);
                //
                // Para no duplicar, literalmente puedes copiar/pegar el cuerpo
                // del try principal de tu ImportarExcelTelefonia (sección 2),
                // reemplazando "archivoExcel.InputStream" por "excelStream".

                var dtTelefonia = CrearTablaTelefonia();

                using (var package = new ExcelPackage(excelStream))
                {
                    var ws = package.Workbook.Worksheets["Detalle Cuotas"] ?? package.Workbook.Worksheets.FirstOrDefault();
                    if (ws == null || ws.Dimension == null || ws.Dimension.End.Column < (int)Columnas.L)
                    {
                        errores.Add(new RegistroExcelTelefoniaRetorno { Errores = "Formato de Excel no válido." });
                        return false;
                    }

                    // >>> Aquí va tu bucle de filas y creación de rows del DataTable
                    // >>> (idéntico a ImportarExcelTelefonia, adaptando a este contexto)
                    // ...
                }

                // Si hay errores, devolvemos false (como ya hacías)
                if (errores.Count > 0) return false;

                // Bulk insert (idéntico a tu ImportarExcelTelefonia)
                var cs = System.Configuration.ConfigurationManager
                    .ConnectionStrings["AccesoDatos.Properties.Settings.FacturacionInternaConnectionString"]
                    .ConnectionString;

                using (var conn = new SqlConnection(cs))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        BulkInsertTelefonia(dtTelefonia, conn, tran);
                        tran.Commit();
                        filasInsertadas = dtTelefonia.Rows.Count;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errores.Add(new RegistroExcelTelefoniaRetorno { Errores = ex.Message });
                return false;
            }
        }
    }

    [Serializable]
    class RegistroExcelTelefoniaRetorno
    {
        public object TFN_Anyo;
        public object TFN_Mes;
        public object TFN_Tipo;
        public object TFN_EMP_Id;
        public object TFN_Planta_EMP_Id;
        public string Errores;
        public int FilaExcel { get; set; }
    }

    class PlantaRow
    {
        public string Telefono { get; set; }   // Columna A: Línea
        public string Extension { get; set; }  // Columna B: EXT.
        public string Div { get; set; }        // Columna F: DIV
        public string UN { get; set; }         // Columna G: UN
        public string Dpto { get; set; }       // Columna H: Dpto.
        public string Sede { get; set; }       // Columna J: Sede
    }

    public enum TipoTelefonia
    {
        Facturacion = 1,
        Roaming = 2
    }

    public class DataTablesRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }    // desde qué fila
        public int Length { get; set; }   // cuántas filas

        public DataTablesSearch Search { get; set; }
        public List<DataTablesOrder> Order { get; set; }
        public List<DataTablesColumn> Columns { get; set; }
    }

    public class DataTablesSearch
    {
        public string Value { get; set; }
        public bool Regex { get; set; }
    }

    public class DataTablesOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; }
    }

    public class DataTablesColumn
    {
        public string Data { get; set; }
        public string Name { get; set; }
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }
        public DataTablesSearch Search { get; set; }
    }

}