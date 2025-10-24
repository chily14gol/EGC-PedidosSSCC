using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Licencias : DAL_Base<Licencias>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Licencias> Tabla => bd.Licencias;

        protected override bool Guardar(Licencias entidad, int idPersonaModificacion)
        {
            // 1) Verificar que no exista otra licencia con el mismo nombre (distinta ID)
            var existingLicencia = bd.Licencias
                .FirstOrDefault(t => t.LIC_Nombre == entidad.LIC_Nombre && t.LIC_Id != entidad.LIC_Id);
            if (existingLicencia != null)
                return false; // Ya existe otra con el mismo nombre

            // 2) Insertar o actualizar la fila principal
            var item = L_PrimaryKey(entidad.LIC_Id);

            if (item == null)
            {
                // Alta nueva
                item = new Licencias
                {
                    LIC_Nombre = entidad.LIC_Nombre,
                    LIC_MaximoGrupo = entidad.LIC_MaximoGrupo,
                    LIC_LIC_Id_Padre = entidad.LIC_LIC_Id_Padre,
                    LIC_TAR_Id_SW = entidad.LIC_TAR_Id_SW,
                    LIC_TAR_Id_Antivirus = entidad.LIC_TAR_Id_Antivirus,
                    LIC_TAR_Id_Backup = entidad.LIC_TAR_Id_Backup,
                    LIC_NombreMS = string.IsNullOrEmpty(entidad.LIC_NombreMS) ? "" : entidad.LIC_NombreMS,
                    LIC_Gestionado = entidad.LIC_Gestionado
                };

                bd.Licencias.InsertOnSubmit(item);
                bd.SubmitChanges(); // Para que item.LIC_Id se autogenere
            }
            else
            {
                // Edición
                item.LIC_Nombre = entidad.LIC_Nombre;
                item.LIC_MaximoGrupo = entidad.LIC_MaximoGrupo;
                item.LIC_LIC_Id_Padre = entidad.LIC_LIC_Id_Padre;
                item.LIC_TAR_Id_SW = entidad.LIC_TAR_Id_SW;
                item.LIC_TAR_Id_Antivirus = entidad.LIC_TAR_Id_Antivirus;
                item.LIC_TAR_Id_Backup = entidad.LIC_TAR_Id_Backup;
                item.LIC_NombreMS = string.IsNullOrEmpty(entidad.LIC_NombreMS) ? "" : entidad.LIC_NombreMS;
                item.LIC_Gestionado = entidad.LIC_Gestionado;

                bd.SubmitChanges();
            }

            // 3) Siempre eliminar todas las filas hijas de TiposEnte y luego reinsertar las que queden
            {
                // Eliminar existentes
                var tiposExistentes = bd.Licencias_TiposEnte.Where(x => x.LTE_LIC_Id == item.LIC_Id);
                bd.Licencias_TiposEnte.DeleteAllOnSubmit(tiposExistentes);

                // Insertar solo si vienen IDs nuevos
                if (entidad.TiposEnte != null && entidad.TiposEnte.Any())
                {
                    foreach (var tenId in entidad.TiposEnte.Distinct())
                    {
                        bd.Licencias_TiposEnte.InsertOnSubmit(new Licencias_TiposEnte
                        {
                            LTE_LIC_Id = item.LIC_Id,
                            LTE_TEN_Id = tenId
                        });
                    }
                }

                bd.SubmitChanges();
            }

            // 4) Hacemos lo mismo con LicenciasIncompatibles
            {
                // Eliminar existentes
                var licAct = bd.Licencias_Incompatibles.Where(x => x.LIL_LIC_Id1 == item.LIC_Id);
                bd.Licencias_Incompatibles.DeleteAllOnSubmit(licAct);

                // Insertar solo si vienen IDs nuevos
                if (entidad.LicenciasIncompatibles != null && entidad.LicenciasIncompatibles.Any())
                {
                    foreach (var idLic in entidad.LicenciasIncompatibles.Distinct())
                    {
                        bd.Licencias_Incompatibles.InsertOnSubmit(new Licencias_Incompatibles
                        {
                            LIL_LIC_Id1 = item.LIC_Id,
                            LIL_LIC_Id2 = idLic
                        });
                    }
                }

                bd.SubmitChanges();
            }

            return true;
        }

        public bool Eliminar(Licencias entidad)
        {
            try
            {
                // Eliminar tarifas
                var tarifas = bd.Licencias_Tarifas.Where(t => t.LIT_LIC_Id == entidad.LIC_Id);
                bd.Licencias_Tarifas.DeleteAllOnSubmit(tarifas);

                // Eliminar excepciones
                var excepciones = bd.Licencias_Excepciones.Where(e => e.LIE_LIC_Id == entidad.LIC_Id);
                bd.Licencias_Excepciones.DeleteAllOnSubmit(excepciones);

                // Eliminar licencia
                var lic = bd.Licencias.FirstOrDefault(l => l.LIC_Id == entidad.LIC_Id);
                if (lic != null)
                    bd.Licencias.DeleteOnSubmit(lic);

                bd.SubmitChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}