using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Proyectos_Partes_Horas : Entidad_Base
    {
        #region Miembros de Entidad_Base

        public override object ValorPK
        {
            get { return this.PPH_PPA_Id + "|" + this.PPH_Fecha; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

        #endregion
    }
}