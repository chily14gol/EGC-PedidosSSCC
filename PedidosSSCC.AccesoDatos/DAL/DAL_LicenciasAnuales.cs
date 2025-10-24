using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_LicenciasAnuales : DAL_Base<LicenciasAnuales>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<LicenciasAnuales> Tabla => bd.LicenciasAnuales;

        protected override bool Guardar(LicenciasAnuales obj, int idPersonaModificacion)
        {
            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    if (obj.LAN_Id == -1)
                    {
                        // Nuevo registro
                        bd.LicenciasAnuales.InsertOnSubmit(obj);
                        bd.SubmitChanges(); // Aquí se genera el ID
                    }
                    else
                    {
                        // Actualización de uno existente
                        var existente = bd.LicenciasAnuales.FirstOrDefault(a => a.LAN_Id == obj.LAN_Id);
                        if (existente == null)
                        {
                            MensajeErrorEspecifico = "No se encontró la licencia anual a actualizar.";
                            return false;
                        }

                        existente.LAN_PRV_Id = obj.LAN_PRV_Id;
                        existente.LAN_TAR_Id = obj.LAN_TAR_Id;
                        existente.LAN_Nombre = obj.LAN_Nombre; 
                    }

                    // 3) Siempre eliminar todas las filas hijas de TiposEnte y luego reinsertar las que queden
                    // Eliminar existentes
                    var tiposExistentes = bd.LicenciasAnuales_TiposEnte.Where(x => x.LAT_LAN_Id == obj.LAN_Id);
                    bd.LicenciasAnuales_TiposEnte.DeleteAllOnSubmit(tiposExistentes);

                    // Insertar solo si vienen IDs nuevos
                    if (obj.TiposEnte != null && obj.TiposEnte.Any())
                    {
                        foreach (var tenId in obj.TiposEnte.Distinct())
                        {
                            bd.LicenciasAnuales_TiposEnte.InsertOnSubmit(new LicenciasAnuales_TiposEnte
                            {
                                LAT_LAN_Id = obj.LAN_Id,
                                LAT_TEN_Id = tenId
                            });
                        }
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

        public bool Eliminar(int idLicenciaAnual)
        {
            try
            {
                var objEliminar2 = bd.LicenciasAnuales_TiposEnte.FirstOrDefault(l => l.LAT_LAN_Id == idLicenciaAnual);
                bd.LicenciasAnuales_TiposEnte.DeleteOnSubmit(objEliminar2);

                var objEliminar = bd.LicenciasAnuales.FirstOrDefault(l => l.LAN_Id == idLicenciaAnual);
                if (objEliminar != null)
                    bd.LicenciasAnuales.DeleteOnSubmit(objEliminar);

                bd.SubmitChanges();
                return true;
            }
            catch (Exception ex)
            {
                // Loguea si quieres: Logger.Error(ex);
                return false;
            }
        }
    }
}

