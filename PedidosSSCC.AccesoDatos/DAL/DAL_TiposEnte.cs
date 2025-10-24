using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_TiposEnte : DAL_Base<TiposEnte>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<TiposEnte> Tabla => bd.TiposEnte;

        protected override bool Guardar(TiposEnte entidad, int idPersonaModificacion)
        {
            if (string.IsNullOrWhiteSpace(entidad.TEN_Nombre))
                return false;

            var item = L_PrimaryKey(entidad.TEN_Id);

            if (item == null)
            {
                if (bd.TiposEnte.Any(t => t.TEN_Nombre == entidad.TEN_Nombre))
                    return false;
                item = new TiposEnte();
                bd.TiposEnte.InsertOnSubmit(item);
            }
            else
            {
                // Si el nombre cambia, comprobar duplicado
                if (item.TEN_Nombre != entidad.TEN_Nombre && bd.TiposEnte.Any(t => t.TEN_Nombre == entidad.TEN_Nombre && t.TEN_Id != entidad.TEN_Id))
                    return false;
            }

            item.TEN_Nombre = entidad.TEN_Nombre;

            bd.SubmitChanges();
            return true;
        }
    }
}
