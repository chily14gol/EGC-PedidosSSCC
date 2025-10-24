using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_ValidacionesTicket : DAL_Base<ValidacionesTicket>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<ValidacionesTicket> Tabla => bd.ValidacionesTicket;

        protected override bool Guardar(ValidacionesTicket entidad, int idPersonaModificacion)
        {
            var existingLicencia = bd.ValidacionesTicket
                .FirstOrDefault(t => t.VTK_Nombre == entidad.VTK_Nombre && t.VTK_Id != entidad.VTK_Id);

            if (existingLicencia != null)
            {
                // Ya hay otra licencia con el mismo nombre → error
                return false;
            }

            var item = L_PrimaryKey(entidad.VTK_Id);

            if (item == null)
            {
                item = new ValidacionesTicket
                {
                    VTK_Nombre = entidad.VTK_Nombre
                };
                bd.ValidacionesTicket.InsertOnSubmit(item);
                bd.SubmitChanges();
            }
            else
            {
                item.VTK_Nombre = entidad.VTK_Nombre;
                bd.SubmitChanges();
            }

            return true;
        }

        public bool Eliminar(ValidacionesTicket entidad)
        {
            try
            {
                // Eliminar la ValidacionesTicket
                var objEliminar = bd.ValidacionesTicket.FirstOrDefault(l => l.VTK_Id == entidad.VTK_Id);

                if (objEliminar != null)
                {
                    bd.ValidacionesTicket.DeleteOnSubmit(objEliminar);
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

