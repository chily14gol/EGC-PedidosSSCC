using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_ContratosCAU : DAL_Base<ContratosCAU>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<ContratosCAU> Tabla => bd.ContratosCAU;

        protected override bool Guardar(ContratosCAU entidad, int idPersonaModificacion)
        {
            var existingContrato = bd.ContratosCAU
                .FirstOrDefault(t =>
                    t.CCA_FechaFin == entidad.CCA_FechaFin &&
                    t.CCA_FechaInicio == entidad.CCA_FechaInicio &&
                    t.CCA_Id != entidad.CCA_Id);

            if (existingContrato != null)
            {
                // Ya hay otro contrato con las mismas fechas → error
                return false;
            }

            var item = L_PrimaryKey(entidad.CCA_Id);

            if (item == null)
            {
                item = new ContratosCAU();
                bd.ContratosCAU.InsertOnSubmit(item);
            }

            item.CCA_FechaInicio = entidad.CCA_FechaInicio;
            item.CCA_FechaFin = entidad.CCA_FechaFin;
            item.CCA_CosteHoraF = entidad.CCA_CosteHoraF;
            item.CCA_CosteHoraD = entidad.CCA_CosteHoraD;
            item.CCA_CosteHoraG = entidad.CCA_CosteHoraG;
            item.CCA_CosteHoraS = entidad.CCA_CosteHoraS;
            item.CCA_PrecioGuardia = entidad.CCA_PrecioGuardia;
            item.CCA_TAR_Id_F = entidad.CCA_TAR_Id_F;
            item.CCA_TAR_Id_D = entidad.CCA_TAR_Id_D;
            item.CCA_TAR_Id_G = entidad.CCA_TAR_Id_G;
            item.CCA_TAR_Id_S = entidad.CCA_TAR_Id_S;

            bd.SubmitChanges();

            return true;
        }

        public bool Eliminar(ContratosCAU entidad)
        {
            try
            {
                // Eliminar la ContratosCAU
                var objEliminar = bd.ContratosCAU.FirstOrDefault(l => l.CCA_Id == entidad.CCA_Id);

                if (objEliminar != null)
                {
                    bd.ContratosCAU.DeleteOnSubmit(objEliminar);
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
