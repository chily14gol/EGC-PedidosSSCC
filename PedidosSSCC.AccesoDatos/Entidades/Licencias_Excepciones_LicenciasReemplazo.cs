using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Licencias_Excepciones_LicenciasReemplazo : Entidad_Base
    {
        public override object ValorPK => LEL_LIE_LIC_Id + "|" + LEL_LIE_EMP_Id + "|" + LEL_LIC_Id_Reemplazo;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}

