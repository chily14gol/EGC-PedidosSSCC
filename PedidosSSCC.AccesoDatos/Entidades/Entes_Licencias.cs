using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Entes_Licencias : Entidad_Base
    {
        public override object ValorPK => ENL_ENT_Id + "|" + ENL_LIC_Id + "|" + ENL_FechaInicio;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}

