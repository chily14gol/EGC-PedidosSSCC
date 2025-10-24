using AccesoDatos;
using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Web.Mvc;
using static PedidosSSCC.Comun.Constantes;

namespace PedidosSSCC.Controllers
{
    public class ProcesoController : BaseController
    {
        //Proceso Automático para obtener Usuarios
        [HttpGet]
        public JsonResult ObtenerUsuariosDA()
        {
            try
            {
                using (BLL_Impersonate.Impersonar())
                {
                    DirectoryEntry entradaDominioPrincipal = new DirectoryEntry("LDAP://ou=Usuarios,dc=erhardti,dc=es");
                    DirectorySearcher buscador = new DirectorySearcher(entradaDominioPrincipal);
                    buscador.Filter = "(&(objectClass=user)(objectCategory=Person)(sAMAccountName=*))";
                    buscador.PageSize = 2000;

                    SearchResultCollection resultados = buscador.FindAll();

                    var dalEntes = new DAL_Entes();
                    var dalOficinas = new DAL_Oficinas();
                    var dalEmpresas = new DAL_Empresas();

                    List<ADUserDTO> listaInfoDA = new List<ADUserDTO>();
                    List<Entes> listaEntes = new List<Entes>();

                    foreach (SearchResult result in resultados)
                    {
                        try
                        {
                            // Saltar cuentas 'sudo'
                            if (result.Properties["sAMAccountName"][0].ToString().StartsWith("sudo"))
                                continue;

                            DirectoryEntry entrada = new DirectoryEntry(result.Path);
                            if (IsActive(entrada))
                            {
                                var adUser = new ADUserDTO
                                {
                                    UsuarioRed = GetProp(result, "sAMAccountName"),
                                    NombreMostrado = GetProp(result, "displayname"),
                                    Email = GetProp(result, "mail"),
                                    Empresa = GetProp(result, "company"),
                                    Oficina = GetProp(result, "physicaldeliveryofficename")
                                };
                                listaInfoDA.Add(adUser);

                                // Sólo procesamos si tenemos nombre mostrado y email
                                if (!String.IsNullOrEmpty(adUser.NombreMostrado) && !String.IsNullOrEmpty(adUser.Email))
                                {
                                    System.Diagnostics.Debug.WriteLine($"Procesando usuario: {adUser.Email}");

                                    // 1) Primero buscamos si ya existe un ente con ese correo:
                                    var entePorEmail = dalEntes.ObtenerPorEmail(adUser.Email);

                                    if (entePorEmail == null)
                                    {
                                        // 2) Si NO existe por email, vemos si existe por nombre EXACTO.
                                        var entePorNombre = dalEntes.ObtenerPorNombre(adUser.NombreMostrado);

                                        if (entePorNombre != null)
                                        {
                                            // Existe un registro con el mismo NombreMostrado pero diferente EMAIL:
                                            if (!entePorNombre.ENT_Email.Equals(adUser.Email, StringComparison.OrdinalIgnoreCase))
                                            {
                                                // ---> ENVIAR UN CORREO A REVISAR MANUALMENTE:
                                                // Preparamos datos para el correo de discrepancia
                                                string emailDestino = ConfigurationManager.AppSettings["EmailSoporteDA"];

                                                // Montamos un objeto con los datos mínimos
                                                var aviso = new
                                                {
                                                    NombreEnBD = entePorNombre.ENT_Nombre,
                                                    EmailEnBD = entePorNombre.ENT_Email,
                                                    EmailEnAD = adUser.Email,
                                                    NombreEnAD = adUser.NombreMostrado,
                                                    OficinaAD = adUser.Oficina,
                                                    EmpresaAD = adUser.Empresa,
                                                    FechaInforme = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                                                };

                                                // Llamamos a una función que envíe el correo de “discrepancia”:
                                                var resultadoEnvio = EnviarMail_DiscrepanciaEmail(
                                                    aviso.NombreEnBD,
                                                    aviso.EmailEnBD,
                                                    aviso.EmailEnAD,
                                                    emailDestino);

                                                System.Diagnostics.Debug.WriteLine(
                                                    $"Discrepancia detectada para “{aviso.NombreEnBD}”: " +
                                                    $"Email en BD = '{aviso.EmailEnBD}', email en AD = '{aviso.EmailEnAD}'. " +
                                                    $"Envío correo: {resultadoEnvio.Message}"
                                                );

                                                // **No guardamos nuevo Ente ni tocamos este registro**,
                                                // simplemente registramos la discrepancia y continuamos.
                                                continue;
                                            }
                                        }

                                        // 3) Si no existe ENTE por email ni por nombre, creamos uno nuevo:
                                        int? idOficina = dalOficinas.ObtenerIdPorNombre(adUser.Oficina);
                                        int? empresaId = dalEmpresas.ObtenerIdPorNombreEmpresa(adUser.Empresa);

                                        var nuevoEnte = new Entes
                                        {
                                            ENT_Nombre = adUser.NombreMostrado,
                                            ENT_Email = adUser.Email,
                                            ENT_EMP_Id = empresaId,
                                            ENT_OFI_Id = idOficina
                                        };

                                        listaEntes.Add(nuevoEnte);
                                        // (En lugar de llamar a dalEntes.GuardarEnte(nuevoEnte) aquí,
                                        //  lo hacemos al final en bloque: dalEntes.GuardarEntesBulk(listaEntes))
                                    }
                                    else
                                    {
                                        // 4) Si ya existe un registro por email, actualizamos compañia/oficina si es necesario:
                                        int? idOficina = dalOficinas.ObtenerIdPorNombre(adUser.Oficina);
                                        int? empresaId = dalEmpresas.ObtenerIdPorNombreEmpresa(adUser.Empresa);

                                        bool hayCambios = false;

                                        if (empresaId.HasValue && entePorEmail.ENT_EMP_Id != empresaId)
                                        {
                                            entePorEmail.ENT_EMP_Id = empresaId;
                                            hayCambios = true;
                                        }

                                        if (idOficina.HasValue && entePorEmail.ENT_OFI_Id != idOficina)
                                        {
                                            entePorEmail.ENT_OFI_Id = idOficina;
                                            hayCambios = true;
                                        }

                                        if (hayCambios)
                                        {
                                            dalEntes.ActualizarEnte(entePorEmail);
                                            System.Diagnostics.Debug.WriteLine($"Usuario actualizado: {adUser.Email}");
                                        }
                                        else
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Usuario sin cambios: {adUser.Email}");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception exLoop)
                        {
                            // Si algo falla procesando UNA fila concreta del AD, lo anotamos y seguimos:
                            System.Diagnostics.Debug.WriteLine(
                                $"Error procesando {GetProp(result, "mail")}: {exLoop.Message}"
                            );
                        }
                    }

                    // 5) Guardamos todos los entes nuevos en bloque:
                    dalEntes.GuardarEntesBulk(listaEntes);

                    // 6) Guardar la información de los usuarios en un archivo CSV,
                    //    tal como ya tenías en tu método original:
                    GuardarUsuariosEnCsv(listaInfoDA);

                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private void GuardarUsuariosEnCsv(List<ADUserDTO> listaUsuarios)
        {
            // Ruta donde quieres guardar el CSV (puede ser configurable)
            string rutaCsv = @"C:\Temp\UsuariosAD.csv";

            // Separador de campos (coma, punto y coma, tab, etc.)
            char separador = ';';

            var sb = new StringBuilder();

            // Cabecera CSV
            sb.AppendLine($"UsuarioRed{separador}Nombre{separador}Apellido{separador}NombreMostrado{separador}Email{separador}Empresa{separador}Departamento{separador}Titulo{separador}Oficina");

            // Cada fila
            foreach (var user in listaUsuarios)
            {
                sb.AppendLine(
                    $"{EnQuotes(user.UsuarioRed)}{separador}" +
                    $"{EnQuotes(user.Nombre)}{separador}" +
                    $"{EnQuotes(user.Apellido)}{separador}" +
                    $"{EnQuotes(user.NombreMostrado)}{separador}" +
                    $"{EnQuotes(user.Email)}{separador}" +
                    $"{EnQuotes(user.Empresa)}{separador}" +
                    $"{EnQuotes(user.Departamento)}{separador}" +
                    $"{EnQuotes(user.Titulo)}{separador}" +
                    $"{EnQuotes(user.Oficina)}"
                );
            }

            // Guardar en archivo
            System.IO.File.WriteAllText(rutaCsv, sb.ToString(), Encoding.UTF8);
        }

        // Función auxiliar para proteger comillas, comas, saltos de línea...
        private string EnQuotes(string texto)
        {
            if (texto == null) return "";
            texto = texto.Replace("\"", "\"\""); // duplicar comillas internas
            return $"\"{texto}\"";
        }

        private string GetProp(SearchResult sr, string propName)
        {
            return sr.Properties.Contains(propName)
                ? sr.Properties[propName][0]?.ToString()
                : null;
        }

        private bool IsActive(DirectoryEntry entry)
        {
            if (entry.Properties["userAccountControl"].Value == null)
                return false;

            int flags = (int)entry.Properties["userAccountControl"].Value;
            return !Convert.ToBoolean(flags & 0x0002); // UF_ACCOUNTDISABLE
        }

        [HttpPost]
        public JsonResult PrevisualizarConceptos()
        {
            try
            {
                DAL_Configuraciones dalConfig = new DAL_Configuraciones();
                Configuraciones objConfig = dalConfig.L_PrimaryKey((int)Constantes.Configuracion.AnioConcepto);

                // Si el año es anterior al actual → diciembre (12), si es el año actual → mes corriente
                var hoy = DateTime.Today;
                int anioConcepto = objConfig != null ? Convert.ToInt32(objConfig.CFG_Valor) : DateTime.Now.Year;
                int mesConcepto = anioConcepto < hoy.Year ? 12 : hoy.Month;
                var periodo = new DateTime(anioConcepto, mesConcepto, 1);

                // — 1) PREVIEW LICENCIAS + ERRORES —
                var licenciasErrors = new List<string>();
                var (previewLicencias, erroresLic) = GenerarPreviewLicencias(periodo);
                if (erroresLic.Any())
                    licenciasErrors.AddRange(erroresLic);

                // — 2) ENTES INVÁLIDOS (ahora como error de licencias, no bloqueo) —
                var entesLic = new DAL_Entes_Licencias().L(false, null);
                var entesDict = new DAL_Entes().L(false, null).ToDictionary(e => e.ENT_Id);
                var invalidEntes = entesLic
                    .Where(el => !entesDict.ContainsKey(el.ENL_ENT_Id) || !entesDict[el.ENL_ENT_Id].ENT_EMP_Id.HasValue)
                    .Select(el => entesDict.ContainsKey(el.ENL_ENT_Id)
                        ? entesDict[el.ENL_ENT_Id].ENT_Nombre
                        : $"Id:{el.ENL_ENT_Id}")
                    .Distinct()
                    .ToList();
                if (invalidEntes.Any())
                    licenciasErrors.Add("Entidades sin empresa asociada: " + string.Join(", ", invalidEntes));

                // — 3) PREVIEW SOPORTE + ERRORES —
                var soporteErrors = new List<string>();
                List<SoportePreviewDto> previewSoporte = new List<SoportePreviewDto>();
                try
                {
                    previewSoporte = GenerarPreviewSoporteCAU(periodo);
                }
                catch (Exception ex)
                {
                    soporteErrors.Add(ex.Message);
                }

                // — 4) PREVIEW ASUNTOS + ERRORES —
                var asuntosErrors = new List<string>();
                List<ConceptoPreviewRow> previewAsuntos = new List<ConceptoPreviewRow>();
                try
                {
                    //previewAsuntos = GenerarPreviewAsuntos(periodo);
                }
                catch (Exception ex)
                {
                    asuntosErrors.Add(ex.Message);
                }

                // — 5) Devolver TODO junto en un único JSON —
                return Json(new
                {
                    success = true,
                    licencias = previewLicencias,
                    licenciasErrors,
                    soporte = previewSoporte,
                    soporteErrors,
                    asuntos = previewAsuntos,
                    asuntosErrors
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GenerarTodosConceptos()
        {
            try
            {
                // Obtenemos el año de configuración o el actual
                DAL_Configuraciones dalConfig = new DAL_Configuraciones();
                Configuraciones objConfig = dalConfig.L_PrimaryKey((int)Constantes.Configuracion.AnioConcepto);
                int anioConcepto = objConfig != null ? Convert.ToInt32(objConfig.CFG_Valor) : DateTime.Now.Year;

                // Decidimos el mes: si el año es anterior al actual → diciembre (12),
                // si es el año actual → mes corriente
                var hoy = DateTime.Today;
                int mesConcepto = anioConcepto < hoy.Year ? 12 : hoy.Month;

                // Finalmente el periodo a usar
                var periodo = new DateTime(anioConcepto, mesConcepto, 1);

                var licencias = EjecutarGenerarConceptosLicencias(periodo);
                var soporte = EjecutarGenerarConceptosSoporteCAU(periodo);  // si tienes uno equivalente
                //var asuntos = EjecutarGenerarConceptosAsuntos(periodo);
                var asuntos = new List<GenerarResultDto>();

                LimpiarCache(TipoCache.Tareas, TipoCache.Conceptos, TipoCache.Pedidos);

                return Json(new
                {
                    success = true,
                    licencias,
                    soporte,
                    asuntos
                });
            }
            catch (ApplicationException ex)
            {
                // aquí recoges la excepción que lanzas dentro de EjecutarGenerarConceptosLicencias
                var errores = ex.Message.Split('\n', (char)StringSplitOptions.RemoveEmptyEntries).ToList();
                return Json(new
                {
                    success = false,
                    errors = errores
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errors = new[] { "Error inesperado generando conceptos" } });
            }
        }

        private (List<ConceptoPreviewRow> Licencias, List<string> Errores) GenerarPreviewLicencias(DateTime periodo)
        {
            var erroresPorEmpresa = new List<string>();
            int anyoConceptos = periodo.Year, mesConceptos = periodo.Month;
            var hoy = DateTime.Today;

            // 1) Carga de datos base
            var todosConc = new DAL_Tareas_Empresas_LineasEsfuerzo().L(true, null).ToList();
            var entesLic = new DAL_Entes_Licencias().L(false, null);
            var entesDict = new DAL_Entes().L(false, null).ToDictionary(e => e.ENT_Id);
            var empDict = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id);
            var licAll = new DAL_Licencias().L(false, null).ToDictionary(l => l.LIC_Id);
            var tarifas = new DAL_Licencias_Tarifas().L(false, null);
            var excs = new DAL_Licencias_Excepciones().L(false, null);
            var repl = new DAL_Licencias_Excepciones_LicenciasReemplazo().L(false, null);
            var tareasEmp = new DAL_Tareas_Empresas().L(false, null);

            // Mapeo hija → padre
            var padreMap = licAll
                .Where(kv => kv.Value.LIC_LIC_Id_Padre.HasValue)
                .ToDictionary(kv => kv.Key, kv => kv.Value.LIC_LIC_Id_Padre.Value);

            // 2) Detalles por concepto
            var asignGroups = entesLic
                .Where(el => entesDict.ContainsKey(el.ENL_ENT_Id)
                          && entesDict[el.ENL_ENT_Id].ENT_EMP_Id.HasValue)
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

            // Diccionario: clave = (empId, tareaId), valor = lista de (licId, entId, importe)
            var detallesPorConcepto = new Dictionary<(int empId, int tId), List<(int licId, int entId, decimal importe)>>();

            foreach (var grp in asignGroups)
            {
                int licPadre = grp.Key.Padre;
                int empId = grp.Key.EMP_Id;

                var exc = excs.FirstOrDefault(e => e.LIE_LIC_Id == licPadre && e.LIE_EMP_Id == empId);
                int ajustePadre = exc?.LIE_CorreccionFacturacion ?? 0;
                if (exc != null && ajustePadre == 0)
                    continue;

                var agrupPorHija = grp
                    .GroupBy(x => x.LIC_Id)
                    .Select(g => new { LicHija = g.Key, Count = g.Count() });

                foreach (var sub in agrupPorHija)
                {
                    int licHija = sub.LicHija;
                    int qtyAsign = sub.Count + ajustePadre;
                    if (qtyAsign <= 0)
                        continue;

                    var reemplazo = repl.FirstOrDefault(r =>
                        r.LEL_LIE_LIC_Id == licPadre && r.LEL_LIE_EMP_Id == empId);

                    int actualLic = (reemplazo != null && reemplazo.LEL_LIC_Id_Reemplazo != 0)
                                    ? reemplazo.LEL_LIC_Id_Reemplazo
                                    : licHija;

                    if (!licAll.TryGetValue(actualLic, out var lic) || lic.LIC_Gestionado != true)
                        continue;

                    // Aplicar tarifa de padre si existe, sino de la propia licencia
                    int licParaTarifa = padreMap.TryGetValue(actualLic, out var padre) ? padre : actualLic;
                    var tarifa = tarifas.FirstOrDefault(t =>
                        t.LIT_LIC_Id == licParaTarifa &&
                        t.LIT_FechaInicio <= hoy &&
                        (t.LIT_FechaFin == null || t.LIT_FechaFin > hoy));

                    if (tarifa == null)
                    {
                        string nombreLic = licAll.TryGetValue(licParaTarifa, out var licencia)
                            ? licencia.LIC_Nombre
                            : $"ID {licParaTarifa}";

                        erroresPorEmpresa.Add(
                            $"No existe tarifa vigente para la licencia “{nombreLic}” en la empresa “{empDict[empId].EMP_Nombre}”."
                        );
                        continue;
                    }

                    // Helper para añadir un detalle solo si entId existe en entesDict
                    void AddDet(int tareaId, int licenciaId, int entIdLocal, decimal precioUnit, int cantidad)
                    {
                        if (precioUnit <= 0 || cantidad <= 0)
                            return;

                        // ← Aquí aseguramos que entIdLocal esté en la tabla Entes
                        if (!entesDict.ContainsKey(entIdLocal))
                            return;

                        var key = (empId, tareaId);
                        if (!detallesPorConcepto.TryGetValue(key, out var list))
                        {
                            list = new List<(int, int, decimal)>();
                            detallesPorConcepto[key] = list;
                        }

                        for (int i = 0; i < cantidad; i++)
                        {
                            list.Add((licenciaId, entIdLocal, precioUnit));
                        }
                    }

                    AddDet(lic.LIC_TAR_Id_Antivirus ?? 0, actualLic, grp.First().ENT_Id,
                           tarifa.LIT_PrecioUnitarioAntivirus ?? 0, qtyAsign);

                    AddDet(lic.LIC_TAR_Id_Backup ?? 0, actualLic, grp.First().ENT_Id,
                           tarifa.LIT_PrecioUnitarioBackup ?? 0, qtyAsign);

                    AddDet(lic.LIC_TAR_Id_SW ?? 0, actualLic, grp.First().ENT_Id,
                           tarifa.LIT_PrecioUnitarioSW, qtyAsign);
                }
            }

            // 3) Ajuste mínimo anual (basado en padre, no en hijo)
            AplicarMinimoFacturar(detallesPorConcepto, periodo);

            // 4) Recalcular importes y conteos
            var importeTotal = new Dictionary<(int empId, int tId), decimal>();
            var licCounts = new Dictionary<(int empId, int tId), List<string>>();

            var dalMS = new DAL_Tareas_Empresas_LineasEsfuerzo_LicenciasMS();
            var todasLineasMS = dalMS.L(false, null)
                                   .Select(ms => ms.TCL_TLE_Id)
                                   .ToHashSet();

            foreach (var kv in detallesPorConcepto)
            {
                var key = kv.Key;
                var lista = kv.Value;

                importeTotal[key] = lista.Sum(x => x.importe);

                // Agrupar por licencia padre + flag ajuste
                var group = lista.GroupBy(d =>
                {
                    int padreId = padreMap.TryGetValue(d.licId, out var p) ? p : d.licId;
                    var nombre = licAll[padreId].LIC_Nombre;
                    bool isAjuste = d.entId == 0;
                    return (nombre, isAjuste);
                });

                var texts = group.Select(g =>
                {
                    var (nombre, isAjuste) = g.Key;
                    int count = g.Count();
                    return isAjuste
                        ? $"{count}-{nombre} (ajuste)"
                        : $"{count}-{nombre}";
                }).ToList();

                licCounts[key] = texts;
            }

            // 5) Construir preview
            var resultado = new List<ConceptoPreviewRow>();
            foreach (var key in importeTotal.Keys)
            {
                var (empId, tId) = key;
                decimal impTot = importeTotal[key];

                // AHORA: comprobamos también que haya al menos una línea MS para ese TLE_Id
                var conceptoExistente = todosConc
                    .FirstOrDefault(c =>
                        c.TLE_EMP_Id == empId
                     && c.TLE_TAR_Id == tId
                     && c.TLE_Anyo == anyoConceptos
                     && c.TLE_Mes == mesConceptos);

                if (conceptoExistente != null
                    && todasLineasMS.Contains(conceptoExistente.TLE_Id))
                {
                    // ya había un Tareas_Empresas_LineasEsfuerzo (concepto)
                    // y además tiene al menos una línea en LicenciasMS
                    continue;
                }

                // Validar asignación TEM
                if (!tareasEmp.Any(te =>
                        te.TEM_EMP_Id == empId &&
                        te.TEM_TAR_Id == tId &&
                        te.TEM_Anyo == anyoConceptos))
                {
                    var objTarea = new DAL_Tareas().L(false, null).First(i => i.TAR_Id == tId);
                    erroresPorEmpresa.Add(
                        $"Sin asignación de '{objTarea.TAR_Nombre}' para la empresa “{empDict[empId].EMP_Nombre}” en {anyoConceptos}."
                    );
                    continue;
                }

                var licText = string.Join(", ", licCounts[key]);
                var objTarea1 = new DAL_Tareas().L(false, null).First(i => i.TAR_Id == tId);

                resultado.Add(new ConceptoPreviewRow
                {
                    TAR_Id = tId,
                    TAR_Nombre = objTarea1.TAR_Nombre,
                    EmpresaId = empId,
                    EmpresaNombre = empDict[empId].EMP_Nombre,
                    Anyo = anyoConceptos,
                    Mes = mesConceptos,
                    ImporteTotal = impTot,
                    LicenciasIncluidas = licText
                });
            }

            if (erroresPorEmpresa.Any())
                return (new List<ConceptoPreviewRow>(), erroresPorEmpresa);

            return (resultado, new List<string>());
        }

        private List<SoportePreviewDto> GenerarPreviewSoporteCAU(DateTime periodo)
        {
            var hoy = DateTime.Today;
            int anyo = periodo.Year, mes = periodo.Month;

            // 1) IDs de tickets ya facturados
            var daoDetickets = new DAL_Tareas_Empresas_LineasEsfuerzo_Tickets();
            var ticketsYaFacturadosIds = daoDetickets
                .L(true, null)
                .Select(d => d.TCT_TKC_Id)
                .ToHashSet();

            // 2) Todos los tickets del sistema
            var todosTickets = new DAL_Tickets().L(false, null);

            // 3) Filtrar los pendientes de facturar
            var ticketsPendientes = todosTickets
                .Where(t => !ticketsYaFacturadosIds.Contains(t.TKC_Id))
                .ToList();

            // 4) Diccionarios de entes y empresas
            var entesDict = new DAL_Entes().L(false, null).ToDictionary(e => e.ENT_Id);
            var empDict = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id);

            // 5) Contrato CAU vigente hoy
            var contratoCAU = new DAL_ContratosCAU()
                .L(false, null)
                .FirstOrDefault(c => c.CCA_FechaInicio <= hoy && c.CCA_FechaFin >= hoy)
                ?? throw new Exception("No hay contrato CAU vigente");

            // 6) Conceptos ya creados este mes/año (para evitar duplicados en la preview)
            var conceptosExist = new DAL_Tareas_Empresas_LineasEsfuerzo()
                .L(true, null)
                .Where(c => c.TLE_Anyo == anyo && c.TLE_Mes == mes)
                .ToList();

            // 7) Agrupar tickets pendientes por empresa solicitante (solo si la empresa está definida)
            var gruposPorEmpresa = ticketsPendientes
                .Where(t =>
                    entesDict.ContainsKey(t.TKC_ENT_Id_Solicitante) &&
                    entesDict[t.TKC_ENT_Id_Solicitante].ENT_EMP_Id.HasValue
                )
                .GroupBy(t => entesDict[t.TKC_ENT_Id_Solicitante].ENT_EMP_Id.Value);

            // 8) Diccionario para acumular los DTOs de preview por categoría
            var salida = new Dictionary<CategoriaTicket, SoportePreviewDto>();

            // ——————————————————————
            // FASE A: DENTRO, FUERA, SOFTWARE y GUARDIA POR HORAS
            // ——————————————————————
            foreach (var grupo in gruposPorEmpresa)
            {
                int empId = grupo.Key;

                // 8.1) Sumar duración en minutos para cada categoría por separado
                int minutosDentro = grupo
                    .Where(t => t.TKC_CTK_Id == (int)CategoriaTicket.DentroDeAlcance)
                    .Sum(t => t.TKC_Duracion);

                int minutosFuera = grupo
                    .Where(t => t.TKC_CTK_Id == (int)CategoriaTicket.FueraDeAlcance)
                    .Sum(t => t.TKC_Duracion);

                int minutosGuardiaHoras = grupo
                    .Where(t => t.TKC_CTK_Id == (int)CategoriaTicket.FueraDeAlcanceGuardia)
                    .Sum(t => t.TKC_Duracion);

                int minutosSoftware = grupo
                    .Where(t => t.TKC_CTK_Id == (int)CategoriaTicket.Software)
                    .Sum(t => t.TKC_Duracion);

                // 8.2) Convertimos minutos a horas decimales (cada categoría por separado)
                decimal horasDentro = minutosDentro / 60m;
                decimal horasFuera = minutosFuera / 60m;
                decimal horasGuardiaHoras = minutosGuardiaHoras / 60m;
                decimal horasSoftware = minutosSoftware / 60m;

                // 8.3) Preparamos los bloques D, F, S y Guardia por horas
                var bloques = new List<(CategoriaTicket cat, int tarId, decimal horas, decimal costeHora, string nombre)>()
                {
                    (CategoriaTicket.DentroDeAlcance,       contratoCAU.CCA_TAR_Id_D, horasDentro,      contratoCAU.CCA_CosteHoraD,   "Dentro de alcance"),
                    (CategoriaTicket.FueraDeAlcance,        contratoCAU.CCA_TAR_Id_F, horasFuera,       contratoCAU.CCA_CosteHoraF,   "Fuera de alcance"),
                    (CategoriaTicket.Software,              contratoCAU.CCA_TAR_Id_S, horasSoftware,     contratoCAU.CCA_CosteHoraS,   "Software"),
                    (CategoriaTicket.FueraDeAlcance, contratoCAU.CCA_TAR_Id_G, horasGuardiaHoras, contratoCAU.CCA_CosteHoraG,  "Fuera de alcance")
                };

                foreach (var (cat, tarId, horas, costeHora, nombreCat) in bloques)
                {
                    // 8.4) Solo incluimos si horas > 0
                    if (horas <= 0)
                        continue;

                    // 8.5) Validar que exista tarifa
                    if (tarId <= 0)
                        throw new ApplicationException(
                            $"Contrato CAU no tiene TAR_Id para la categoría {cat}."
                        );

                    // 8.6) Evitar duplicar si ya existe concepto para esta empresa, misma tarifa, mismo mes/año
                    if (conceptosExist.Any(c =>
                        c.TLE_EMP_Id == empId &&
                        c.TLE_TAR_Id == tarId &&
                        c.TLE_Anyo == anyo &&
                        c.TLE_Mes == mes))
                    {
                        continue;
                    }

                    // 8.7) Crear el DTO de preview para esta categoría si no existe aún
                    if (!salida.TryGetValue(cat, out var dto))
                    {
                        dto = new SoportePreviewDto
                        {
                            CategoriaNombre = nombreCat,
                            Filas = new List<ConceptoPreviewRow>()
                        };
                        salida[cat] = dto;
                    }

                    // 8.8) Añadir la fila con ImporteTotal = horas * costeHora
                    dto.Filas.Add(new ConceptoPreviewRow
                    {
                        TAR_Id = tarId,
                        TAR_Nombre = nombreCat,
                        EmpresaId = empId,
                        EmpresaNombre = empDict[empId].EMP_Nombre,
                        Anyo = anyo,
                        Mes = mes,
                        ImporteTotal = Math.Round(horas * costeHora, 2)
                    });
                }
            }

            // ——————————————————————————————————————————————————————————
            // FASE B: REPARTO DEL IMPORTE FIJO DE GUARDIA ENTRE 3 ENTIDADES (con múltiples subgrupos B)
            // ——————————————————————————————————————————————————————————

            // 9) Importe fijo de Guardia
            decimal precioGuardiaTotal = contratoCAU.CCA_PrecioGuardia;
            if (precioGuardiaTotal > 0)
            {
                // 10) Grupo A = empresas con EMP_ExcluidaGuardia == false
                var grupoA = empDict.Values.Where(e => !e.EMP_ExcluidaGuardia).ToList();

                // 11) Grupo B: se subdivide en subgrupos según EMP_GRG_Id, pero solo aquellas que 
                //     tienen EMP_ExcluidaGuardia == true. Cada valor distinto de EMP_GRG_Id forma un subgrupo B_i.
                var gruposB = empDict.Values
                    .Where(e => e.EMP_ExcluidaGuardia && e.EMP_GRG_Id.HasValue)
                    .GroupBy(e => e.EMP_GRG_Id.Value)
                    .ToList();

                // 12) Filtrar conceptos Guardia ya existentes para este mes/año
                var conceptosGuardiaExist = conceptosExist
                    .Where(c =>
                        c.TLE_TAR_Id == contratoCAU.CCA_TAR_Id_G &&
                        c.TLE_Anyo == anyo &&
                        c.TLE_Mes == mes
                    )
                    .Select(c => c.TLE_EMP_Id)
                    .ToHashSet();

                // 13) Total de “entidades” que recibirán 1 parte entera:
                //     - Cada empresa de grupoA → 1 entidad propia
                //     - Cada subgrupo de gruposB → 1 entidad conjunta
                int entidadesA = grupoA.Count;
                int entidadesB = gruposB.Count;
                int totalEntidades = entidadesA + entidadesB;
                if (totalEntidades > 0)
                {
                    // 14) Cada “entidad” recibe:
                    decimal porEntidad = Math.Round(precioGuardiaTotal / totalEntidades, 2);

                    // 15) Asignar porEntidad a cada empresa de grupoA (si no tiene ya concepto Guardia)
                    foreach (var empresa in grupoA)
                    {
                        int empId = empresa.EMP_Id;
                        if (conceptosGuardiaExist.Contains(empId))
                            continue;

                        var catGuardia = CategoriaTicket.FueraDeAlcanceGuardia;
                        string nombreGuardia = "Fuera de alcance - Guardia";
                        if (!salida.TryGetValue(catGuardia, out var dtoG))
                        {
                            dtoG = new SoportePreviewDto
                            {
                                CategoriaNombre = nombreGuardia,
                                Filas = new List<ConceptoPreviewRow>()
                            };
                            salida[catGuardia] = dtoG;
                        }

                        dtoG.Filas.Add(new ConceptoPreviewRow
                        {
                            TAR_Id = contratoCAU.CCA_TAR_Id_G,
                            TAR_Nombre = nombreGuardia,
                            EmpresaId = empId,
                            EmpresaNombre = empDict[empId].EMP_Nombre,
                            Anyo = anyo,
                            Mes = mes,
                            ImporteTotal = porEntidad
                        });
                    }

                    // 16) Asignar la parte de cada subgrupo B entre sus miembros
                    foreach (var subgrupo in gruposB)
                    {
                        int miembros = subgrupo.Count();
                        if (miembros == 0) continue;

                        decimal porMiembro = Math.Round(porEntidad / miembros, 2);
                        string nombreGuardia = "CAU Guardia Fija";

                        foreach (var empresa in subgrupo)
                        {
                            int empId = empresa.EMP_Id;
                            if (conceptosGuardiaExist.Contains(empId))
                                continue;

                            var catGuardia = CategoriaTicket.FueraDeAlcanceGuardia;
                            if (!salida.TryGetValue(catGuardia, out var dtoG))
                            {
                                dtoG = new SoportePreviewDto
                                {
                                    CategoriaNombre = nombreGuardia,
                                    Filas = new List<ConceptoPreviewRow>()
                                };
                                salida[catGuardia] = dtoG;
                            }

                            dtoG.Filas.Add(new ConceptoPreviewRow
                            {
                                TAR_Id = contratoCAU.CCA_TAR_Id_G,
                                TAR_Nombre = nombreGuardia,
                                EmpresaId = empId,
                                EmpresaNombre = empDict[empId].EMP_Nombre,
                                Anyo = anyo,
                                Mes = mes,
                                ImporteTotal = porMiembro
                            });
                        }
                    }
                }
            }

            // 17) Devolver la lista de DTOs ordenada por valor de la enumeración
            return salida
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => kvp.Value)
                .ToList();
        }

        private List<GenerarResultDto> EjecutarGenerarConceptosLicencias(DateTime periodo)
        {
            int anyoConceptos = periodo.Year;
            int mesConceptos = periodo.Month;
            var hoy = DateTime.Today;

            // 1) Datos base
            var entesLic = new DAL_Entes_Licencias().L(false, null);
            var entesDict = new DAL_Entes().L(false, null).ToDictionary(e => e.ENT_Id);
            var empDict = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id);
            var licAll = new DAL_Licencias().L(false, null).ToDictionary(l => l.LIC_Id);
            var padreMap = licAll
                .Where(kv => kv.Value.LIC_LIC_Id_Padre.HasValue)
                .ToDictionary(kv => kv.Key, kv => kv.Value.LIC_LIC_Id_Padre.Value);
            var tarifas = new DAL_Licencias_Tarifas().L(false, null);
            var excs = new DAL_Licencias_Excepciones().L(false, null);
            var repl = new DAL_Licencias_Excepciones_LicenciasReemplazo().L(false, null);

            // 2) Conceptos existentes y validaciones TEM
            var dalConc = new DAL_Tareas_Empresas_LineasEsfuerzo();
            var todosConceptos = dalConc.L(true, null).ToList();
            var tareasEmp = new DAL_Tareas_Empresas().L(false, null);

            // 3) Acumulador de detalles por (empresa, tarea)
            //    Nota: cada entrada es List<(licId, entId, importe)>
            var detallesPorConcepto = new Dictionary<(int empId, int tId), List<(int licId, int entId, decimal importe)>>();

            // 4) Recorrer asignaciones agrupadas por (licenciaPadre, empresa)
            var agrupados = entesLic
                .Where(el => entesDict.ContainsKey(el.ENL_ENT_Id) && entesDict[el.ENL_ENT_Id].ENT_EMP_Id.HasValue)
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

            foreach (var grupo in agrupados)
            {
                int licPadre = grupo.Key.Padre;
                int empId = grupo.Key.EMP_Id;

                // Excepción de corrección
                var exc = excs.FirstOrDefault(e => e.LIE_LIC_Id == licPadre && e.LIE_EMP_Id == empId);
                if (exc != null && exc.LIE_CorreccionFacturacion == 0)
                    continue;
                int ajustePadre = exc?.LIE_CorreccionFacturacion ?? 0;

                foreach (var asign in grupo)
                {
                    int origLic = asign.LIC_Id;
                    int entId = asign.ENT_Id;

                    // Licencias efectivas con reemplazos
                    var licFact = repl
                        .Where(r => r.LEL_LIE_LIC_Id == licPadre && r.LEL_LIE_EMP_Id == empId)
                        .Select(r => r.LEL_LIC_Id_Reemplazo)
                        .Distinct()
                        .DefaultIfEmpty(origLic)
                        .Select(id => id == 0 ? origLic : id)
                        .Distinct();

                    foreach (var actualLic in licFact)
                    {
                        if (!licAll.TryGetValue(actualLic, out var lic) || lic.LIC_Gestionado != true)
                            continue;

                        // Si actualLic tiene padre, aplicamos la tarifa del padre; si no, la suya
                        int licParaTarifa = padreMap.TryGetValue(actualLic, out var padre) ? padre : actualLic;
                        var tarifa = tarifas.FirstOrDefault(t =>
                            t.LIT_LIC_Id == licParaTarifa &&
                            t.LIT_FechaInicio <= hoy &&
                            (t.LIT_FechaFin == null || t.LIT_FechaFin > hoy));
                        if (tarifa == null) continue;

                        // Helper para añadir un detalle: 
                        //   los detalles sólo se añaden si 'entId' existe en entesDict (clave foránea válida)
                        void AddDetalle(int tareaId, int licenciaId, int entIdLocal, decimal precioUnit)
                        {
                            if (precioUnit <= 0) return;

                            // Filtrar aquellos entId que no existan en tabla Entes
                            if (!entesDict.ContainsKey(entIdLocal))
                                return;

                            var key = (empId, tareaId);
                            if (!detallesPorConcepto.TryGetValue(key, out var list))
                            {
                                list = new List<(int, int, decimal)>();
                                detallesPorConcepto[key] = list;
                            }
                            int count = 1 + ajustePadre;
                            for (int i = 0; i < count; i++)
                                list.Add((licenciaId, entIdLocal, precioUnit));
                        }

                        if (lic.LIC_TAR_Id_Antivirus.HasValue)
                            AddDetalle(lic.LIC_TAR_Id_Antivirus.Value, actualLic, entId, tarifa.LIT_PrecioUnitarioAntivirus ?? 0m);
                        if (lic.LIC_TAR_Id_Backup.HasValue)
                            AddDetalle(lic.LIC_TAR_Id_Backup.Value, actualLic, entId, tarifa.LIT_PrecioUnitarioBackup ?? 0m);
                        if (lic.LIC_TAR_Id_SW.HasValue)
                            AddDetalle(lic.LIC_TAR_Id_SW.Value, actualLic, entId, tarifa.LIT_PrecioUnitarioSW);
                    }
                }
            }

            // 5) Aplicar mínimo anual (basado en padre, no en hijo)
            AplicarMinimoFacturar(detallesPorConcepto, periodo);

            // 6) Crear o actualizar conceptos
            var resumen = new Dictionary<int, int>();
            var conceptosGen = new Dictionary<(int empId, int tId), int>();

            var dalMS = new DAL_Tareas_Empresas_LineasEsfuerzo_LicenciasMS();
            var todasLineasMS = dalMS.L(false, null)
                                   .Select(ms => ms.TCL_TLE_Id)
                                   .ToHashSet();

            foreach (var kvp in detallesPorConcepto)
            {
                var key = kvp.Key;
                int empId = key.empId;
                int tId = key.tId;
                var detalles = kvp.Value;

                // Validación TEM
                if (!tareasEmp.Any(te => te.TEM_EMP_Id == empId && te.TEM_TAR_Id == tId && te.TEM_Anyo == anyoConceptos))
                    throw new ApplicationException($"No hay asignación de tarea {tId} para empresa {empId} en {anyoConceptos}.");

                decimal sumaImportes = detalles.Sum(d => d.importe);
                if (sumaImportes <= 0) continue;

                // Construir texto de licencias incluidas
                var grupoTexto = detalles
                    .GroupBy(d =>
                    {
                        int padreId = padreMap.TryGetValue(d.licId, out var p) ? p : d.licId;
                        var nombre = licAll[padreId].LIC_Nombre;
                        bool isAjuste = d.entId == 0;
                        return (nombre, isAjuste);
                    })
                    .Select(g =>
                    {
                        var (nombre, isAjuste) = g.Key;
                        int count = g.Count();
                        return isAjuste
                            ? $"{count}-{nombre} (ajuste)"
                            : $"{count}-{nombre}";
                    });

                StringBuilder sbLicText = new StringBuilder();
                sbLicText.AppendLine("Licencias incluidas " + anyoConceptos + "-" + mesConceptos + ": ");
                foreach (string texto in grupoTexto)
                {
                    sbLicText.AppendLine(texto);
                }

                int tleId;
                if (!conceptosGen.TryGetValue(key, out tleId))
                {
                    // AHORA: comprobamos también que haya al menos una línea MS para ese TLE_Id
                    var conceptoExistente = todosConceptos
                        .FirstOrDefault(c =>
                            c.TLE_EMP_Id == empId
                         && c.TLE_TAR_Id == tId
                         && c.TLE_Anyo == anyoConceptos
                         && c.TLE_Mes == mesConceptos);

                    if (conceptoExistente != null
                        && todasLineasMS.Contains(conceptoExistente.TLE_Id))
                    {
                        // ya había un Tareas_Empresas_LineasEsfuerzo (concepto)
                        // y además tiene al menos una línea en LicenciasMS
                        continue;
                    }

                    // Crear nuevo concepto
                    var nuevo = new Tareas_Empresas_LineasEsfuerzo
                    {
                        TLE_TAR_Id = tId,
                        TLE_EMP_Id = empId,
                        TLE_Anyo = anyoConceptos,
                        TLE_Mes = mesConceptos,
                        TLE_Cantidad = (int)Math.Round(sumaImportes),
                        TLE_Descripcion = sbLicText.ToString(),
                        TLE_ESO_Id = (int)Constantes.EstadosSolicitud.PendienteAprobacion,
                        TLE_ESO_Id_Original = (int)Constantes.EstadosSolicitud.SinSolicitar,
                        TLE_PER_Id_Aprobador = empDict[empId].EMP_PER_Id_AprobadorDefault,
                        TLE_FechaAprobacion = DateTime.Now,
                        TLE_ComentarioAprobacion = Resources.Resource.AprobadoAutomatico,
                        FechaAlta = DateTime.Now,
                        FechaModificacion = DateTime.Now,
                        PER_Id_Modificacion = Sesion.SPersonaId,
                        TLE_Inversion = false
                    };
                    new DAL_Tareas_Empresas_LineasEsfuerzo().G(nuevo, Sesion.SPersonaId);
                    tleId = nuevo.TLE_Id;
                    resumen[empId] = resumen.TryGetValue(empId, out var val) ? val + 1 : 1;

                    conceptosGen[key] = tleId;
                }

                // 7) Persistir líneas de detalle SOLO con entId válido
                foreach (var det in detalles)
                {
                    // VERIFICAR QUE det.entId existe en la tabla Entes
                    if (!entesDict.ContainsKey(det.entId))
                        continue;

                    var ms = new Tareas_Empresas_LineasEsfuerzo_LicenciasMS
                    {
                        TCL_TLE_Id = tleId,
                        TCL_LIC_Id = det.licId,
                        TCL_ENT_Id = det.entId,
                        TCL_Importe = det.importe
                    };
                    dalMS.G(ms, Sesion.SPersonaId);
                }
            }

            // 8) Devolver resumen
            return resumen.Select(r => new GenerarResultDto
            {
                EmpresaId = r.Key,
                EmpresaNombre = empDict[r.Key].EMP_Nombre,
                ConceptosCreados = r.Value
            }).ToList();
        }

        private List<GenerarResultDto> EjecutarGenerarConceptosSoporteCAU(DateTime periodo)
        {
            var resumen = new Dictionary<int, int>();

            var hoy = DateTime.Today;
            int anyo = periodo.Year, mes = periodo.Month;

            var daoConceptos = new DAL_Tareas_Empresas_LineasEsfuerzo();

            // 1) Tickets ya facturados
            var daoDetickets = new DAL_Tareas_Empresas_LineasEsfuerzo_Tickets();
            var ticketsYaFacturadosIds = daoDetickets.L(true, null).Select(d => d.TCT_TKC_Id).ToHashSet();

            // 2) Todos los tickets y filtramos pendientes de facturar
            var todosTickets = new DAL_Tickets().L(false, null);
            var ticketsPendientes = todosTickets
                .Where(t => !ticketsYaFacturadosIds.Contains(t.TKC_Id))
                .ToList();

            // 3) Diccionarios de entes y empresas
            var entesDict = new DAL_Entes().L(false, null).ToDictionary(e => e.ENT_Id);
            var empDict = new DAL_Empresas().GetEmpresasGenerarConceptos().ToDictionary(e => e.EMP_Id);

            // 4) Contrato CAU vigente
            var contrato = new DAL_ContratosCAU().L(false, null)
                .FirstOrDefault(c => c.CCA_FechaInicio <= hoy && c.CCA_FechaFin >= hoy);

            if (contrato != null)
            {
                // 5) Conceptos ya creados este mes/año (para no duplicar)
                var daoConc = new DAL_Tareas_Empresas_LineasEsfuerzo();
                var conceptosExist = daoConc.L(true, null)
                    .Where(c => c.TLE_Anyo == anyo && c.TLE_Mes == mes)
                    .ToList();

                // 6) Agrupamos tickets pendientes por empresa solicitante
                var grupos = ticketsPendientes
                    .Where(t =>
                        entesDict.ContainsKey(t.TKC_ENT_Id_Solicitante) &&
                        entesDict[t.TKC_ENT_Id_Solicitante].ENT_EMP_Id.HasValue
                    )
                    .GroupBy(t => entesDict[t.TKC_ENT_Id_Solicitante].ENT_EMP_Id.Value);

                // 7) Recorremos cada “empresa” para generar conceptos D, F, S y Guardia por horas
                foreach (var grupo in grupos)
                {
                    int empId = grupo.Key;

                    // 7.1) Sumamos minutos de cada categoría:
                    int minutosDentro = grupo
                        .Where(t => t.TKC_CTK_Id == (int)CategoriaTicket.DentroDeAlcance)
                        .Sum(t => t.TKC_Duracion);

                    int minutosFuera = grupo
                        .Where(t => t.TKC_CTK_Id == (int)CategoriaTicket.FueraDeAlcance)
                        .Sum(t => t.TKC_Duracion);

                    int minutosGuardiaHoras = grupo
                        .Where(t => t.TKC_CTK_Id == (int)CategoriaTicket.FueraDeAlcanceGuardia)
                        .Sum(t => t.TKC_Duracion);

                    int minutosSoftware = grupo
                        .Where(t => t.TKC_CTK_Id == (int)CategoriaTicket.Software)
                        .Sum(t => t.TKC_Duracion);

                    // 7.2) Convertimos a horas decimales:
                    decimal horasDentro = minutosDentro / 60m;
                    decimal horasFuera = minutosFuera / 60m;
                    decimal horasGuardiaHoras = minutosGuardiaHoras / 60m;
                    decimal horasSoft = minutosSoftware / 60m;

                    // 7.3) Bloques D, F, S y Guardia por horas
                    var bloques = new List<(CategoriaTicket cat, int tarId, decimal horas, decimal costeHora, string nombre)>()
                    {
                        (CategoriaTicket.DentroDeAlcance,    contrato.CCA_TAR_Id_D, horasDentro,      contrato.CCA_CosteHoraD,   "Dentro de alcance"),
                        (CategoriaTicket.FueraDeAlcance,     contrato.CCA_TAR_Id_F, horasFuera,       contrato.CCA_CosteHoraF,   "Fuera de alcance"),
                        (CategoriaTicket.Software,           contrato.CCA_TAR_Id_S, horasSoft,        contrato.CCA_CosteHoraS,   "Software"),
                        // Para “Guardia por horas” usamos misma categoría que “Fuera de alcance” pero con otra tarifa/CosteHoraG
                        (CategoriaTicket.FueraDeAlcanceGuardia, contrato.CCA_TAR_Id_G, horasGuardiaHoras, contrato.CCA_CosteHoraG, "Fuera de alcance - Guardia")
                    };

                    foreach (var (cat, tarId, horas, costeHora, nombreCat) in bloques)
                    {
                        // a) Si la cantidad de horas <= 0, saltamos
                        if (horas <= 0) continue;

                        // b) Validar que exista tarifa
                        if (tarId <= 0)
                            throw new ApplicationException(
                                $"Contrato CAU no tiene TAR_Id para la categoría {cat}."
                            );

                        // c) Evitar duplicar conceptos ya creados este mes/año
                        if (conceptosExist.Any(c =>
                            c.TLE_EMP_Id == empId &&
                            c.TLE_TAR_Id == tarId &&
                            c.TLE_Anyo == anyo &&
                            c.TLE_Mes == mes))
                        {
                            continue;
                        }

                        // d) Calculamos el importe total: horas * costeHora
                        decimal importeTotal = Math.Round(horas * costeHora, 2);

                        // e) Insertar cabecera en Tareas_Empresas_LineasEsfuerzo,
                        //    guardando en TLE_Cantidad el importe total en lugar de las horas:
                        var cab = new Tareas_Empresas_LineasEsfuerzo
                        {
                            TLE_EMP_Id = empId,
                            TLE_Anyo = anyo,
                            TLE_Mes = mes,
                            TLE_TAR_Id = tarId,
                            TLE_Cantidad = importeTotal, // <--- Aquí guardamos el importe total
                            TLE_Descripcion = $"Generado Auto {nombreCat} " + anyo + "-" + mes,
                            TLE_ESO_Id = (int)Constantes.EstadosSolicitud.PendienteAprobacion,
                            TLE_ESO_Id_Original = (int)Constantes.EstadosSolicitud.SinSolicitar,
                            TLE_PER_Id_Aprobador = empDict[empId].EMP_PER_Id_AprobadorDefault,
                            TLE_FechaAprobacion = DateTime.Now,
                            TLE_ComentarioAprobacion = "Aprobado automáticamente",
                            FechaAlta = DateTime.Now,
                            FechaModificacion = DateTime.Now,
                            PER_Id_Modificacion = Sesion.SPersonaId,
                            TLE_Inversion = false
                        };
                        daoConc.G(cab, Sesion.SPersonaId);

                        // f) Insertar detalle en Tareas_Empresas_LineasEsfuerzo_Tickets 
                        //    (seguimos grabando el importe de cada ticket individual en TCT_Importe)
                        foreach (var t in grupo)
                        {
                            bool incluir =
                                (cat == CategoriaTicket.DentroDeAlcance && t.TKC_CTK_Id == (int)CategoriaTicket.DentroDeAlcance) ||
                                (cat == CategoriaTicket.FueraDeAlcance && t.TKC_CTK_Id == (int)CategoriaTicket.FueraDeAlcance) ||
                                (cat == CategoriaTicket.Software && t.TKC_CTK_Id == (int)CategoriaTicket.Software) ||
                                (cat == CategoriaTicket.FueraDeAlcanceGuardia && t.TKC_CTK_Id == (int)CategoriaTicket.FueraDeAlcanceGuardia);

                            if (!incluir) continue;

                            decimal horasTicket = t.TKC_Duracion / 60m;
                            decimal importeTicket = Math.Round(horasTicket * costeHora, 2);

                            var det = new Tareas_Empresas_LineasEsfuerzo_Tickets
                            {
                                TCT_TLE_Id = cab.TLE_Id,
                                TCT_TKC_Id = t.TKC_Id,
                                TCT_Importe = importeTicket
                            };
                            daoDetickets.G(det, Sesion.SPersonaId);
                        }

                        // g) Acumular para el resumen (conteo de conceptos creados por empresa)
                        if (!resumen.ContainsKey(empId))
                            resumen[empId] = 0;
                        resumen[empId]++;
                    }
                }

                // ——————————————————————————————————————————————————————————
                // FASE B: REPARTO DEL IMPORTE FIJO DE GUARDIA ENTRE 3 ENTIDADES (con subgrupos B por EMP_GRG_Id)
                // ——————————————————————————————————————————————————————————
                decimal precioGuardiaTotalFijo = contrato.CCA_PrecioGuardia;
                if (precioGuardiaTotalFijo > 0)
                {
                    // 1) Grupo A = empresas con EMP_ExcluidaGuardia == false
                    var grupoA_Emp = empDict.Values
                        .Where(e => !e.EMP_ExcluidaGuardia)
                        .ToList();

                    // 2) Grupo B: subdividir en subgrupos según EMP_GRG_Id, solo con EMP_ExcluidaGuardia == true
                    var gruposB = empDict.Values
                        .Where(e => e.EMP_ExcluidaGuardia && e.EMP_GRG_Id.HasValue)
                        .GroupBy(e => e.EMP_GRG_Id.Value)
                        .ToList();

                    // 3) Conceptos Guardia ya existentes este mes/año
                    var conceptosGuardiaExist = conceptosExist
                        .Where(c =>
                            c.TLE_TAR_Id == contrato.CCA_TAR_Id_G &&
                            c.TLE_Anyo == anyo &&
                            c.TLE_Mes == mes
                        )
                        .Select(c => c.TLE_EMP_Id)
                        .ToHashSet();

                    // 4) Cada “entidad” contará como:
                    //    - Cada empresa en grupoA_Emp → 1 entidad
                    //    - Cada subgrupo en gruposB → 1 entidad
                    int entidadesA = grupoA_Emp.Count;
                    int entidadesB = gruposB.Count;
                    int totalEntidades = entidadesA + entidadesB;
                    if (totalEntidades > 0)
                    {
                        // 5) Cada entidad recibe:
                        decimal porEntidad = Math.Round(precioGuardiaTotalFijo / totalEntidades, 2);

                        // 6) Asignar porEntidad a cada empresa de grupoA_Emp (si no tiene concepto Guardia)
                        foreach (var empresa in grupoA_Emp)
                        {
                            int empId = empresa.EMP_Id;
                            if (conceptosGuardiaExist.Contains(empId))
                                continue;

                            // Tomar, si existe, el primer ticket de tipo Guardia de esta empresa
                            var primerTicketGuardia = ticketsPendientes
                                .FirstOrDefault(t =>
                                    t.TKC_CTK_Id == (int)CategoriaTicket.FueraDeAlcanceGuardia &&
                                    entesDict[t.TKC_ENT_Id_Solicitante].ENT_EMP_Id == empId
                                );
                            int ticketIdA = primerTicketGuardia?.TKC_Id ?? 0;

                            // Insertar cabecera para Guardia (TLE_Cantidad = 1, pero luego ajustamos importe en detalle)
                            var cabG = new Tareas_Empresas_LineasEsfuerzo
                            {
                                TLE_EMP_Id = empId,
                                TLE_Anyo = anyo,
                                TLE_Mes = mes,
                                TLE_TAR_Id = contrato.CCA_TAR_Id_G,
                                TLE_Cantidad = porEntidad, // <-- Guardamos aquí el importe fijo por entidad
                                TLE_Descripcion = "Generado Auto Fuera de alcance - Guardia " + anyo + "-" + mes,
                                TLE_ESO_Id = (int)Constantes.EstadosSolicitud.PendienteAprobacion,
                                TLE_ESO_Id_Original = (int)Constantes.EstadosSolicitud.SinSolicitar,
                                TLE_PER_Id_Aprobador = empDict[empId].EMP_PER_Id_AprobadorDefault,
                                TLE_FechaAprobacion = DateTime.Now,
                                TLE_ComentarioAprobacion = "Aprobado automáticamente",
                                FechaAlta = DateTime.Now,
                                FechaModificacion = DateTime.Now,
                                PER_Id_Modificacion = Sesion.SPersonaId,
                                TLE_Inversion = false
                            };
                            daoConceptos.G(cabG, Sesion.SPersonaId);

                            // Insertar el detalle con el importe por entidad
                            var detG_A = new Tareas_Empresas_LineasEsfuerzo_Tickets
                            {
                                TCT_TLE_Id = cabG.TLE_Id,
                                TCT_TKC_Id = ticketIdA,
                                TCT_Importe = porEntidad
                            };
                            daoDetickets.G(detG_A, Sesion.SPersonaId);

                            if (!resumen.ContainsKey(empId))
                                resumen[empId] = 0;
                            resumen[empId]++;
                        }

                        // 7) Para cada subgrupo B_i, repartir porEntidad entre sus miembros
                        foreach (var subgrupo in gruposB)
                        {
                            int miembros = subgrupo.Count();
                            if (miembros == 0) continue;

                            decimal porMiembro = Math.Round(porEntidad / miembros, 2);

                            foreach (var empresa in subgrupo)
                            {
                                int empId = empresa.EMP_Id;
                                if (conceptosGuardiaExist.Contains(empId))
                                    continue;

                                var primerTicketGuardia = ticketsPendientes
                                    .FirstOrDefault(t =>
                                        t.TKC_CTK_Id == (int)CategoriaTicket.FueraDeAlcanceGuardia &&
                                        entesDict[t.TKC_ENT_Id_Solicitante].ENT_EMP_Id == empId
                                    );
                                int ticketIdB = primerTicketGuardia?.TKC_Id ?? 0;

                                var cabG = new Tareas_Empresas_LineasEsfuerzo
                                {
                                    TLE_EMP_Id = empId,
                                    TLE_Anyo = anyo,
                                    TLE_Mes = mes,
                                    TLE_TAR_Id = contrato.CCA_TAR_Id_G,
                                    TLE_Cantidad = porMiembro, // <-- Guardamos aquí importe por miembro de subgrupo
                                    TLE_Descripcion = "Generado Auto Fuera de alcance - Guardia " + anyo + "-" + mes,
                                    TLE_ESO_Id = (int)Constantes.EstadosSolicitud.PendienteAprobacion,
                                    TLE_ESO_Id_Original = (int)Constantes.EstadosSolicitud.SinSolicitar,
                                    TLE_PER_Id_Aprobador = empDict[empId].EMP_PER_Id_AprobadorDefault,
                                    TLE_FechaAprobacion = DateTime.Now,
                                    TLE_ComentarioAprobacion = "Aprobado automáticamente",
                                    FechaAlta = DateTime.Now,
                                    FechaModificacion = DateTime.Now,
                                    PER_Id_Modificacion = Sesion.SPersonaId,
                                    TLE_Inversion = false
                                };
                                daoConceptos.G(cabG, Sesion.SPersonaId);

                                var detG_B = new Tareas_Empresas_LineasEsfuerzo_Tickets
                                {
                                    TCT_TLE_Id = cabG.TLE_Id,
                                    TCT_TKC_Id = ticketIdB,
                                    TCT_Importe = porMiembro
                                };
                                daoDetickets.G(detG_B, Sesion.SPersonaId);

                                if (!resumen.ContainsKey(empId))
                                    resumen[empId] = 0;
                                resumen[empId]++;
                            }
                        }
                    }
                }
            }

            // 8) Devolver lista con el resumen
            return resumen
                .Select(kvp => new GenerarResultDto
                {
                    EmpresaId = kvp.Key,
                    EmpresaNombre = empDict[kvp.Key].EMP_Nombre,
                    ConceptosCreados = kvp.Value
                })
                .ToList();
        }

        private void AplicarMinimoFacturar(Dictionary<(int empId, int tId), List<(int licId, int entId, decimal importe)>> detallesPorConcepto,
            DateTime periodo)
        {
            int año = periodo.Year;
            int mesActual = periodo.Month;
            var hoy = DateTime.Today;

            // -------------------------------------------------
            // 1) Cargar todos los mínimos existentes en BD
            // -------------------------------------------------
            var dalMinimos = new DAL_Licencias_Minimos();
            // Pedimos toda la tabla; cada fila: (LIC_Id, EMP_Id, MinimoFacturar)
            var listaMinimos = dalMinimos.L(false, null)
                .Select(m => new
                {
                    m.LEM_LIC_Id,
                    m.LEM_EMP_Id,
                    m.LEM_MinimoFacturar
                })
                .ToList();

            // Convertimos a diccionario para lookup rápido: (empId, licId_padre) -> MinimoFacturar
            var dictMinimos = listaMinimos
                .ToDictionary(x => (x.LEM_EMP_Id, x.LEM_LIC_Id), x => x.LEM_MinimoFacturar);

            // -------------------------------------------------
            // 2) Necesitamos también tarifas y datos de licencia
            // -------------------------------------------------
            var licAll = new DAL_Licencias().L(false, null).ToDictionary(l => l.LIC_Id);
            var tarifas = new DAL_Licencias_Tarifas().L(false, null);

            // -------------------------------------------------
            // 3) Recorremos cada clave (empresa, tarea) en detallesPorConcepto
            // -------------------------------------------------
            foreach (var clave in detallesPorConcepto.Keys.ToList())
            {
                int empId = clave.empId;
                int tId = clave.tId;
                var listaDetalle = detallesPorConcepto[clave];

                // 3.1) Contar cuántas licencias “reales” (entId != 0) hay este mes por cada licId original
                // Luego agrupar por la licencia padre
                var conteoPorParent = listaDetalle
                    .Where(d => d.entId != 0)
                    .GroupBy(d =>
                    {
                        // Determinamos el padre de d.licId
                        if (licAll[d.licId].LIC_LIC_Id_Padre.HasValue)
                            return licAll[d.licId].LIC_LIC_Id_Padre.Value;
                        else
                            return d.licId;
                    })
                    .ToDictionary(g => g.Key, g => g.Count());

                // 3.2) Para cada licPadre que aparece en el mes actual
                foreach (var kvLic in conteoPorParent)
                {
                    int licPadre = kvLic.Key;
                    int actualCount = kvLic.Value;

                    // 3.2.1) Buscamos el mínimo ya grabado para (empId, licPadre)
                    dictMinimos.TryGetValue((empId, licPadre), out int storedMin);

                    // 3.2.2) Si el consumo real supera el mínimo almacenado, lo actualizamos en BD
                    if (actualCount > storedMin)
                    {
                        // Creamos/actualizamos entidad Licencias_Minimos (solo en padre)
                        var entidadMin = new Licencias_Minimos
                        {
                            LEM_LIC_Id = licPadre,
                            LEM_EMP_Id = empId,
                            LEM_MinimoFacturar = actualCount
                        };
                        // El propio método Insert/Update en dalMinimos se encarga de reemplazar o insertar
                        dalMinimos.GuardarOModificar(entidadMin, Sesion.SPersonaId);

                        // También actualizamos nuestro diccionario en memoria:
                        storedMin = actualCount;
                        dictMinimos[(empId, licPadre)] = storedMin;
                    }

                    // 3.2.3) Si el consumo real es menor que el mínimo almacenado,
                    //        añadimos “deficit” entradas de ajuste (entId = 0, usando licPadre)
                    if (storedMin > actualCount)
                    {
                        int deficit = storedMin - actualCount;

                        // Para saber el precio unitario, obtenemos la licencia padre
                        if (!licAll.TryGetValue(licPadre, out var lic))
                            continue;

                        // 3.2.3.1) Recuperamos la tarifa vigente para esa licPadre y fecha “hoy”
                        var tarifa = tarifas
                            .FirstOrDefault(t => t.LIT_LIC_Id == licPadre
                                                 && t.LIT_FechaInicio <= hoy
                                                 && (t.LIT_FechaFin == null || t.LIT_FechaFin > hoy));
                        if (tarifa == null)
                            continue;

                        // 3.2.3.2) Para la tarea tId concreta, asignamos el precio unitario que corresponda:
                        decimal precioUnit = 0;
                        if (lic.LIC_TAR_Id_Antivirus.HasValue && tId == lic.LIC_TAR_Id_Antivirus.Value)
                            precioUnit = tarifa.LIT_PrecioUnitarioAntivirus ?? 0;
                        else if (lic.LIC_TAR_Id_Backup.HasValue && tId == lic.LIC_TAR_Id_Backup.Value)
                            precioUnit = tarifa.LIT_PrecioUnitarioBackup ?? 0;
                        else if (tId == lic.LIC_TAR_Id_SW)
                            precioUnit = tarifa.LIT_PrecioUnitarioSW;
                        else
                            precioUnit = 0;

                        if (precioUnit <= 0)
                            continue;

                        // 3.2.3.3) Insertamos N veces la tupla (licPadre, entId=0, precioUnit)
                        for (int i = 0; i < deficit; i++)
                        {
                            listaDetalle.Add((licPadre, 0, precioUnit));
                        }
                    }
                }
            }
        }
    }

    #region Impersonar
    public static class BLL_Impersonate
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LogonUser(
                string lpszUsername,
                string lpszDomain,
                string lpszPassword,
                int dwLogonType,
                int dwLogonProvider,
                out IntPtr phToken);

        public static WindowsImpersonationContext Impersonar(ImpersonateUser usuario)
        {
            IntPtr userToken = IntPtr.Zero;
            bool success = BLL_Impersonate.LogonUser(
                              usuario.User,
                              usuario.Domain,
                              usuario.Password,
                              2, //(int)AdvApi32Utility.LogonType.LOGON32_LOGON_INTERACTIVE, //2
                              0, //(int)AdvApi32Utility.LogonProvider.LOGON32_PROVIDER_DEFAULT, //0
                              out userToken);

            if (!success)
            {
                throw new Exception("LogonUser failed.");
            }
            else return WindowsIdentity.Impersonate(userToken);
        }

        public static WindowsImpersonationContext Impersonar()
        {
            CifradoDLL.CifradoAES descifrador = new CifradoDLL.CifradoAES();
            string lstrPass;
            descifrador.DescifrarStringHEX(System.Configuration.ConfigurationManager.AppSettings["LDAPPass"], out lstrPass);

            ImpersonateUser usuario = new ImpersonateUser(
                              System.Configuration.ConfigurationManager.AppSettings["LDAPUser"],
                              lstrPass,
                              System.Configuration.ConfigurationManager.AppSettings["LDAPDomain"]);
            return Impersonar(usuario);
        }
    }

    public class ImpersonateUser
    {
        public ImpersonateUser() { } //Para poder serializar la clase
        public ImpersonateUser(string pstrUser, string pstrPassword, string pstrDomain)
        {
            User = pstrUser;
            Password = pstrPassword;
            Domain = pstrDomain;
        }

        public string User;
        public string Password;
        public string Domain;
    }

    public class ADUserDTO
    {
        public string UsuarioRed { get; set; }        // sAMAccountName
        public string Nombre { get; set; }            // givenname
        public string Apellido { get; set; }          // sn
        public string NombreMostrado { get; set; }    // displayname
        public string Email { get; set; }             // mail
        public string Empresa { get; set; }           // company
        public string Departamento { get; set; }      // department
        public string Titulo { get; set; }            // title
        public string Oficina { get; set; }           // physicaldeliveryofficename
                                                      // etc.
    }
    #endregion

    public class SoportePreviewDto
    {
        /// <summary>Nombre de la categoría de ticket (p.ej. “Software”, “FueraDeAlcance”, etc.)</summary>
        public string CategoriaNombre { get; set; }

        /// <summary>Filas que se van a facturar bajo esta categoría</summary>
        public List<ConceptoPreviewRow> Filas { get; set; }
    }

    public class ConceptoPreviewRow
    {
        public int TAR_Id { get; set; }
        public string TAR_Nombre { get; set; }
        public int EmpresaId { get; set; }
        public string EmpresaNombre { get; set; }
        public int Anyo { get; set; }
        public int Mes { get; set; }
        public decimal ImporteTotal { get; set; }
        public string LicenciasIncluidas { get; set; }
        public int Cantidad { get; set; }
    }

    public class GenerarResultDto
    {
        public int EmpresaId { get; set; }
        public string EmpresaNombre { get; set; }
        public int ConceptosCreados { get; set; }
    }
}