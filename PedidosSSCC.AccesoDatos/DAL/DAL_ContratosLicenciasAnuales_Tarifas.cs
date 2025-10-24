using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_ContratosLicenciasAnuales_Tarifas : DAL_Base<ContratosLicenciasAnuales_Tarifas>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<ContratosLicenciasAnuales_Tarifas> Tabla => bd.ContratosLicenciasAnuales_Tarifas;

        protected override bool Guardar(ContratosLicenciasAnuales_Tarifas obj, int idPersonaModificacion)
        {
            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    var existente = bd.ContratosLicenciasAnuales_Tarifas
                                      .FirstOrDefault(t => t.CLT_CLA_Id == obj.CLT_CLA_Id
                                      && t.CLT_LAN_Id == obj.CLT_LAN_Id);
                    if (existente == null)
                    {
                        bd.ContratosLicenciasAnuales_Tarifas.InsertOnSubmit(obj);
                    }
                    else
                    {
                        // Aquí asignas los campos que quieras actualizar:
                        existente.CLT_CLA_Id = obj.CLT_CLA_Id;
                        existente.CLT_LAN_Id = obj.CLT_LAN_Id;
                        existente.CLT_ImporteAnual = obj.CLT_ImporteAnual;
                        //existente.CLT_NumLicencias = obj.CLT_NumLicencias;
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
