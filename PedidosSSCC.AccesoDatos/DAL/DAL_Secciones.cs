using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Secciones : DAL_Base<Secciones>
	{
		public DAL_Secciones() : base() { }

		public DAL_Secciones(TransactionScope transaccion) : base(transaccion) {}

		protected override System.Data.Linq.Table<Secciones> Tabla
		{
			get { return bd.Secciones; }
		}

		public override string ComboText { get { return "DepartamentoSeccion"; } }

		public override string ComboValue { get { return "SEC_Id"; } }

        public override List<Secciones> L(bool sinFiltrar = false, Expression<Func<Secciones, bool>> preFiltro = null)
        {
            List<Secciones> retorno = get_L(bd != null ? bd : new FacturacionInternaDataContext()).ToList();
            return retorno;
        }

		public override Secciones L_PrimaryKey(object valorPK, bool sinFiltrar = false)
        {
            Secciones retorno = get_PrimaryKey(bd != null ? bd : new FacturacionInternaDataContext(), int.Parse(valorPK.ToString()));
			if(retorno == null) return retorno;
            return retorno;
        }

        protected override bool Guardar(Secciones entidad, int idPersonaModificacion)
        {
            bool retorno = false;

            Secciones item = L_PrimaryKey(entidad.SEC_Id);

            if (item == null)
            {
                item = new Secciones();
                // Propiedades que solo se asignan en el alta
                int? intId = (from reg in bd.Secciones select (int?)reg.SEC_Id).Max();
                item.SEC_Id = (intId != null) ? intId.Value + 1 : 1;
                item.FechaAlta = DateTime.Now;
                bd.Secciones.InsertOnSubmit(item);
            }
            else
            {
                // Propiedades que solo se asignan una vez dado de alta, en updates
            }
            item.FechaModificacion = DateTime.Now;
            item.PER_Id_Modificacion = idPersonaModificacion;

            item.SEC_Codigo = entidad.SEC_Codigo;
            item.SEC_Nombre = entidad.SEC_Nombre;
            item.SEC_DEP_Id = entidad.SEC_DEP_Id;
            item.SEC_Ficticia = entidad.SEC_Ficticia;

            bd.SubmitChanges();
            entidad.SEC_Id = item.SEC_Id; //Asignamos el ID autonumerico al objeto "origen" para devolverlo en el caso de un alta.try

			retorno = true;

			return retorno;
		}

        #region "CompiledQuerys"
        public static Func<FacturacionInternaDataContext, IEnumerable<Secciones>>
            get_L = CompiledQuery.Compile<FacturacionInternaDataContext, IEnumerable<Secciones>>((FacturacionInternaDataContext bd)
                => (from reg in bd.Secciones
                    select reg));

        public static Func<FacturacionInternaDataContext, int, Secciones>
            get_PrimaryKey = CompiledQuery.Compile<FacturacionInternaDataContext, int, Secciones>((FacturacionInternaDataContext bd, int valorFK)
                => (from reg in bd.Secciones
                    where reg.SEC_Id.Equals(valorFK)
                    select reg).FirstOrDefault());
        #endregion
    }
}