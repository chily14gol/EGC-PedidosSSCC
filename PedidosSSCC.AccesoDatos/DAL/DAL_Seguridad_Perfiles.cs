using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Seguridad_Perfiles : DAL_Base<Seguridad_Perfiles>
	{
        public override string ComboText { get { return "SPE_Nombre"; } }
        public override string ComboValue { get { return "SPE_Id"; } }

		protected override System.Data.Linq.Table<Seguridad_Perfiles> Tabla
		{
			get { return bd.Seguridad_Perfiles; }
		}

		protected override Expression<Func<Seguridad_Perfiles, bool>> ObtenerPrefiltroPrimaryKey(object valorPK)
		{
			int valor = int.Parse(valorPK.ToString());
			Expression<Func<Seguridad_Perfiles, bool>> retorno = p => p.SPE_Id == valor;
			return retorno;
		}

        protected override bool Guardar(Seguridad_Perfiles entidad, int idPersonaModificacion)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                Seguridad_Perfiles perfil = L_PrimaryKey(entidad.SPE_Id);
                if (perfil == null)
                {
                    // Alta de nuevo perfil
                    perfil = new Seguridad_Perfiles
                    {
                        FechaAlta = DateTime.Now
                    };

                    // Asignar un nuevo ID si no es identidad
                    int? maxId = bd.Seguridad_Perfiles.Max(p => (int?)p.SPE_Id);
                    perfil.SPE_Id = maxId.HasValue ? maxId.Value + 1 : 1;

                    bd.Seguridad_Perfiles.InsertOnSubmit(perfil);
                }

                // Actualizar datos del perfil (común a alta y edición)
                perfil.SPE_Nombre = entidad.SPE_Nombre;
                perfil.FechaModificacion = DateTime.Now;
                perfil.PER_Id_Modificacion = idPersonaModificacion;

                // Eliminar las opciones antiguas y añadir las nuevas
                var nuevasOpciones = entidad.Seguridad_Perfiles_Opciones
                    .Select(o => new PermisoDTO
                    {
                        Id = o.SPO_SOP_Id,
                        Permiso = true // Siempre verdadero porque vienen seleccionadas
                    }).ToList();

                var opcionesEscritura = entidad.Seguridad_Perfiles_Opciones
                    .Where(o => o.SPO_Escritura)
                    .Select(o => new PermisoDTO
                    {
                        Id = o.SPO_SOP_Id,
                        Permiso = true
                    }).ToList();

                ReemplazarOpcionesPerfil(perfil.SPE_Id, nuevasOpciones, opcionesEscritura);

                bd.SubmitChanges();
                scope.Complete();
                return true;
            }
        }

        public bool ModificarPerfilesOpciones(PerfilPermisosDTO objPerfilPermisos, int idPersonaModificacion)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                Seguridad_Perfiles objPerfil = bd.Seguridad_Perfiles
                    .FirstOrDefault(i => i.SPE_Id == objPerfilPermisos.IdPerfil);

                if (objPerfil != null)
                {
                    // Actualización de perfil existente
                    objPerfil.SPE_Nombre = objPerfilPermisos.NombrePerfil;
                    objPerfil.FechaModificacion = DateTime.Now;
                    objPerfil.PER_Id_Modificacion = idPersonaModificacion;
                }
                else
                {
                    // Alta de nuevo perfil
                    Seguridad_Perfiles objNuevoPerfil = new Seguridad_Perfiles
                    {
                        SPE_Nombre = objPerfilPermisos.NombrePerfil,
                        FechaAlta = DateTime.Now,
                        FechaModificacion = DateTime.Now,
                        PER_Id_Modificacion = idPersonaModificacion
                    };

                    bd.Seguridad_Perfiles.InsertOnSubmit(objNuevoPerfil);
                    bd.SubmitChanges(); // Necesario para obtener el nuevo SPE_Id
                    objPerfilPermisos.IdPerfil = objNuevoPerfil.SPE_Id;
                }

                // Reemplazar las opciones del perfil
                ReemplazarOpcionesPerfil(objPerfilPermisos.IdPerfil, objPerfilPermisos.PermisosAcceso, objPerfilPermisos.PermisosEdicion);

                // Guardar todos los cambios
                bd.SubmitChanges();
                scope.Complete();

                return true;
            }
        }

        public bool EliminarPerfil(int idPerfil)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                // Verificar si hay usuarios con este perfil
                bool hayUsuariosConPerfil = bd.Usuarios.Any(u => u.USU_SPE_Id == idPerfil);
                if (hayUsuariosConPerfil)
                {
                    // No se puede eliminar el perfil si está en uso
                    return false;
                }

                // Eliminar las opciones asociadas al perfil
                List<Seguridad_Perfiles_Opciones> lstOpcionesEliminar = bd.Seguridad_Perfiles_Opciones
                    .Where(i => i.SPO_SPE_Id == idPerfil).ToList();

                if (lstOpcionesEliminar.Any())
                {
                    bd.Seguridad_Perfiles_Opciones.DeleteAllOnSubmit(lstOpcionesEliminar);
                }

                // Eliminar el perfil
                Seguridad_Perfiles perfil = bd.Seguridad_Perfiles.SingleOrDefault(p => p.SPE_Id == idPerfil);
                if (perfil != null)
                {
                    bd.Seguridad_Perfiles.DeleteOnSubmit(perfil);
                }

                // Guardar cambios y completar la transacción
                bd.SubmitChanges();
                scope.Complete();

                return true;
            }
        }

        public List<SeguridadPerfilesOpcionesDTO> GetPermisos(int idPerfil)
        {
            var resultado = (from opcion in bd.Seguridad_Opciones
                             join permiso in bd.Seguridad_Perfiles_Opciones
                                 on new { SOP_Id = opcion.SOP_Id, SPE_Id = idPerfil }
                                 equals new { SOP_Id = permiso.SPO_SOP_Id, SPE_Id = permiso.SPO_SPE_Id }
                                 into permisosJoin
                             from permiso in permisosJoin.DefaultIfEmpty()
                             select new { opcion, permiso }).ToList() // ← ejecución aquí

                             .Select(x => new SeguridadPerfilesOpcionesDTO
                             {
                                 SPO_SPE_Id = x.permiso != null ? x.permiso.SPO_SPE_Id : 0, // 👈 Aquí el cambio
                                 SPO_SOP_Id = x.opcion.SOP_Id,
                                 SPO_Escritura = x.permiso?.SPO_Escritura ?? false,
                                 SOI_Nombre = x.opcion.SOP_Nombre
                             }).ToList();

            return resultado;
        }

        private void ReemplazarOpcionesPerfil(int idPerfil, List<PermisoDTO> permisosAcceso, List<PermisoDTO> permisosEdicion)
        {
            var existentes = bd.Seguridad_Perfiles_Opciones.Where(i => i.SPO_SPE_Id == idPerfil).ToList();
            if (existentes.Any()) bd.Seguridad_Perfiles_Opciones.DeleteAllOnSubmit(existentes);

            var nuevasOpciones = permisosAcceso
                .Where(pa => pa.Permiso)
                .Select(pa => new Seguridad_Perfiles_Opciones
                {
                    SPO_SPE_Id = idPerfil,
                    SPO_SOP_Id = pa.Id,
                    SPO_Escritura = permisosEdicion.Any(pe => pe.Id == pa.Id && pe.Permiso)
                }).ToList();

            bd.Seguridad_Perfiles_Opciones.InsertAllOnSubmit(nuevasOpciones);
        }
    }
}