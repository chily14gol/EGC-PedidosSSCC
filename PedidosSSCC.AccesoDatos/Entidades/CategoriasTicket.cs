using PedidosSSCC.Comun;

namespace AccesoDatos
{
    public partial class CategoriasTicket : Entidad_Base
    {
        public override object ValorPK => CTK_Id;

        public override Constantes.Modulo Modulo => Constantes.Modulo.Mantenimiento;
    }
}

