using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Aplicaciones_Modulos : Entidad_Base
    {
        public override object ValorPK => APM_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}


