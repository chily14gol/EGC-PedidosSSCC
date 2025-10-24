using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Proyectos : DAL_Base<Proyectos>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Proyectos> Tabla => bd.Proyectos;

        protected override bool Guardar(Proyectos entidad, int idPersonaModificacion)
        {
            // Este método es invocado internamente al hacer SubmitChanges
            bd.SubmitChanges();
            return true;
        }

        /// <summary>
        /// Obtiene todos los proyectos, ordenados por nombre
        /// </summary>
        public List<Proyectos> ObtenerTodos()
        {
            return bd.Proyectos
                     .OrderBy(p => p.PRY_Nombre)
                     .ToList();
        }

        /// <summary>
        /// Obtiene un proyecto por su ID
        /// </summary>
        public Proyectos ObtenerPorId(int id)
        {
            return Tabla.FirstOrDefault(p => p.PRY_Id == id);
        }

        /// <summary>
        /// Inserta un nuevo proyecto
        /// </summary>
        public bool Crear(Proyectos entidad, int idPersonaModificacion)
        {
            // Valores por defecto
            entidad.PRY_Imputable = entidad.PRY_Imputable; // ya viene en el DTO
            entidad.PRY_Activo = entidad.PRY_Activo;       // ya viene en el DTO
            bd.Proyectos.InsertOnSubmit(entidad);
            bd.SubmitChanges();
            return true;
        }

        /// <summary>
        /// Actualiza un proyecto existente (sólo campos modificables)
        /// </summary>
        public bool Actualizar(Proyectos entidad, int idPersonaModificacion)
        {
            try
            {
                var original = Tabla.SingleOrDefault(p => p.PRY_Id == entidad.PRY_Id);
                if (original == null) return false;

                // Actualizar campos modificables
                original.PRY_Nombre = entidad.PRY_Nombre;
                original.PRY_TAR_Id = entidad.PRY_TAR_Id;
                original.PRY_Imputable = entidad.PRY_Imputable;
                original.PRY_Activo = entidad.PRY_Activo;

                // Si tuvieras auditoría:
                // original.ModificadoPor = idPersonaModificacion;
                // original.FechaModificacion = DateTime.Now;

                bd.SubmitChanges();
                return true;
            }
            catch (Exception ex)
            {
                // Logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Elimina un proyecto por Id
        /// </summary>
        public bool Eliminar(int id)
        {
            try
            {
                var entidad = ObtenerPorId(id);
                if (entidad == null) return false;

                Tabla.DeleteOnSubmit(entidad);
                bd.SubmitChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
