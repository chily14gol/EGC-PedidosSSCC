using PedidosSSCC.Comun;
using System.Data.Linq.Mapping;

namespace AccesoDatos
{
    public partial class Proveedores_Asuntos : Entidad_Base
    {
        #region Miembros de Entidad_Base

        public override object ValorPK
        {
            get { return this.PAS_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

        #endregion
    }
}

