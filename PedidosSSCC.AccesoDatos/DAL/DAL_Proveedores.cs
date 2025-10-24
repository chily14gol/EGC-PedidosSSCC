using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Proveedores : DAL_Base<Proveedores>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Proveedores> Tabla => bd.Proveedores;

        protected override bool Guardar(Proveedores entidad, int idPersonaModificacion)
        {
            var proveedorExistente = bd.Proveedores
                .FirstOrDefault(p => p.PRV_CIF == entidad.PRV_CIF && p.PRV_Id != entidad.PRV_Id);

            if (proveedorExistente != null)
            {
                // Ya existe otro proveedor con el mismo CIF
                return false;
            }

            var item = L_PrimaryKey(entidad.PRV_Id);

            if (item == null)
            {
                item = new Proveedores
                {
                    PRV_CIF = entidad.PRV_CIF,
                    PRV_Nombre = entidad.PRV_Nombre,
                    PRV_Activo = entidad.PRV_Activo,
                    PRV_TAR_Id_Soporte = entidad.PRV_TAR_Id_Soporte,
                    PRV_PlantillaExcel = entidad.PRV_PlantillaExcel,
                    FechaAlta = DateTime.Now,
                    FechaModificacion = DateTime.Now,
                    PER_Id_Modificacion = idPersonaModificacion
                };
                bd.Proveedores.InsertOnSubmit(item);
            }
            else
            {
                item.PRV_CIF = entidad.PRV_CIF;
                item.PRV_Nombre = entidad.PRV_Nombre;
                item.PRV_Activo = entidad.PRV_Activo;
                item.PRV_TAR_Id_Soporte = entidad.PRV_TAR_Id_Soporte;
                item.PRV_PlantillaExcel = entidad.PRV_PlantillaExcel;
                item.FechaModificacion = DateTime.Now;
                item.PER_Id_Modificacion = idPersonaModificacion;
            }

            bd.SubmitChanges();
            return true;
        }


        public bool Eliminar(Proveedores entidad)
        {
            try
            {
                var proveedor = bd.Proveedores.FirstOrDefault(p => p.PRV_Id == entidad.PRV_Id);

                if (proveedor != null)
                {
                    bd.Proveedores.DeleteOnSubmit(proveedor);
                    bd.SubmitChanges();
                }

                return true;
            }
            catch (Exception ex)
            {
                // Logger.Error(ex);
                return false;
            }
        }
    }
}
