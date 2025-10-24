using PedidosSSCC.Comun;
using System;

namespace AccesoDatos
{
    public partial class Tareas_Empresas_LineasEsfuerzo_Asuntos : Entidad_Base
    {
        public override object ValorPK
        {
            get { return this.TCA_TLE_Id + "|" + this.TCA_PAS_Id + "|" + this.TCA_PVC_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }
    }
}
