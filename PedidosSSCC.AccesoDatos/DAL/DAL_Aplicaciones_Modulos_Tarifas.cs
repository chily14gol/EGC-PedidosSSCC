using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Aplicaciones_Modulos_Tarifas : DAL_Base<Aplicaciones_Modulos_Tarifas>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Aplicaciones_Modulos_Tarifas> Tabla => bd.Aplicaciones_Modulos_Tarifas;

        protected override bool Guardar(Aplicaciones_Modulos_Tarifas obj, int idPersonaModificacion)
        {
            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    var finEntidad = obj.AMT_FechaFin ?? DateTime.MaxValue;

                    // Validación de solapamiento de fechas
                    bool haySolapamiento = bd.Aplicaciones_Modulos_Tarifas
                        .Where(c =>
                            c.AMT_APM_Id == obj.AMT_APM_Id
                            // excluimos el propio registro cuando actualizamos
                            && c.AMT_Id != obj.AMT_Id
                            && c.AMT_FechaInicio <= finEntidad
                            && (c.AMT_FechaFin ?? DateTime.MaxValue) >= obj.AMT_FechaInicio
                        )
                        .Any();

                    if (haySolapamiento)
                        return false;

                    if (obj.AMT_Id == 0)
                    {
                        // Nuevo módulo
                        bd.Aplicaciones_Modulos_Tarifas.InsertOnSubmit(obj);
                    }
                    else
                    {
                        // Actualizar módulo existente
                        var existente = bd.Aplicaciones_Modulos_Tarifas.FirstOrDefault(m => m.AMT_Id == obj.AMT_Id);
                        if (existente == null)
                        {
                            MensajeErrorEspecifico = "No se encontró el módulo-tarifa con Id " + obj.AMT_Id;
                            return false;
                        }

                        existente.AMT_APM_Id = obj.AMT_APM_Id;
                        existente.AMT_FechaInicio = obj.AMT_FechaInicio;
                        existente.AMT_FechaFin = obj.AMT_FechaFin;
                        existente.AMT_ImporteMensualReparto = obj.AMT_ImporteMensualReparto;
                        existente.AMT_ImporteMensualRepartoPorcentajes = obj.AMT_ImporteMensualRepartoPorcentajes;
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