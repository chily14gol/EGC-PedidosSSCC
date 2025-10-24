
using System;
using System.Collections.Generic;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Licencias_Excepciones_LicenciasReemplazo : DAL_Base<Licencias_Excepciones_LicenciasReemplazo>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Licencias_Excepciones_LicenciasReemplazo> Tabla => bd.Licencias_Excepciones_LicenciasReemplazo;

        protected override bool Guardar(Licencias_Excepciones_LicenciasReemplazo entidad, int idPersonaModificacion)
        {
            return true;
        }
    }
}

