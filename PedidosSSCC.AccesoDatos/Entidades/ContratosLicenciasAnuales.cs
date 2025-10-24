using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class ContratosLicenciasAnuales : Entidad_Base
    {
        public override object ValorPK => CLA_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}


