using System;
using System.Linq;
using System.Security.Cryptography;

namespace AccesoDatos
{
    public class DAL_Proveedores_ContratosSoporte : DAL_Base<Proveedores_ContratosSoporte>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Proveedores_ContratosSoporte> Tabla => bd.Proveedores_ContratosSoporte;

        protected override bool Guardar(Proveedores_ContratosSoporte obj, int idPersonaModificacion)
        {
            var finEntidad = obj.PVC_FechaFin ?? DateTime.MaxValue;

            // Validación de solapamiento de fechas
            bool haySolapamiento = bd.Proveedores_ContratosSoporte
                .Where(c =>
                    c.PVC_PRV_Id == obj.PVC_PRV_Id
                    // excluimos el propio registro cuando actualizamos
                    && c.PVC_Id != obj.PVC_Id
                    // tratamos FechaFin null como infinito:
                    && c.PVC_FechaInicio <= finEntidad
                    && (c.PVC_FechaFin ?? DateTime.MaxValue) >= obj.PVC_FechaInicio
                )
                .Any();

            if (haySolapamiento)
                return false;

            var item = L_PrimaryKey(obj.PVC_Id);

            if (item == null)
            {
                // Nuevo
                item = new Proveedores_ContratosSoporte
                {
                    PVC_PRV_Id = obj.PVC_PRV_Id,
                    PVC_FechaInicio = obj.PVC_FechaInicio,
                    PVC_FechaFin = obj.PVC_FechaFin,
                    PVC_PrecioHora = obj.PVC_PrecioHora,
                    PVC_HorasContratadas = obj.PVC_HorasContratadas
                };
                bd.Proveedores_ContratosSoporte.InsertOnSubmit(item);
            }
            else
            {
                // Actualizar
                item.PVC_PRV_Id = obj.PVC_PRV_Id;
                item.PVC_FechaInicio = obj.PVC_FechaInicio;
                item.PVC_FechaFin = obj.PVC_FechaFin;
                item.PVC_PrecioHora = obj.PVC_PrecioHora;
                item.PVC_HorasContratadas = obj.PVC_HorasContratadas;
            }

            bd.SubmitChanges();

            obj.PVC_Id = item.PVC_Id;

            return true;
        }
    }
}