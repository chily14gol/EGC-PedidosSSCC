using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class EstadosTicket : Entidad_Base
    {
        public override object ValorPK => ETK_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}

