using PedidosSSCC.Comun;
using System.Collections.Generic;

namespace AccesoDatos
{
    public partial class Licencias : Entidad_Base
    {
        public override object ValorPK => LIC_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;

        public List<int> TiposEnte { get; set; }
        public List<int> LicenciasIncompatibles { get; set; }
    }
}