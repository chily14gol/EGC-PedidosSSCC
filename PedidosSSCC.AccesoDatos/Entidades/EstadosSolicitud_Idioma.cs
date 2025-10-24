using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class EstadosSolicitud_Idioma : Entidad_Base
	{
		#region Miembros de Entidad_Base

		public override object ValorPK
		{
			get { return string.Format("{1}{0}{2}", Constantes.SeparadorPK, this.ESI_ESO_Id, this.ESI_IDI_Id); }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

		#endregion
	}
}
