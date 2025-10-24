using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Tareas_Empresas_LineasEsfuerzo_Modulos : DAL_Base<Tareas_Empresas_LineasEsfuerzo_Modulos>
    {
        public DAL_Tareas_Empresas_LineasEsfuerzo_Modulos() : base() { }

        public DAL_Tareas_Empresas_LineasEsfuerzo_Modulos(TransactionScope transaccion) : base(transaccion) { }

        protected override System.Data.Linq.Table<Tareas_Empresas_LineasEsfuerzo_Modulos> Tabla
        {
            get { return bd.Tareas_Empresas_LineasEsfuerzo_Modulos; }
        }

        protected override bool Guardar(Tareas_Empresas_LineasEsfuerzo_Modulos objConcepto, int idPersonaModificacion)
        {
            bool retorno = false;
            Tareas_Empresas_LineasEsfuerzo_Modulos item = new Tareas_Empresas_LineasEsfuerzo_Modulos();
            item.TCM_TLE_Id = objConcepto.TCM_TLE_Id;
            item.TCM_AME_Id = objConcepto.TCM_AME_Id;
            item.TCM_Importe = objConcepto.TCM_Importe;

            bd.Tareas_Empresas_LineasEsfuerzo_Modulos.InsertOnSubmit(item);

            bd.SubmitChanges();
            objConcepto.TCM_TLE_Id = item.TCM_TLE_Id;

            retorno = true;

            return retorno;
        }
    }
}
