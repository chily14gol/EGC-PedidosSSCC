using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class ValidacionesTicket : Entidad_Base
    {
        public override object ValorPK => VTK_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}

