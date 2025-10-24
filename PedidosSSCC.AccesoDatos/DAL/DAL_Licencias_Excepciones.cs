using System;
using System.Collections.Generic;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Licencias_Excepciones : DAL_Base<Licencias_Excepciones>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Licencias_Excepciones> Tabla => bd.Licencias_Excepciones;

        protected override bool Guardar(Licencias_Excepciones entidad, int idPersonaModificacion)
        {
            if (entidad.LIE_EMP_Id_Original == null || entidad.LIE_EMP_Id_Original == 0)
            {
                // Alta: validar que no exista ya esa combinación empresa + licencia
                bool yaExiste = bd.Licencias_Excepciones.Any(e =>
                    e.LIE_EMP_Id == entidad.LIE_EMP_Id &&
                    e.LIE_LIC_Id == entidad.LIE_LIC_Id);

                if (yaExiste)
                    return false;

                bd.Licencias_Excepciones.InsertOnSubmit(entidad);
                bd.SubmitChanges();
            }
            else
            {
                // Edición: buscar el registro original
                var existente = bd.Licencias_Excepciones.FirstOrDefault(e =>
                    e.LIE_EMP_Id == entidad.LIE_EMP_Id_Original &&
                    e.LIE_LIC_Id == entidad.LIE_LIC_Id);

                if (existente == null)
                    return false;

                // Si se cambia la empresa, verificar duplicado (empresa + licencia)
                if (entidad.LIE_EMP_Id != entidad.LIE_EMP_Id_Original)
                {
                    bool yaExiste = bd.Licencias_Excepciones.Any(e =>
                        e.LIE_EMP_Id == entidad.LIE_EMP_Id &&
                        e.LIE_LIC_Id == entidad.LIE_LIC_Id);

                    if (yaExiste)
                        return false;
                }

                existente.LIE_EMP_Id = entidad.LIE_EMP_Id;
                existente.LIE_CorreccionFacturacion = entidad.LIE_CorreccionFacturacion;
                bd.SubmitChanges();
            }

            // --- Actualizar Licencias Reemplazo: siempre borramos los anteriores ---
            var dalReemplazos = new DAL_Licencias_Excepciones_LicenciasReemplazo();

            // Elimina los reemplazos anteriores para esa licencia y empresa
            var reemplazosAnteriores = bd.Licencias_Excepciones_LicenciasReemplazo
                .Where(x => x.LEL_LIE_LIC_Id == entidad.LIE_LIC_Id && x.LEL_LIE_EMP_Id == entidad.LIE_EMP_Id)
                .ToList();
            bd.Licencias_Excepciones_LicenciasReemplazo.DeleteAllOnSubmit(reemplazosAnteriores);
            bd.SubmitChanges();

            // Si vienen nuevos reemplazos, los insertamos
            if (entidad.LicenciasReemplazo != null && entidad.LicenciasReemplazo.Any())
            {
                foreach (int idReemplazo in entidad.LicenciasReemplazo.Distinct())
                {
                    var nuevo = new Licencias_Excepciones_LicenciasReemplazo
                    {
                        LEL_LIE_LIC_Id = entidad.LIE_LIC_Id,
                        LEL_LIE_EMP_Id = entidad.LIE_EMP_Id,
                        LEL_LIC_Id_Reemplazo = idReemplazo
                    };
                    bd.Licencias_Excepciones_LicenciasReemplazo.InsertOnSubmit(nuevo);
                }
                bd.SubmitChanges();
            }

            return true;
        }

        public List<object> ObtenerExcepcionesPorLicencia(int licenciaId)
        {
            return bd.Licencias_Excepciones
              .Where(t => t.LIE_LIC_Id == licenciaId)
              .Select(t => new
              {
                  t.LIE_LIC_Id,
                  t.LIE_EMP_Id,
                  t.LIE_CorreccionFacturacion,
                  EmpresaNombre = t.Empresas.EMP_Nombre,
                  // Aquí buscamos en la tabla intermedia todos los reemplazos
                  LicenciasReemplazoNombres = bd.Licencias_Excepciones_LicenciasReemplazo
                      .Where(r => r.LEL_LIE_LIC_Id == t.LIE_LIC_Id
                               && r.LEL_LIE_EMP_Id == t.LIE_EMP_Id)
                      .Select(r => r.Licencias.LIC_Nombre)  // o el nombre exacto de la columna
                      .ToList()
              })
              .ToList<object>();
        }

        public bool EliminarExcepcion(int idLicencia, int idEmpresa)
        {
            // 1. Eliminar primero los reemplazos asociados
            var reemplazos = bd.Licencias_Excepciones_LicenciasReemplazo
                .Where(r => r.LEL_LIE_LIC_Id == idLicencia && r.LEL_LIE_EMP_Id == idEmpresa)
                .ToList();

            if (reemplazos.Any())
            {
                bd.Licencias_Excepciones_LicenciasReemplazo.DeleteAllOnSubmit(reemplazos);
                bd.SubmitChanges();
            }

            // 2. Eliminar la excepción principal
            var excepcion = bd.Licencias_Excepciones
                .FirstOrDefault(e => e.LIE_LIC_Id == idLicencia && e.LIE_EMP_Id == idEmpresa);

            if (excepcion != null)
            {
                bd.Licencias_Excepciones.DeleteOnSubmit(excepcion);
                bd.SubmitChanges();
                return true;
            }

            return false;
        }
    }
}
