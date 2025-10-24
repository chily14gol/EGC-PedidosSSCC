using PedidosSSCC.Comun;
using System.Collections.Generic;

namespace AccesoDatos
{
    public partial class LicenciasAnuales : Entidad_Base
    {
        public override object ValorPK => LAN_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;

        public List<int> TiposEnte { get; set; }
    }
}

