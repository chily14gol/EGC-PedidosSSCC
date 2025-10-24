using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_EstadosTicket : DAL_Base<EstadosTicket>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<EstadosTicket> Tabla => bd.EstadosTicket;

        protected override bool Guardar(EstadosTicket entidad, int idPersonaModificacion)
        {
            var existingLicencia = bd.EstadosTicket
                .FirstOrDefault(t => t.ETK_Nombre == entidad.ETK_Nombre && t.ETK_Id != entidad.ETK_Id);

            if (existingLicencia != null)
            {
                // Ya hay otra licencia con el mismo nombre → error
                return false;
            }

            var item = L_PrimaryKey(entidad.ETK_Id);

            if (item == null)
            {
                item = new EstadosTicket
                {
                    ETK_Nombre = entidad.ETK_Nombre
                };
                bd.EstadosTicket.InsertOnSubmit(item);
                bd.SubmitChanges();
            }
            else
            {
                item.ETK_Nombre = entidad.ETK_Nombre;
                bd.SubmitChanges();
            }

            return true;
        }

        public bool Eliminar(EstadosTicket entidad)
        {
            try
            {
                // Eliminar la EstadosTicket
                var objEliminar = bd.EstadosTicket.FirstOrDefault(l => l.ETK_Id == entidad.ETK_Id);

                if (objEliminar != null)
                {
                    bd.EstadosTicket.DeleteOnSubmit(objEliminar);
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

