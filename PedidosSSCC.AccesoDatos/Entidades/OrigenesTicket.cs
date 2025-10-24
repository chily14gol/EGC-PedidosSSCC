using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class OrigenesTicket : Entidad_Base
    {
        public override object ValorPK => OTK_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}


