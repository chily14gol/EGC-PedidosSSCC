using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class ContratosLicenciasAnuales_Tarifas : Entidad_Base
    {
        public override object ValorPK => CLT_CLA_Id + "|" + CLT_LAN_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}
