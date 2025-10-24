using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class ConceptosFacturacion_Idioma : Entidad_Base
	{
		#region Miembros de Entidad_Base

		public override object ValorPK
		{
			get { return string.Format("{1}{0}{2}", Constantes.SeparadorPK, this.CFI_CFA_Id, this.CFI_IDI_Id); }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

		#endregion
	}
}
