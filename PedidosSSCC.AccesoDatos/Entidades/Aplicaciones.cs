using PedidosSSCC.Comun;
using System.Collections.Generic;

namespace AccesoDatos
{
    public partial class Aplicaciones : Entidad_Base
    {
        public override object ValorPK => APP_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;

        public List<int> TiposEnte { get; set; }
    }
}

