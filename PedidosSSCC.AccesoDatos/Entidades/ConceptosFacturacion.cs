using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class ConceptosFacturacion : Entidad_Base
	{
        #region Miembros de Entidad_Base

		public override object ValorPK
		{
			get { return this.CFA_Id; }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

		#endregion

	}
}
