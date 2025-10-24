using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Aplicaciones_Modulos_Tarifas : Entidad_Base
    {
        public override object ValorPK => AMT_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}



