using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;
using static PedidosSSCC.Comun.Constantes;

namespace AccesoDatos
{
    public class DAL_Empresas : DAL_Base<Empresas>
	{
		public DAL_Empresas() : base() { }

		public DAL_Empresas(TransactionScope transaccion) : base(transaccion) {}

		protected override System.Data.Linq.Table<Empresas> Tabla
		{
			get { return bd.Empresas; }
        }

        public enum TipoClaveExterna
        {
            Nombre = 1
        }

        public override bool ConsultarMedianteClaveExterna { get { return true; } }

        private TipoClaveExterna _modoConsultaClaveExterna;

        public TipoClaveExterna ModoConsultaClaveExterna
        {
            get { return _modoConsultaClaveExterna; }
            set { _modoConsultaClaveExterna = value; }
        }
        
        public override string ComboText { get { return "EMP_Nombre"; } }

		public override string ComboValue { get { return "EMP_Id"; } }

        public override List<Empresas> L(bool sinFiltrar = false, Expression<Func<Empresas, bool>> preFiltro = null)
        {
            List<Empresas> retorno = get_L(bd ?? new FacturacionInternaDataContext()).ToList();

            if (preFiltro == null)
                retorno = get_L(bd ?? new FacturacionInternaDataContext()).ToList();
            else
                retorno = (from reg in bd.Empresas.Where(preFiltro) select reg).ToList();

            return retorno.OrderBy(r => r.EMP_Nombre).ToList();
        }

        public override List<Empresas> L_ClaveExterna(object valorFK, Expression<Func<Empresas, bool>> preFiltro = null)
        {
            List<Empresas> retorno = new List<Empresas>();

            switch (ModoConsultaClaveExterna)
            {
                case TipoClaveExterna.Nombre:
                    retorno = (from reg in bd.Empresas where reg.EMP_Nombre.Equals(valorFK) select reg).ToList();
                    break;
                default:
                    throw new Exception("ModoConsultaClaveExterna no definido");
            }

            return retorno;
        }

        public override Empresas L_PrimaryKey(object valorPK, bool sinFiltrar = false)
        {
            Empresas retorno = get_PrimaryKey(bd ?? new FacturacionInternaDataContext(), int.Parse(valorPK.ToString()));
			
            if (retorno == null) 
                return retorno;

            return retorno;
        }

        protected override bool Guardar(Empresas entidad, int idPersonaModificacion)
        {
            bool retorno = false;

            Empresas item = L_PrimaryKey(entidad.EMP_Id);

            if (item == null)
            {
                item = new Empresas();
                int? intId = (from reg in bd.Empresas select (int?)reg.EMP_Id).Max();
                item.EMP_Id = (intId != null) ? intId.Value + 1 : 1;
                item.FechaAlta = DateTime.Now;
                bd.Empresas.InsertOnSubmit(item);
            }

            item.EMP_Nombre = entidad.EMP_Nombre;
            item.EMP_NombreDA = entidad.EMP_NombreDA;
            item.EMP_RazonSocial = entidad.EMP_RazonSocial;
            item.EMP_CIF = entidad.EMP_CIF;
            item.EMP_Direccion = entidad.EMP_Direccion;
            item.EMP_PER_Id_AprobadorDefault = entidad.EMP_PER_Id_AprobadorDefault;
            item.EMP_LNE_Id = entidad.EMP_LNE_Id;
            item.EMP_CodigoAPIKA = entidad.EMP_CodigoAPIKA;
            item.EMP_CodigoD365 = entidad.EMP_CodigoD365;
            item.EMP_FPA_CodigoAPIKA = entidad.EMP_FPA_CodigoAPIKA;
            item.EMP_FPA_D365 = entidad.EMP_FPA_D365;
            item.EMP_TipoCliente = entidad.EMP_TipoCliente;
            item.EMP_EGrupoD365 = entidad.EMP_EGrupoD365;
            item.EMP_EmpresaFacturar = entidad.EMP_EmpresaFacturar;
            item.EMP_EmpresaFacturar_Id = entidad.EMP_EmpresaFacturar_Id;
            item.EMP_ExcluidaGuardia = entidad.EMP_ExcluidaGuardia;
            item.EMP_GRG_Id = entidad.EMP_GRG_Id;
            item.EMP_NumFacturaVDF = entidad.EMP_NumFacturaVDF;

            item.FechaModificacion = DateTime.Now;
            item.PER_Id_Modificacion = idPersonaModificacion;

            bd.SubmitChanges();
            entidad.EMP_Id = item.EMP_Id;

            retorno = true;
            return retorno;
        }

        public int? ObtenerIdPorNombreEmpresa(string nombreEmpresa)
        {
            var empresa = bd.Empresas.FirstOrDefault(e => e.EMP_NombreDA == nombreEmpresa);
            return empresa?.EMP_Id;
        }

        public List<Empresas> GetEmpresasGenerarConceptos()
        {
            return L(false, null)
                .Where(e => e.EMP_Id != (int)EmpresaExcluyenteConceptos.EGC)
                .ToList();
        }

        #region "CompiledQuerys"
        public static Func<FacturacionInternaDataContext, IEnumerable<Empresas>>
            get_L = CompiledQuery.Compile<FacturacionInternaDataContext, IEnumerable<Empresas>>((FacturacionInternaDataContext bd)
                => (from reg in bd.Empresas
                    select reg));

        public static Func<FacturacionInternaDataContext, int, Empresas>
            get_PrimaryKey = CompiledQuery.Compile<FacturacionInternaDataContext, int, Empresas>((FacturacionInternaDataContext bd, int valorFK)
                => (from reg in bd.Empresas
                    where reg.EMP_Id.Equals(valorFK)
                    select reg).FirstOrDefault());
        #endregion
    }
}