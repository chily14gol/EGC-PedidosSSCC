using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Aplicaciones_Modulos_Empresas : DAL_Base<Aplicaciones_Modulos_Empresas>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Aplicaciones_Modulos_Empresas> Tabla => bd.Aplicaciones_Modulos_Empresas;

        protected override bool Guardar(Aplicaciones_Modulos_Empresas obj, int idPersonaModificacion)
        {
            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                MensajeErrorEspecifico = ex.Message;
                return false;
            }
        }

        public bool GuardarModuloEmpresa(Aplicaciones_Modulos_Empresas obj, out string mensaje)
        {
            mensaje = null;

            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    // 1) Comprobación de solapamiento
                    // 1) Comprobación de solapamiento con fechas nulas
                    var solapado = bd.Aplicaciones_Modulos_Empresas
                        .Where(m =>
                            m.AME_EMP_Id == obj.AME_EMP_Id &&              // misma empresa
                            m.AME_APM_Id == obj.AME_APM_Id &&              // mismo módulo (opcional)
                            m.AME_Id != obj.AME_Id &&                  // excluimos el propio (en actualización)

                            // (a) si existe una fecha fin definida en BD, nuevo inicio < fin_existente;
                            //     si fin_existente es null, la cláusula es true => todo inicio se considera antes de "∞"
                            (m.AME_FechaFin == null || obj.AME_FechaInicio < m.AME_FechaFin) &&

                            // (b) si obj.AME_FechaFin es null (nuevo sin límite), se considera > inicio_existente;
                            //     si no es null, fin_nuevo > inicio_existente.
                            (obj.AME_FechaFin == null || obj.AME_FechaFin > m.AME_FechaInicio)
                        )
                        .FirstOrDefault();

                    if (solapado != null)
                    {
                        mensaje = $"Hay solapamiento de fechas";
                        return false;
                    }

                    if (obj.AME_Id == 0)
                    {
                        // Nuevo módulo
                        bd.Aplicaciones_Modulos_Empresas.InsertOnSubmit(obj);
                    }
                    else
                    {
                        // Actualizar módulo existente
                        var existente = bd.Aplicaciones_Modulos_Empresas
                            .FirstOrDefault(m => m.AME_Id == obj.AME_Id);
                        if (existente == null)
                        {
                            mensaje = "No se encontró el módulo-empresa con Id " + obj.AME_Id;
                            return false;
                        }

                        existente.AME_APM_Id = obj.AME_APM_Id;
                        existente.AME_EMP_Id = obj.AME_EMP_Id;
                        existente.AME_FechaInicio = obj.AME_FechaInicio;
                        existente.AME_FechaFin = obj.AME_FechaFin;
                        existente.AME_ImporteMensual = obj.AME_ImporteMensual;
                        existente.AME_PorcentajeReparto = obj.AME_PorcentajeReparto;
                        existente.AME_DescripcionConcepto = obj.AME_DescripcionConcepto;
                    }

                    bd.SubmitChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
                return false;
            }
        }

    }
}
