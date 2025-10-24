using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class ItemNumbersD365 : Entidad_Base
	{
        #region Miembros de Entidad_Base

		public override object ValorPK
		{
			get { return this.IN3_Id; }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

		#endregion

	}
}
