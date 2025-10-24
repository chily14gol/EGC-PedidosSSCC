using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_TiposTarea : DAL_Base<TiposTarea>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<TiposTarea> Tabla => bd.TiposTarea;

        protected override bool Guardar(TiposTarea entidad, int idPersonaModificacion)
        {
            if (string.IsNullOrWhiteSpace(entidad.TTA_Nombre))
                return false;

            var item = L_PrimaryKey(entidad.TTA_Id);

            if (item == null)
            {
                if (bd.TiposTarea.Any(t => t.TTA_Nombre == entidad.TTA_Nombre))
                    return false;
                item = new TiposTarea();
                bd.TiposTarea.InsertOnSubmit(item);
            }
            else
            {
                // Si el nombre cambia, comprobar duplicado
                if (item.TTA_Nombre != entidad.TTA_Nombre && bd.TiposTarea.Any(t => t.TTA_Nombre == entidad.TTA_Nombre && t.TTA_Id != entidad.TTA_Id))
                    return false;
            }

            item.TTA_Nombre = entidad.TTA_Nombre;

            bd.SubmitChanges();
            return true;
        }
    }
}

