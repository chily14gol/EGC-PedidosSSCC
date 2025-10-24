using AccesoDatos;
using ExcelExport.HelperClasses;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using static PedidosSSCC.Comun.Constantes;

namespace PedidosSSCC.Controllers
{
    public class TicketsController : BaseController
    {
        private const string FormatoFecha = "dd/MM/yyyy HH:mm";

        // Repositorios DAL
        private static readonly DAL_EstadosTicket EstadosDAL = new DAL_EstadosTicket();

        public ActionResult EstadosTicket() => View();
        public ActionResult OrigenesTicket() => View();
        public ActionResult Tickets() => View();
        public ActionResult TiposTicket() => View();
        public ActionResult ValidacionesTicket() => View();

        private ActionResult GetLookup<T>(Func<IEnumerable<T>> fetch, Func<T, object> project)
        {
            var list = fetch().Select(project).ToList();
            return new LargeJsonResult
            {
                Data = list,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        private JsonResult TryDelete(Func<bool> action)
        {
            try
            {
                bool ok = action();
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerEstadosTicket() =>
            GetLookup(() => EstadosDAL.L(false, null),
                      e => new { e.ETK_Id, e.ETK_Nombre });

        [HttpPost]
        public JsonResult GuardarEstadoTicket(EstadosTicket estado)
        {
            try
            {
                bool ok = EstadosDAL.G(estado, Sesion.SPersonaId);

                if (!ok)
                {
                    return Json(new { success = false, message = "Ya existe un estado con ese nombre." });
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarEstadoTicket(int idEstado) =>
             TryDelete(() => EstadosDAL.D(idEstado));

        [HttpGet]
        public ActionResult ObtenerOrigenesTicket()
        {
            DAL_OrigenesTicket dal = new DAL_OrigenesTicket();
            List<OrigenesTicket> lista = dal.L(false, null);

            var listaFiltrada = lista.Select(i => new
            {
                i.OTK_Id,
                i.OTK_Nombre
            }).ToList();

            return new LargeJsonResult
            {
                Data = listaFiltrada,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpPost]
        public ActionResult GuardarOrigenTicket(OrigenesTicket origen)
        {
            try
            {
                DAL_OrigenesTicket dal = new DAL_OrigenesTicket();
                bool ok = dal.G(origen, Sesion.SPersonaId);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult EliminarOrigenTicket(int idOrigen)
        {
            try
            {
                DAL_OrigenesTicket dal = new DAL_OrigenesTicket();
                bool ok = dal.D(idOrigen);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerTiposTicket()
        {
            DAL_TiposTicket dal = new DAL_TiposTicket();
            List<TiposTicket> lista = dal.L(false, null);

            var listaFiltrada = lista.Select(i => new
            {
                i.TTK_Id,
                i.TTK_Nombre
            }).ToList();

            return new LargeJsonResult
            {
                Data = listaFiltrada,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpPost]
        public ActionResult GuardarTipoTicket(TiposTicket tipo)
        {
            try
            {
                DAL_TiposTicket dal = new DAL_TiposTicket();
                bool ok = dal.G(tipo, Sesion.SPersonaId);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult EliminarTipoTicket(int idTipo)
        {
            try
            {
                DAL_TiposTicket dal = new DAL_TiposTicket();
                bool ok = dal.D(idTipo);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerTickets()
        {
            DAL_Tickets TicketsDAL = new DAL_Tickets();
            List<Tickets> lista = TicketsDAL.L(false, null);

            var resultado = lista.Select(i => new
            {
                i.TKC_Id,
                i.TKC_Titulo,
                i.TKC_ETK_Id,
                i.TKC_TTK_Id,
                i.TKC_FechaApertura,
                i.TKC_ENT_Id_Solicitante,

                ENT_EMP_Id = i.Entes?.ENT_EMP_Id,
                EMP_Nombre = i.Entes?.Empresas?.EMP_Nombre,

                i.TKC_GrupoAsignado,
                i.TKC_Categoria,
                i.TKC_Descripcion,
                i.TKC_VTK_Id,
                i.TKC_Duracion,
                i.TKC_Ubicacion,
                i.TKC_ProveedorAsignado,
                i.TKC_GrupoCargo,
                i.TKC_OTK_Id,
                i.TKC_FechaResolucion,
                i.TKC_CTK_Id,
                i.TKC_Id_GLPI
            }).ToList();

            return new LargeJsonResult
            {
                Data = resultado,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpPost]
        public JsonResult GuardarTicket(Tickets ticket)
        {
            try
            {
                DAL_Tickets dal = new DAL_Tickets();
                bool ok = dal.G(ticket, Sesion.SPersonaId);

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarTicket(int idTicket)
        {
            try
            {
                DAL_Tickets dal = new DAL_Tickets();
                bool ok = dal.D(idTicket);

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ImportarExcelTickets(HttpPostedFileBase archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.ContentLength == 0)
            {
                return Json(new { success = false, message = "No se ha seleccionado un archivo." });
            }

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var errores = new List<RegistroExcelRetorno>();

                using (var package = new ExcelPackage(archivoExcel.InputStream))
                {
                    var ws = package.Workbook.Worksheets.First();

                    // Comprobamos que tenga al menos hasta la columna Y
                    if (ws.Dimension == null || ws.Dimension.End.Column < (int)Columnas.Y)
                    {
                        return Json(new
                        {
                            success = false,
                            excelErroneo = true,
                            message = "Formato incorrecto."
                        });
                    }

                    // Verificar que la cabecera de la columna X sea "Fecha de resolución"
                    var headerX = ws.Cells[1, (int)Columnas.X].Text?.Trim();
                    if (!string.Equals(headerX, "Fecha de resolución", StringComparison.OrdinalIgnoreCase))
                    {
                        return Json(new
                        {
                            success = false,
                            excelErroneo = true,
                            //message = $"La columna X debe llamarse 'Fecha de resolución' (encontrado: '{headerX ?? "(vacío)"}')."
                            message = $"Formato de plantilla incorrecto."
                        });
                    }

                    var dal = new DAL_Tickets();
                    var dalE = new DAL_EstadosTicket();
                    var dalT = new DAL_TiposTicket();
                    var dalO = new DAL_OrigenesTicket();
                    var dalV = new DAL_ValidacionesTicket();
                    var dalEnt = new DAL_Entes();

                    // Diccionarios para lookup (claves normalizadas)
                    var estadosDict = dalE.L(false, null)
                        .GroupBy(e => QuitarTildesYMayus(e.ETK_Nombre))
                        .Select(g => g.Last())
                        .ToDictionary(e => QuitarTildesYMayus(e.ETK_Nombre));

                    var tiposDict = dalT.L(false, null)
                        .GroupBy(t => QuitarTildesYMayus(t.TTK_Nombre))
                        .Select(g => g.Last())
                        .ToDictionary(t => QuitarTildesYMayus(t.TTK_Nombre));

                    var origenesDict = dalO.L(false, null)
                        .GroupBy(o => QuitarTildesYMayus(o.OTK_Nombre))
                        .Select(g => g.Last())
                        .ToDictionary(o => QuitarTildesYMayus(o.OTK_Nombre));

                    var validacionesDict = dalV.L(false, null)
                        .GroupBy(v => QuitarTildesYMayus(v.VTK_Nombre))
                        .Select(g => g.Last())
                        .ToDictionary(v => QuitarTildesYMayus(v.VTK_Nombre));

                    // OJO: Entes con NormalizarClave (quita tildes, comas y puntuación)
                    var entesDict = dalEnt.L(false, null)
                        .GroupBy(e => NormalizarClave(e.ENT_Nombre))
                        .Select(g => g.Last())
                        .ToDictionary(e => NormalizarClave(e.ENT_Nombre));

                    int fila = 2;
                    int erroresColumna = ws.Dimension.End.Column + 1;
                    ws.Cells[1, erroresColumna].Value = "Errores";

                    while (true)
                    {
                        var titulo = ws.Cells[fila, (int)Columnas.B].Text?.Trim();
                        if (string.IsNullOrEmpty(titulo))
                            break;

                        var sbErrores = new StringBuilder();

                        // Leemos las columnas necesarias
                        var id_GLPI = ws.Cells[fila, (int)Columnas.A].Text?.Trim().Replace(" ", "");
                        var estadoNombre = ws.Cells[fila, (int)Columnas.C].Text?.Trim();
                        var tipoNombre = ws.Cells[fila, (int)Columnas.D].Text?.Trim();
                        var fechaAperturaStr = ws.Cells[fila, (int)Columnas.E].Text?.Trim();
                        var enteNombre = ws.Cells[fila, (int)Columnas.H].Text?.Trim();
                        var grupoAsignado = ws.Cells[fila, (int)Columnas.I].Text?.Trim();
                        var categoriaExcel = ws.Cells[fila, (int)Columnas.J].Text?.Trim();
                        var descripcion = ws.Cells[fila, (int)Columnas.L].Text?.Trim();
                        var validacionNombre = ws.Cells[fila, (int)Columnas.N].Text?.Trim();
                        var duracionStr = ws.Cells[fila, (int)Columnas.P].Text?.Trim();
                        var ubicacion = ws.Cells[fila, (int)Columnas.Q].Text?.Trim();
                        var proveedorAsignado = ws.Cells[fila, (int)Columnas.R].Text?.Trim();
                        var grupoCargo = ws.Cells[fila, (int)Columnas.S].Text?.Trim();
                        var origenNombre = ws.Cells[fila, (int)Columnas.V].Text?.Trim();
                        var fechaResolucionStr = ws.Cells[fila, (int)Columnas.X].Text?.Trim();

                        estadosDict.TryGetValue(QuitarTildesYMayus(estadoNombre ?? ""), out var estado);
                        tiposDict.TryGetValue(QuitarTildesYMayus(tipoNombre ?? ""), out var tipo);

                        // Si el excel trae Origen en blanco: NO es error; si trae valor y no existe, SÍ es error
                        OrigenesTicket origen = null;
                        if (string.IsNullOrWhiteSpace(origenNombre))
                        {
                            origen = null; // permitido
                        }
                        else
                        {
                            origenesDict.TryGetValue(QuitarTildesYMayus(origenNombre), out origen);
                        }

                        validacionesDict.TryGetValue(QuitarTildesYMayus(validacionNombre ?? ""), out var validacion);

                        if (estado == null) sbErrores.AppendLine($"Estado no válido: {estadoNombre}");
                        if (tipo == null) sbErrores.AppendLine($"Tipo no válido: {tipoNombre}");
                        if (!string.IsNullOrWhiteSpace(origenNombre) && origen == null) sbErrores.AppendLine($"Origen no válido: {origenNombre}");
                        if (validacion == null) sbErrores.AppendLine($"Validación no válida: {validacionNombre}");

                        // --- Buscar ENTE con normalización que quita tildes y PUNTUACIÓN (comas, etc.) ---
                        Entes ente = null;
                        var candidatos = (enteNombre ?? string.Empty)
                            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(NormalizarClave)
                            .Where(s => s.Length > 0)
                            .Distinct();

                        foreach (var cand in candidatos)
                        {
                            if (entesDict.TryGetValue(cand, out ente))
                                break;
                        }

                        if (ente == null)
                            sbErrores.AppendLine($"Ente no encontrado en: {enteNombre}");

                        // Validar fechas
                        ValidarFecha(fechaAperturaStr, out DateTime fechaApertura, sbErrores, "apertura");
                        ValidarFecha(fechaResolucionStr, out DateTime fechaResolucion, sbErrores, "resolución");

                        // Validar duración
                        if (!int.TryParse(duracionStr, out int duracion) || duracion < 0)
                            sbErrores.AppendLine("Duración incorrecta");

                        // Si no hay errores, creamos y guardamos el ticket
                        if (sbErrores.Length == 0)
                        {
                            // ——— CÁLCULO DE TKC_CTK_Id (categoría) ———
                            int categoriaId;
                            var grp = (grupoAsignado ?? "").ToUpperInvariant();
                            bool esGuardia = string.Equals(origen?.OTK_Nombre, "Guardia", StringComparison.OrdinalIgnoreCase);

                            if (grp.Contains("SK-SOFTWARE")
                             || grp.Contains("EQUIPO UX/DISEÑO")
                             || grp.Contains("MIDDLEWARE/FUSE")
                             || grp.Contains("SOPORTE POWERBI"))
                            {
                                categoriaId = (int)CategoriaTicket.Software;
                            }
                            else if (esGuardia)
                            {
                                categoriaId = (int)CategoriaTicket.FueraDeAlcanceGuardia;
                            }
                            else if (
                                (string.Equals(validacion?.VTK_Nombre, "Concedido", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(validacion?.VTK_Nombre, "En espera de validación", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(validacion?.VTK_Nombre, "Rechazado", StringComparison.OrdinalIgnoreCase))
                              && !esGuardia
                            )
                            {
                                categoriaId = (int)CategoriaTicket.FueraDeAlcance;
                            }
                            else if (
                                string.Equals(validacion?.VTK_Nombre, "No está sujeto a Validación", StringComparison.OrdinalIgnoreCase)
                              && !esGuardia
                            )
                            {
                                categoriaId = (int)CategoriaTicket.DentroDeAlcance;
                            }
                            else
                            {
                                categoriaId = (int)CategoriaTicket.DentroDeAlcance;
                            }

                            var ticket = new Tickets
                            {
                                TKC_Titulo = titulo,
                                TKC_ETK_Id = estado.ETK_Id,
                                TKC_TTK_Id = tipo.TTK_Id,
                                TKC_FechaApertura = fechaApertura,
                                TKC_ENT_Id_Solicitante = ente.ENT_Id,
                                TKC_GrupoAsignado = grupoAsignado,
                                TKC_Categoria = categoriaExcel,
                                TKC_Descripcion = descripcion,
                                TKC_VTK_Id = validacion.VTK_Id,
                                TKC_Duracion = duracion,
                                TKC_Ubicacion = ubicacion,
                                TKC_ProveedorAsignado = proveedorAsignado,
                                TKC_GrupoCargo = grupoCargo,
                                // Permitir nulo si la columna lo acepta (TKC_OTK_Id nullable en DB/EF)
                                TKC_OTK_Id = origen?.OTK_Id,
                                TKC_FechaResolucion = fechaResolucion,
                                TKC_CTK_Id = categoriaId,
                                TKC_Id_GLPI = Convert.ToInt32(id_GLPI)
                            };

                            try
                            {
                                if (!dal.G(ticket, Sesion.SPersonaId))
                                    sbErrores.AppendLine(dal.MensajeErrorEspecifico ?? "Error al guardar el ticket.");
                            }
                            catch (Exception ex)
                            {
                                sbErrores.AppendLine(ex.Message);
                            }
                        }

                        // Escribimos la columna de errores / OK
                        ws.Cells[fila, erroresColumna].Value =
                            sbErrores.Length > 0 ? sbErrores.ToString() : "OK";

                        if (sbErrores.Length > 0)
                        {
                            errores.Add(new RegistroExcelRetorno
                            {
                                Tarea = titulo,
                                Empresa = grupoAsignado,
                                Elementos = categoriaExcel,
                                Errores = sbErrores.ToString()
                            });
                        }

                        fila++;
                    }

                    // Si hubo errores, devolvemos el Excel con la columna de errores
                    if (errores.Count > 0)
                    {
                        var rutaTemp = ConfigurationManager.AppSettings["PathTemporales"];
                        var nombreFich = $"Errores_Tickets_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        var path = Path.Combine(rutaTemp, nombreFich);

                        System.IO.File.WriteAllBytes(path, package.GetAsByteArray());
                        var url = Url.Action("DescargarErrores", "Portal", new { fileName = nombreFich }, Request.Url.Scheme);

                        return Json(new { success = false, erroresExcel = errores, fileUrl = url });
                    }

                    return Json(new { success = true, message = "Importación correcta" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message, excelErroneo = true });
            }
        }

        private static string QuitarTildesYMayus(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var normalized = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant().Trim();
        }

        // Quita tildes, convierte a MAYÚSCULAS y elimina signos de puntuación (comas, puntos, etc.)
        private static string NormalizarClave(string s)
        {
            var t = QuitarTildesYMayus(s);
            var sb = new StringBuilder(t.Length);
            foreach (var ch in t)
            {
                if (!char.IsPunctuation(ch)) sb.Append(ch);
            }
            // También colapsamos espacios múltiples en uno solo
            var sinPuntuacion = Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
            return sinPuntuacion;
        }

        string QuitarTildes(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;

            var normalized = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // Normaliza igual que para las claves del diccionario
        private static string Normalizar(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            return s.Replace(" ,", "")
                    .Replace(",", "")
                    .Replace(";", "")
                    .Replace("\t", " ")
                    .Trim()
                    .ToUpperInvariant();
        }

        public ActionResult DescargarPlantillaTickets()
        {
            string path = Server.MapPath("~/Plantillas/Plantilla_Tickets.xlsx");
            if (!System.IO.File.Exists(path))
                return HttpNotFound("Archivo no encontrado.");

            return File(path, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plantilla_Tickets.xlsx");
        }

        [HttpGet]
        public ActionResult ObtenerValidacionesTicket()
        {
            DAL_ValidacionesTicket dal = new DAL_ValidacionesTicket();
            var lista = dal.L(false, null)
                .Select(v => new
                {
                    v.VTK_Id,
                    v.VTK_Nombre
                })
                .ToList();

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarValidacionTicket(ValidacionesTicket validacion)
        {
            try
            {
                DAL_ValidacionesTicket dal = new DAL_ValidacionesTicket();
                bool ok = dal.G(validacion, Sesion.SPersonaId);

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarValidacionTicket(int idValidacion)
        {
            try
            {
                DAL_ValidacionesTicket dal = new DAL_ValidacionesTicket();
                dal.D(idValidacion);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerCategoriasTicket()
        {
            DAL_CategoriasTicket dal = new DAL_CategoriasTicket();
            var lista = dal.L(false, null)
                           .Select(c => new { c.CTK_Id, c.CTK_Nombre })
                           .ToList();

            return new LargeJsonResult
            {
                Data = lista,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
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
    }

    public enum CategoriaTicket
    {
        DentroDeAlcance = 1,
        FueraDeAlcance = 2,
        FueraDeAlcanceGuardia = 3,
        Software = 4,
        CosteGuardia = 5
    }
}