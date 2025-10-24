using PedidosSSCC.Comun;
using System;

namespace AccesoDatos
{
    public partial class Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales : Entidad_Base
    {
        public override object ValorPK
        {
            get { return this.TCL_TLE_Id + "|" + this.TCL_LAN_Id + "|" + this.TCL_ENT_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }
    }
}
