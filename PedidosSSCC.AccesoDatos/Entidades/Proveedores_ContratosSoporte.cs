using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Proveedores_ContratosSoporte : Entidad_Base
    {
        #region Miembros de Entidad_Base

        public override object ValorPK
        {
            get { return this.PVC_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

        #endregion
    }
}

