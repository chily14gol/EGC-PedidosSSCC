using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class ContratosCAU : Entidad_Base
    {
        public override object ValorPK => CCA_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Facturacion;
    }
}
