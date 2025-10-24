using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class ContratosCAU_ExcluidasGuardia : Entidad_Base
    {
        public override object ValorPK => CEE_CCA_Id + "|" + CEE_EMP_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Facturacion;
    }
}

