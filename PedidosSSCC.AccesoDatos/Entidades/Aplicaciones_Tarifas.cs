using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Aplicaciones_Tarifas : Entidad_Base
    {
        public override object ValorPK => APT_APP_Id + "|" + APT_FechaInicio;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}

