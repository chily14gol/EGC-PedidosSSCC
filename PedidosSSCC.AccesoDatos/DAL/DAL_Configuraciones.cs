using System;
using System.Linq;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Configuraciones : DAL_Base<Configuraciones>
	{
		public DAL_Configuraciones() : base() { }
		public DAL_Configuraciones(TransactionScope transaccion) : base(transaccion) {}

		protected override System.Data.Linq.Table<Configuraciones> Tabla
		{
			get { return bd.Configuraciones; }
		}

		public override string ComboText { get { return "CFG_Descripcion"; } }
		public override string ComboValue { get { return "CFG_Id"; } }

        protected override bool Guardar(Configuraciones entidad, int idPersonaModificacion)
        {
            bool retorno = false;

            Configuraciones item = L_PrimaryKey(entidad.CFG_Id);

            if (item == null)
            {
                item = new Configuraciones();
                int? intId = (from reg in bd.Configuraciones select (int?)reg.CFG_Id).Max();
                item.CFG_Id = (intId != null) ? intId.Value + 1 : 1;
                item.FechaAlta = DateTime.Now;

                bd.Configuraciones.InsertOnSubmit(item);
            }

            item.CFG_Descripcion = entidad.CFG_Descripcion;
            item.CFG_Valor = entidad.CFG_Valor;
            item.FechaModificacion = DateTime.Now;
            item.PER_Id_Modificacion = idPersonaModificacion;

            bd.SubmitChanges();
            entidad.CFG_Id = item.CFG_Id; 

			retorno = true;

			return retorno;
		}
    }
}