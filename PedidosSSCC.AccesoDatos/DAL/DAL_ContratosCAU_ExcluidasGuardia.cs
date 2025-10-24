using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_ContratosCAU_ExcluidasGuardia : DAL_Base<ContratosCAU_ExcluidasGuardia>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<ContratosCAU_ExcluidasGuardia> Tabla => bd.ContratosCAU_ExcluidasGuardia;

        protected override bool Guardar(ContratosCAU_ExcluidasGuardia entidad, int idPersonaModificacion)
        {
            var existingContrato = bd.ContratosCAU_ExcluidasGuardia
                .FirstOrDefault(t =>
                    t.CEE_CCA_Id == entidad.CEE_CCA_Id &&
                    t.CEE_EMP_Id == entidad.CEE_EMP_Id);

            if (existingContrato != null)
            {
                return false;
            }

            var item = new ContratosCAU_ExcluidasGuardia();
            item.CEE_CCA_Id = entidad.CEE_CCA_Id;
            item.CEE_EMP_Id = entidad.CEE_EMP_Id;

            bd.ContratosCAU_ExcluidasGuardia.InsertOnSubmit(item);
            bd.SubmitChanges();

            return true;
        }

        public bool Eliminar(ContratosCAU_ExcluidasGuardia entidad)
        {
            try
            {
                var objEliminar = bd.ContratosCAU_ExcluidasGuardia
                    .FirstOrDefault(l => l.CEE_CCA_Id == entidad.CEE_CCA_Id && l.CEE_EMP_Id == entidad.CEE_EMP_Id);

                if (objEliminar != null)
                {
                    bd.ContratosCAU_ExcluidasGuardia.DeleteOnSubmit(objEliminar);
                }

                bd.SubmitChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}

