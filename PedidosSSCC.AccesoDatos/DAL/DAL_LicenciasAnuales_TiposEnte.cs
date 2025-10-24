using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_LicenciasAnuales_TiposEnte : DAL_Base<LicenciasAnuales_TiposEnte>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<LicenciasAnuales_TiposEnte> Tabla => bd.LicenciasAnuales_TiposEnte;

        protected override bool Guardar(LicenciasAnuales_TiposEnte obj, int idPersonaModificacion)
        {
            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    bd.LicenciasAnuales_TiposEnte.InsertOnSubmit(obj);
                    bd.SubmitChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MensajeErrorEspecifico = ex.Message;
                return false;
            }
        }
    }
}


