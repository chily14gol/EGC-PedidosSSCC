using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Licencias_Minimos : Entidad_Base
    {
        public override object ValorPK => LEM_LIC_Id + "|" + LEM_EMP_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}
