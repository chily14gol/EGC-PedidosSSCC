using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Tareas : DAL_Base<Tareas>
    {
        public DAL_Tareas() : base() { }

        public DAL_Tareas(TransactionScope transaccion) : base(transaccion) { }

        protected override System.Data.Linq.Table<Tareas> Tabla
        {
            get { return bd.Tareas; }
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

        public override string ComboText { get { return "TAR_Nombre"; } }

        public override string ComboValue { get { return "TAR_Id"; } }

        public override List<Tareas> L(bool sinFiltrar = false, Expression<Func<Tareas, bool>> preFiltro = null)
        {
            List<Tareas> retorno;

            if (preFiltro == null)
                retorno = get_L(bd != null ? bd : new FacturacionInternaDataContext()).ToList();
            else
                retorno = (from reg in bd.Tareas.Where(preFiltro) select reg).ToList();

            if (!sinFiltrar)
                retorno = retorno.Where(c => c.PuedeAccederRegistro).ToList();

            return retorno;
        }

        public override List<Tareas> L_ClaveExterna(object valorFK, Expression<Func<Tareas, bool>> preFiltro = null)
        {
            List<Tareas> retorno = new List<Tareas>();

            switch (ModoConsultaClaveExterna)
            {
                case TipoClaveExterna.Nombre:
                    retorno = (from reg in bd.Tareas where reg.TAR_Nombre.Equals(valorFK) select reg).ToList();
                    break;
                default:
                    throw new Exception("ModoConsultaClaveExterna no definido");
            }

            return retorno;
        }

        public override Tareas L_PrimaryKey(object valorPK, bool sinFiltrar = false)
        {
            Tareas retorno = get_PrimaryKey(bd != null ? bd : new FacturacionInternaDataContext(), int.Parse(valorPK.ToString()));
            if (retorno == null) return retorno;
            //if (!sinFiltrar && !retorno.PuedeAccederRegistro) return null;
            return retorno;
        }

        public override bool ValidacionesGuardarEspecificas(ref Tareas reg)
        {
            int idTarea = reg.TAR_Id;
            string TAR_Nombre = reg.TAR_Nombre;

            //Comprobamos que no exista otra tarea con el mismo nombre
            bool lbolRetorno = ((from tar in bd.Tareas where tar.TAR_Id != idTarea && tar.TAR_Nombre.Equals(TAR_Nombre) select tar).Count() == 0);
            
            if (!lbolRetorno)
                MensajeErrorEspecifico = Resources.Resource.errTareaExistente;
            return 
                lbolRetorno;
        }

        protected override bool Guardar(Tareas entidad, int idPersonaModificacion)
        {
            bool retorno = false;

            Tareas item = L_PrimaryKey(entidad.TAR_Id);

            if (item == null)
            {
                item = new Tareas();
                int? intId = (from reg in bd.Tareas select (int?)reg.TAR_Id).Max();
                item.TAR_Id = (intId != null) ? intId.Value + 1 : 1;
                item.FechaAlta = DateTime.Now;
                bd.Tareas.InsertOnSubmit(item);
            }

            item.TAR_Nombre = entidad.TAR_Nombre;
            item.TAR_SEC_Id = entidad.TAR_SEC_Id;
            item.TAR_TTA_Id = entidad.TAR_TTA_Id;
            item.TAR_CFA_Id = entidad.TAR_CFA_Id;

            item.TAR_ImporteUnitario = entidad.TAR_ImporteUnitario;

            //Actulizamos el presupuesto de las empresas con el nuevo importe unitario (solo si es por horas o unidades)
            if (item.TAR_TTA_Id == (int)Constantes.TipoTarea.PorHoras || item.TAR_TTA_Id == (int)Constantes.TipoTarea.PorUnidades)
            {
                DAL_Tareas_Empresas dal = new DAL_Tareas_Empresas();
                Expression<Func<Tareas_Empresas, bool>> filtroTarea = t => t.TEM_TAR_Id == item.TAR_Id;
                List<Tareas_Empresas> listaEmpresas = dal.L(false, filtroTarea);

                foreach (Tareas_Empresas empresa in item.Tareas_Empresas)
                {
                    //TEM_Elementos => Campo Unidades
                    empresa.TEM_Presupuesto = empresa.TEM_Elementos * item.TAR_ImporteUnitario.Value;
                }
            }

            item.TAR_UTA_Id = entidad.TAR_UTA_Id;
            item.TAR_TipoIva = entidad.TAR_TipoIva;
            item.TAR_PR3_Id = entidad.TAR_PR3_Id;
            item.TAR_IN3_Id = entidad.TAR_IN3_Id;
            item.TAR_Activo = entidad.TAR_Activo;

            item.FechaModificacion = DateTime.Now;
            item.PER_Id_Modificacion = idPersonaModificacion;

            bd.SubmitChanges();
            entidad.TAR_Id = item.TAR_Id;

            retorno = true;

            return retorno;
        }

        public override bool D(object valorPK)
        {
            DAL_Tareas_Empresas dalTEM = new DAL_Tareas_Empresas();

            // Comprobar si hay empresas asociadas a la tarea
            var empresasAsociadas = dalTEM.L_ClaveExterna(valorPK);

            if (empresasAsociadas != null && empresasAsociadas.Count > 0)
            {
                return false;
            }

            return base.D(valorPK);
        }

        public List<object> GetTareasCombo(int[] tiposDeTarea)
        {
            using (var db = new FacturacionInternaDataContext())
            {
                var query = db.Tareas
                    .Where(t => t.TAR_Activo);

                // Si se pasan tipos de tarea, filtramos
                if (tiposDeTarea != null && tiposDeTarea.Any())
                {
                    query = query.Where(t => tiposDeTarea.Contains(t.TAR_TTA_Id));
                }

                return query
                    .OrderBy(t => t.TAR_Nombre)
                    .Select(t => new
                    {
                        t.TAR_Id,
                        t.TAR_Nombre
                    })
                    .Cast<object>()
                    .ToList();
            }
        }

        #region "CompiledQuerys"
        public static Func<FacturacionInternaDataContext, IEnumerable<Tareas>>
            get_L = CompiledQuery.Compile<FacturacionInternaDataContext, IEnumerable<Tareas>>((FacturacionInternaDataContext bd)
                => (from reg in bd.Tareas
                    select reg));

        public static readonly Func<FacturacionInternaDataContext, int, Tareas> get_PrimaryKey =
            CompiledQuery.Compile((FacturacionInternaDataContext bd, int id) =>
                bd.Tareas.FirstOrDefault(t => t.TAR_Id == id));
        #endregion
    }
}
