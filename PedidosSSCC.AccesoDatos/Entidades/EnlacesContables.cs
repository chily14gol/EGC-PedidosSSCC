using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class EnlacesContables : Entidad_Base
	{
		public override object ValorPK
		{
			get { return this.ECO_Id; }
		}

		public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }
	}
}
