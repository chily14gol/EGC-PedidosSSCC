using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class PeriodosPartes : Entidad_Base
    {
        #region Miembros de Entidad_Base

        public override object ValorPK
        {
            get { return this.PEP_Anyo + "|" + this.PEP_Mes; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

        #endregion
    }
}


