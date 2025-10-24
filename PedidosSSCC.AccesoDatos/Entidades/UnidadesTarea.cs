using PedidosSSCC.Comun;
using System.Linq;

namespace AccesoDatos
{
    public partial class UnidadesTarea : Entidad_Base
	{
		#region Miembros de Entidad_Base

		public override object ValorPK
		{
			get { return this.UTA_Id; }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

		#endregion


		public string NombreIdiomaSeleccionado
		{
			get
			{
				if (this.UnidadesTarea_Idioma == null) return null;
				return this.UnidadesTarea_Idioma.Where(p => p.UTI_IDI_Id == 1).Select(p => p.UTI_Nombre).FirstOrDefault();
			}
		}
	}
}
