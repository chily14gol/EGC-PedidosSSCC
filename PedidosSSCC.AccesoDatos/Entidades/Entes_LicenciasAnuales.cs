using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Entes_LicenciasAnuales : Entidad_Base
    {
        public override object ValorPK => ELA_ENT_Id + "|" + ELA_LAN_Id + "|" + ELA_CLA_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}


