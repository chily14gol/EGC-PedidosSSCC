using System;
using System.Collections.Generic;
using System.Linq;

namespace AccesoDatos
{
    public class EnteLicenciaAnualDto
    {
        public int EntidadId { get; set; }
        public int LicenciaAnualId { get; set; }
        public string NombreLicenciaAnual { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int ContratoId { get; set; }
        public bool Facturada { get; set; }
    }

    public class DAL_Entes_LicenciasAnuales : DAL_Base<Entes_LicenciasAnuales>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Entes_LicenciasAnuales> Tabla => bd.Entes_LicenciasAnuales;

        protected override bool Guardar(Entes_LicenciasAnuales entidad, int id)
        {
            return true;
        }

        public bool GuardarEnte(Entes_LicenciasAnuales entidad, out string mensaje)
        {
            mensaje = null;

            // 0) Compruebo que la licencia tenga tarifa en el contrato
            bool tieneTarifa = bd.ContratosLicenciasAnuales_Tarifas.Any(t =>
                t.CLT_CLA_Id == entidad.ELA_CLA_Id &&
                t.CLT_LAN_Id == entidad.ELA_LAN_Id
            );
            if (!tieneTarifa)
            {
                mensaje = "La licencia seleccionada no tiene tarifa definida en este contrato.";
                return false;
            }

            // 1) ¿Ya existe un registro para esta Entidad + Licencia + Contrato? -> Modo edición
            var existente = bd.Entes_LicenciasAnuales
                .FirstOrDefault(e =>
                    e.ELA_ENT_Id == entidad.ELA_ENT_Id &&
                    e.ELA_LAN_Id == entidad.ELA_LAN_Id &&
                    e.ELA_CLA_Id == entidad.ELA_CLA_Id
                );

            if (existente != null)
            {
                // ————————— EDITAR —————————
                existente.ELA_FechaInicio = entidad.ELA_FechaInicio;
                existente.ELA_FechaFin = entidad.ELA_FechaFin;

                bd.SubmitChanges();
                return true;
            }

            // ————————— INSERTAR —————————

            // 2) Control de solapamiento
            var newStart = entidad.ELA_FechaInicio;
            var newEnd = entidad.ELA_FechaFin;

            if (bd.Entes_LicenciasAnuales.Any(e =>
                e.ELA_ENT_Id == entidad.ELA_ENT_Id &&
                e.ELA_LAN_Id == entidad.ELA_LAN_Id &&
                e.ELA_CLA_Id == entidad.ELA_CLA_Id &&
                e.ELA_FechaInicio <= newEnd &&
                e.ELA_FechaFin >= newStart
            ))
            {
                mensaje = "El periodo indicado se solapa con otro existente.";
                return false;
            }

            // 3) Control de duplicado exacto
            if (bd.Entes_LicenciasAnuales.Any(e =>
                e.ELA_ENT_Id == entidad.ELA_ENT_Id &&
                e.ELA_LAN_Id == entidad.ELA_LAN_Id &&
                e.ELA_CLA_Id == entidad.ELA_CLA_Id &&
                e.ELA_FechaInicio == entidad.ELA_FechaInicio
            ))
            {
                mensaje = "Ya existe un registro para la Entidad - Licencia Anual - Contrato.";
                return false;
            }

            var nuevo = new Entes_LicenciasAnuales
            {
                ELA_ENT_Id = entidad.ELA_ENT_Id,
                ELA_LAN_Id = entidad.ELA_LAN_Id,
                ELA_CLA_Id = entidad.ELA_CLA_Id,
                ELA_FechaInicio = newStart,
                ELA_FechaFin = newEnd
            };
            bd.Entes_LicenciasAnuales.InsertOnSubmit(nuevo);
            bd.SubmitChanges();

            return true;
        }

        public List<EnteLicenciaAnualDto> ObtenerLicenciasAnualPorEnte(int idEnte)
        {
            var query = from el in bd.Entes_LicenciasAnuales
                        join lic in bd.LicenciasAnuales on el.ELA_LAN_Id equals lic.LAN_Id
                        where el.ELA_ENT_Id == idEnte
                        select new EnteLicenciaAnualDto
                        {
                            EntidadId = el.ELA_ENT_Id,
                            LicenciaAnualId = lic.LAN_Id,
                            NombreLicenciaAnual = lic.LAN_Nombre,
                            FechaInicio = el.ELA_FechaInicio,
                            FechaFin = el.ELA_FechaFin,
                            ContratoId = el.ELA_CLA_Id,
                            //Facturada = el.ELA_Facturada
                        };

            return query.ToList();
        }

        public bool EliminarEnteLicenciaAnual(int idEnte, int idLicenciaAnual, DateTime fechaInicio)
        {
            var enteLicencia = bd.Entes_LicenciasAnuales
                .FirstOrDefault(t => t.ELA_ENT_Id == idEnte && t.ELA_LAN_Id == idLicenciaAnual && t.ELA_FechaInicio == fechaInicio);

            if (enteLicencia != null)
            {
                bd.Entes_LicenciasAnuales.DeleteOnSubmit(enteLicencia);
                bd.SubmitChanges();

                return true;
            }

            return false;
        }

        public bool ActualizarFacturada(int idEnte, int idLicenciaAnual, int idContrato, bool nuevoValor, out string mensaje)
        {
            mensaje = null;

            var registro = bd.Entes_LicenciasAnuales
                .FirstOrDefault(e =>
                    e.ELA_ENT_Id == idEnte &&
                    e.ELA_LAN_Id == idLicenciaAnual &&
                    e.ELA_CLA_Id == idContrato
                );

            if (registro == null)
            {
                mensaje = "No se encontró el registro con los datos proporcionados.";
                return false;
            }

            //registro.ELA_Facturada = nuevoValor;
            bd.SubmitChanges();

            return true;
        }

        public bool ActualizarNumLicencias(int idContrato, int idLicenciaAnual, int nuevoNumero, out string mensaje)
        {
            mensaje = null;

            var tarifa = bd.ContratosLicenciasAnuales_Tarifas
                .FirstOrDefault(t => t.CLT_CLA_Id == idContrato && t.CLT_LAN_Id == idLicenciaAnual);

            if (tarifa == null)
            {
                mensaje = "No se encontró la tarifa para el contrato y licencia especificados.";
                return false;
            }

            //tarifa.CLT_NumLicencias = tarifa.CLT_NumLicencias + nuevoNumero;
            bd.SubmitChanges();

            return true;
        }

    }
}