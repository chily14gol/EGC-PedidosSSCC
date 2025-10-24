using AccesoDatos;
using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using static PedidosSSCC.Comun.Constantes;

namespace PedidosSSCC.Controllers
{
    public class ContratosCAUController : BaseController
    {
        public ActionResult ContratosCAU()
        {
            return View();
        }

        [HttpGet]
        public ActionResult ObtenerContratosCAU()
        {
            DAL_ContratosCAU dal = new DAL_ContratosCAU();
            var lista = dal.L(false, null);

            var listaFiltro = lista.Select(i => new
            {
                i.CCA_Id,
                i.CCA_FechaInicio,
                i.CCA_FechaFin,
                i.CCA_CosteHoraF,
                i.CCA_CosteHoraD,
                i.CCA_CosteHoraG,
                i.CCA_CosteHoraS,
                i.CCA_PrecioGuardia,
                i.CCA_TAR_Id_F,
                i.CCA_TAR_Id_D,
                i.CCA_TAR_Id_G,
                i.CCA_TAR_Id_S
            }).ToList();

            return new LargeJsonResult { Data = listaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarContratoCAU(ContratosCAU contrato)
        {
            try
            {
                DAL_ContratosCAU dal = new DAL_ContratosCAU();
                bool ok = dal.G(contrato, Sesion.SPersonaId);

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarContratoCAU(int idContrato)
        {
            try
            {
                DAL_ContratosCAU dal = new DAL_ContratosCAU();
                dal.D(idContrato);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Determina si una empresa es “auxiliar” (no debe generar guardia fija),
        /// según patrón de nombre. Sustituir por flag de BD si existe.
        /// </summary>
        private static bool EsAuxiliar(Empresas e)
        {
            var n = (e?.EMP_Nombre ?? string.Empty).ToUpperInvariant();
            return n.Contains(" - ") || n.StartsWith("E.LOGISTICS-") || n.StartsWith("ERHARDT LOGISTICS");
        }

        private List<(int empId, int tarId, int anyo, int mes, int tkcId, decimal importe, CategoriaTicket cat, string nombreCat)>
            CalcularDetallesTickets(DateTime periodo, bool usarFechaTicket)
        {
            var daoDetickets = new DAL_Tareas_Empresas_LineasEsfuerzo_Tickets();
            var ticketsYaFacturadosIds = daoDetickets.L(true, null)
                .Select(d => d.TCT_TKC_Id)
                .ToHashSet();

            var todosTickets = new DAL_Tickets().L(false, null);
            var ticketsPendientes = todosTickets
                .Where(t => !ticketsYaFacturadosIds.Contains(t.TKC_Id))
                .ToList();

            var entesDict = new DAL_Entes().L(false, null).ToDictionary(e => e.ENT_Id);
            var empDict = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id);
            var tareasDict = new DAL_Tareas().L(false, null).ToDictionary(t => t.TAR_Id, t => t.TAR_Nombre);
            var conceptosExist = new DAL_Tareas_Empresas_LineasEsfuerzo().L(true, null).ToList();

            var contratoCAU = new DAL_ContratosCAU()
                .L(false, null)
                .FirstOrDefault(c => c.CCA_FechaInicio <= periodo && c.CCA_FechaFin >= periodo)
                ?? throw new Exception("No hay contrato CAU vigente");

            var teaSet = new DAL_Tareas_Empresas()
                .L(false, null)
                .Select(x => (empId: x.TEM_EMP_Id, tarId: x.TEM_TAR_Id, anyo: x.TEM_Anyo))
                .ToHashSet();

            var detalles = new List<(int empId, int tarId, int anyo, int mes, int tkcId, decimal importe, CategoriaTicket cat, string nombreCat)>();

            // Acumulador de errores bloqueantes de validación
            var erroresBloqueantes = new List<string>();
            void ComprobarAsignacionTareaEmpresaAnyo(int empId, int tarId, int anyo)
            {
                if (!teaSet.Contains((empId, tarId, anyo)))
                {
                    var empNom = empDict.TryGetValue(empId, out var emp) ? emp.EMP_Nombre : $"EMP:{empId}";
                    var tarNom = tareasDict.TryGetValue(tarId, out var tnom) ? tnom : $"TAR:{tarId}";
                    erroresBloqueantes.Add(
                        $"Sin asignación de tarea “{tarNom}” para la empresa “{empNom}” en {periodo.Year}.");
                }
            }

            foreach (var t in ticketsPendientes)
            {
                if (!entesDict.TryGetValue(t.TKC_ENT_Id_Solicitante, out var ente) || !ente.ENT_EMP_Id.HasValue)
                    continue;

                if (ente.ENT_EMP_Id.Value == (int)EmpresaExcluyenteConceptos.EGC)
                    continue;

                int empId = ente.ENT_EMP_Id.Value;
                int anyo = usarFechaTicket ? t.TKC_FechaApertura.Year : periodo.Year;
                int mes = usarFechaTicket ? t.TKC_FechaApertura.Month : periodo.Month;

                int tarId;
                decimal costeHora;
                CategoriaTicket cat;
                string nombreCat;

                switch ((CategoriaTicket)t.TKC_CTK_Id)
                {
                    case CategoriaTicket.DentroDeAlcance:
                        tarId = contratoCAU.CCA_TAR_Id_D;
                        costeHora = contratoCAU.CCA_CosteHoraD;
                        cat = CategoriaTicket.DentroDeAlcance;
                        nombreCat = "Dentro de alcance";
                        break;

                    case CategoriaTicket.FueraDeAlcance:
                        tarId = contratoCAU.CCA_TAR_Id_F;
                        costeHora = contratoCAU.CCA_CosteHoraF;
                        cat = CategoriaTicket.FueraDeAlcance;
                        nombreCat = "Fuera de alcance";
                        break;

                    case CategoriaTicket.Software:
                        tarId = contratoCAU.CCA_TAR_Id_S;
                        costeHora = contratoCAU.CCA_CosteHoraS;
                        cat = CategoriaTicket.Software;
                        nombreCat = "Software";
                        break;

                    case CategoriaTicket.FueraDeAlcanceGuardia:
                        tarId = contratoCAU.CCA_TAR_Id_G;
                        costeHora = contratoCAU.CCA_CosteHoraG;
                        cat = CategoriaTicket.FueraDeAlcanceGuardia;
                        nombreCat = "Fuera de alcance - Guardia";
                        break;

                    default:
                        continue;
                }

                if (tarId <= 0) continue;

                ComprobarAsignacionTareaEmpresaAnyo(empId, tarId, anyo);

                decimal horas = t.TKC_Duracion / 60m;
                decimal importe = Math.Round(horas * costeHora, 2);

                detalles.Add((empId, tarId, anyo, mes, t.TKC_Id, importe, cat, nombreCat));
            }

            decimal precioGuardia = contratoCAU.CCA_PrecioGuardia;
            if (precioGuardia > 0 && contratoCAU.CCA_TAR_Id_G > 0)
            {
                // Empresas que ya tienen concepto guardia generado en este mes/año
                var conceptosGuardiaExist = conceptosExist
                    .Where(c => c.TLE_TAR_Id == contratoCAU.CCA_TAR_Id_G &&
                                c.TLE_Anyo == periodo.Year &&
                                c.TLE_Mes == periodo.Month)
                    .Select(c => c.TLE_EMP_Id)
                    .ToHashSet();

                // Cargar empresas excluidas para este contrato
                var excluidasGuardia = new DAL_ContratosCAU_ExcluidasGuardia()
                    .L(false, null)
                    .Where(x => x.CEE_CCA_Id == contratoCAU.CCA_Id)
                    .Select(x => x.CEE_EMP_Id)
                    .ToHashSet();

                // Empresas destino = todas menos auxiliares y excluidas
                var empresasDestino = empDict.Values
                    .Where(e => !EsAuxiliar(e) && !excluidasGuardia.Contains(e.EMP_Id))
                    .ToList();

                foreach (var empresa in empresasDestino)
                {
                    if (conceptosGuardiaExist.Contains(empresa.EMP_Id)) continue;

                    // Validación bloqueante TEA para Guardia (usa el año del periodo)
                    ComprobarAsignacionTareaEmpresaAnyo(empresa.EMP_Id, contratoCAU.CCA_TAR_Id_G, periodo.Year);

                    detalles.Add((
                        empId: empresa.EMP_Id,
                        tarId: contratoCAU.CCA_TAR_Id_G,
                        anyo: periodo.Year,
                        mes: periodo.Month,
                        tkcId: 0,
                        importe: Math.Round(precioGuardia, 2),
                        cat: CategoriaTicket.CosteGuardia,
                        nombreCat: "Coste Guardia"
                    ));
                }
            }

            if (erroresBloqueantes.Count > 0)
            {
                throw new Exception("Errores bloqueantes:\n" + string.Join("\n", erroresBloqueantes.Distinct()));
            }

            return detalles;
        }

        [HttpPost]
        public JsonResult PrevisualizarConceptos()
        {
            try
            {
                var dalConfig = new DAL_Configuraciones();
                var objConfig = dalConfig.L_PrimaryKey((int)Constantes.Configuracion.AnioConcepto);
                var hoy = DateTime.Today;
                int anioConcepto = objConfig != null ? Convert.ToInt32(objConfig.CFG_Valor) : hoy.Year;
                int mesConcepto = anioConcepto < hoy.Year ? 12 : hoy.Month;
                var periodo = new DateTime(anioConcepto, mesConcepto, 1);
                var empDict = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id, e => e.EMP_Nombre);

                var detalles = CalcularDetallesTickets(periodo, usarFechaTicket: false);

                var preview = detalles
                    .GroupBy(d => new { d.cat, d.nombreCat })
                    .Select(g => new SoportePreviewDto
                    {
                        CategoriaNombre = g.Key.nombreCat,
                        Filas = g.GroupBy(x => new { x.empId, x.tarId, x.anyo, x.mes })
                            .Select(empGrp => new ConceptoPreviewRow
                            {
                                EmpresaId = empGrp.Key.empId,
                                EmpresaNombre = empDict[empGrp.Key.empId],
                                TAR_Id = empGrp.Key.tarId,
                                TAR_Nombre = g.Key.nombreCat,
                                Anyo = empGrp.Key.anyo,
                                Mes = empGrp.Key.mes,
                                ImporteTotal = empGrp.Sum(x => x.importe),
                                LicenciasIncluidas = $"Tickets: {string.Join(",", empGrp.Select(x => x.tkcId))}"
                            })
                            .ToList()
                    })
                    .ToList();

                return Json(new { success = true, soporte = preview, soporteErrors = new List<string>() });
            }
            catch (Exception ex)
            {
                var errores = (ex.Message ?? "Error desconocido")
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                return Json(new
                {
                    success = true,
                    soporte = new List<SoportePreviewDto>(),
                    soporteErrors = new List<string>(), 
                    soporteBlockingErrors = errores
                });
            }
        }

        [HttpPost]
        public JsonResult GenerarTodosConceptos()
        {
            try
            {
                var dalConfig = new DAL_Configuraciones();
                var objConfig = dalConfig.L_PrimaryKey((int)Constantes.Configuracion.AnioConcepto);
                var hoy = DateTime.Today;
                int anioConcepto = objConfig != null ? Convert.ToInt32(objConfig.CFG_Valor) : hoy.Year;
                int mesConcepto = anioConcepto < hoy.Year ? 12 : hoy.Month;
                var periodo = new DateTime(anioConcepto, mesConcepto, 1);

                var empDict = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id);
                var daoConceptos = new DAL_Tareas_Empresas_LineasEsfuerzo();
                var daoDetickets = new DAL_Tareas_Empresas_LineasEsfuerzo_Tickets();
                var tareasDict = new DAL_Tareas().L(false, null).ToDictionary(t => t.TAR_Id, t => t.TAR_Nombre);

                var detalles = CalcularDetallesTickets(periodo, usarFechaTicket: false);

                var resumen = new Dictionary<int, int>();

                foreach (var grupo in detalles.GroupBy(d => new { d.empId, d.tarId, d.anyo, d.mes }))
                {
                    int empId = grupo.Key.empId;
                    decimal importeTotal = grupo.Sum(x => x.importe);
                    var nombreTarea = tareasDict.TryGetValue(grupo.Key.tarId, out var n) ? n : $"TAR {grupo.Key.tarId}";

                    // 🔹 Concepto único por grupo
                    var cab = new Tareas_Empresas_LineasEsfuerzo
                    {
                        TLE_EMP_Id = empId,
                        TLE_TAR_Id = grupo.Key.tarId,
                        TLE_Anyo = grupo.Key.anyo,
                        TLE_Mes = grupo.Key.mes,
                        TLE_Cantidad = importeTotal,
                        TLE_Descripcion = $"Generado Auto {nombreTarea} {grupo.Key.anyo}-{grupo.Key.mes}",
                        TLE_ESO_Id = (int)Constantes.EstadosSolicitud.PendienteAprobacion,
                        TLE_ESO_Id_Original = (int)Constantes.EstadosSolicitud.SinSolicitar,
                        TLE_PER_Id_Aprobador = empDict[empId].EMP_PER_Id_AprobadorDefault,
                        FechaAlta = DateTime.Now,
                        FechaModificacion = DateTime.Now,
                        PER_Id_Modificacion = Sesion.SPersonaId,
                        TLE_Inversion = false
                    };
                    daoConceptos.G(cab, Sesion.SPersonaId);

                    // 🔹 Todos los tickets de ese grupo se asocian al mismo concepto
                    foreach (var det in grupo)
                    {
                        var linea = new Tareas_Empresas_LineasEsfuerzo_Tickets
                        {
                            TCT_TLE_Id = cab.TLE_Id,   // mismo concepto
                            TCT_TKC_Id = det.tkcId,
                            TCT_Importe = det.importe
                        };
                        daoDetickets.G(linea, Sesion.SPersonaId);
                    }

                    // 🔹 Aquí 1 concepto por grupo, aunque tenga 20 tickets dentro
                    resumen[empId] = resumen.TryGetValue(empId, out var val) ? val + 1 : 1;
                }

                LimpiarCache(TipoCache.Tareas, TipoCache.Conceptos, TipoCache.Pedidos);

                return Json(new
                {
                    success = true,
                    soporte = resumen.Select(r => new GenerarResultDto
                    {
                        EmpresaId = r.Key,
                        EmpresaNombre = empDict[r.Key].EMP_Nombre,
                        ConceptosCreados = r.Value
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, errors = new[] { "Error generando conceptos: " + ex.Message } });
            }
        }

        [HttpGet]
        public ActionResult ObtenerEmpresasCombo()
        {
            var dalEmp = new DAL_Empresas();
            // Si tienes flag de activas, filtra aquí. Ej: .Where(e => e.EMP_Activo)
            var lista = dalEmp.L(false, null)
                .Select(e => new { e.EMP_Id, e.EMP_Nombre })
                .OrderBy(e => e.EMP_Nombre)
                .ToList();

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerExcluidasGuardia(int ccaId)
        {
            var dal = new DAL_ContratosCAU_ExcluidasGuardia();
            var dalEmp = new DAL_Empresas();

            var excl = dal.L(false, null)
                .Where(x => x.CEE_CCA_Id == ccaId)
                .ToList();

            // join para nombre empresa
            var empDict = dalEmp.L(false, null).ToDictionary(e => e.EMP_Id, e => e.EMP_Nombre);

            var data = excl.Select(x => new
            {
                x.CEE_CCA_Id,
                EMP_Id = x.CEE_EMP_Id,
                EMP_Nombre = empDict.TryGetValue(x.CEE_EMP_Id.Value, out var nom) ? nom : $"EMP {x.CEE_EMP_Id}"
            }).OrderBy(x => x.EMP_Nombre).ToList();

            return new LargeJsonResult { Data = data, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        public class ExcluidaDto { public int ccaId { get; set; } public int empId { get; set; } }

        [HttpPost]
        public JsonResult AgregarExcluidaGuardia(ExcluidaDto dto)
        {
            try
            {
                if (dto == null || dto.ccaId <= 0 || dto.empId <= 0)
                    return Json(new { success = false, message = "Datos inválidos." });

                var dal = new DAL_ContratosCAU_ExcluidasGuardia();
                var ok = dal.G(new ContratosCAU_ExcluidasGuardia
                {
                    CEE_CCA_Id = dto.ccaId,
                    CEE_EMP_Id = dto.empId
                }, Sesion.SPersonaId);

                if (!ok)
                    return Json(new { success = false, message = "La empresa ya está excluida para este contrato." });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarExcluidaGuardia(ExcluidaDto dto)
        {
            try
            {
                if (dto == null || dto.ccaId <= 0 || dto.empId <= 0)
                    return Json(new { success = false, message = "Datos inválidos." });

                var dal = new DAL_ContratosCAU_ExcluidasGuardia();
                var ok = dal.Eliminar(new ContratosCAU_ExcluidasGuardia
                {
                    CEE_CCA_Id = dto.ccaId,
                    CEE_EMP_Id = dto.empId
                });

                if (!ok)
                    return Json(new { success = false, message = "No se encontró el registro a eliminar." });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}