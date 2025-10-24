using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Proyectos_Empresas : Entidad_Base
    {
        #region Miembros de Entidad_Base

        public override object ValorPK
        {
            get { return this.PRE_PRY_Id + "|" + this.PRE_EMP_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

        #endregion
    }
}
