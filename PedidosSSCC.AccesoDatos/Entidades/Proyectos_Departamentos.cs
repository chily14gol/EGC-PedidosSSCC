using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Proyectos_Departamentos : Entidad_Base
    {
        #region Miembros de Entidad_Base

        public override object ValorPK
        {
            get { return this.PRD_PRY_Id + "|" + this.PRD_DEP_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

        #endregion
    }
}


