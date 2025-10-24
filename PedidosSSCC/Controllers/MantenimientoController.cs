using AccesoDatos;
using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Web.Mvc;
using static PedidosSSCC.Comun.Constantes;

namespace PedidosSSCC.Controllers
{
    public class MantenimientoController : BaseController
    {
        public ActionResult Perfiles() => View();
        public ActionResult Usuarios() => View();
        public ActionResult Configuracion() => View();
        public ActionResult ProductosD365() => View();
        public ActionResult ItemNumbersD365() => View();
        public ActionResult Departamentos() => View();
        public ActionResult Empresas() => View();
        public ActionResult Licencias() => View();
        public ActionResult Oficinas() => View();
        public ActionResult TiposEnte() => View();
        public ActionResult Entes() => View();
        public ActionResult ContratosCAU() => View();
        public ActionResult GruposGuardia() => View();
        public ActionResult Proveedores() => View();
        public ActionResult ProveedoresAsuntos() => View();
        public ActionResult GetUsuarios() => View();

        [HttpGet]
        public ActionResult ObtenerPerfiles()
        {
            DAL_Seguridad_Perfiles dal = new DAL_Seguridad_Perfiles();
            List<Seguridad_Perfiles> lista = dal.L(false, null);

            // Seleccionar las propiedades deseadas
            var listaFiltro = lista
                .Select(i => new
                {
                    i.SPE_Id,
                    i.SPE_Nombre
                })
                .ToList();

            return new LargeJsonResult { Data = listaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerPermisos(int idPerfil)
        {
            DAL_Seguridad_Perfiles dal = new DAL_Seguridad_Perfiles();
            List<SeguridadPerfilesOpcionesDTO> lista = dal.GetPermisos(idPerfil);

            var listaFiltro = lista
                .Select(i => new
                {
                    i.SPO_SPE_Id,
                    i.SPO_SOP_Id,
                    i.SPO_Escritura,
                    i.SOI_Nombre
                })
                //.Where(i =>
                //    i.SPO_SOP_Id != "2.8" &&
                //    i.SPO_SOP_Id != "2.9" &&
                //    i.SPO_SOP_Id != "2.10" &&
                //    i.SPO_SOP_Id != "2.11")
                .ToList();

            return new LargeJsonResult { Data = listaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarPerfil(PerfilPermisosDTO objPerfilPermisos)
        {
            try
            {
                if (objPerfilPermisos == null)
                {
                    return Json(new { success = false, message = "Datos inválidos." });
                }

                DAL_Seguridad_Perfiles dal = new DAL_Seguridad_Perfiles();
                dal.ModificarPerfilesOpciones(objPerfilPermisos, Sesion.SPersonaId);

                LimpiarCache(TipoCache.Tareas, TipoCache.Conceptos, TipoCache.Pedidos);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarPerfil(int idPerfil)
        {
            try
            {
                DAL_Seguridad_Perfiles dal = new DAL_Seguridad_Perfiles();
                bool deleteOk = dal.EliminarPerfil(idPerfil);

                LimpiarCache(TipoCache.Tareas, TipoCache.Conceptos, TipoCache.Pedidos);

                return Json(new { success = deleteOk });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarItemNumber(int idItemNumber)
        {
            try
            {
                DAL_ItemNumbersD365 dal = new DAL_ItemNumbersD365();
                bool deleteOk = dal.D(idItemNumber);

                return Json(new { success = deleteOk });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerUsuarios()
        {
            DAL_Usuarios dal = new DAL_Usuarios();
            List<Usuarios> lista = dal.L(false, null);

            var listaFiltro = lista
                .Select(i => new
                {
                    i.USU_Id,
                    i.USU_PER_Id,
                    i.NombrePersona,
                    Email = i.PersonaUsuario.PER_Email,
                    IdPerfil = i.USU_SPE_Id,
                    Perfil = i.Seguridad_Perfiles.SPE_Nombre,
                    VerTodo = i.USU_VerTodo
                })
                .ToList();

            return new LargeJsonResult { Data = listaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerDepartamentos()
        {
            DAL_Departamentos dalDep = new DAL_Departamentos();
            DAL_Personas dalPer = new DAL_Personas();

            // Obtener la lista de departamentos
            List<Departamentos> listaDep = dalDep.L(false, null);

            // Obtener la lista de personas
            List<Personas> listaPer = dalPer.L(false, null);

            var listaFiltro = listaDep
                .Join(listaPer,
                      dep => dep.DEP_PER_Id_Responsable, // Clave de unión en Departamentos
                      per => per.PER_Id,                 // Clave de unión en Personas
                      (dep, per) => new
                      {
                          dep.DEP_Id,
                          dep.DEP_Codigo,
                          dep.DEP_Nombre,
                          dep.DEP_PER_Id_Responsable,
                          NombreResponsable = per.ApellidosNombre,  // Campo de Personas
                          dep.DEP_CodigoD365
                      })
                .OrderBy(i => i.DEP_Nombre)
                .ToList();

            return new LargeJsonResult { Data = listaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };

        }

        [HttpGet]
        public ActionResult ObtenerEmpresas()
        {
            DAL_Empresas dal = new DAL_Empresas();
            List<Empresas> lista = dal.L(false, null);

            DAL_LineasNegocio_Idioma dalLNI = new DAL_LineasNegocio_Idioma();
            List<LineasNegocio_Idioma> lineasNegocio = dalLNI.L(false, null);

            var listaFiltro = lista
                .GroupJoin(lineasNegocio,
                    empresa => empresa.EMP_LNE_Id,
                    linea => linea.LNI_LNE_Id,
                    (empresa, lineas) => new { empresa, lineas })
                .SelectMany(
                    x => x.lineas.DefaultIfEmpty(),
                    (x, linea) => new
                    {
                        x.empresa.EMP_Id,
                        x.empresa.EMP_Nombre,
                        x.empresa.EMP_NombreDA,
                        x.empresa.EMP_RazonSocial,
                        x.empresa.EMP_CIF,
                        x.empresa.EMP_Direccion,
                        x.empresa.EMP_LNE_Id,
                        x.empresa.EMP_PER_Id_AprobadorDefault,
                        LineaNegocioNombre = linea != null ? linea.LNI_Nombre : null,
                        x.empresa.EMP_CodigoAPIKA,
                        x.empresa.EMP_CodigoD365,
                        x.empresa.EMP_FPA_CodigoAPIKA,
                        x.empresa.EMP_FPA_D365,
                        x.empresa.EMP_TipoCliente,
                        x.empresa.EMP_EGrupoD365,
                        x.empresa.EMP_EmpresaFacturar,
                        x.empresa.EMP_EmpresaFacturar_Id,
                        x.empresa.EMP_ExcluidaGuardia,
                        x.empresa.EMP_GRG_Id,
                        x.empresa.EMP_NumFacturaVDF
                    })
                .ToList();

            return new LargeJsonResult { Data = listaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult VerificarSesionActiva()
        {
            ClaimsPrincipal obj = (ClaimsPrincipal)Thread.CurrentPrincipal;

            //Verificar si la sesión ha caducado
            if (string.IsNullOrEmpty(Sesion.SUsuarioId))
            {
                // Intentar restaurar la sesión desde el usuario autenticado
                if (obj == null || string.IsNullOrEmpty(obj.Identity.Name))
                {
                    return Json(new { success = false, message = "Sesión expirada" }, JsonRequestBehavior.AllowGet);
                }

                // Cargar datos del usuario autenticado
                CargarDatosUsuario(obj.Identity.Name);

                // Verificar si la sesión se cargó correctamente
                if (string.IsNullOrEmpty(Sesion.SUsuarioId))
                {
                    return Json(new { success = false, message = "Sesión expirada" }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult ObtenerCabeceraPermisos()
        {
            //Obtener datos del usuario desde la BD
            DAL_Usuarios dalUsu = new DAL_Usuarios();
            Usuarios objUsuario = dalUsu.L_PrimaryKey(Sesion.SUsuarioId);

            if (objUsuario == null)
            {
                return Json(new { success = false, message = "No se pudo recuperar el usuario" }, JsonRequestBehavior.AllowGet);
            }

            //Obtener permisos del perfil del usuario
            DAL_Seguridad_Perfiles dal = new DAL_Seguridad_Perfiles();
            int idPerfil = objUsuario.USU_SPE_Id ?? 0;
            Seguridad_Perfiles objPerfil = dal.L_PrimaryKey(idPerfil, false);

            List<SeguridadPerfilesOpcionesDTO> lista = dal.GetPermisos(objPerfil.SPE_Id);
            var listaFiltro = lista
                .Select(i => new
                {
                    i.SPO_SPE_Id,
                    i.SPO_SOP_Id,
                    i.SPO_Escritura,
                    i.SOI_Nombre
                })
                .ToList();

            return new LargeJsonResult
            {
                Data = new { success = true, Menu = listaFiltro, NombreUsuario = objUsuario.NombrePersona },
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpPost]
        public ActionResult ObtenerPermisoVista(string idVista)
        {
            DAL_Usuarios dalUsu = new DAL_Usuarios();
            Usuarios objUsuario = dalUsu.L_PrimaryKey(Sesion.SUsuarioId);

            DAL_Seguridad_Perfiles dal = new DAL_Seguridad_Perfiles();
            Seguridad_Perfiles objPerfil = dal.L_PrimaryKey(objUsuario.USU_SPE_Id, false);

            List<SeguridadPerfilesOpcionesDTO> lista = dal.GetPermisos(objPerfil.SPE_Id);
            SeguridadPerfilesOpcionesDTO objSeguridad = lista.Where(i => i.SPO_SOP_Id == idVista).FirstOrDefault();

            return Json(new { success = (objSeguridad.SPO_SPE_Id == 0 ? false : true) });
        }

        [HttpGet]
        public ActionResult ObtenerConfiguraciones()
        {
            DAL_Configuraciones dal = new DAL_Configuraciones();
            List<Configuraciones> lista = dal.L(false, null);

            // Seleccionar las propiedades deseadas
            var listaFiltro = lista
                .Select(i => new
                {
                    i.CFG_Id,
                    i.CFG_Descripcion,
                    i.CFG_Valor
                })
                .ToList();

            return new LargeJsonResult { Data = listaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult CargarConfiguracionAnio()
        {
            DAL_Configuraciones dal = new DAL_Configuraciones();
            Configuraciones objAnio = dal.L_PrimaryKey((int)Constantes.Configuracion.AnioConcepto);
            return new LargeJsonResult { Data = objAnio.CFG_Valor, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerProductosD365()
        {
            DAL_ProductosD365 dal = new DAL_ProductosD365();
            List<ProductosD365> lista = dal.L(false, null);

            // Seleccionar las propiedades deseadas
            var listaFiltro = lista
                .Select(i => new
                {
                    i.PR3_Id,
                    PR3_Nombre = i.PR3_Nombre,
                    PR3_Activo = i.PR3_Activo
                })
                .ToList();

            return new LargeJsonResult { Data = listaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerItemNumbersD365()
        {
            DAL_ItemNumbersD365 dal = new DAL_ItemNumbersD365();
            List<ItemNumbersD365> lista = dal.L(false, null);

            // Seleccionar las propiedades deseadas
            var listaFiltro = lista
                .Select(i => new
                {
                    i.IN3_Id,
                    i.IN3_Nombre,
                    i.IN3_Activo
                })
                .ToList();

            return new LargeJsonResult { Data = listaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public ActionResult GuardarUsuario(UsuarioRequest objUsuarioRequest)
        {
            DAL_Usuarios dal = new DAL_Usuarios();
            dal.G(objUsuarioRequest.Usuario, Sesion.SPersonaId);

            DAL_Personas dalPer = new DAL_Personas();
            Personas objPersona = dalPer.L_PrimaryKey(objUsuarioRequest.Usuario.USU_PER_Id);
            objPersona.PER_Email = objUsuarioRequest.Email;

            dalPer.G(objPersona, Sesion.SPersonaId);

            LimpiarCache(TipoCache.Tareas, TipoCache.Conceptos, TipoCache.Pedidos);

            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult GuardarConfiguracion(Configuraciones objConfig)
        {
            try
            {
                DAL_Configuraciones dal = new DAL_Configuraciones();
                bool ok = dal.G(objConfig, Sesion.SPersonaId);

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult EliminarConfiguracion(int idConfiguracion)
        {
            try
            {
                DAL_Configuraciones dal = new DAL_Configuraciones();

                dal.D(idConfiguracion);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerEmailPersona(int idPersona)
        {
            DAL_Personas dal = new DAL_Personas();
            Personas objPersonas = dal.L_PrimaryKey(idPersona, false);

            return new LargeJsonResult { Data = objPersonas.PER_Email, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult EliminarUsuario(string idUsuario)
        {
            try
            {
                DAL_Usuarios dal = new DAL_Usuarios();
                bool deleteOk = dal.D(idUsuario);

                LimpiarCache(TipoCache.Tareas, TipoCache.Conceptos, TipoCache.Pedidos);

                return Json(new { success = deleteOk });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult GuardarProducto(ProductosD365 objProducto)
        {
            DAL_ProductosD365 dal = new DAL_ProductosD365();
            dal.G(objProducto, Sesion.SPersonaId);

            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult GuardarItemNumber(ItemNumbersD365 objItemNumber)
        {
            DAL_ItemNumbersD365 dal = new DAL_ItemNumbersD365();
            bool ok = dal.G(objItemNumber, Sesion.SPersonaId);

            return Json(new { success = ok });
        }

        [HttpPost]
        public ActionResult EliminarProducto(int idProducto)
        {
            DAL_ProductosD365 dal = new DAL_ProductosD365();
            bool ok = dal.D(idProducto);

            return Json(new { success = ok });
        }

        [HttpGet]
        public ActionResult ObtenerPersonas()
        {
            DAL_Personas dalPer = new DAL_Personas();
            List<Personas> listaPersonas = dalPer.L(false, null);

            var listaFiltro = listaPersonas
                .Select(i => new
                {
                    i.PER_Id,
                    i.ApellidosNombre
                })
                .OrderBy(i => i.ApellidosNombre)
                .ToList();

            return new LargeJsonResult { Data = listaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerComboLineasNegocio()
        {
            DAL_LineasNegocio_Idioma dal = new DAL_LineasNegocio_Idioma();
            List<LineasNegocio_Idioma> lstLineas = dal.L(false, null);

            var tareasEmpresaFiltro = lstLineas
                .Select(i => new
                {
                    i.LNI_LNE_Id,
                    i.LNI_Nombre
                })
                .ToList();

            return new LargeJsonResult { Data = tareasEmpresaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerComboEmpresas()
        {
            DAL_Empresas dal = new DAL_Empresas();
            List<Empresas> lstEmpresas = dal.L(false, null);

            var tareasEmpresaFiltro = lstEmpresas
                .Select(i => new
                {
                    i.EMP_Id,
                    i.EMP_Nombre
                })
                .ToList();

            return new LargeJsonResult { Data = tareasEmpresaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public ActionResult GuardarDepartamento(Departamentos objDepartamento)
        {
            DAL_Departamentos dal = new DAL_Departamentos();
            bool ok = dal.G(objDepartamento, Sesion.SPersonaId);

            return Json(new { success = ok });
        }

        [HttpPost]
        public ActionResult EliminarDepartamento(int idDepartamento)
        {
            DAL_Departamentos dal = new DAL_Departamentos();
            bool ok = dal.D(idDepartamento);

            return Json(new { success = ok });
        }

        [HttpPost]
        public ActionResult EliminarEmpresa(int idEmpresa)
        {
            try
            {
                DAL_Empresas_Aprobadores dalEmpApr = new DAL_Empresas_Aprobadores();
                dalEmpApr.EliminarAprobadores(idEmpresa, Array.Empty<int>().ToList());

                DAL_Empresas dal = new DAL_Empresas();
                bool ok = dal.D(idEmpresa);

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult GuardarEmpresa(EmpresaRequest objData)
        {
            try
            {
                DAL_Empresas dal = new DAL_Empresas();
                dal.G(objData.Empresa, Sesion.SPersonaId);

                DAL_Empresas_Aprobadores dalPers = new DAL_Empresas_Aprobadores();

                // Elimina solo los aprobadores que ya no están en la nueva lista
                dalPers.EliminarAprobadores(objData.Empresa.EMP_Id, objData.idsAprobadores);

                // Obtener los aprobadores actuales en la BD
                dalPers.ModoConsultaClaveExterna = DAL_Empresas_Aprobadores.TipoClaveExterna.Empresa;
                var aprobadoresActuales = dalPers.L_ClaveExterna(objData.Empresa.EMP_Id, null)
                       .Select(i => i.EMA_PER_Id)
                       .ToList();

                //Agregar solo los nuevos aprobadores que no existan
                if (objData.idsAprobadores != null)
                {
                    foreach (int idPersona in objData.idsAprobadores)
                    {
                        if (!aprobadoresActuales.Contains(idPersona))
                        {
                            Empresas_Aprobadores objEmp = new Empresas_Aprobadores
                            {
                                EMA_EMP_Id = objData.Empresa.EMP_Id,
                                EMA_PER_Id = idPersona
                            };
                            dalPers.G(objEmp, Sesion.SPersonaId);
                        }
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerComboEmpresaAprobadores(int idEmpresa)
        {
            DAL_Empresas_Aprobadores dalEmpApr = new DAL_Empresas_Aprobadores();
            dalEmpApr.ModoConsultaClaveExterna = DAL_Empresas_Aprobadores.TipoClaveExterna.Empresa;
            List<Empresas_Aprobadores> lista = dalEmpApr.L_ClaveExterna(idEmpresa, null);
            var listaFiltro = lista
                .Select(i => new
                {
                    i.EMA_EMP_Id,
                    i.EMA_PER_Id,
                    i.Personas.ApellidosNombre
                })
                .ToList();

            return new LargeJsonResult { Data = listaFiltro, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public JsonResult ObtenerOficinas()
        {
            DAL_Oficinas dal = new DAL_Oficinas();
            var lista = dal.L(false, null)
                .Select(t => new {
                    t.OFI_Id,
                    t.OFI_Nombre,
                    t.OFI_NombreDA
                })
                .OrderBy(i => i.OFI_Nombre)
                .ToList();

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarOficina(Oficinas oficina)
        {
            try
            {
                DAL_Oficinas dal = new DAL_Oficinas();
                bool ok = dal.G(oficina, Sesion.SPersonaId);

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarOficina(int idOficina)
        {
            try
            {
                DAL_Oficinas dal = new DAL_Oficinas();
                dal.D(idOficina);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerTiposEnte()
        {
            DAL_TiposEnte dal = new DAL_TiposEnte();
            var lista = dal.L(false, null)
                .Select(t => new {
                    t.TEN_Id,
                    t.TEN_Nombre
                })
                .ToList();

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarTipoEnte(TiposEnte tipoEnte)
        {
            try
            {
                DAL_TiposEnte dal = new DAL_TiposEnte();
                bool ok = dal.G(tipoEnte, Sesion.SPersonaId);

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarTipoEnte(int idTipoEnte)
        {
            try
            {
                DAL_TiposEnte dal = new DAL_TiposEnte();
                dal.D(idTipoEnte);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerEntes()
        {
            DAL_Entes dal = new DAL_Entes();
            var lista = dal.ObtenerEntes();

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarEnte(Entes ente)
        {
            try
            {
                DAL_Entes dal = new DAL_Entes();
                bool ok = dal.G(ente, Sesion.SPersonaId);

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarEnte(int idEnte)
        {
            try
            {
                DAL_Entes dal = new DAL_Entes();
                dal.D(idEnte);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerLicenciasPorEnte(int idEnte)
        {
            DAL_Entes_Licencias dal = new DAL_Entes_Licencias();
            var lista = dal.ObtenerLicenciasPorEnte(idEnte);

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public JsonResult ObtenerEntesPorLicencia(int idLicencia)
        {
            DAL_Entes_Licencias dal = new DAL_Entes_Licencias();
            var lista = dal.ObtenerEntesPorLicencia(idLicencia);

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public JsonResult ObtenerLicenciasAnualPorEnte(int idEnte)
        {
            DAL_Entes_LicenciasAnuales dal = new DAL_Entes_LicenciasAnuales();
            var lista = dal.ObtenerLicenciasAnualPorEnte(idEnte);

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarEnteLicencia(Entes_Licencias enteLicencia)
        {
            try
            {
                DAL_Entes_Licencias dal = new DAL_Entes_Licencias();
                bool ok = dal.G(enteLicencia, Sesion.SPersonaId);

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarEnteLicencia(int idEnte, int idEnteLicencia, DateTime fechaInicio)
        {
            try
            {
                DAL_Entes_Licencias dal = new DAL_Entes_Licencias();
                bool ok = dal.EliminarEnteLicencia(idEnte, idEnteLicencia, fechaInicio);

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarEnteLicenciaAnual(int idEnte, int idLicenciaAnual, DateTime fechaInicio)
        {
            try
            {
                DAL_Entes_LicenciasAnuales dal = new DAL_Entes_LicenciasAnuales();
                bool ok = dal.EliminarEnteLicenciaAnual(idEnte, idLicenciaAnual, fechaInicio);

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerTareasCombo(int[] listaTiposTarea)
        {
            try
            {
                DAL_Tareas dal = new DAL_Tareas();
                var listaTareas = dal.GetTareasCombo(listaTiposTarea);

                return Json(listaTareas, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult ObtenerGruposGuardia()
        {
            DAL_GruposGuardia dal = new DAL_GruposGuardia();
            var lista = dal.L(false, null)
                .Select(g => new
                {
                    g.GRG_Id,
                    g.GRG_Nombre
                })
                .ToList();

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarGrupoGuardia(GruposGuardia grupo)
        {
            try
            {
                DAL_GruposGuardia dal = new DAL_GruposGuardia();
                bool ok = dal.G(grupo, Sesion.SPersonaId);

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarGrupoGuardia(int idGrupo)
        {
            try
            {
                DAL_GruposGuardia dal = new DAL_GruposGuardia();
                dal.D(idGrupo);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerProveedores()
        {
            DAL_Proveedores dal = new DAL_Proveedores();
            var lista = dal.L(false, null)
                .Select(p => new
                {
                    p.PRV_Id,
                    p.PRV_CIF,
                    p.PRV_Nombre,
                    p.PRV_Activo,
                    p.PRV_TAR_Id_Soporte
                }).
                OrderBy(i => i.PRV_Nombre)
                .ToList();

            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarProveedor(Proveedores proveedor)
        {
            try
            {
                proveedor.FechaModificacion = DateTime.Now;
                proveedor.PER_Id_Modificacion = Sesion.SPersonaId;

                if (proveedor.PRV_Id == 0)
                {
                    proveedor.FechaAlta = DateTime.Now;
                }

                DAL_Proveedores dal = new DAL_Proveedores();
                bool ok = dal.G(proveedor, Sesion.SPersonaId);

                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarProveedor(int idProveedor)
        {
            try
            {
                DAL_Proveedores dal = new DAL_Proveedores();
                dal.D(idProveedor);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerComboGruposGuardia()
        {
            try
            {
                var dal = new DAL_GruposGuardia();
                var grupos = dal.L(false, null);

                var resultado = grupos
                    .Select(g => new
                    {
                        GRG_Id = g.GRG_Id,
                        GRG_Nombre = g.GRG_Nombre
                    })
                    .OrderBy(g => g.GRG_Nombre)
                    .ToList();

                return Json(resultado, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerRepartoContrato(int contratoId)
        {
            // Instancia tu DAL para la tabla de reparto
            var dal = new DAL_Proveedores_ContratosSoporte_Reparto();

            // Recupera sólo las filas que coincidan con el contrato
            var lista = dal
                .L(false, r => r.PVR_PVC_Id == contratoId)
                .Select(r => new
                {
                    r.PVR_EMP_Id, 
                    r.PVR_Porcentaje 
                })
                .ToList();

            // Devolvemos JSON puro para que $.getJSON lo consuma
            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult ObtenerAsuntos()
        {
            // 1) Obtengo todos los asuntos
            var dalAsuntos = new DAL_Proveedores_Asuntos();
            var listaAsuntos = dalAsuntos.L(false, null);

            // 2) Cargo todos los proveedores en un diccionario para lookup rápido
            var dalProv = new DAL_Proveedores();
            var listaProv = dalProv.L(false, null)
                                   .ToDictionary(p => p.PRV_Id, p => p.PRV_Nombre);

            // 3) Proyección con join manual
            var resultado = listaAsuntos
                .Select(p => new
                {
                    p.PAS_Id,
                    p.PAS_Anyo,
                    p.PAS_Mes,
                    p.PAS_Fecha,
                    p.PAS_TKC_Id_GLPI,
                    p.PAS_ENT_Id,
                    p.PAS_EMP_Id,
                    p.PAS_PRV_Id,
                    ProveedorNombre = listaProv.TryGetValue(p.PAS_PRV_Id, out var nom) ? nom : "(Desconocido)",
                    p.PAS_Descripcion,
                    p.PAS_Horas,
                    p.PAS_NumFacturaP,
                    p.PAS_Importe
                })
                .ToList();

            return new LargeJsonResult
            {
                Data = resultado,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        [HttpGet]
        public ActionResult ObtenerEntidadesCombo()
        {
            var dal = new DAL_Entes();
            var lista = dal.L(false, null)
                .Select(e => new { e.ENT_Id, e.ENT_Nombre })
                .OrderBy(i => i.ENT_Nombre)
                .ToList();
            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerEmpresasCombo()
        {
            var dal = new DAL_Empresas();
            var lista = dal.L(false, null)
                .Select(e => new { e.EMP_Id, e.EMP_Nombre })
                .ToList();
            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpGet]
        public ActionResult ObtenerTicketsITSMCombo()
        {
            var dal = new DAL_Tickets();
            var lista = dal.L(false, null)
                .Select(t => new { t.TKC_Id, t.TKC_Titulo })
                .ToList();
            return new LargeJsonResult { Data = lista, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public JsonResult GuardarAsunto(Proveedores_Asuntos dto)
        {
            try
            {
                dto.PAS_Fecha = dto.PAS_Fecha;
                bool ok = new DAL_Proveedores_Asuntos().G(dto, Sesion.SPersonaId);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult EliminarAsunto(int id)
        {
            try
            {
                bool ok = new DAL_Proveedores_Asuntos().D(id);
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult ObtenerDepartamentosEmpresa(int idEmpresa)
        {
            var dal = new DAL_Empresas_Departamentos();
            var lista = dal.L(false, null);
            var anon = lista.Where(i => i.EDE_EMP_Id == idEmpresa).Select(d => new {
                d.EDE_Id,
                d.EDE_Nombre
            }).OrderBy(i => i.EDE_Nombre);
            return Json(anon, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GuardarDepartamentoEmpresa(Empresas_Departamentos dto)
        {
            var dal = new DAL_Empresas_Departamentos();
            dal.G(dto, Sesion.SPersonaId);
            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult EliminarDepartamentoEmpresa(int id)
        {
            var dal = new DAL_Empresas_Departamentos();
            bool ok = dal.D(id);
            return Json(new { success = ok });
        }
    }

    public class UsuarioRequest
    {
        public Usuarios Usuario { get; set; }
        public string Email { get; set; }
    }

    public class EmpresaRequest
    {
        public Empresas Empresa { get; set; }
        public List<int> idsAprobadores { get; set; }
    }

    public class ContratoSoporteDto
    {
        public Proveedores_ContratosSoporte objContrato { get; set; }

        public List<RepartoItemDto> Reparto { get; set; }
    }
    public class RepartoItemDto
    {
        public int EMP_Id { get; set; }
        public decimal Porcentaje { get; set; }
    }
}