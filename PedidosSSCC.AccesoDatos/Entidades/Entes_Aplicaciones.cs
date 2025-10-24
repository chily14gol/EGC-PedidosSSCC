using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Entes_Aplicaciones : Entidad_Base
    {
        public override object ValorPK => ENL_ENT_Id + "|" + ENL_APP_Id + "|" + ENL_FechaInicio;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}

