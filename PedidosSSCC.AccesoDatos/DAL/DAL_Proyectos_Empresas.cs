using System;
using System.Collections.Generic;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Proyectos_Empresas : DAL_Base<Proyectos_Empresas>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Proyectos_Empresas> Tabla => bd.Proyectos_Empresas;

        protected override bool Guardar(Proyectos_Empresas entidad, int idPersonaModificacion)
        {
            // Igual que en Departamentos, aquí podrías hacer auditoría
            bd.SubmitChanges();
            return true;
        }

        /// <summary>
        /// Obtiene todas las relaciones Proyectos_Empresas asociadas a un proyecto dado.
        /// </summary>
        public List<Proyectos_Empresas> ObtenerEmpresasPorProyecto(int proyectoId)
        {
            return Tabla.Where(pe => pe.PRE_PRY_Id == proyectoId).ToList();
        }

        /// <summary>
        /// Elimina todas las relaciones Proyectos_Empresas de un proyecto
        /// </summary>
        public void EliminarPorProyecto(int proyectoId)
        {
            var hijos = Tabla.Where(pe => pe.PRE_PRY_Id == proyectoId);
            if (hijos.Any())
            {
                Tabla.DeleteAllOnSubmit(hijos);
                bd.SubmitChanges();
            }
        }

        /// <summary>
        /// Guarda la lista de empresas + porcentaje para un proyecto: elimina las anteriores y vuelve a insertar.
        /// </summary>
        /// <param name="proyectoId">Id del proyecto</param>
        /// <param name="empresas">Lista de pares { EmpresaId, Porcentaje }</param>
        public bool GuardarEmpresas(int proyectoId, List<Proyectos_Empresas> empresas)
        {
            try
            {
                // 1) Borramos todas las asignaciones anteriores
                EliminarPorProyecto(proyectoId);

                // 2) Insertamos las nuevas relaciones
                foreach (var e in empresas ?? new List<Proyectos_Empresas>())
                {
                    var nuevo = new Proyectos_Empresas
                    {
                        PRE_PRY_Id = proyectoId,
                        PRE_EMP_Id = e.PRE_EMP_Id,
                        PRE_Porcentaje = e.PRE_Porcentaje
                    };
                    Tabla.InsertOnSubmit(nuevo);
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
    }
}
