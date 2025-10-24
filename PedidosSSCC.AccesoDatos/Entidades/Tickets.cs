using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Tickets : Entidad_Base
    {
        public override object ValorPK => TKC_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}


