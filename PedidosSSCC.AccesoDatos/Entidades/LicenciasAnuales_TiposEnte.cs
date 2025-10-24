using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class LicenciasAnuales_TiposEnte : Entidad_Base
    {
        public override object ValorPK => LAT_LAN_Id + "|" + LAT_TEN_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}


