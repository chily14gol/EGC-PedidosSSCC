using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Aplicaciones_Modulos : DAL_Base<Aplicaciones_Modulos>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Aplicaciones_Modulos> Tabla => bd.Aplicaciones_Modulos;

        protected override bool Guardar(Aplicaciones_Modulos objModulo, int idPersonaModificacion)
        {
            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    if (objModulo.APM_Id == 0)
                    {
                        // Nuevo módulo
                        bd.Aplicaciones_Modulos.InsertOnSubmit(objModulo);
                    }
                    else
                    {
                        // Actualizar módulo existente
                        var existente = bd.Aplicaciones_Modulos.FirstOrDefault(m => m.APM_Id == objModulo.APM_Id);
                        if (existente == null)
                        {
                            MensajeErrorEspecifico = "No se encontró el módulo con Id " + objModulo.APM_Id;
                            return false;
                        }

                        existente.APM_APP_Id = objModulo.APM_APP_Id;
                        existente.APM_Nombre = objModulo.APM_Nombre;
                        existente.APM_TAR_Id = objModulo.APM_TAR_Id;
                    }

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