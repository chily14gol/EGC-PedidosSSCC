using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Telefonia : Entidad_Base
    {
        public override object ValorPK => TFN_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}
