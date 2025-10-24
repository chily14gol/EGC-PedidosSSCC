using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_OrigenesTicket : DAL_Base<OrigenesTicket>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<OrigenesTicket> Tabla => bd.OrigenesTicket;

        protected override bool Guardar(OrigenesTicket entidad, int idPersonaModificacion)
        {
            var existingLicencia = bd.OrigenesTicket
                .FirstOrDefault(t => t.OTK_Nombre == entidad.OTK_Nombre && t.OTK_Id != entidad.OTK_Id);

            if (existingLicencia != null)
            {
                // Ya hay otra OrigenesTicket con el mismo nombre → error
                return false;
            }

            var item = L_PrimaryKey(entidad.OTK_Id);

            if (item == null)
            {
                item = new OrigenesTicket
                {
                    OTK_Nombre = entidad.OTK_Nombre
                };
                bd.OrigenesTicket.InsertOnSubmit(item);
                bd.SubmitChanges();
            }
            else
            {
                item.OTK_Nombre = entidad.OTK_Nombre;
                bd.SubmitChanges();
            }

            return true;
        }

        public bool Eliminar(OrigenesTicket entidad)
        {
            try
            {
                // Eliminar la OrigenesTicket
                var objEliminar = bd.OrigenesTicket.FirstOrDefault(l => l.OTK_Id == entidad.OTK_Id);

                if (objEliminar != null)
                {
                    bd.OrigenesTicket.DeleteOnSubmit(objEliminar);
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


