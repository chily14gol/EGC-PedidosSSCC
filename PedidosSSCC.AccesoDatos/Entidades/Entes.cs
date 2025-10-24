using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Entes : Entidad_Base
    {
        public override object ValorPK => ENT_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}

