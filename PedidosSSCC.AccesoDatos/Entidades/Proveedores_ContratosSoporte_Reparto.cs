using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Proveedores_ContratosSoporte_Reparto : Entidad_Base
    {
        #region Miembros de Entidad_Base

        public override object ValorPK
        {
            get { return this.PVR_PVC_Id + "|" + this.PVR_EMP_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

        #endregion
    }
}

