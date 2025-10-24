using PedidosSSCC.Comun;
using System.Linq;

namespace AccesoDatos
{
    public partial class EstadosSolicitud : Entidad_Base
	{
		#region Miembros de Entidad_Base

		public override object ValorPK
		{
			get { return this.ESO_Id; }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

		#endregion

		public string Nombre
		{
			get
			{
                if (this.EstadosSolicitud_Idioma == null) return null;
                return this.EstadosSolicitud_Idioma.Where(p => p.ESI_IDI_Id == 1).Select(p => p.ESI_Nombre).FirstOrDefault();
			}
		}
	}
}
