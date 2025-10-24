using PedidosSSCC.Comun;
using System;

namespace AccesoDatos
{
    public partial class Tareas_Empresas_LineasEsfuerzo_Modulos : Entidad_Base
    {
        public override object ValorPK
        {
            get { return this.TCM_TLE_Id + "|" + this.TCM_AME_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }
    }
}

