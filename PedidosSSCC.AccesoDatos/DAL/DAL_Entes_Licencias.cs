using System;
using System.Collections.Generic;
using System.Linq;

namespace AccesoDatos
{
    public class EnteLicenciaDto
    {
        public int EntidadId { get; set; }
        public string NombreEntidad { get; set; }
        public int LicenciaId { get; set; }
        public string NombreLicencia { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string EmpresaNombre { get; set; }
    }

    public class DAL_Entes_Licencias : DAL_Base<Entes_Licencias>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Entes_Licencias> Tabla => bd.Entes_Licencias;

        protected override bool Guardar(Entes_Licencias entidad, int idPersonaModificacion)
        {
            // Normalizo fecha fin vacía
            if (entidad.ENL_FechaFin.HasValue && entidad.ENL_FechaFin.Value == DateTime.MinValue)
                entidad.ENL_FechaFin = null;

            // Valores originales para localizar registro (edición)
            DateTime fechaInicioOrig = entidad.ENL_FechaInicioOriginal ?? entidad.ENL_FechaInicio;
            int licOrig = (entidad.ENL_LIC_IdOriginal.HasValue && entidad.ENL_LIC_IdOriginal.Value != 0)
                   ? entidad.ENL_LIC_IdOriginal.Value
                   : entidad.ENL_LIC_Id;

            // Nuevo periodo que vamos a guardar
            DateTime newStart = entidad.ENL_FechaInicio;
            DateTime newEndNonNull = entidad.ENL_FechaFin ?? DateTime.MaxValue;

            // 1) Construyo consulta base para el mismo ente+licencia
            var q = bd.Entes_Licencias.Where(e =>
                e.ENL_ENT_Id == entidad.ENL_ENT_Id &&
                e.ENL_LIC_Id == entidad.ENL_LIC_Id);

            // Si edito, excluyo el registro original de la comprobación
            if (entidad.ENL_FechaInicioOriginal.HasValue)
            {
                q = q.Where(e =>
                    !(e.ENL_LIC_Id == licOrig &&
                      e.ENL_FechaInicio == fechaInicioOrig)
                );
            }

            // 2) Comprobación DE SOLAPAMIENTO en línea
            //    Un solapamiento existe si:
            //      existente.Start <= newEnd && existente.End   >= newStart
            if (q.Any(e =>
                e.ENL_FechaInicio <= newEndNonNull
                && (e.ENL_FechaFin ?? DateTime.MaxValue) >= newStart
            ))
            {
                throw new ApplicationException(
                    "El periodo indicado se solapa con otro existente para esta entidad y licencia."
                );
            }

            // 3) Localizo el registro original si existe (edición)
            var licenciaOriginal = bd.Entes_Licencias.FirstOrDefault(e =>
                e.ENL_ENT_Id == entidad.ENL_ENT_Id &&
                e.ENL_LIC_Id == licOrig &&
                e.ENL_FechaInicio == fechaInicioOrig);

            bool estaEditando = licenciaOriginal != null;
            bool cambiarLicencia = estaEditando && entidad.ENL_LIC_Id != licOrig;
            bool cambiarFechaInicio = estaEditando && fechaInicioOrig != entidad.ENL_FechaInicio;

            if (cambiarLicencia || cambiarFechaInicio)
            {
                // Elimino original y creo uno nuevo
                bd.Entes_Licencias.DeleteOnSubmit(licenciaOriginal);
                var nueva = new Entes_Licencias
                {
                    ENL_ENT_Id = entidad.ENL_ENT_Id,
                    ENL_LIC_Id = entidad.ENL_LIC_Id,
                    ENL_FechaInicio = entidad.ENL_FechaInicio,
                    ENL_FechaFin = entidad.ENL_FechaFin
                };
                bd.Entes_Licencias.InsertOnSubmit(nueva);
            }
            else if (estaEditando)
            {
                // Sólo actualizo fecha fin
                licenciaOriginal.ENL_FechaFin = entidad.ENL_FechaFin;
            }
            else
            {
                // Alta nueva (ya validé solapamientos): prevengo duplicados exactos
                bool yaExiste = bd.Entes_Licencias.Any(e =>
                    e.ENL_ENT_Id == entidad.ENL_ENT_Id &&
                    e.ENL_LIC_Id == entidad.ENL_LIC_Id &&
                    e.ENL_FechaInicio == entidad.ENL_FechaInicio);

                if (yaExiste)
                    throw new ApplicationException("Ya existe un registro idéntico.");

                var nueva = new Entes_Licencias
                {
                    ENL_ENT_Id = entidad.ENL_ENT_Id,
                    ENL_LIC_Id = entidad.ENL_LIC_Id,
                    ENL_FechaInicio = entidad.ENL_FechaInicio,
                    ENL_FechaFin = entidad.ENL_FechaFin
                };
                bd.Entes_Licencias.InsertOnSubmit(nueva);
            }

            bd.SubmitChanges();
            return true;
        }

        public List<EnteLicenciaDto> ObtenerLicenciasPorEnte(int idEnte)
        {
            var query = from el in bd.Entes_Licencias
                        join lic in bd.Licencias on el.ENL_LIC_Id equals lic.LIC_Id
                        where el.ENL_ENT_Id == idEnte
                        orderby lic.LIC_Nombre
                        select new EnteLicenciaDto
                        {
                            EntidadId = el.ENL_ENT_Id,
                            LicenciaId = lic.LIC_Id,
                            NombreLicencia = lic.LIC_Nombre,
                            FechaInicio = el.ENL_FechaInicio,
                            FechaFin = el.ENL_FechaFin
                        };

            return query.ToList();
        }

        public List<EnteLicenciaDto> ObtenerEntesPorLicencia(int idLicencia)
        {
            var query =
                from el in bd.Entes_Licencias
                join ent in bd.Entes
                    on el.ENL_ENT_Id equals ent.ENT_Id
                join emp in bd.Empresas
                    on ent.ENT_EMP_Id equals emp.EMP_Id into empJoin
                from emp in empJoin.DefaultIfEmpty()
                join ofi in bd.Oficinas
                    on ent.ENT_OFI_Id equals ofi.OFI_Id into ofiJoin
                from ofi in ofiJoin.DefaultIfEmpty()
                where el.ENL_LIC_Id == idLicencia
                orderby ent.ENT_Nombre
                select new EnteLicenciaDto
                {
                    EntidadId = el.ENL_ENT_Id,
                    NombreEntidad = ent.ENT_Nombre,
                    LicenciaId = el.ENL_LIC_Id,
                    FechaInicio = el.ENL_FechaInicio,
                    FechaFin = el.ENL_FechaFin,
                    EmpresaNombre = emp != null ? emp.EMP_Nombre : null,
                };

            return query.ToList();
        }

        public bool EliminarEnteLicencia(int idEnte, int idLicencia, DateTime fechaInicio)
        {
            var enteLicencia = bd.Entes_Licencias
                .FirstOrDefault(t => t.ENL_ENT_Id == idEnte && t.ENL_LIC_Id == idLicencia && t.ENL_FechaInicio == fechaInicio);

            if (enteLicencia != null)
            {
                bd.Entes_Licencias.DeleteOnSubmit(enteLicencia);
                bd.SubmitChanges();

                return true;
            }

            return false;
        }
    }
}

