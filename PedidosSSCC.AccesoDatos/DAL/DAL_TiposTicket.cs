using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_TiposTicket : DAL_Base<TiposTicket>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<TiposTicket> Tabla => bd.TiposTicket;

        protected override bool Guardar(TiposTicket entidad, int idPersonaModificacion)
        {
            var existingLicencia = bd.TiposTicket
                .FirstOrDefault(t => t.TTK_Nombre == entidad.TTK_Nombre && t.TTK_Id != entidad.TTK_Id);

            if (existingLicencia != null)
            {
                // Ya hay otra TiposTicket con el mismo nombre → error
                return false;
            }

            var item = L_PrimaryKey(entidad.TTK_Id);

            if (item == null)
            {
                item = new TiposTicket
                {
                    TTK_Nombre = entidad.TTK_Nombre
                };
                bd.TiposTicket.InsertOnSubmit(item);
                bd.SubmitChanges();
            }
            else
            {
                item.TTK_Nombre = entidad.TTK_Nombre;
                bd.SubmitChanges();
            }

            return true;
        }

        public bool Eliminar(TiposTicket entidad)
        {
            try
            {
                // Eliminar la TiposTicket
                var objEliminar = bd.TiposTicket.FirstOrDefault(l => l.TTK_Id == entidad.TTK_Id);

                if (objEliminar != null)
                {
                    bd.TiposTicket.DeleteOnSubmit(objEliminar);
                }

                // Confirmar cambios
                bd.SubmitChanges();
                return true;
            }
            catch (Exception ex)
            {
                // Loguea si quieres: Logger.Error(ex);
                return false;
            }
        }
    }
}




