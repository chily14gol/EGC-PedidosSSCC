using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Entes_Aplicaciones : DAL_Base<Entes_Aplicaciones>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Entes_Aplicaciones> Tabla => bd.Entes_Aplicaciones;

        protected override bool Guardar(Entes_Aplicaciones obj, int idPersonaModificacion)
        {
            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    bd.Entes_Aplicaciones.InsertOnSubmit(obj);
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
