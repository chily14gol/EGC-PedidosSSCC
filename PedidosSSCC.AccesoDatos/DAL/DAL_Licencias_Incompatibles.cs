using System;
using System.Collections.Generic;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Licencias_Incompatibles : DAL_Base<Licencias_Incompatibles>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Licencias_Incompatibles> Tabla => bd.Licencias_Incompatibles;

        protected override bool Guardar(Licencias_Incompatibles entidad, int idPersonaModificacion)
        {
            // Validar que no exista ya la incompatibilidad en ningún orden (LIL_LIC_Id1 - LIL_LIC_Id2 o al revés)
            bool yaExiste = bd.Licencias_Incompatibles.Any(l =>
                (l.LIL_LIC_Id1 == entidad.LIL_LIC_Id1 && l.LIL_LIC_Id2 == entidad.LIL_LIC_Id2) ||
                (l.LIL_LIC_Id1 == entidad.LIL_LIC_Id2 && l.LIL_LIC_Id2 == entidad.LIL_LIC_Id1));

            if (yaExiste)
                return false;

            bd.Licencias_Incompatibles.InsertOnSubmit(entidad);
            bd.SubmitChanges();
            return true;
        }

        public List<object> ObtenerIncompatibilidadesPorLicencia(int licenciaId)
        {
            var licencias = bd.Licencias.ToDictionary(l => l.LIC_Id, l => l.LIC_Nombre);

            return bd.Licencias_Incompatibles
                .Where(l => l.LIL_LIC_Id1 == licenciaId || l.LIL_LIC_Id2 == licenciaId)
                .Select(l => new
                {
                    LIC_Id = l.LIL_LIC_Id1 == licenciaId ? l.LIL_LIC_Id2 : l.LIL_LIC_Id1
                })
                .AsEnumerable()
                .Select(l => new
                {
                    l.LIC_Id,
                    NombreLicenciaIncompatible = licencias.ContainsKey(l.LIC_Id) ? licencias[l.LIC_Id] : "Desconocida"
                })
                .ToList<object>();
        }

        public bool EliminarIncompatibilidad(int idLic1, int idLic2)
        {
            var incompatibilidad = bd.Licencias_Incompatibles.FirstOrDefault(l =>
                (l.LIL_LIC_Id1 == idLic1 && l.LIL_LIC_Id2 == idLic2) ||
                (l.LIL_LIC_Id1 == idLic2 && l.LIL_LIC_Id2 == idLic1));

            if (incompatibilidad != null)
            {
                bd.Licencias_Incompatibles.DeleteOnSubmit(incompatibilidad);
                bd.SubmitChanges();
                return true;
            }

            return false;
        }
    }
}
