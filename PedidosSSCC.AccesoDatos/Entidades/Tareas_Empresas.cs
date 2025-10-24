using PedidosSSCC.Comun;
using System;
using System.Linq;

namespace AccesoDatos
{
    public partial class Tareas_Empresas : Entidad_Base
	{
        #region Miembros de Entidad_Base
		public override object ValorPK
		{
			get { return String.Format("{1}{0}{2}{0}{3}", Constantes.SeparadorPK, this.TEM_TAR_Id, this.TEM_EMP_Id, this.TEM_Anyo); }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }
        #endregion

        public double TEM_PresupuestoConsumido;

        public decimal TAR_ImporteUnitario;

		public string EmpresaNombre
        {
			get
            {
				return this.Empresas != null ? this.Empresas.EMP_Nombre : String.Empty;
            }
        }

        public bool Editable
        {
            get
            {
                //Se puede dar de alta siempre que el año coincida con el "año en curso". La edicion unicamente a los que tengan permiso para ello
                return this.TEM_Anyo.Equals(Parametros.AnyoConcepto) && Sesion.SOpcionesAcceso.ContainsKey(Constantes.SeguridadOpciones.Facturacion_Tareas_EdicionPresupuesto);
            }
        }

        public bool PuedeAccederRegistro
        {
            get
            {
                return Sesion.SVerTodo ||
                    //La empresa tiene como Responsable o Aprobador a la persona conectada.
                    Sesion.SEmpresasAprobador.Contains(this.TEM_EMP_Id) ||
                    //La tarea esta asociada a una Sección del Departamento al que esta asociado la persona conectada.
                    Sesion.SSeccionesAcceso.Contains(this.Tareas.Secciones.SEC_Id);
            }
        }
    }
}