using System;
using System.Collections.Generic;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Proyectos_Partes : DAL_Base<Proyectos_Partes>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);
        protected override System.Data.Linq.Table<Proyectos_Partes> Tabla => bd.Proyectos_Partes;

        protected override bool Guardar(Proyectos_Partes entidad, int idPersonaModificacion)
        {
            bd.SubmitChanges();
            return true;
        }

        /// <summary>
        /// Devuelve todas las líneas de parte para un año y mes (Periodo) dado.
        /// </summary>
        public List<Proyectos_Partes> ObtenerPorPeriodo(int anyo, int mes)
        {
            return Tabla
                .Where(p => p.PPA_PEP_Anyo == anyo && p.PPA_PEP_Mes == mes)
                .OrderBy(p => p.PPA_Descripcion)
                .ToList();
        }

        /// <summary>
        /// Inserta una nueva línea (parte).
        /// </summary>
        public bool Crear(Proyectos_Partes entidad, int idPersonaModificacion)
        {
            // Asignar audit campos si existen (p.ej. CreadoPor, FechaCreacion)…
            entidad.PPA_Validado = false;
            Tabla.InsertOnSubmit(entidad);
            bd.SubmitChanges();
            return true;
        }

        /// <summary>
        /// Actualiza una línea existente.
        /// </summary>
        public bool Actualizar(Proyectos_Partes entidad, int idPersonaModificacion)
        {
            try
            {
                var original = Tabla.SingleOrDefault(p => p.PPA_Id == entidad.PPA_Id);
                if (original == null) return false;

                original.PPA_PRY_Id = entidad.PPA_PRY_Id;
                original.PPA_Descripcion = entidad.PPA_Descripcion;
                // original.PPA_Validado = entidad.PPA_Validado; // si hay lógica de validación aparte

                bd.SubmitChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Elimina una línea de parte (sin eliminar horas, pues lo hace quien invoca).
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
            catch
            {
                return false;
            }
        }

        private Proyectos_Partes ObtenerPorId(int id)
        {
            return Tabla.FirstOrDefault(p => p.PPA_Id == id);
        }
    }
}
