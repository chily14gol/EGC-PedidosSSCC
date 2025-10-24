using System;
using System.Collections.Generic;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Proyectos_Departamentos : DAL_Base<Proyectos_Departamentos>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Proyectos_Departamentos> Tabla => bd.Proyectos_Departamentos;

        protected override bool Guardar(Proyectos_Departamentos entidad, int idPersonaModificacion)
        {
            // Este método se invoca si quieres guardar algún campo extra de auditoría, etc.
            bd.SubmitChanges();
            return true;
        }

        /// <summary>
        /// Devuelve todos los registros de Proyectos_Departamentos para un proyecto en particular.
        /// </summary>
        public List<Proyectos_Departamentos> ObtenerDepartamentosPorProyecto(int proyectoId)
        {
            return Tabla.Where(pd => pd.PRD_PRY_Id == proyectoId).ToList();
        }

        public List<Proyectos_Departamentos> ObtenerPorDepartamento(int depId)
        {
            return Tabla.Where(pd => pd.PRD_DEP_Id == depId).ToList();
        }

        /// <summary>
        /// Elimina todos los registros en Proyectos_Departamentos asociados a un proyecto.
        /// </summary>
        public void EliminarPorProyecto(int proyectoId)
        {
            var hijos = Tabla.Where(pd => pd.PRD_PRY_Id == proyectoId);
            if (hijos.Any())
            {
                Tabla.DeleteAllOnSubmit(hijos);
                bd.SubmitChanges();
            }
        }

        /// <summary>
        /// Guarda la lista de departamentos para un proyecto: elimina los anteriores y vuelve a insertar.
        /// </summary>
        /// <param name="proyectoId">Id del proyecto</param>
        /// <param name="listaDeptIds">Lista de IDs de departamentos</param>
        /// <returns>True si se completó correctamente</returns>
        public bool GuardarDepartamentos(int proyectoId, List<int> listaDeptIds)
        {
            try
            {
                // 1) Borrar todas las asignaciones existentes
                EliminarPorProyecto(proyectoId);

                // 2) Insertar las nuevas
                foreach (var depId in listaDeptIds ?? new List<int>())
                {
                    var nuevaAsign = new Proyectos_Departamentos
                    {
                        PRD_PRY_Id = proyectoId,
                        PRD_DEP_Id = depId
                    };
                    Tabla.InsertOnSubmit(nuevaAsign);
                }

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
        /// Obtiene el departamento principal de una persona.
        /// Supone que en el modelo existe la tabla 'Personas' con columna 'DEP_Id'.
        /// </summary>
        public int ObtenerDepartamentoPorPersona(int personaId)
        {
            // Asumimos que existe una tabla 'Personas' en el DataContext con columna DEP_Id.
            // Si tu entidad Persona tiene otro nombre de propiedad para el departamento, ajústalo aquí.
            var persona = bd.Personas.SingleOrDefault(p => p.PER_Id == personaId);
            if (persona == null)
                throw new InvalidOperationException($"No se encontró la persona con Id = {personaId}");

            return persona.PER_DEP_Id.Value;
        }

        /// <summary>
        /// Devuelve todos los Proyectos activos (PRY_Activo = true) asociados a un departamento dado.
        /// </summary>
        public List<Proyectos> ObtenerProyectosPorDepartamento(int deptoId)
        {
            // Hacemos join entre Proyectos_Departamentos y Proyectos para filtrar por DEP_Id y PRY_Activo
            var query =
                from pd in bd.Proyectos_Departamentos
                join p in bd.Proyectos on pd.PRD_PRY_Id equals p.PRY_Id
                where pd.PRD_DEP_Id == deptoId
                   && p.PRY_Activo == true
                select p;

            return query
                .Distinct()       // En caso de que un proyecto esté duplicado en múltiples filas, sacamos distinc
                .OrderBy(p => p.PRY_Nombre)
                .ToList();
        }
    }
}
