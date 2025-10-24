using PedidosSSCC.Comun;
using System;
using System.Linq;

namespace AccesoDatos
{
    public partial class Tareas : Entidad_Base
	{
        #region Miembros de Entidad_Base
		public override object ValorPK
		{
			get { return this.TAR_Id; }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }
		#endregion

		public string TipoTarea
        {
			get
            {
                switch (this.TAR_TTA_Id)
                {
                    case (int)Constantes.TipoTarea.PorHoras:
                        return Resources.Resource.litPorHoras;
                    case (int)Constantes.TipoTarea.PorUnidades:
                        return Resources.Resource.litPorUnidades;
                    case (int)Constantes.TipoTarea.CantidadFija:
                        return Resources.Resource.litCantidadFija;
                }
                return String.Empty;
            }
        }

        public string SeccionNombre
        {
            get
            {
                if (this.Secciones != null)
                    return this.Secciones.SEC_Nombre;
                else return String.Empty;
            }
        }

        public string DescripcionUnidad
        {
            get
            {
                if (this.UnidadesTarea != null)
                    return this.UnidadesTarea.NombreIdiomaSeleccionado;
                else return String.Empty;
            }
        }

        public bool PuedeAccederRegistro
        {
            get
            {
                return Sesion.SVerTodo ||
                    //La tarea tiene asociada alguna empresa que tiene como Responsable o Aprobador a la persona conectada.
                    Sesion.SEmpresasAprobador.Any(emp => this.Tareas_Empresas.Select(i => i.TEM_EMP_Id).Contains(emp)) ||
                    //La tarea esta asociada a una Sección del Departamento al que esta asociado la persona conectada.
                    Sesion.SSeccionesAcceso.Contains(this.TAR_SEC_Id);
            }
        }
    }
}
