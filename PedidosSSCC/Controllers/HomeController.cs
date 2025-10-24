using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using PedidosSSCC.ViewModels;

namespace PedidosSSCC.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Error(string message, string source, string path, string status)
        {
            ViewBag.ErrorMessage = (message ?? "Error desconocido.")
                            .Replace("\r", " ") // reemplaza retorno de carro
                            .Replace("\n", " "); // reemplaza salto de línea
            ViewBag.ErrorSource = source ?? "No disponible.";
            ViewBag.ErrorPath = path ?? "Ruta no disponible.";
            ViewBag.Status = status ?? "Status no disponible.";

            return View();
        }

        public ActionResult SesionExpirada()
        {
            return View();
        }

        public ActionResult UsuarioNoLogin()
        {
            return View();
        }

        public ActionResult PaginaNoEncontrada()
        {
            return View();
        }

        public ActionResult SinPermiso()
        {
            return View();
        }

        public ActionResult VerErrores(string fecha = null)
        {
            log4net.LogManager.Shutdown();

            string logPath = Server.MapPath("~/Logs/");
            var errores = new List<LogEntryViewModel>();
            var fechasDisponibles = new List<string>();
            bool lecturaCompletada = false;
            var errores = new List<string>();
            var fechasDisponibles = new List<string>();

            try
            {
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }

                var archivos = Directory.GetFiles(logPath, "log_*.txt")
                    .OrderByDescending(path => Path.GetFileNameWithoutExtension(path))
                    .ToList();
                var archivos = Directory.GetFiles(logPath, "log_*.txt");

                // Obtener lista de fechas disponibles desde los nombres de archivo
                fechasDisponibles = archivos
                    .Select(path => Path.GetFileNameWithoutExtension(path).Replace("log_", ""))
                    .OrderByDescending(f => f)
                    .ToList();

                if (string.IsNullOrEmpty(fecha))
                {
                    // Si no se ha seleccionado fecha, mostrar todos los registros
                    foreach (var archivo in archivos)
                    {
                        AgregarEntradasDesdeArchivo(archivo, errores);
                    }
                    // Si no se ha seleccionado fecha, mostrar todos
                    errores = archivos
                        .SelectMany(path => System.IO.File.ReadAllLines(path, Encoding.UTF8))
                        .Reverse()
                        .ToList();
                }
                else
                {
                    // Mostrar solo el archivo de la fecha seleccionada
                    string archivoSeleccionado = Path.Combine(logPath, $"log_{fecha}.txt");

                    if (System.IO.File.Exists(archivoSeleccionado))
                    {
                        AgregarEntradasDesdeArchivo(archivoSeleccionado, errores);
                    }
                }

                lecturaCompletada = true;
            }
            catch (UnauthorizedAccessException ex)
            {
                log.Error("No se pudo acceder a la carpeta de logs.", ex);
                ViewBag.ErrorLecturaLogs = "No se pudo acceder a la carpeta de registros por permisos insuficientes.";
            }
            catch (IOException ex)
            {
                log.Error("Error al leer los archivos de log.", ex);
                ViewBag.ErrorLecturaLogs = "Ocurrió un problema al leer los archivos de registro.";
            }

            if (lecturaCompletada && errores.Count == 0)
            {
                ViewBag.SinLogs = true;
                    errores = System.IO.File.Exists(archivoSeleccionado)
                        ? System.IO.File.ReadAllLines(archivoSeleccionado, Encoding.UTF8).Reverse().ToList()
                        : new List<string>();
                }

                if (errores.Count == 0)
                {
                    ViewBag.SinLogs = true;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                log.Error("No se pudo acceder a la carpeta de logs.", ex);
                ViewBag.ErrorLecturaLogs = "No se pudo acceder a la carpeta de registros por permisos insuficientes.";
            }
            catch (IOException ex)
            {
                log.Error("Error al leer los archivos de log.", ex);
                ViewBag.ErrorLecturaLogs = "Ocurrió un problema al leer los archivos de registro.";
            }

            ViewBag.Fechas = fechasDisponibles;
            ViewBag.FechaSeleccionada = fecha;

            log4net.Config.XmlConfigurator.Configure();

            return View(errores);
        }

        private static void AgregarEntradasDesdeArchivo(string rutaArchivo, List<LogEntryViewModel> acumulador)
        {
            var lineas = System.IO.File.ReadAllLines(rutaArchivo, Encoding.UTF8);
            Array.Reverse(lineas);

            foreach (var linea in lineas)
            {
                var entrada = ParsearLineaLog(linea);
                if (entrada != null)
                {
                    acumulador.Add(entrada);
                }
            }
        }

        private static LogEntryViewModel ParsearLineaLog(string linea)
        {
            if (string.IsNullOrWhiteSpace(linea))
            {
                return null;
            }

            var partesCabecera = linea.Split(new[] { " | " }, 3, StringSplitOptions.None);
            if (partesCabecera.Length < 3)
            {
                return null;
            }

            string nivel = partesCabecera[0].Trim().ToUpperInvariant();
            string fechaTexto = partesCabecera[1].Trim();
            string restoMensaje = partesCabecera[2].Trim();

            DateTime fechaParseada;
            DateTime? fecha = null;
            if (DateTime.TryParseExact(fechaTexto, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaParseada) ||
                DateTime.TryParse(fechaTexto, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out fechaParseada) ||
                DateTime.TryParse(fechaTexto, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out fechaParseada))
            {
                fecha = fechaParseada;
            }

            string url = "-";
            string mensajeCabecera = restoMensaje;

            int indiceSeparador = restoMensaje.IndexOf(" | ", StringComparison.Ordinal);
            if (indiceSeparador >= 0)
            {
                string posibleUrl = restoMensaje.Substring(0, indiceSeparador).Trim();
                string resto = restoMensaje.Substring(indiceSeparador + 3).Trim();

                if (!string.IsNullOrEmpty(resto) && EsValorUrl(posibleUrl))
                {
                    url = string.IsNullOrWhiteSpace(posibleUrl) ? "-" : posibleUrl;
                    mensajeCabecera = resto;
                }
            }

            if (string.IsNullOrWhiteSpace(mensajeCabecera))
            {
                mensajeCabecera = restoMensaje;
            }

            string mensaje = WebUtility.HtmlDecode(mensajeCabecera);

            return new LogEntryViewModel
            {
                Level = nivel,
                Timestamp = fecha,
                RawTimestamp = fechaTexto,
                Url = string.IsNullOrWhiteSpace(url) ? "-" : url,
                Message = mensaje,
                FullMessage = mensaje,
                OriginalLine = linea
            };
        }

        private static bool EsValorUrl(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return false;
            }

            if (valor == "-")
            {
                return true;
            }

            return Uri.TryCreate(valor, UriKind.Absolute, out _) || valor.StartsWith("/", StringComparison.Ordinal);
        }

        [HttpPost]
        public ActionResult RegistrarError(string mensajeError, string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                    url = "-";

                string errorDetalle = $"{url} | {mensajeError}";
                log.Error(errorDetalle);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                log.Fatal("Error al registrar el log en servidor", ex);
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
