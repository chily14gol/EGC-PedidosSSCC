using PedidosSSCC.Comun;
using System;

namespace AccesoDatos
{
    public partial class Facturas_Tareas_LineasEsfuerzo : Entidad_Base
	{
        #region Miembros de Entidad_Base

		public override object ValorPK
		{
			get { return String.Format("{1}{0}{2}", Constantes.SeparadorPK, this.FLE_FAC_Id, this.FLE_TLE_Id); }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }

        #endregion

    }
}
