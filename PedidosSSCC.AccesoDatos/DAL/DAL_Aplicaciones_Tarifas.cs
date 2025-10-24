using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Aplicaciones_Tarifas : DAL_Base<Aplicaciones_Tarifas>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Aplicaciones_Tarifas> Tabla => bd.Aplicaciones_Tarifas;

        protected override bool Guardar(Aplicaciones_Tarifas obj, int idPersonaModificacion)
        {
            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    var finEntidad = obj.APT_FechaFin ?? DateTime.MaxValue;

                    var q = bd.Aplicaciones_Tarifas
                        .Where(c => c.APT_APP_Id == obj.APT_APP_Id);

                    // Excluir el propio registro sólo si estamos editando (tiene FechaInicioOriginal)
                    if (obj.APT_FechaInicioOriginal.HasValue)
                    {
                        q = q.Where(c => c.APT_FechaInicio != obj.APT_FechaInicioOriginal.Value);
                    }

                    q = q.Where(c =>
                        c.APT_FechaInicio <= finEntidad &&
                        (c.APT_FechaFin ?? DateTime.MaxValue) >= obj.APT_FechaInicio);

                    bool haySolapamiento = q.Any();

                    if (haySolapamiento)
                        return false;

                    if (obj.APT_FechaInicioOriginal == null)
                    {
                        // Nuevo módulo
                        bd.Aplicaciones_Tarifas.InsertOnSubmit(obj);
                    }
                    else
                    {
                        // Actualizar módulo existente
                        var existente = bd.Aplicaciones_Tarifas
                            .FirstOrDefault(m => m.APT_APP_Id == obj.APT_APP_Id && m.APT_FechaInicio == obj.APT_FechaInicio);
                        if (existente == null)
                        {
                            MensajeErrorEspecifico = "No se encontró el tarifa";
                            return false;
                        }

                        existente.APT_APP_Id = obj.APT_APP_Id;
                        existente.APT_FechaInicio = obj.APT_FechaInicio;
                        existente.APT_FechaFin = obj.APT_FechaFin;
                        existente.APT_PrecioUnitario = obj.APT_PrecioUnitario;
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