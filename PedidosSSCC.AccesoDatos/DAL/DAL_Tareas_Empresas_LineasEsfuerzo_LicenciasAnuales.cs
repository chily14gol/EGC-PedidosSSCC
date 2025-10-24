using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales : DAL_Base<Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales>
    {
        public DAL_Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales() : base() { }

        public DAL_Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales(TransactionScope transaccion) : base(transaccion) { }

        protected override System.Data.Linq.Table<Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales> Tabla
        {
            get { return bd.Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales; }
        }

        protected override bool Guardar(Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales objConcepto, int idPersonaModificacion)
        {
            bool retorno = false;
            Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales item = new Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales();
            item.TCL_TLE_Id = objConcepto.TCL_TLE_Id;
            item.TCL_LAN_Id = objConcepto.TCL_LAN_Id;
            item.TCL_ENT_Id = objConcepto.TCL_ENT_Id;
            item.TCL_Importe = objConcepto.TCL_Importe;

            bd.Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales.InsertOnSubmit(item);

            bd.SubmitChanges();
            objConcepto.TCL_TLE_Id = item.TCL_TLE_Id;

            retorno = true;

            return retorno;
        }
    }
}

