using AccesoDatos;
using PedidosSSCC.Comun;
using System;
using System.Linq;
using System.Web.Mvc;
using static PedidosSSCC.Comun.Constantes;

namespace PedidosSSCC.Controllers
{
    public class AplicacionesModulosController : BaseController
    {
        public ActionResult AplicacionesModulos() => View();

        [HttpGet]
        public JsonResult ObtenerModulos()
        {
            var dal = new DAL_Aplicaciones_Modulos();
            var lst = dal.L(false, null)
                         .Select(m => new {
                             m.APM_Id,
                             m.APM_APP_Id,
                             m.APM_Nombre,
                             NombreAplicacion = m.Aplicaciones != null ? m.Aplicaciones.APP_Nombre : ""
                         }).ToList();
            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GuardarModulo(Aplicaciones_Modulos dto)
        {
            try
            {
                var dal = new DAL_Aplicaciones_Modulos();
                bool ok = dal.G(dto, Sesion.SPersonaId);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarModulo(int id)
        {
            try
            {
                var dalEmp = new DAL_Aplicaciones_Modulos_Empresas();
                var lstEmp = dalEmp.L(false, null).Where(e => e.AME_APM_Id == id).ToList();
                foreach (Aplicaciones_Modulos_Empresas objEmp in lstEmp)
                {
                    dalEmp.D(objEmp.AME_Id);
                }
                
                var dalTarifas = new DAL_Aplicaciones_Modulos_Tarifas();
                var lstTarifas = dalTarifas.L(false, null).Where(t => t.AMT_APM_Id == id).ToList();
                foreach (Aplicaciones_Modulos_Tarifas objTarifa in lstTarifas)
                {
                    dalTarifas.D(objTarifa.AMT_Id);
                }

                var dal = new DAL_Aplicaciones_Modulos();
                dal.D(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // — Empresas por Módulo —
        [HttpGet]
        public JsonResult ObtenerModulosEmpresas(int idModulo)
        {
            var dal = new DAL_Aplicaciones_Modulos_Empresas();
            var lst = dal.L(false, null)
                         .Where(e => e.AME_APM_Id == idModulo)
                         .Select(e => new {
                             e.AME_Id,
                             e.AME_APM_Id,
                             e.AME_EMP_Id,
                             e.AME_FechaInicio,
                             e.AME_FechaFin,
                             e.AME_ImporteMensual,
                             e.AME_PorcentajeReparto,
                             e.AME_DescripcionConcepto,
                             EmpresaNombre = e.Empresas.EMP_Nombre
                         }).ToList();
            return new LargeJsonResult { Data = lst, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarModuloEmpresa(Aplicaciones_Modulos_Empresas dto)
        {
            try
            {
                var dal = new DAL_Aplicaciones_Modulos_Empresas();
                string mensaje;
                bool ok = dal.GuardarModuloEmpresa(dto, out mensaje);
                return Json(new { success = ok, message = mensaje });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarModuloEmpresa(int id)
        {
            try
            {
                var dal = new DAL_Aplicaciones_Modulos_Empresas();
                dal.D(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // — Tarifas por Módulo —
        [HttpGet]
        public JsonResult ObtenerModulosTarifas(int idModulo)
        {
            var dal = new DAL_Aplicaciones_Modulos_Tarifas();
            var lst = dal.L(false, null)
                         .Where(t => t.AMT_APM_Id == idModulo)
                         .Select(t => new {
                             t.AMT_Id,
                             t.AMT_APM_Id,
                             t.AMT_FechaInicio,
                             t.AMT_FechaFin,
                             t.AMT_ImporteMensualReparto,
                             t.AMT_ImporteMensualRepartoPorcentajes
                         }).ToList();
            return new LargeJsonResult { Data = lst, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarModuloTarifa(Aplicaciones_Modulos_Tarifas dto)
        {
            try
            {
                var dal = new DAL_Aplicaciones_Modulos_Tarifas();
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
        public JsonResult EliminarModuloTarifa(int id)
        {
            try
            {
                var dal = new DAL_Aplicaciones_Modulos_Tarifas();
                dal.D(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
