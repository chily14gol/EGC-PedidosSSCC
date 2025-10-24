using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class TiposTicket : Entidad_Base
    {
        public override object ValorPK => TTK_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}



