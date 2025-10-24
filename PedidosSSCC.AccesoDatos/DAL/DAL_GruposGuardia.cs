using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_GruposGuardia : DAL_Base<GruposGuardia>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<GruposGuardia> Tabla => bd.GruposGuardia;

        protected override bool Guardar(GruposGuardia entidad, int idPersonaModificacion)
        {
            var existingLicencia = bd.GruposGuardia
                .FirstOrDefault(t => t.GRG_Nombre == entidad.GRG_Nombre && t.GRG_Id != entidad.GRG_Id);

            if (existingLicencia != null)
            {
                // Ya hay otra licencia con el mismo nombre → error
                return false;
            }

            var item = L_PrimaryKey(entidad.GRG_Id);

            if (item == null)
            {
                item = new GruposGuardia
                {
                    GRG_Nombre = entidad.GRG_Nombre
                };
                bd.GruposGuardia.InsertOnSubmit(item);
                bd.SubmitChanges();
            }
            else
            {
                item.GRG_Nombre = entidad.GRG_Nombre;
                bd.SubmitChanges();
            }

            return true;
        }

        public bool Eliminar(GruposGuardia entidad)
        {
            try
            {
                // Eliminar la GruposGuardia
                var objEliminar = bd.GruposGuardia.FirstOrDefault(l => l.GRG_Id == entidad.GRG_Id);

                if (objEliminar != null)
                {
                    bd.GruposGuardia.DeleteOnSubmit(objEliminar);
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

