using PedidosSSCC.Comun;
using System.Collections.Generic;
using System.Linq;

namespace AccesoDatos
{
    public partial class Seguridad_Perfiles : Entidad_Base
	{
		#region Miembros de Entidad_Base

		public override object ValorPK
		{
			get { return this.SPE_Id; }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.General; } }

		#endregion

        public List<string> PermisosAcceso
        {
            get
            {
                List<string> retorno = (from reg in this.Seguridad_Perfiles_Opciones select reg.SPO_SOP_Id).ToList();
                return retorno;
            }
        }

        public List<string> PermisosEscritura
        {
            get
            {
                List<string> retorno = (from reg in this.Seguridad_Perfiles_Opciones where reg.SPO_Escritura select reg.SPO_SOP_Id).ToList();
                return retorno;
            }
        }
	}

    public class SeguridadPerfilesOpcionesDTO
    {
        public int SPO_SPE_Id { get; set; }
        public string SPO_SOP_Id { get; set; }
        public bool SPO_Escritura { get; set; }
        public string SOI_Nombre { get; set; }
    }

    public class PerfilPermisosDTO
    {
        public int IdPerfil { get; set; }
        public string NombrePerfil { get; set; }
        public List<PermisoDTO> PermisosAcceso { get; set; }
        public List<PermisoDTO> PermisosEdicion { get; set; }
    }

    public class PermisoDTO
    {
        public string Id { get; set; }
        public bool Permiso { get; set; }
    }
}
