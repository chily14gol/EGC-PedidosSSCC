using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;

namespace AccesoDatos
{
    public class DAL_TiposCuota : DAL_Base<TiposCuota>
    {
        public DAL_TiposCuota() : base() { }
        public DAL_TiposCuota(System.Transactions.TransactionScope transaccion) : base(transaccion) { }

        protected override Table<TiposCuota> Tabla => bd.TiposCuota;

        // --- Clave externa opcional: por nombre de cuota ---
        public enum TipoClaveExterna
        {
            Cuota = 1
        }

        public override bool ConsultarMedianteClaveExterna => true;

        private TipoClaveExterna _modoConsultaClaveExterna;
        public TipoClaveExterna ModoConsultaClaveExterna
        {
            get => _modoConsultaClaveExterna;
            set => _modoConsultaClaveExterna = value;
        }

        public override string ComboText => "TCU_Cuota";
        public override string ComboValue => "TCU_Id";

        // --- Listados ---
        public override List<TiposCuota> L(bool sinFiltrar = false, Expression<Func<TiposCuota, bool>> preFiltro = null)
        {
            List<TiposCuota> retorno;

            if (preFiltro == null)
                retorno = get_L(bd ?? new FacturacionInternaDataContext()).ToList();
            else
                retorno = (from reg in bd.TiposCuota.Where(preFiltro) select reg).ToList();

            return retorno.OrderBy(r => r.TCU_Cuota).ToList();
        }

        public override List<TiposCuota> L_ClaveExterna(object valorFK, Expression<Func<TiposCuota, bool>> preFiltro = null)
        {
            switch (ModoConsultaClaveExterna)
            {
                case TipoClaveExterna.Cuota:
                    return (from reg in bd.TiposCuota
                            where reg.TCU_Cuota.Equals(valorFK)
                            select reg).ToList();

                default:
                    throw new Exception("ModoConsultaClaveExterna no definido");
            }
        }

        public override TiposCuota L_PrimaryKey(object valorPK, bool sinFiltrar = false)
        {
            var id = int.Parse(valorPK.ToString());
            var retorno = get_PrimaryKey(bd ?? new FacturacionInternaDataContext(), id);
            return retorno;
        }

        // --- Guardar (alta / edición) ---
        protected override bool Guardar(TiposCuota entidad, int idPersonaModificacion)
        {
            bool retorno = false;

            TiposCuota item = L_PrimaryKey(entidad.TCU_Id);

            if (item == null)
            {
                item = new TiposCuota();
                bd.TiposCuota.InsertOnSubmit(item);
            }

            item.TCU_Cuota = entidad.TCU_Cuota;
            item.TCU_Departamento = entidad.TCU_Departamento;
            item.TCU_Sede = entidad.TCU_Sede;
            item.TCU_Uso = entidad.TCU_Uso;

            bd.SubmitChanges();
            entidad.TCU_Id = item.TCU_Id;

            retorno = true;
            return retorno;
        }

        // --- Helpers opcionales ---
        public int? ObtenerIdPorCuota(string nombreCuota)
        {
            var reg = bd.TiposCuota.FirstOrDefault(t => t.TCU_Cuota == nombreCuota);
            return reg?.TCU_Id;
        }

        #region CompiledQueries
        public static readonly Func<FacturacionInternaDataContext, IEnumerable<TiposCuota>>
            get_L = CompiledQuery.Compile<FacturacionInternaDataContext, IEnumerable<TiposCuota>>(
                (FacturacionInternaDataContext ctx)
                    => from reg in ctx.TiposCuota
                       select reg
            );

        public static readonly Func<FacturacionInternaDataContext, int, TiposCuota>
            get_PrimaryKey = CompiledQuery.Compile<FacturacionInternaDataContext, int, TiposCuota>(
                (FacturacionInternaDataContext ctx, int id)
                    => (from reg in ctx.TiposCuota
                        where reg.TCU_Id == id
                        select reg).FirstOrDefault()
            );
        #endregion
    }
}
