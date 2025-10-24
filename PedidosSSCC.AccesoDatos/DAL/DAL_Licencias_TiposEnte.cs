using System;
using System.Collections.Generic;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Licencias_TiposEnte : DAL_Base<Licencias_TiposEnte>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Licencias_TiposEnte> Tabla => bd.Licencias_TiposEnte;

        protected override bool Guardar(Licencias_TiposEnte entidad, int idPersonaModificacion)
        {
            bd.SubmitChanges();
            return true;
        }

        public List<Licencias_TiposEnte> ObtenerTarifasPorLicencia(int licenciaId)
        {
            return bd.Licencias_TiposEnte.Where(t => t.LTE_LIC_Id == licenciaId).ToList();
        }

        public bool EliminarTarifa(int idLicencia, DateTime fechaInicio)
        {
            var ente = bd.Licencias_TiposEnte
                .FirstOrDefault(t => t.LTE_LIC_Id == idLicencia);

            if (ente != null)
            {
                bd.Licencias_TiposEnte.DeleteOnSubmit(ente);
                bd.SubmitChanges();

                return true;
            }

            return false;
        }
    }
}

