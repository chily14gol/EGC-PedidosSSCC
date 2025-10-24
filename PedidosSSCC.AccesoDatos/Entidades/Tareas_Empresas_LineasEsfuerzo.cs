using PedidosSSCC.Comun;
using System;

namespace AccesoDatos
{
    public partial class Tareas_Empresas_LineasEsfuerzo : Entidad_Base
	{
		public override object ValorPK
		{
			get { return this.TLE_Id; }
		}

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }

        public int? TLE_ESO_Id_Original; //Utilizado para poder cambiar el estado (al tramitar la solicitud)

        public string TareaNombre
        {
            get
            {
                if (this.Tareas_Empresas != null && this.Tareas_Empresas.Tareas != null)
                    return this.Tareas_Empresas.Tareas.TAR_Nombre;
                else 
                    return String.Empty;
            }
        }

        public string EmpresaNombre
        {
            get
            {
                if (this.Tareas_Empresas != null && this.Tareas_Empresas.Empresas != null)
                    return this.Tareas_Empresas.Empresas.EMP_Nombre;
                else 
                    return String.Empty;
            }
        }

        public string EstadoNombre
        {
            get
            {
                if (this.EstadosSolicitud != null)
                    return this.EstadosSolicitud.Nombre;
                else 
                    return String.Empty;
            }
        }

        public string CantidadNombre
        {
            get
            {
                if (this.Tareas_Empresas != null && this.Tareas_Empresas.Tareas != null)
                {
                    switch (this.Tareas_Empresas.Tareas.TAR_TTA_Id)
                    {
                        case (int)Constantes.TipoTarea.PorHoras:
                            return this.TLE_Cantidad.ToHoras();
                        case (int)Constantes.TipoTarea.PorUnidades:
                            {
                                if (this.Tareas_Empresas.Tareas.TAR_UTA_Id == (int)Constantes.TareaUnidades.Horas)
                                    return this.TLE_Cantidad.ToUnidades("Horas");
                                else
                                    return this.TLE_Cantidad.ToUnidades(String.Empty);
                            }
                        case (int)Constantes.TipoTarea.CantidadFija:
                            return String.Empty;
                        default: return String.Empty;
                    }
                }
                else return String.Empty;
            }
        }

        public decimal ImporteTotal
        {
            get
            {
                return CalcularImporteTotal(this.Tareas_Empresas);
            }
        }

        public bool PuedeAccederRegistro
        {
            get
            {
                return Sesion.SVerTodo ||
                       this.TLE_PER_Id_Aprobador.Equals(Sesion.SPersonaId) ||
                       (this.Tareas_Empresas != null && this.Tareas_Empresas.Tareas != null && Sesion.SSeccionesAcceso.Contains(this.Tareas_Empresas.Tareas.TAR_SEC_Id));
            }
        }

        public decimal CalcularImporteTotal(Tareas_Empresas tareasEmpresa)
        {
            if (tareasEmpresa != null && tareasEmpresa.Tareas != null)
            {
                switch (tareasEmpresa.Tareas.TAR_TTA_Id)
                {
                    case (int)Constantes.TipoTarea.PorHoras:
                    case (int)Constantes.TipoTarea.PorUnidades:
                        return Math.Round(this.TLE_Cantidad * tareasEmpresa.Tareas.TAR_ImporteUnitario.Value, 2); //Cantidad x Precio unitario
                    case (int)Constantes.TipoTarea.CantidadFija:
                        return this.TLE_Cantidad;
                    default: 
                        return 0;
                }
            }
            else return 0;
        }
    }
}
