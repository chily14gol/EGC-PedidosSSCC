using PedidosSSCC.Comun;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccesoDatos
{
    public partial class Proyectos : Entidad_Base
    {
        #region Miembros de Entidad_Base

        public override object ValorPK
        {
            get { return this.PRY_Id; }
        }

        public string TareaNombre
        {
            get
            {
                if (this.Tareas != null)
                    return this.Tareas.TAR_Nombre;
                else return String.Empty;
            }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Mantenimiento; } }

        #endregion
    }
}

