using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace PedidosSSCC.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Error(string message, string source, string path, string status)
        {
            ViewBag.ErrorMessage = (message ?? "Error desconocido.")
                            .Replace("\r", " ") // reemplaza retorno de carro
                            .Replace("\n", " "); // reemplaza salto de líne
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
            var archivos = Directory.GetFiles(logPath, "log_*.txt");

            // Obtener lista de fechas disponibles desde los nombres de archivo
            var fechasDisponibles = archivos
                .Select(path => Path.GetFileNameWithoutExtension(path).Replace("log_", ""))
                .OrderByDescending(f => f)
                .ToList();

            List<string> errores;

            if (string.IsNullOrEmpty(fecha))
            {
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

                errores = System.IO.File.Exists(archivoSeleccionado)
                    ? System.IO.File.ReadAllLines(archivoSeleccionado, Encoding.UTF8).Reverse().ToList()
                    : new List<string>();
            }

            ViewBag.Fechas = fechasDisponibles;
            ViewBag.FechaSeleccionada = fecha;

            log4net.Config.XmlConfigurator.Configure();

            return View(errores);
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