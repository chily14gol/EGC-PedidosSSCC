using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Oficinas : Entidad_Base
    {
        public override object ValorPK => OFI_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}

