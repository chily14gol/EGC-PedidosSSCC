using PedidosSSCC.Comun;
using System;
using System.Linq;

namespace AccesoDatos
{
    public partial class Facturas : Entidad_Base
    {
        #region Miembros de Entidad_Base
        public override object ValorPK
        {
            get { return this.FAC_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }
        #endregion

        public string EmpresaNombre
        {
            get
            {
                if (this.Empresas != null)
                    return this.Empresas.EMP_Nombre;
                else return String.Empty;
            }
        }

        public string EmpresaFacturarNombre
        {
            get
            {
                if (this.EmpresaFacturar != null)
                    return this.EmpresaFacturar.EMP_Nombre;
                else return String.Empty;
            }
        }

        public string EstadoNombre
        {
            get
            {
                if (this.EstadosSolicitud != null)
                    return this.EstadosSolicitud.Nombre;
                else return String.Empty;
            }
        }

        public decimal? ImporteTotalConIVA
        {
            get { return FAC_ImporteTotal.HasValue && FAC_IVATotal.HasValue ? FAC_ImporteTotal + FAC_IVATotal : null; }
        }

        public DateTime? FechaEnlace
        {
            get { return this.EnlacesContables != null ? (DateTime?)this.EnlacesContables.ECO_Fecha : null; }
        }

        public bool PuedeAccederRegistro
        {
            get
            {
                return
                    // 1) Tiene permiso global
                    Sesion.SVerTodo

                    // 2) Es responsable o aprobador de la empresa
                    || Sesion.SEmpresasAprobador.Contains(this.FAC_EMP_Id)
                    || Sesion.SEmpresasAprobador.Contains(this.FAC_EMP_Id_Facturar.ToInt())

                    // 3) Es el aprobador asignado
                    || this.FAC_PER_Id_Aprobador.Equals(Sesion.SPersonaId)

                    // 4) No tiene líneas de concepto (alta)
                    || this.Facturas_Tareas_LineasEsfuerzo.Count == 0

                    // 5) Alguna línea está en una sección a la que tiene acceso
                    || this.Facturas_Tareas_LineasEsfuerzo.Any(r =>
                           // Asegurarnos de que ninguna parte sea null antes de acceder
                           r.Tareas_Empresas_LineasEsfuerzo != null
                           && r.Tareas_Empresas_LineasEsfuerzo.Tareas_Empresas != null
                           && Sesion.SSeccionesAcceso.Contains(
                                  r.Tareas_Empresas_LineasEsfuerzo
                                   .Tareas_Empresas
                                   .Tareas
                                   .TAR_SEC_Id
                              )
                       );
            }
        }
    }
}