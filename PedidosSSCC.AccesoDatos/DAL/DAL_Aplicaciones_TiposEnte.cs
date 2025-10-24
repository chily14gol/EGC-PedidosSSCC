using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Aplicaciones_TiposEnte : DAL_Base<Aplicaciones_TiposEnte>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Aplicaciones_TiposEnte> Tabla => bd.Aplicaciones_TiposEnte;

        protected override bool Guardar(Aplicaciones_TiposEnte obj, int idPersonaModificacion)
        {
            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    bd.Aplicaciones_TiposEnte.InsertOnSubmit(obj);
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




