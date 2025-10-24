using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class GruposGuardia : Entidad_Base
    {
        public override object ValorPK => GRG_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}


