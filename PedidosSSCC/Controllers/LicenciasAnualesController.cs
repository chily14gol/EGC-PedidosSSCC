using AccesoDatos;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PedidosSSCC.Controllers
{
    public class LicenciasAnualesController : BaseController
    {
        public ActionResult LicenciasAnuales() => View();

        [HttpGet]
        public ActionResult ObtenerLicenciasAnuales()
        {
            var dal = new DAL_LicenciasAnuales();
            var list = dal.L(false, null)
                          .Select(l => new {
                              l.LAN_Id,
                              l.LAN_Nombre,
                              l.LAN_PRV_Id,
                              NombreProveedor = l.Proveedores != null ? l.Proveedores.PRV_Nombre : String.Empty,
                              NombreTarea = l.Tareas != null ? l.Tareas.TAR_Nombre : String.Empty
                          })
                          .ToList();
            return new LargeJsonResult
            {
                Data = list,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpPost]
        public JsonResult GuardarLicenciaAnual(LicenciasAnuales dto)
        {
            try
            {
                var dal = new DAL_LicenciasAnuales();
                bool ok = dal.G(dto, Sesion.SPersonaId);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarLicenciaAnual(int idLicencia)
        {
            try
            {
                var dal = new DAL_LicenciasAnuales();
                dal.Eliminar(idLicencia);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerTiposEntePorLicenciaAnual(int idLicencia)
        {
            var dalTipos = new DAL_TiposEnte();
            var dalAsociaciones = new DAL_LicenciasAnuales_TiposEnte();

            // Obtener todos los tipos de ente
            var todos = dalTipos.L(false, null)
                .Select(t => new
                {
                    id = t.TEN_Id,
                    text = t.TEN_Nombre
                }).ToList();

            // Obtener IDs ya asociados a esta licencia
            var asociados = dalAsociaciones.L(false, null)
                .Where(l => l.LAT_LAN_Id == idLicencia)
                .Select(l => l.LAT_TEN_Id)
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

        // ——————————————
        // Licencias Anuales → Entidades
        // ——————————————
        [HttpGet]
        public ActionResult ObtenerEntesPorLicenciaAnual(int idContrato)
        {
            var dal = new DAL_Entes_LicenciasAnuales();
            var lista = dal.L(false, null)
                           .Where(e => e.ELA_CLA_Id == idContrato)
                           .Select(e => new {
                               e.ELA_ENT_Id,
                               e.ELA_LAN_Id,
                               e.ELA_CLA_Id,
                               e.ELA_FechaInicio,
                               e.ELA_FechaFin,
                               ELA_Facturada = false, //AQUI
                               NombreEntidad = e.Entes != null ? e.Entes.ENT_Nombre : "",
                               NombreLicencia = e.LicenciasAnuales != null ? e.LicenciasAnuales.LAN_Nombre : ""
                           })
                           .ToList();

            return new LargeJsonResult
            {
                Data = lista,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpPost]
        public JsonResult GuardarEnteLicenciaAnual(Entes_LicenciasAnuales dto)
        {
            var dal = new DAL_Entes_LicenciasAnuales();
            string mensaje;
            bool ok = dal.GuardarEnte(dto, out mensaje);
            return Json(new { success = ok, message = mensaje });
        }

        [HttpPost]
        public JsonResult EliminarEnteLicenciaAnual(int idEntidad, int idLicenciaAnual, int idContrato)
        {
            try
            {
                var dal = new DAL_Entes_LicenciasAnuales();
                string pk = $"{idEntidad}|{idLicenciaAnual}|{idContrato}";
                bool ok = dal.D(pk);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
