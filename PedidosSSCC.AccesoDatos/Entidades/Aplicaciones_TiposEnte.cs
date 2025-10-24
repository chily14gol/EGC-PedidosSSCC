using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Aplicaciones_TiposEnte : Entidad_Base
    {
        public override object ValorPK => ATE_APP_Id + "|" + ATE_TEN_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}


