using PedidosSSCC.Comun;
using System;

namespace AccesoDatos
{
    public partial class Licencias_Tarifas : Entidad_Base
    {
        public override object ValorPK => LIT_LIC_Id + "|" + LIT_FechaInicio;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}
