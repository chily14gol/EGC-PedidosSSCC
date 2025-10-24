using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class TiposTarea : Entidad_Base
    {
        public override object ValorPK => TTA_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}

