using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Tareas_Empresas_LineasEsfuerzo_Partes : DAL_Base<Tareas_Empresas_LineasEsfuerzo_Partes>
    {
        public DAL_Tareas_Empresas_LineasEsfuerzo_Partes() : base() { }

        public DAL_Tareas_Empresas_LineasEsfuerzo_Partes(TransactionScope transaccion) : base(transaccion) { }

        protected override System.Data.Linq.Table<Tareas_Empresas_LineasEsfuerzo_Partes> Tabla
        {
            get { return bd.Tareas_Empresas_LineasEsfuerzo_Partes; }
        }

        protected override bool Guardar(Tareas_Empresas_LineasEsfuerzo_Partes objConcepto, int idPersonaModificacion)
        {
            bool retorno = false;
            Tareas_Empresas_LineasEsfuerzo_Partes item = new Tareas_Empresas_LineasEsfuerzo_Partes();
            item.TCP_TLE_Id = objConcepto.TCP_TLE_Id;
            item.TCP_PPA_Id = objConcepto.TCP_PPA_Id;
            item.TCP_Fecha = objConcepto.TCP_Fecha;
            item.TCP_Horas = objConcepto.TCP_Horas;

            bd.Tareas_Empresas_LineasEsfuerzo_Partes.InsertOnSubmit(item);

            bd.SubmitChanges();
            objConcepto.TCP_TLE_Id = item.TCP_TLE_Id;

            retorno = true;

            return retorno;
        }
    }
}



