using PedidosSSCC.Comun;
using System;

namespace AccesoDatos
{
    public partial class Tareas_Empresas_LineasEsfuerzo_Aplicaciones : Entidad_Base
    {
        public override object ValorPK
        {
            get { return this.TCA_TLE_Id + "|" + this.TCA_APP_Id + "|" + this.TCA_ENT_Id; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }
    }
}