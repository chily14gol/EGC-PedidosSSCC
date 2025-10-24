using System;
using System.Collections.Generic;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Licencias_Tarifas : DAL_Base<Licencias_Tarifas>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Licencias_Tarifas> Tabla => bd.Licencias_Tarifas;

        protected override bool Guardar(Licencias_Tarifas entidad, int idPersonaModificacion)
        {
            return true;
        }

        public bool GuardarTarifa(Licencias_Tarifas entidad, bool esAlta, int idPersonaModificacion)
        {
            if (entidad.LIT_FechaFin.HasValue && entidad.LIT_FechaFin.Value == DateTime.MinValue)
                entidad.LIT_FechaFin = null;

            // Construimos un IQueryable con todas las tarifas de la misma licencia
            var query = bd.Licencias_Tarifas
                          .Where(t => t.LIT_LIC_Id == entidad.LIT_LIC_Id);

            if (esAlta)
            {
                // Traer todas las tarifas existentes a memoria
                var tarifasExistentes = query.ToList();

                // Verificar si alguna se solapa con el nuevo rango
                bool haySolapamiento = tarifasExistentes.Any(t =>
                {
                    var finExistente = t.LIT_FechaFin ?? DateTime.MaxValue;
                    var finNueva = entidad.LIT_FechaFin ?? DateTime.MaxValue;
                    return t.LIT_FechaInicio <= finNueva && entidad.LIT_FechaInicio <= finExistente;
                });

                if (haySolapamiento)
                    return false;

                var nueva = new Licencias_Tarifas
                {
                    LIT_LIC_Id = entidad.LIT_LIC_Id,
                    LIT_FechaInicio = entidad.LIT_FechaInicio,
                    LIT_FechaFin = entidad.LIT_FechaFin,
                    LIT_PrecioUnitarioSW = entidad.LIT_PrecioUnitarioSW,
                    LIT_PrecioUnitarioAntivirus = entidad.LIT_PrecioUnitarioAntivirus,
                    LIT_PrecioUnitarioBackup = entidad.LIT_PrecioUnitarioBackup
                };

                bd.Licencias_Tarifas.InsertOnSubmit(nueva);
            }

            else
            {
                // Edición: localizamos el registro original
                var orig = query.FirstOrDefault(t =>
                    t.LIT_FechaInicio == entidad.LIT_FechaInicio);
                if (orig == null) return false;

                // Chequeo de solapamiento contra _otras_ tarifas
                bool haySolap = query
                    .Where(t => t.LIT_FechaInicio != orig.LIT_FechaInicio)
                    .Any(t => FechasSeSolapan(
                        t.LIT_FechaInicio, t.LIT_FechaFin,
                        entidad.LIT_FechaInicio, entidad.LIT_FechaFin));
                if (haySolap)
                    return false;

                // Actualizamos sólo fechas y precios
                orig.LIT_FechaFin = entidad.LIT_FechaFin;
                orig.LIT_PrecioUnitarioSW = entidad.LIT_PrecioUnitarioSW;
                orig.LIT_PrecioUnitarioAntivirus = entidad.LIT_PrecioUnitarioAntivirus;
                orig.LIT_PrecioUnitarioBackup = entidad.LIT_PrecioUnitarioBackup;
            }

            bd.SubmitChanges();
            return true;
        }

        // Función auxiliar (puede vivir en la misma clase o en una util)
        private bool FechasSeSolapan(DateTime start1, DateTime? end1, DateTime start2, DateTime? end2)
        {
            var fin1 = end1 ?? DateTime.MaxValue;
            var fin2 = end2 ?? DateTime.MaxValue;
            return start1 <= fin2 && start2 <= fin1;
        }

        public List<Licencias_Tarifas> ObtenerTarifasPorLicencia(int licenciaId)
        {
            return bd.Licencias_Tarifas.Where(t => t.LIT_LIC_Id == licenciaId).ToList();
        }

        public bool EliminarTarifa(int idLicencia, DateTime fechaInicio)
        {
            var tarifa = bd.Licencias_Tarifas
                .FirstOrDefault(t => t.LIT_LIC_Id == idLicencia && t.LIT_FechaInicio == fechaInicio);

            if (tarifa != null)
            {
                bd.Licencias_Tarifas.DeleteOnSubmit(tarifa);
                bd.SubmitChanges();
                return true;
            }
            return false;
        }
    }
}
