using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Tareas_Empresas_LineasEsfuerzo_Tickets : DAL_Base<Tareas_Empresas_LineasEsfuerzo_Tickets>
    {
        public DAL_Tareas_Empresas_LineasEsfuerzo_Tickets() : base() { }

        public DAL_Tareas_Empresas_LineasEsfuerzo_Tickets(TransactionScope transaccion) : base(transaccion) { }

        protected override System.Data.Linq.Table<Tareas_Empresas_LineasEsfuerzo_Tickets> Tabla
        {
            get { return bd.Tareas_Empresas_LineasEsfuerzo_Tickets; }
        }

        protected override bool Guardar(Tareas_Empresas_LineasEsfuerzo_Tickets objConcepto, int idPersonaModificacion)
        {
            bool retorno = false;
            Tareas_Empresas_LineasEsfuerzo_Tickets item = new Tareas_Empresas_LineasEsfuerzo_Tickets();
            item.TCT_TLE_Id = objConcepto.TCT_TLE_Id;
            item.TCT_TKC_Id = objConcepto.TCT_TKC_Id;
            item.TCT_Importe = objConcepto.TCT_Importe;

            bd.Tareas_Empresas_LineasEsfuerzo_Tickets.InsertOnSubmit(item);

            bd.SubmitChanges();
            objConcepto.TCT_TLE_Id = item.TCT_TLE_Id;

            retorno = true;

            return retorno;
        }
    }
}


