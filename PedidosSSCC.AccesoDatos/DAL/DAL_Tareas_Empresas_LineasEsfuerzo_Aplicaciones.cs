using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Tareas_Empresas_LineasEsfuerzo_Aplicaciones : DAL_Base<Tareas_Empresas_LineasEsfuerzo_Aplicaciones>
    {
        public DAL_Tareas_Empresas_LineasEsfuerzo_Aplicaciones() : base() { }

        public DAL_Tareas_Empresas_LineasEsfuerzo_Aplicaciones(TransactionScope transaccion) : base(transaccion) { }

        protected override System.Data.Linq.Table<Tareas_Empresas_LineasEsfuerzo_Aplicaciones> Tabla
        {
            get { return bd.Tareas_Empresas_LineasEsfuerzo_Aplicaciones; }
        }

        protected override bool Guardar(Tareas_Empresas_LineasEsfuerzo_Aplicaciones objConcepto, int idPersonaModificacion)
        {
            bool retorno = false;
            Tareas_Empresas_LineasEsfuerzo_Aplicaciones item = new Tareas_Empresas_LineasEsfuerzo_Aplicaciones();
            item.TCA_TLE_Id = objConcepto.TCA_TLE_Id;
            item.TCA_APP_Id = objConcepto.TCA_APP_Id;
            item.TCA_ENT_Id = objConcepto.TCA_ENT_Id;
            item.TCA_Importe = objConcepto.TCA_Importe;

            bd.Tareas_Empresas_LineasEsfuerzo_Aplicaciones.InsertOnSubmit(item);

            bd.SubmitChanges();
            objConcepto.TCA_TLE_Id = item.TCA_TLE_Id;

            retorno = true;

            return retorno;
        }
    }
}