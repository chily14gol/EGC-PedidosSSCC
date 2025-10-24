using AccesoDatos;
using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using static PedidosSSCC.Comun.Constantes;

namespace PedidosSSCC.Controllers
{
    public class ProyectosController : BaseController
    {
        private readonly DAL_Personas _dalPersonas = new DAL_Personas();
        private readonly DAL_Departamentos _dalDepartamentos = new DAL_Departamentos();
        private readonly DAL_Proyectos _dalProyecto = new DAL_Proyectos();
        private readonly DAL_PeriodosPartes _dalPeriodos = new DAL_PeriodosPartes();
        private readonly DAL_Proyectos _dalProyectos = new DAL_Proyectos();
        private readonly DAL_Proyectos_Departamentos _dalProjDeps = new DAL_Proyectos_Departamentos();
        private readonly DAL_Proyectos_Partes _dalPartes = new DAL_Proyectos_Partes();
        private readonly DAL_Proyectos_Partes_Horas _dalHorasParte = new DAL_Proyectos_Partes_Horas();

        public ActionResult Proyectos() => View();

        public ActionResult Personas() => View();

        public ActionResult ParteHoras()
        {
            var personaId = Sesion.SPersonaId;
            var deptos = _dalDepartamentos.L(false, d => d.DEP_PER_Id_Responsable == personaId).ToList();

            bool isResponsable = deptos.Any();
            ViewBag.IsResponsable = isResponsable;
            ViewBag.IdPersona = personaId;
            ViewBag.DepartamentosResponsable = deptos.Select(d => d.DEP_Id).ToList();
            ViewBag.DepartamentosResponsableNombre = deptos.Select(d => d.DEP_Nombre).ToList();

            return View();
        }

        [HttpGet]
        public JsonResult GetProyectos()
        {
            var lista = _dalProyecto
                .ObtenerTodos()
                .Select(p => new
                {
                    p.PRY_Id,
                    p.PRY_Nombre,
                    p.PRY_TAR_Id,
                    p.TareaNombre,
                    p.PRY_Imputable,
                    p.PRY_Activo
                })
                .OrderBy(p => p.PRY_Nombre)
                .ToList();

            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetDepartamentos()
        {
            DAL_Departamentos _dalDepartamentos = new DAL_Departamentos();
            var lista = _dalDepartamentos
                .L(false, null)
                .Select(d => new { d.DEP_Id, d.DEP_Nombre })
                .OrderBy(d => d.DEP_Nombre)
                .ToList();

            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetEmpresas()
        {
            DAL_Empresas _dalEmpresas = new DAL_Empresas();
            var lista = _dalEmpresas
                .L(false, null)
                .Select(e => new { e.EMP_Id, e.EMP_Nombre })
                .OrderBy(e => e.EMP_Nombre)
                .ToList();

            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetProyecto(int id)
        {
            DAL_Proyectos_Departamentos _dalProjDeps = new DAL_Proyectos_Departamentos();
            DAL_Proyectos_Empresas _dalProjEmps = new DAL_Proyectos_Empresas();

            // 1) Obtener la entidad Proyectos
            var p = _dalProyecto.ObtenerPorId(id);
            if (p == null)
            {
                return Json(new { success = false, message = "Proyecto no encontrado" }, JsonRequestBehavior.AllowGet);
            }

            // 2) Mapear campos principales
            var dto = new ProyectoDto
            {
                PRY_Id = p.PRY_Id,
                PRY_Nombre = p.PRY_Nombre,
                PRY_TAR_Id = p.PRY_TAR_Id,
                PRY_Imputable = p.PRY_Imputable,
                PRY_Activo = p.PRY_Activo,

                // 3) Departamentos asignados
                Departamentos = _dalProjDeps.ObtenerDepartamentosPorProyecto(id)
                                   .Select(x => x.PRD_DEP_Id)
                                   .ToList(),

                // 4) Empresas asignadas
                Empresas = _dalProjEmps.ObtenerEmpresasPorProyecto(id)
                              .Select(x => new Proyectos_Empresas
                              {
                                  PRE_PRY_Id = x.PRE_PRY_Id,
                                  PRE_EMP_Id = x.PRE_EMP_Id,
                                  PRE_Porcentaje = x.PRE_Porcentaje
                              })
                              .ToList()
            };

            return Json(new { success = true, proyecto = dto }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveProyecto(ProyectoDto dto)
        {
            DAL_Proyectos_Departamentos _dalProjDeps = new DAL_Proyectos_Departamentos();
            DAL_Proyectos_Empresas _dalProjEmps = new DAL_Proyectos_Empresas();

            bool okMain = false;

            // 1) Convertir DTO a entidad Proyectos (para crear / actualizar)
            var entidad = new Proyectos
            {
                PRY_Id = dto.PRY_Id,
                PRY_Nombre = dto.PRY_Nombre,
                PRY_TAR_Id = dto.PRY_TAR_Id,
                PRY_Imputable = dto.PRY_Imputable,
                PRY_Activo = dto.PRY_Activo
            };

            // 2) Crear o actualizar registro principal
            if (dto.PRY_Id == 0)
            {
                okMain = _dalProyecto.Crear(entidad, Sesion.SPersonaId);
            }
            else
            {
                okMain = _dalProyecto.Actualizar(entidad, Sesion.SPersonaId);
            }

            if (!okMain)
            {
                return Json(new { success = false, message = "No se pudo guardar el proyecto principal." });
            }

            // 3) Si se creó (dto.PRY_Id==0), recuperar el ID recién generado
            if (dto.PRY_Id == 0)
            {
                // Linq-to-SQL suele actualizar PRY_Id automáticamente en la entidad "entidad"
                dto.PRY_Id = entidad.PRY_Id;
            }

            // 4) Guardar Departamentos asociados
            bool okDeps = _dalProjDeps.GuardarDepartamentos(dto.PRY_Id, dto.Departamentos);

            // 5) Guardar Empresas asociadas
            bool okEmps = _dalProjEmps.GuardarEmpresas(dto.PRY_Id, dto.Empresas);

            bool overall = okMain && okDeps; // && okEmps;
            return Json(new { success = overall });
        }

        [HttpPost]
        public JsonResult DeleteProyecto(int id)
        {
            DAL_Proyectos_Departamentos _dalProjDeps = new DAL_Proyectos_Departamentos();
            DAL_Proyectos_Empresas _dalProjEmps = new DAL_Proyectos_Empresas();

            // 1) Eliminar las relaciones hijas primero
            _dalProjDeps.EliminarPorProyecto(id);
            _dalProjEmps.EliminarPorProyecto(id);

            // 2) Eliminar registro principal
            var ok = _dalProyecto.Eliminar(id);
            return Json(new { success = ok });
        }

        [HttpGet]
        public JsonResult GetPeriodosPartes(int anyo)
        {
            var lista = _dalPeriodos.ObtenerPorAnyo(anyo)
                .Select(x => new
                {
                    x.PEP_Anyo,
                    x.PEP_Mes,
                    PEP_FechaInicio = x.PEP_FechaInicio.ToString("yyyy-MM-dd"),
                    PEP_FechaFin = x.PEP_FechaFin.ToString("yyyy-MM-dd")
                })
                .OrderBy(x => x.PEP_Mes)
                .ToList();
            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetUsuariosPorDepartamento(int anyo, int mes)
        {
            var personaId = Sesion.SPersonaId;
            var deptos = _dalDepartamentos.L(false, d => d.DEP_PER_Id_Responsable == personaId).ToList();
            if (!deptos.Any())
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            var deptoIds = deptos.Select(d => d.DEP_Id).ToList();
            var pendientesIds = _dalPartes
                .L(false, p => p.PPA_PEP_Anyo == anyo && p.PPA_PEP_Mes == mes && !p.PPA_Validado)
                .Select(p => p.PPA_PER_Id).Distinct().ToList();

            var usuariosDept = _dalPersonas
                .L(false, p =>
                    p.PER_DEP_Id.HasValue &&
                    deptoIds.Contains(p.PER_DEP_Id.Value) &&
                    (p.PER_Activo || pendientesIds.Contains(p.PER_Id))
                )
                .Select(p => new
                {
                    p.PER_Id,
                    Nombre = p.ApellidosNombre,
                    DepartamentoId = p.PER_DEP_Id  // <-- aquí lo añadimos
                })
                .ToList();

            // 4) Asegurar que el usuario de sesión esté siempre presente
            if (!usuariosDept.Any(u => u.PER_Id == personaId))
            {
                var yo = _dalPersonas.L_PrimaryKey(personaId);
                if (yo != null)
                    usuariosDept.Add(new
                    {
                        PER_Id = yo.PER_Id,
                        Nombre = yo.ApellidosNombre,
                        DepartamentoId = yo.PER_DEP_Id
                    });
            }

            // 5) Ordenar: primero yo, luego alfabéticamente
            var listaUsuarios = usuariosDept
                .OrderByDescending(u => u.PER_Id == personaId)
                .ThenBy(u => u.Nombre)
                .ToList();

            return Json(listaUsuarios, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetProyectosPorDepartamento(int? userId)
        {
            // 1) Obtenemos el usuario actual y su entidad Persona
            int idPersona = Sesion.SPersonaId;
            if (userId.HasValue)
                idPersona = userId.Value;

            var persona = _dalPersonas.L_PrimaryKey(idPersona);
            if (persona == null || persona.PER_DEP_Id == null)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            // 2) Sacamos el ID del departamento al que pertenece esa persona
            int deptoId = persona.PER_DEP_Id.Value;

            // 3) Obtenemos todos los Proyectos_Departamentos donde PRD_DEP_Id == deptoId
            //    y donde el proyecto esté activo
            var listaProyectos = _dalProjDeps
                .L(false, pd => pd.PRD_DEP_Id == deptoId && pd.Proyectos.PRY_Activo)
                .Select(pd => new
                {
                    PRY_Id = pd.PRD_PRY_Id,
                    PRY_Nombre = pd.Proyectos.PRY_Nombre
                })
                .Distinct()
                .ToList();

            return Json(listaProyectos, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetPartesPorPeriodo(int anyo, int mes, int? userId)
        {
            DAL_Tareas_Empresas_LineasEsfuerzo _dalTareasEmpresas = new DAL_Tareas_Empresas_LineasEsfuerzo();
            int idPersona = Sesion.SPersonaId;
            if (userId.HasValue) idPersona = userId.Value;

            var periodoFact = ObtenerPeriodoFacturacion();

            // 1) Traigo las PPA del periodo
            var partes = _dalPartes.ObtenerPorPeriodo(anyo, mes)
                .Where(p => p.PPA_PER_Id == idPersona)
                .Select(p => new {
                    p.PPA_Id,
                    p.PPA_PEP_Anyo,
                    p.PPA_PEP_Mes,
                    p.PPA_PRY_Id,
                    p.PPA_PER_Id,
                    p.PPA_Descripcion,
                    p.PPA_Validado,
                    UserDeptId = p.Personas.PER_DEP_Id,

                    // <-- aquí añadimos:
                    ConceptoGenerado = _dalTareasEmpresas
                        .L(false, te =>
                            te.TLE_Anyo == periodoFact.Year
                            && te.TLE_Mes == periodoFact.Month
                            && te.TLE_TAR_Id == p.Proyectos.PRY_TAR_Id
                            // existe detalle para esta PPA_Id?
                            && te.Tareas_Empresas_LineasEsfuerzo_Partes.Any(lp => lp.TCP_PPA_Id == p.PPA_Id)
                        )
                        .Any()
                })
                .ToList();

            return Json(partes, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveParteLinea(Proyectos_Partes model)
        {
            bool ok;

            if (model.PPA_Id == 0)
            {
                if (model.PPA_PER_Id == 0)
                    model.PPA_PER_Id = Sesion.SPersonaId;

                ok = _dalPartes.Crear(model, Sesion.SPersonaId);
            }
            else
            {
                ok = _dalPartes.Actualizar(model, Sesion.SPersonaId);
            }
            return Json(new { success = ok });
        }

        [HttpPost]
        public JsonResult DeleteParteLinea(int PPA_Id)
        {
            // Primero eliminamos sus horas hijas
            _dalHorasParte.EliminarPorParte(PPA_Id);
            // Luego eliminamos la línea
            var ok = _dalPartes.Eliminar(PPA_Id);
            return Json(new { success = ok });
        }

        [HttpGet]
        public JsonResult GetHorasPorParte(int PPA_Id)
        {
            var horas = _dalHorasParte.ObtenerPorParte(PPA_Id)
                .Select(h => new
                {
                    Fecha = h.PPH_Fecha.ToString("yyyy-MM-dd"),
                    h.PPH_Horas
                })
                .ToList();
            return Json(horas, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveHoraParte(int PPA_Id, string Fecha, string Horas)
        {
            DateTime dt = DateTime.Parse(Fecha);
            decimal horasDec = 0;
            try
            {
                // recibimos como cadena "1,50" → convertimos a decimal
                horasDec = Convert.ToDecimal(Horas.Replace(".", ","));
            }
            catch
            {
                return Json(new { success = false });
            }
            var ok = _dalHorasParte.GuardarHora(PPA_Id, dt, horasDec, Sesion.SPersonaId);
            return Json(new { success = ok });
        }

        [HttpPost]
        public JsonResult ValidateParte(int PPA_Id)
        {
            var parte = _dalPartes.L_PrimaryKey(PPA_Id);
            if (parte == null)
            {
                return Json(new { success = false });
            }
            // Si ya estaba validado, nada que hacer
            if (parte.PPA_Validado)
            {
                return Json(new { success = true });
            }

            parte.PPA_Validado = true;
            var ok = _dalPartes.Actualizar(parte, Sesion.SPersonaId);
            return Json(new { success = ok });
        }

        [HttpGet]
        public JsonResult ObtenerPersonas()
        {
            var lista = _dalPersonas
                .L(false, null)
                .Select(p => new
                {
                    p.PER_Id,
                    NombreCompleto = p.ApellidosNombre,
                    p.PER_Nombre,
                    p.PER_Apellido1,
                    p.PER_Apellido2,
                    DepartamentoId = p.PER_DEP_Id,
                    DepartamentoNombre = p.PER_DEP_Id.HasValue
                        ? _dalDepartamentos.L_PrimaryKey(p.PER_DEP_Id.Value).DEP_Nombre
                        : "",
                    p.PER_Email,
                    p.PER_Activo
                })
                .OrderBy(p => p.NombreCompleto)
                .ToList();
            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GuardarPersona(Personas modelo)
        {
            bool ok = _dalPersonas.G(modelo, Sesion.SPersonaId);
            return Json(new { success = ok });
        }

        [HttpPost]
        public JsonResult EliminarPersona(int id)
        {
            bool ok = _dalPersonas.EliminarPersona(id, Sesion.SPersonaId);
            return Json(new { success = ok });
        }

        [HttpPost]
        public JsonResult UnvalidateParte(int PPA_Id)
        {
            var parte = _dalPartes.L_PrimaryKey(PPA_Id);
            if (parte == null)
                return Json(new { success = false, message = "Parte no encontrado" });

            parte.PPA_Validado = false;
            var ok = _dalPartes.Actualizar(parte, Sesion.SPersonaId);
            return Json(new { success = ok });
        }

        [HttpPost]
        public JsonResult PrevisualizarConceptosHoras(int anio, int mes, int? userId)
        {
            try
            {
                if (anio < 2000 || mes < 1 || mes > 12)
                {
                    return Json(new { success = false, mensaje = "Año o mes no válidos." });
                }

                var partesErrors = new List<string>();
                List<ConceptoConDetalleDto> previewConDetalle = new List<ConceptoConDetalleDto>();
                try
                {
                    previewConDetalle = GenerarPreviewPartesHoras(new DateTime(anio, mes, 1), userId, partesErrors);
                }
                catch (Exception ex)
                {
                    partesErrors.Add(ex.Message);
                }

                if (partesErrors.Any())
                {
                    // unificamos en HTML para que llegue bonito al Swal
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
        public JsonResult GenerarConceptosHoras(int anio, int mes, int? userId)
        {
            try
            {
                if (anio < 2000 || mes < 1 || mes > 12)
                {
                    return Json(new { success = false, mensaje = "Año o mes no válidos." });
                }

                var errores = new List<string>();
                List<GenerarResultDto> resultado = new List<GenerarResultDto>();
                try
                {
                    resultado = EjecutarGenerarConceptosPartesHoras(new DateTime(anio, mes, 1), userId);
                }
                catch (Exception ex)
                {
                    errores.Add(ex.Message);
                }

                return Json(new
                {
                    success = (errores.Count == 0),
                    errors = errores,
                    resultado = resultado
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        private List<ConceptoConDetalleDto> GenerarPreviewPartesHoras(DateTime periodo, int? userId, List<string> Errores)
        {
            int anyo = periodo.Year;
            int mes = periodo.Month;

            var periodoFact = ObtenerPeriodoFacturacion();

            // 1) Tarifas de las tareas
            var daoTareas = new DAL_Tareas();
            var tareasDic = daoTareas.L(false, null).ToDictionary(t => t.TAR_Id, t => t.TAR_ImporteUnitario ?? 1m);

            // 2) Departamentos de los que soy responsable
            var personaId = Sesion.SPersonaId;
            var deptosResp = _dalDepartamentos.L(false, d => d.DEP_PER_Id_Responsable == personaId).Select(d => d.DEP_Id).ToList();

            // 3) Cargamos sólo las PPA validadas de mi área (o del userId si viene)
            // Proyectos imputables (cache)
            var proysImputables = new DAL_Proyectos().L(false, null)
                .Where(p => p.PRY_Imputable)
                .ToDictionary(p => p.PRY_Id);

            // 3) PPA validadas SOLO de proyectos imputables
            var daoPPA = new DAL_Proyectos_Partes();
            var todasPPA = daoPPA.L(false, null)
                .Where(p => p.PPA_PEP_Anyo == anyo
                         && p.PPA_PEP_Mes == mes
                         && p.PPA_Validado
                         && proysImputables.ContainsKey(p.PPA_PRY_Id)   // <-- filtro clave
                         && (userId.HasValue
                             ? p.PPA_PER_Id == userId.Value
                             : (p.Personas.PER_DEP_Id.HasValue && deptosResp.Contains(p.Personas.PER_DEP_Id.Value))))
                .ToList();

            // 4) Detectar las PPA NO validadas en el mismo ámbito
            var noValidados = daoPPA.L(false, null)
                .Where(p => p.PPA_PEP_Anyo == anyo
                         && p.PPA_PEP_Mes == mes
                         && !p.PPA_Validado
                         && (
                               userId.HasValue
                                 ? p.PPA_PER_Id == userId.Value
                                 : (p.Personas.PER_DEP_Id.HasValue && deptosResp.Contains(p.Personas.PER_DEP_Id.Value))
                            )
                )
                .Select(p => $"Parte «{p.PPA_Descripcion}» de {p.Personas.ApellidosNombre}")
                .ToList();

            if (noValidados.Any())
                Errores.Add($"Hay partes sin validar: {string.Join("; ", noValidados)}");

            // 5) Detalles de horas sólo para esas PPA
            var daoPPH = new DAL_Proyectos_Partes_Horas();
            var todasPPH = daoPPH.L(false, null)
                .Where(h => todasPPA.Select(p => p.PPA_Id).Contains(h.PPH_PPA_Id))
                .ToList();

            // 6) Datos de proyecto y empresas
            var daoProy = new DAL_Proyectos();
            var proyectosDict = daoProy.L(false, null)
                .Where(p => p.PRY_Imputable)
                .ToDictionary(x => x.PRY_Id, x => new { x.PRY_Id, x.PRY_TAR_Id, x.PRY_Nombre, x.PRY_Imputable });

            // Relaciones: (a) todas y (b) solo imputables (excluye EGC)
            var daoProjEmp = new DAL_Proyectos_Empresas();
            var relAll = daoProjEmp.L(false, null)
                .GroupBy(pe => pe.PRE_PRY_Id)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new { x.PRE_EMP_Id, x.PRE_Porcentaje }).ToList()
                );

            var relImputables = daoProjEmp.L(false, null)
                .Where(pe => pe.PRE_EMP_Id != (int)EmpresaExcluyenteConceptos.EGC)
                .GroupBy(pe => pe.PRE_PRY_Id)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new { x.PRE_EMP_Id, x.PRE_Porcentaje }).ToList()
                );

            var empDict = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id);
            var daoTE = new DAL_Tareas_Empresas();

            // Proyectos relevantes: solo con PPA validadas del usuario/área **y** imputables
            var proyectosRelevantes = todasPPA
                .Select(p => p.PPA_PRY_Id)
                .Distinct()
                .Where(pid => proyectosDict.ContainsKey(pid) && proyectosDict[pid].PRY_Imputable)
                .ToList();

            // Combos tarea–empresa solo de proyectos imputables y con relaciones imputables
            var combos = proyectosRelevantes
                .SelectMany(pid =>
                {
                    var info = proyectosDict[pid];
                    if (!relImputables.TryGetValue(pid, out var listaRel)) 
                        return Enumerable.Empty<(int tarifaId, int empresaId)>();

                    return listaRel.Select(rel => (tarifaId: info.PRY_TAR_Id, empresaId: rel.PRE_EMP_Id));
                })
                .Distinct()
                .ToList();

            foreach (var c in combos)
            {
                bool existe = daoTE
                  .L(false, te => te.TEM_TAR_Id == c.tarifaId
                               && te.TEM_EMP_Id == c.empresaId
                               && te.TEM_Anyo == periodoFact.Year)
                  .Any();
                if (!existe)
                {
                    var nombreTar = ObtenerNombreTarea(c.tarifaId);
                    var nombreEmp = empDict.TryGetValue(c.empresaId, out var emp) ? emp.EMP_Nombre : $"(ID {c.empresaId} no encontrado)";
                    Errores.Add($"❌ Para la Tarea «{nombreTar}» falta el importe para la Empresa «{nombreEmp}» - Año {periodoFact.Year}.");
                }
            }

            if (Errores.Any())
            {
                return new List<ConceptoConDetalleDto>();
            }

            // 7) Construimos los nuevos conceptos
            var dictConceptos = new Dictionary<(int tarifaId, int empresaId, int ppaId), ConceptoConDetalleDto>();
            foreach (var ppa in todasPPA)
            {
                int ppaId = ppa.PPA_Id;
                int proyectoId = ppa.PPA_PRY_Id;

                // Si no existe en diccionario (borrar raro) o NO imputable -> saltar sin error
                //if (!proyectosDict.ContainsKey(proyectoId) || !proyectosDict[proyectoId].PRY_Imputable)
                //    continue;

                if (!proyectosDict.ContainsKey(proyectoId))
                {
                    var daoProyTmp = new DAL_Proyectos();
                    var proyTmp = daoProyTmp.L(false, p => p.PRY_Id == proyectoId).FirstOrDefault();
                    var nombreProyTmp = proyTmp?.PRY_Nombre ?? "(nombre no encontrado)";
                    throw new ApplicationException($"Proyecto '{nombreProyTmp}' no tiene asignado empresa.");
                }

                var infoProy = proyectosDict[proyectoId];
                int tareaId = infoProy.PRY_TAR_Id;
                string nombreProy = infoProy.PRY_Nombre;

                // Tarea sin tarifa definida
                if (!tareasDic.ContainsKey(tareaId))
                {
                    var daoTarTmp = new DAL_Tareas();
                    var tarTmp = daoTarTmp.L(false, t => t.TAR_Id == tareaId).FirstOrDefault();
                    var nombreTarTmp = tarTmp?.TAR_Nombre ?? "(nombre no encontrado)";
                    Errores.Add($"La tarea «{nombreTarTmp}» del proyecto «{nombreProy}» no tiene tarifa definida.");
                    continue;
                }

                // Horas de la PPA
                var detallesPPA = todasPPH.Where(h => h.PPH_PPA_Id == ppaId).ToList();
                if (!detallesPPA.Any()) continue;

                int totalMin = detallesPPA.Sum(d => (int)Math.Round(d.PPH_Horas * 60m));
                decimal horasDec = Math.Round(totalMin / 60m, 2);

                // ¿Tiene empresas imputables?
                if (!relImputables.ContainsKey(proyectoId))
                {
                    Errores.Add($"El proyecto «{nombreProy}» no tiene empresas imputables.");
                    continue; 
                }

                // Reparto por empresas imputables
                foreach (var rel in relImputables[proyectoId])
                {
                    int empresaId = rel.PRE_EMP_Id;
                    decimal porcentaje = rel.PRE_Porcentaje;

                    decimal horasPorcentaje = Math.Round((horasDec * porcentaje) / 100m, 2);
                    decimal importeParte = Math.Round(horasPorcentaje * tareasDic[tareaId], 2);
                    var key = (tareaId, empresaId, ppaId);

                    if (!dictConceptos.ContainsKey(key))
                    {
                        dictConceptos[key] = new ConceptoConDetalleDto
                        {
                            TAR_Id = tareaId,
                            TAR_Nombre = ObtenerNombreTarea(tareaId),
                            EmpresaId = empresaId,
                            EmpresaNombre = empDict.TryGetValue(empresaId, out var emp) ? emp.EMP_Nombre : $"(ID {empresaId} no encontrado)",
                            Anyo = anyo,
                            Mes = mes,
                            TarifaHora = tareasDic[tareaId],
                            PorcentajeEmpresa = porcentaje,
                            ImporteTotal = 0m,
                            Detalles = new List<DetalleParteDto>(),
                            DepartamentoNombre = _dalDepartamentos.L_PrimaryKey(ppa.Personas.PER_DEP_Id ?? 0)?.DEP_Nombre ?? ""
                        };
                    }

                    var dto = dictConceptos[key];
                    dto.ImporteTotal += importeParte;
                    dto.Detalles.Add(new DetalleParteDto
                    {
                        ProyectoId = proyectoId,
                        ProyectoNombre = nombreProy,
                        PPA_Id = ppaId,
                        Actividad = ppa.PPA_Descripcion,
                        Horas = horasPorcentaje,
                        ImporteParte = importeParte,
                        NombreEmpleado = ppa.Personas.ApellidosNombre
                    });
                }
            }

            // 8) Detectar existentes con detalle para evitar duplicados (solo los generados con "Conceptos Horas")
            var daoConc = new DAL_Tareas_Empresas_LineasEsfuerzo();
            var daoTcp = new DAL_Tareas_Empresas_LineasEsfuerzo_Partes();

            // Primero, los detalles para las PPA en preview
            var tcpList = daoTcp.L(true, null)
                .Where(t => dictConceptos.Keys.Select(k => k.ppaId).Contains(t.TCP_PPA_Id))
                .ToList();

            // Luego, detectamos solo los encabezados de ese mes/año **y** cuya descripción contiene "Conceptos Horas"
            var existen = daoConc.L(true, null)
                .Where(c =>
                    c.TLE_Anyo == periodoFact.Year &&
                    c.TLE_Mes == periodoFact.Month &&
                    (c.TLE_Descripcion?.Contains("Conceptos Horas") ?? false)
                )
                .Join(tcpList,
                      cab => cab.TLE_Id,
                      det => det.TCP_TLE_Id,
                      (cab, det) => (cab.TLE_TAR_Id, cab.TLE_EMP_Id, det.TCP_PPA_Id))
                .ToHashSet();

            // 10) Devolver sólo los nuevos
            return dictConceptos
                .Where(kvp => !existen.Contains(kvp.Key))
                .Select(kvp => kvp.Value)
                .ToList();
        }

        private List<GenerarResultDto> EjecutarGenerarConceptosPartesHoras(DateTime periodo, int? userId)
        {
            int anyo = periodo.Year;
            int mes = periodo.Month;
            var periodoFact = ObtenerPeriodoFacturacion();

            // 1) Reconstruir preview
            var errores = new List<string>();
            var preview = GenerarPreviewPartesHoras(periodo, userId, errores);

            // 2) Diccionarios y DAOs
            var daoTareas = new DAL_Tareas();
            var tareasDic = daoTareas.L(false, null).ToDictionary(t => t.TAR_Id, t => t.TAR_ImporteUnitario ?? 1m);

            var daoConc = new DAL_Tareas_Empresas_LineasEsfuerzo();
            var daoDetalle = new DAL_Tareas_Empresas_LineasEsfuerzo_Partes();

            // 3) Detectar líneas ya existentes (cabecera + detalle) para no duplicar
            var daoDetalleExist = new DAL_Tareas_Empresas_LineasEsfuerzo_Partes();
            var detallesExistentes = daoDetalleExist.L(true, null)
                .Where(d => preview
                    .SelectMany(c => c.Detalles)
                    .Select(dt => dt.PPA_Id)
                    .Contains(d.TCP_PPA_Id))
                .ToList();

            var cabeExistentes = daoConc.L(true, null)
                .Where(c =>
                    c.TLE_Anyo == periodoFact.Year &&
                    c.TLE_Mes == periodoFact.Month &&
                    (c.TLE_Descripcion?.Contains("Conceptos Horas") ?? false)
                )
                .ToList();

            var existentes = cabeExistentes
                .Join(detallesExistentes,
                      cab => cab.TLE_Id,
                      det => det.TCP_TLE_Id,
                      (cab, det) => (cab.TLE_TAR_Id, cab.TLE_EMP_Id, det.TCP_PPA_Id))
                .ToHashSet();

            // 4) Cargar relaciones proyecto→empresas y nombres de empresas
            var daoProjEmp = new DAL_Proyectos_Empresas();
            var todasProjEmp = daoProjEmp.L(false, null)
                .Where(pe => pe.PRE_EMP_Id != (int)EmpresaExcluyenteConceptos.EGC)
                .GroupBy(pe => pe.PRE_PRY_Id)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new { x.PRE_EMP_Id, x.PRE_Porcentaje }).ToList()
                );

            var empDict = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id, e => e.EMP_Nombre);

            // 5) Reagrupar preview por (tarifaId, empresaId)
            var grupos = preview
                .GroupBy(c => new { c.TAR_Id, c.EmpresaId, c.TAR_Nombre, c.EmpresaNombre, c.DepartamentoNombre })
                .Select(g => new {
                    TareaId = g.Key.TAR_Id,
                    EmpresaId = g.Key.EmpresaId,
                    TarifaNombre = g.Key.TAR_Nombre,
                    EmpresaNombre = g.Key.EmpresaNombre,
                    DepartamentoNombre = g.Key.DepartamentoNombre,
                    Detalles = g.SelectMany(c => c.Detalles).ToList()
                });

            var resumen = new Dictionary<int, int>();

            // 6) Insertar un bloque por cada grupo
            foreach (var grp in grupos)
            {
                var ppaIds = grp.Detalles.Select(d => d.PPA_Id).Distinct();

                // si todas las PPAs de este grupo ya existen, saltamos
                if (ppaIds.All(id => existentes.Contains((grp.TareaId, grp.EmpresaId, id))))
                    continue;

                // 6.1) Insertar cabecera única
                decimal sumHoras = grp.Detalles.Sum(d => d.Horas);
                var cab = new Tareas_Empresas_LineasEsfuerzo
                {
                    TLE_EMP_Id = grp.EmpresaId,
                    TLE_Anyo = periodoFact.Year,
                    TLE_Mes = periodoFact.Month,
                    TLE_TAR_Id = grp.TareaId,
                    TLE_Cantidad = sumHoras,
                    TLE_Descripcion = $"Generado auto. Conceptos Horas - Departamento: {grp.DepartamentoNombre}",
                    TLE_ESO_Id = (int)Constantes.EstadosSolicitud.PendienteAprobacion,
                    TLE_ESO_Id_Original = (int)Constantes.EstadosSolicitud.SinSolicitar,
                    TLE_PER_Id_Aprobador = 0,
                    TLE_FechaAprobacion = DateTime.Now,
                    TLE_ComentarioAprobacion = "Aprobado automáticamente",
                    FechaAlta = DateTime.Now,
                    FechaModificacion = DateTime.Now,
                    PER_Id_Modificacion = Sesion.SPersonaId,
                    TLE_Inversion = false
                };
                daoConc.G(cab, Sesion.SPersonaId);
                int nuevoTLEId = cab.TLE_Id;

                // 6.2) Insertar todos los detalles de horas para esas PPAs
                var daoHoras = new DAL_Proyectos_Partes_Horas();
                var horasPPA = daoHoras.L(false, null).Where(h => ppaIds.Contains(h.PPH_PPA_Id)).ToList();

                foreach (var h in horasPPA)
                {
                    if (existentes.Contains((grp.TareaId, grp.EmpresaId, h.PPH_PPA_Id)))
                        continue;

                    // cálculo de importe
                    int minutos = (int)Math.Round(h.PPH_Horas * 60m);
                    var detallePreview = grp.Detalles.First(d => d.PPA_Id == h.PPH_PPA_Id);
                    var proyectoId = detallePreview.ProyectoId;
                    decimal costeHora = tareasDic[grp.TareaId];
                    decimal porcentaje = todasProjEmp[proyectoId].First(x => x.PRE_EMP_Id == grp.EmpresaId).PRE_Porcentaje;
                    decimal horasDetalle = Math.Round((minutos / 60m) * porcentaje / 100m, 2, MidpointRounding.AwayFromZero);

                    var detInsert = new Tareas_Empresas_LineasEsfuerzo_Partes
                    {
                        TCP_TLE_Id = nuevoTLEId,
                        TCP_PPA_Id = h.PPH_PPA_Id,
                        TCP_Fecha = h.PPH_Fecha,
                        TCP_Horas = horasDetalle
                    };
                    daoDetalle.G(detInsert, Sesion.SPersonaId);
                }

                // 6.3) Contabilizar un único concepto generado para esta empresa
                resumen[grp.EmpresaId] = resumen.ContainsKey(grp.EmpresaId)
                    ? resumen[grp.EmpresaId] + 1
                    : 1;
            }

            // 7) Devolver el DTO de resultados
            return resumen
                .Select(kvp => new GenerarResultDto
                {
                    EmpresaId = kvp.Key,
                    EmpresaNombre = empDict[kvp.Key],
                    ConceptosCreados = kvp.Value
                })
                .ToList();
        }

        private string ObtenerNombreTarea(int tarifaId)
        {
            var dalTareas = new DAL_Tareas();
            var tarea = dalTareas.L_PrimaryKey(tarifaId);
            return tarea != null ? tarea.TAR_Nombre : $"Tarea {tarifaId}";
        }
    }

    /// <summary>
    /// DTO que encapsula todos los campos del proyecto + listas de Departamentos y Empresas.
    /// </summary>
    public class ProyectoDto
    {
        public int PRY_Id { get; set; }
        public string PRY_Nombre { get; set; }
        public int PRY_TAR_Id { get; set; }
        public bool PRY_Imputable { get; set; }
        public bool PRY_Activo { get; set; }
        public List<int> Departamentos { get; set; } = new List<int>();
        public List<Proyectos_Empresas> Empresas { get; set; } = new List<Proyectos_Empresas>();
    }
}

public class DetalleParteDto
{
    public int ProyectoId { get; set; }
    public string ProyectoNombre { get; set; }
    public int PPA_Id { get; set; }
    public string Actividad { get; set; }
    public decimal Horas { get; set; }
    public decimal ImporteParte { get; set; }
    public string NombreEmpleado { get; set; }
}

public class ConceptoConDetalleDto
{
    public int TAR_Id { get; set; }
    public string TAR_Nombre { get; set; }
    public int EmpresaId { get; set; }
    public string EmpresaNombre { get; set; }
    public int Anyo { get; set; }
    public int Mes { get; set; }
    public decimal TarifaHora { get; set; }
    public decimal PorcentajeEmpresa { get; set; }
    public decimal ImporteTotal { get; set; }
    public List<DetalleParteDto> Detalles { get; set; }
    public string DepartamentoNombre { get; set; }
}
