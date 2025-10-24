using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Empresas_Departamentos : Entidad_Base
    {
        #region Miembros de Entidad_Base

        public override object ValorPK
        {
            get { return this.EDE_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

        #endregion

    }
}
