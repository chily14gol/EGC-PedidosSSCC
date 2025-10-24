using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Facturas_Tareas_LineasEsfuerzo : DAL_Base<Facturas_Tareas_LineasEsfuerzo>
    {
        public DAL_Facturas_Tareas_LineasEsfuerzo() : base() { }
        public DAL_Facturas_Tareas_LineasEsfuerzo(TransactionScope transaccion) : base(transaccion) { }

        protected override System.Data.Linq.Table<Facturas_Tareas_LineasEsfuerzo> Tabla
        {
            get { return bd.Facturas_Tareas_LineasEsfuerzo; }
        }

        public override List<Facturas_Tareas_LineasEsfuerzo> L(bool sinFiltrar = false, Expression<Func<Facturas_Tareas_LineasEsfuerzo, bool>> preFiltro = null)
        {
            List<Facturas_Tareas_LineasEsfuerzo> retorno = get_L(bd != null ? bd : new FacturacionInternaDataContext()).ToList();
            return retorno;
        }

        public override Facturas_Tareas_LineasEsfuerzo L_PrimaryKey(object valorPK, bool sinFiltrar = false)
        {
            string[] arrPK = valorPK.ToString().Split(Constantes.SeparadorPK);

            Facturas_Tareas_LineasEsfuerzo retorno = get_PrimaryKey(bd != null ? bd : new FacturacionInternaDataContext(), arrPK[0].ToInt().Value, arrPK[1].ToInt().Value);
            if (retorno == null) return retorno;
            return retorno;
        }

        public override List<Facturas_Tareas_LineasEsfuerzo> L_ClaveExterna(object valorFK, Expression<Func<Facturas_Tareas_LineasEsfuerzo, bool>> preFiltro = null)
        {
            return (from reg in bd.Facturas_Tareas_LineasEsfuerzo where reg.FLE_FAC_Id.Equals(valorFK) select reg).ToList();

        }

        protected override bool Guardar(Facturas_Tareas_LineasEsfuerzo entidad, int idPersonaModificacion)
        {
            bool retorno = false;

            Facturas_Tareas_LineasEsfuerzo item = L_PrimaryKey(String.Format("{1}{0}{2}", Constantes.SeparadorPK, entidad.FLE_FAC_Id, entidad.FLE_TLE_Id));

            if (item == null)
            {
                item = new Facturas_Tareas_LineasEsfuerzo();
                item.FLE_FAC_Id = entidad.FLE_FAC_Id;
                item.FLE_TLE_Id = entidad.FLE_TLE_Id;
                bd.Facturas_Tareas_LineasEsfuerzo.InsertOnSubmit(item);
            }

            bd.SubmitChanges();

            retorno = true;

            return retorno;
        }

        public bool EliminarPedidoConceptos(int idPedido)
        {
            bool retorno = false;

            try
            {
                // Obtener los registros que coinciden con el idPedido
                var registrosAEliminar = bd.Facturas_Tareas_LineasEsfuerzo
                    .Where(f => f.FLE_FAC_Id == idPedido)
                    .ToList(); // Convertimos a lista para evitar múltiples accesos a la BD

                if (registrosAEliminar.Any())
                {
                    // Eliminar los registros de la base de datos
                    bd.Facturas_Tareas_LineasEsfuerzo.DeleteAllOnSubmit(registrosAEliminar);

                    // Confirmar los cambios
                    bd.SubmitChanges();

                    retorno = true;
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores (puedes loguear el error si es necesario)
                Console.WriteLine("Error al eliminar conceptos: " + ex.Message);
            }

            return retorno;
        }

        #region "CompiledQuerys"
        public static Func<FacturacionInternaDataContext, IEnumerable<Facturas_Tareas_LineasEsfuerzo>>
            get_L = CompiledQuery.Compile<FacturacionInternaDataContext, IEnumerable<Facturas_Tareas_LineasEsfuerzo>>((FacturacionInternaDataContext bd)
                => (from reg in bd.Facturas_Tareas_LineasEsfuerzo
                    select reg));

        public static Func<FacturacionInternaDataContext, int, int, Facturas_Tareas_LineasEsfuerzo>
            get_PrimaryKey = CompiledQuery.Compile<FacturacionInternaDataContext, int, int, Facturas_Tareas_LineasEsfuerzo>((FacturacionInternaDataContext bd, int pintFAC_Id, int pintTLE_Id)
                => (from reg in bd.Facturas_Tareas_LineasEsfuerzo
                    where reg.FLE_FAC_Id.Equals(pintFAC_Id) && reg.FLE_TLE_Id.Equals(pintTLE_Id)
                    select reg).FirstOrDefault());
        #endregion
    }
}
