using PedidosSSCC.Comun;
using System;

namespace AccesoDatos
{
    public static class Parametros
    {
        public static int AnyoConcepto
        {
            get
            {
                return CargarAnioConcepto();
            }
        }

        private static int CargarAnioConcepto()
        {
            DAL_Configuraciones dal = new DAL_Configuraciones();
            Configuraciones objAnio = dal.L_PrimaryKey((int)Constantes.Configuracion.AnioConcepto);
            return Convert.ToInt32(objAnio.CFG_Valor);
        }
    }
}
