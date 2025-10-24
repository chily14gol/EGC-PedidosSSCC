using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Aplicaciones : DAL_Base<Aplicaciones>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Aplicaciones> Tabla => bd.Aplicaciones;

        protected override bool Guardar(Aplicaciones objAplicacion, int idPersonaModificacion)
        {
            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    // Opcional: bd.Transaction = bd.Connection.BeginTransaction();

                    // 1) Insert/update aplicación
                    Aplicaciones entidad;
                    bool esNuevo = objAplicacion.APP_Id <= 0; // usas -1 como "nuevo"

                    if (esNuevo)
                    {
                        entidad = new Aplicaciones
                        {
                            APP_Nombre = objAplicacion.APP_Nombre,
                            APP_TAR_Id = objAplicacion.APP_TAR_Id,
                            // APP_FechaModificacion = DateTime.Now; APP_PER_Id_Modificacion = idPersonaModificacion; (si procede)
                        };
                        bd.Aplicaciones.InsertOnSubmit(entidad);
                        bd.SubmitChanges();   // aquí ya tiene APP_Id (identity)
                    }
                    else
                    {
                        entidad = bd.Aplicaciones.FirstOrDefault(a => a.APP_Id == objAplicacion.APP_Id);
                        if (entidad == null)
                        {
                            MensajeErrorEspecifico = "No se encontró la aplicación a actualizar.";
                            return false;
                        }
                        entidad.APP_Nombre = objAplicacion.APP_Nombre;
                        entidad.APP_TAR_Id = objAplicacion.APP_TAR_Id;
                        bd.SubmitChanges();
                    }

                    int appId = entidad.APP_Id;

                    // 2) Refrescar TiposEnte de esa app
                    var tiposExistentes = bd.Aplicaciones_TiposEnte.Where(x => x.ATE_APP_Id == appId);
                    bd.Aplicaciones_TiposEnte.DeleteAllOnSubmit(tiposExistentes);

                    if (objAplicacion.TiposEnte != null && objAplicacion.TiposEnte.Any())
                    {
                        foreach (var tenId in objAplicacion.TiposEnte.Distinct())
                        {
                            bd.Aplicaciones_TiposEnte.InsertOnSubmit(new Aplicaciones_TiposEnte
                            {
                                ATE_APP_Id = appId,
                                ATE_TEN_Id = tenId
                            });
                        }
                    }

                    bd.SubmitChanges();
                    // Opcional: bd.Transaction.Commit();

                    return true;
                }
            }
            catch (Exception ex)
            {
                // Opcional: bd.Transaction?.Rollback();
                MensajeErrorEspecifico = ex.Message;
                return false;
            }
        }

        public bool Eliminar(int idAplicacion)
        {
            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    // 1) Buscar la aplicación
                    var aplicacion = bd.Aplicaciones.FirstOrDefault(a => a.APP_Id == idAplicacion);
                    if (aplicacion == null)
                    {
                        MensajeErrorEspecifico = "No se encontró la aplicación a eliminar.";
                        return false;
                    }

                    // Bloquear si tiene módulos asociados
                    bool tieneModulos = bd.Aplicaciones_Modulos.Any(m => m.APM_APP_Id == idAplicacion);
                    if (tieneModulos)
                    {
                        MensajeErrorEspecifico = "No se puede eliminar la aplicación porque tiene módulos asociados.";
                        return false;
                    }

                    // Bloquear si tiene tarifas asociados
                    bool tieneTarifas = bd.Aplicaciones_Tarifas.Any(m => m.APT_APP_Id == idAplicacion);
                    if (tieneTarifas)
                    {
                        MensajeErrorEspecifico = "No se puede eliminar la aplicación porque tiene tarifas asociadas.";
                        return false;
                    }

                    // 2) Eliminar TiposEnte asociados
                    var tiposEnte = bd.Aplicaciones_TiposEnte.Where(te => te.ATE_APP_Id == idAplicacion);
                    bd.Aplicaciones_TiposEnte.DeleteAllOnSubmit(tiposEnte);

                    // 3) Eliminar la aplicación
                    bd.Aplicaciones.DeleteOnSubmit(aplicacion);

                    // 4) Guardar cambios
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
