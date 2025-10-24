using PedidosSSCC.Comun;
using System;

namespace AccesoDatos
{
    public partial class Secciones : Entidad_Base
	{
        #region Miembros de Entidad_Base

		public override object ValorPK
		{
			get { return this.SEC_Id; }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

		#endregion

		public string DepartamentoSeccion
        {
			get
            {
				return (this.Departamentos != null ? (this.Departamentos.DEP_Nombre + " - ") : String.Empty) + this.SEC_Nombre;
            }
        }
    }
}
