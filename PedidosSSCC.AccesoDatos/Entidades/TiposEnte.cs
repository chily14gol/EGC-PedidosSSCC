using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class TiposEnte : Entidad_Base
    {
        public override object ValorPK => TEN_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}
