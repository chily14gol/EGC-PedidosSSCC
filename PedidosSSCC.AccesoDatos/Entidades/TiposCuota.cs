using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class TiposCuota : Entidad_Base
    {
        #region Miembros de Entidad_Base

        public override object ValorPK
        {
            get { return this.TCU_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }

        #endregion

    }
}
