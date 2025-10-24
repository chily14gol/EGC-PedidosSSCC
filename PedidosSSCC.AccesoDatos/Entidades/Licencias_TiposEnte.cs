using PedidosSSCC.Comun;
using System;

namespace AccesoDatos
{
    public partial class Licencias_TiposEnte : Entidad_Base
    {
        public override object ValorPK => LTE_LIC_Id + "|" + LTE_TEN_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}

