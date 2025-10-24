using PedidosSSCC.Comun;
using System;

namespace AccesoDatos
{
    public partial class Tareas_Empresas_LineasEsfuerzo_ModulosReparto : Entidad_Base
    {
        public override object ValorPK
        {
            get { return this.TCR_TLE_Id + "|" + this.TCR_AMT_Id + "|" + this.TCR_Equitativo; }
        }

        public override Constantes.Modulo Modulo { get { return Constantes.Modulo.Facturacion; } }
    }
}

