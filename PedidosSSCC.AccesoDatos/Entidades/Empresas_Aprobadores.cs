using Newtonsoft.Json;
using PedidosSSCC.Comun;
using System;

namespace AccesoDatos
{
    public partial class Empresas_Aprobadores : Entidad_Base
	{
        #region Miembros de Entidad_Base

		public override object ValorPK
		{
			get { return string.Format("{1}{0}{2}", Constantes.SeparadorPK, this.EMA_EMP_Id, this.EMA_PER_Id); }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

        #endregion

        public string NombrePersona
        {
            get { return this.Personas != null ? this.Personas.ApellidosNombre : String.Empty; }
        }

	}
}
