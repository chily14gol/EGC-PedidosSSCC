using PedidosSSCC.Comun;
using System;

namespace AccesoDatos
{
    public partial class Tareas_Empresas_LineasEsfuerzo_LicenciasMS : Entidad_Base
    {
        public override object ValorPK
        {
            get { return this.TCL_TLE_Id + "|" + this.TCL_LIC_Id + "|" + this.TCL_ENT_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }
    }
}

