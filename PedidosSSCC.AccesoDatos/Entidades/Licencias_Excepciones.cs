using PedidosSSCC.Comun;
using System.Collections.Generic;

namespace AccesoDatos
{
    public partial class Licencias_Excepciones : Entidad_Base
    {
        public override object ValorPK => LIE_LIC_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;

        public List<int> LicenciasReemplazo { get; set; }
    }
}
