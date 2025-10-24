using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Aplicaciones_Modulos_Empresas : Entidad_Base
    {
        public override object ValorPK => AME_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}


