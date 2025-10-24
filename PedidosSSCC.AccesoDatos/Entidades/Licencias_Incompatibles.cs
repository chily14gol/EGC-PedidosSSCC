using PedidosSSCC.Comun;
using System.Collections.Generic;

namespace AccesoDatos
{
    public partial class Licencias_Incompatibles : Entidad_Base
    {
        public override object ValorPK => LIL_LIC_Id1 + "|" + LIL_LIC_Id2;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;

        public List<int> LicenciasReemplazo { get; set; }
    }
}

