using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Tareas_Empresas_LineasEsfuerzo_Asuntos : DAL_Base<Tareas_Empresas_LineasEsfuerzo_Asuntos>
    {
        public DAL_Tareas_Empresas_LineasEsfuerzo_Asuntos() : base() { }

        public DAL_Tareas_Empresas_LineasEsfuerzo_Asuntos(TransactionScope transaccion) : base(transaccion) { }

        protected override System.Data.Linq.Table<Tareas_Empresas_LineasEsfuerzo_Asuntos> Tabla
        {
            get { return bd.Tareas_Empresas_LineasEsfuerzo_Asuntos; }
        }

        protected override bool Guardar(Tareas_Empresas_LineasEsfuerzo_Asuntos objConcepto, int idPersonaModificacion)
        {
            bool retorno = false;
            Tareas_Empresas_LineasEsfuerzo_Asuntos item = new Tareas_Empresas_LineasEsfuerzo_Asuntos();
            item.TCA_TLE_Id = objConcepto.TCA_TLE_Id;
            item.TCA_PAS_Id = objConcepto.TCA_PAS_Id;
            item.TCA_PVC_Id = objConcepto.TCA_PVC_Id;
            item.TCA_Horas = objConcepto.TCA_Horas;
            item.TCA_Importe = objConcepto.TCA_Importe;

            bd.Tareas_Empresas_LineasEsfuerzo_Asuntos.InsertOnSubmit(item);

            bd.SubmitChanges();
            objConcepto.TCA_TLE_Id = item.TCA_TLE_Id;

            retorno = true;

            return retorno;
        }
    }
}
