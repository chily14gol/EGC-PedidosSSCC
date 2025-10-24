using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_CategoriasTicket : DAL_Base<CategoriasTicket>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<CategoriasTicket> Tabla => bd.CategoriasTicket;

        protected override bool Guardar(CategoriasTicket entidad, int idPersonaModificacion)
        {
            bd.SubmitChanges();
            return true;
        }
    }
}

