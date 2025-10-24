using PedidosSSCC.Comun;
using System;

namespace AccesoDatos
{
    public partial class Tareas_Empresas_LineasEsfuerzo_Tickets : Entidad_Base
    {
        public override object ValorPK
        {
            get { return this.TCT_TLE_Id + "|" + this.TCT_TKC_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }
    }
}


