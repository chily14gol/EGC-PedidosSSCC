using System;
using System.Collections.Generic;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_PeriodosPartes : DAL_Base<PeriodosPartes>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);
        protected override System.Data.Linq.Table<PeriodosPartes> Tabla => bd.PeriodosPartes;

        protected override bool Guardar(PeriodosPartes entidad, int idPersonaModificacion)
        {
            // Este método se invoca si quieres guardar algún campo extra de auditoría, etc.
            bd.SubmitChanges();
            return true;
        }

        /// <summary>
        /// Obtiene todos los periodos para un año dado (ordenados por mes).
        /// </summary>
        public List<PeriodosPartes> ObtenerPorAnyo(int anyo)
        {
            return Tabla
                .Where(p => p.PEP_Anyo == anyo)
                .OrderBy(p => p.PEP_Mes)
                .ToList();
        }

        // No necesitamos más métodos especiales, pues no hacemos CRUD a periodos desde la UI.
    }
}
