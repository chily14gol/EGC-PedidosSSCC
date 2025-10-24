using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_ConceptosFacturacion : DAL_Base<ConceptosFacturacion>
	{
		public DAL_ConceptosFacturacion() : base() { }
		public DAL_ConceptosFacturacion(TransactionScope transaccion) : base(transaccion) {}

		protected override System.Data.Linq.Table<ConceptosFacturacion> Tabla
		{
			get { return bd.ConceptosFacturacion; }
		}

		public override List<ConceptosFacturacion> L(bool sinFiltrar = false, Expression<Func<ConceptosFacturacion, bool>> preFiltro = null)
        {
            List<ConceptosFacturacion> retorno = get_L(bd != null ? bd : new FacturacionInternaDataContext()).ToList();
            return retorno;
        }

        // override sin sentido, por defecto sinFiltrar vale false
        /*
		public override ConceptosFacturacion L_PrimaryKey(object valorPK)
        {
            return L_PrimaryKey(valorPK, sinFiltrar: false);
        }*/

        [Obsolete]
        public override ConceptosFacturacion L_PrimaryKey(object valorPK, bool sinFiltrar = false)
        {
            ConceptosFacturacion retorno = get_PrimaryKey(bd != null ? bd : new FacturacionInternaDataContext(), int.Parse(valorPK.ToString()));
			if(retorno == null) return retorno;
            return retorno;
        }

        protected override bool Guardar(ConceptosFacturacion entidad, int idPersonaModificacion)
        {
            throw new Exception("No se dan de alta conceptos de facturación");
		}

        #region "CompiledQuerys"
        public static Func<FacturacionInternaDataContext, IEnumerable<ConceptosFacturacion>>
            get_L = CompiledQuery.Compile<FacturacionInternaDataContext, IEnumerable<ConceptosFacturacion>>((FacturacionInternaDataContext bd)
                => (from reg in bd.ConceptosFacturacion
                    select reg));

        public static Func<FacturacionInternaDataContext, int, ConceptosFacturacion>
            get_PrimaryKey = CompiledQuery.Compile<FacturacionInternaDataContext, int, ConceptosFacturacion>((FacturacionInternaDataContext bd, int valorFK)
                => (from reg in bd.ConceptosFacturacion
                    where reg.CFA_Id.Equals(valorFK)
                    select reg).FirstOrDefault());
        #endregion
    }
}
