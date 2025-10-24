using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Proveedores_ContratosSoporte_Reparto : DAL_Base<Proveedores_ContratosSoporte_Reparto>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Proveedores_ContratosSoporte_Reparto> Tabla => bd.Proveedores_ContratosSoporte_Reparto;

        protected override bool Guardar(Proveedores_ContratosSoporte_Reparto obj, int idPersonaModificacion)
        {
            var item = L_PrimaryKey(obj.PVR_PVC_Id + "|" + obj.PVR_EMP_Id);

            if (item == null)
            {
                bd.Proveedores_ContratosSoporte_Reparto.InsertOnSubmit(obj);
            }
            else
            {
                item.PVR_EMP_Id = obj.PVR_EMP_Id;
                item.PVR_Porcentaje = obj.PVR_Porcentaje;
            }

            bd.SubmitChanges();
            return true;
        }

        public void EliminarPorContrato(int contratoId)
        {
            var items = bd.Proveedores_ContratosSoporte_Reparto
                          .Where(r => r.PVR_PVC_Id == contratoId);
            bd.Proveedores_ContratosSoporte_Reparto.DeleteAllOnSubmit(items);
            bd.SubmitChanges();
        }

    }
}