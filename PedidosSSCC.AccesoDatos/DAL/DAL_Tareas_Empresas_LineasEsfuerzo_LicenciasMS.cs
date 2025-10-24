using System.Data.Linq;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Tareas_Empresas_LineasEsfuerzo_LicenciasMS : DAL_Base<Tareas_Empresas_LineasEsfuerzo_LicenciasMS>
    {
        public DAL_Tareas_Empresas_LineasEsfuerzo_LicenciasMS() : base() { }

        public DAL_Tareas_Empresas_LineasEsfuerzo_LicenciasMS(TransactionScope transaccion) : base(transaccion) { }

        protected override System.Data.Linq.Table<Tareas_Empresas_LineasEsfuerzo_LicenciasMS> Tabla
        {
            get { return bd.Tareas_Empresas_LineasEsfuerzo_LicenciasMS; }
        }

        //public override List<Tareas_Empresas_LineasEsfuerzo_LicenciasMS> L(bool sinFiltrar = false, Expression<Func<Tareas_Empresas_LineasEsfuerzo_LicenciasMS, bool>> preFiltro = null)
        //{
        //    List<Tareas_Empresas_LineasEsfuerzo> retorno;

        //    if (preFiltro == null)
        //        retorno = get_L(bd != null ? bd : new FacturacionInternaDataContext()).ToList();
        //    else retorno = (from reg in bd.Tareas_Empresas_LineasEsfuerzo
        //                    .Where(preFiltro)
        //                    select reg)
        //                    .ToList();

        //    return retorno;
        //}

        protected override bool Guardar(Tareas_Empresas_LineasEsfuerzo_LicenciasMS objConcepto, int idPersonaModificacion)
        {
            bool retorno = false;

            using (var ctx = new FacturacionInternaDataContext()) // 👈 nuevo contexto por operación
            {
                var item = new Tareas_Empresas_LineasEsfuerzo_LicenciasMS
                {
                    TCL_TLE_Id = objConcepto.TCL_TLE_Id,
                    TCL_LIC_Id = objConcepto.TCL_LIC_Id,
                    TCL_ENT_Id = objConcepto.TCL_ENT_Id,
                    TCL_Importe = objConcepto.TCL_Importe
                };

                ctx.Tareas_Empresas_LineasEsfuerzo_LicenciasMS.InsertOnSubmit(item);
                ctx.SubmitChanges();

                objConcepto.TCL_TLE_Id = item.TCL_TLE_Id;
                retorno = true;
            }

            return retorno;
        }
    }
}

