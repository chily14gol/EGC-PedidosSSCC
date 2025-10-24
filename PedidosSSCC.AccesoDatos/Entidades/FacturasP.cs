using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class FacturasP : Entidad_Base
    {
        #region Miembros de Entidad_Base
        public override object ValorPK
        {
            get { return this.FAP_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }
        #endregion
    }
}
