using PedidosSSCC.Comun;
using System;
using System.Linq;

namespace AccesoDatos
{
    public partial class Seguridad_Opciones : Entidad_Base
	{
		#region Miembros de Entidad_Base

		public override object ValorPK
		{
			get { return this.SOP_Id; }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.General; } }

		#endregion
	}
}
