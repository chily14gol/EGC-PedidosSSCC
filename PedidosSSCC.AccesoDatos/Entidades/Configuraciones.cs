using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class Configuraciones : Entidad_Base
	{
        #region Miembros de Entidad_Base

		public override object ValorPK
		{
			get { return this.CFG_Id; }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

		#endregion

	}
}
