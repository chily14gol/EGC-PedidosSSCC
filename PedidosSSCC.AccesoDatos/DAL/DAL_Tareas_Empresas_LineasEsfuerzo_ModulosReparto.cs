using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Tareas_Empresas_LineasEsfuerzo_ModulosReparto : DAL_Base<Tareas_Empresas_LineasEsfuerzo_ModulosReparto>
    {
        public DAL_Tareas_Empresas_LineasEsfuerzo_ModulosReparto() : base() { }

        public DAL_Tareas_Empresas_LineasEsfuerzo_ModulosReparto(TransactionScope transaccion) : base(transaccion) { }

        protected override System.Data.Linq.Table<Tareas_Empresas_LineasEsfuerzo_ModulosReparto> Tabla
        {
            get { return bd.Tareas_Empresas_LineasEsfuerzo_ModulosReparto; }
        }

        protected override bool Guardar(Tareas_Empresas_LineasEsfuerzo_ModulosReparto objConcepto, int idPersonaModificacion)
        {
            bool retorno = false;
            Tareas_Empresas_LineasEsfuerzo_ModulosReparto item = new Tareas_Empresas_LineasEsfuerzo_ModulosReparto();
            item.TCR_TLE_Id = objConcepto.TCR_TLE_Id;
            item.TCR_AMT_Id = objConcepto.TCR_AMT_Id;
            item.TCR_Equitativo = objConcepto.TCR_Equitativo;
            item.TCR_ImporteTotal = objConcepto.TCR_ImporteTotal;
            item.TCR_Porcentaje = objConcepto.TCR_Porcentaje;
            item.TCR_Importe = objConcepto.TCR_Importe;

            bd.Tareas_Empresas_LineasEsfuerzo_ModulosReparto.InsertOnSubmit(item);

            bd.SubmitChanges();
            objConcepto.TCR_TLE_Id = item.TCR_TLE_Id;

            retorno = true;

            return retorno;
        }
    }
}
