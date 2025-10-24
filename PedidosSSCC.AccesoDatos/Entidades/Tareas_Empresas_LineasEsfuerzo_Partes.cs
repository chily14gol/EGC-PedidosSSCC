using PedidosSSCC.Comun;
using System;

namespace AccesoDatos
{
    public partial class Tareas_Empresas_LineasEsfuerzo_Partes : Entidad_Base
    {
        public override object ValorPK
        {
            get { return this.TCP_TLE_Id + "|" + this.TCP_PPA_Id + "|" + this.TCP_Fecha; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }
    }
}


