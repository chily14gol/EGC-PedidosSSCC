using PedidosSSCC.Comun;

namespace AccesoDatos
{
    partial class Personas : Entidad_Base
    {
		#region Miembros de Entidad_Base

		public override object ValorPK
		{
			get { return this.PER_Id; }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

		#endregion

        public string ApellidosNombre
        {
            get
            {
                string retorno = this.PER_Apellido1 + " " + this.PER_Apellido2 + ", " + this.PER_Nombre;
                if (retorno == " , ") return null;
                return this.PER_Apellido1 + " " + this.PER_Apellido2 + ", " + this.PER_Nombre;
            }
        }

	}
}
