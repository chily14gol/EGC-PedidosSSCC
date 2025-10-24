using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Empresas_Aprobadores : DAL_Base<Empresas_Aprobadores>
    {
        public DAL_Empresas_Aprobadores() : base() { }

        public DAL_Empresas_Aprobadores(TransactionScope transaccion) : base(transaccion) { }

        protected override System.Data.Linq.Table<Empresas_Aprobadores> Tabla
        {
            get { return bd.Empresas_Aprobadores; }
        }

        public override bool ConsultarMedianteClaveExterna { get { return true; } }

        private TipoClaveExterna _modoConsultaClaveExterna;

        public TipoClaveExterna ModoConsultaClaveExterna
        {
            get { return _modoConsultaClaveExterna; }
            set { _modoConsultaClaveExterna = value; }
        }

        public enum TipoClaveExterna
        {
            Empresa = 1,
            Persona = 2
        }

        public override List<Empresas_Aprobadores> L_ClaveExterna(object valorFK, Expression<Func<Empresas_Aprobadores, bool>> preFiltro = null)
        {
            List<Empresas_Aprobadores> retorno = new List<Empresas_Aprobadores>();

            switch (ModoConsultaClaveExterna)
            {
                case TipoClaveExterna.Persona:
                    retorno = (from reg in bd.Empresas_Aprobadores where reg.EMA_PER_Id.Equals(valorFK) select reg).ToList();
                    break;
                case TipoClaveExterna.Empresa:
                default:
                    retorno = (from reg in bd.Empresas_Aprobadores where reg.EMA_EMP_Id.Equals(valorFK) select reg).ToList();
                    break;
            }

            return retorno;
        }

        public bool EliminarAprobadores(int idEmpresa, List<int> idsAprobadores)
        {
            try
            {
                bool retorno = false;

                List<Empresas_Aprobadores> registrosAEliminar = null;

                if (idsAprobadores != null)
                {
                    // Eliminar solo los que ya no están en la lista
                    registrosAEliminar = bd.Empresas_Aprobadores
                        .Where(e => e.EMA_EMP_Id == idEmpresa && !idsAprobadores.Contains(e.EMA_PER_Id))
                        .ToList();
                }
                else
                {
                    registrosAEliminar = bd.Empresas_Aprobadores
                        .Where(e => e.EMA_EMP_Id == idEmpresa)
                        .ToList();
                }
              
                if (registrosAEliminar != null)
                {
                    bd.Empresas_Aprobadores.DeleteAllOnSubmit(registrosAEliminar);
                    bd.SubmitChanges();
                }

                retorno = true;
                return retorno;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        protected override bool Guardar(Empresas_Aprobadores entidad, int idPersonaModificacion)
        {
            try
            {
                bool retorno = false;

                bd.Empresas_Aprobadores.InsertOnSubmit(entidad);
                bd.SubmitChanges();

                retorno = true;

                return retorno;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public override string ComboText { get { return "NombrePersona"; } }
        public override string ComboValue { get { return "EMA_PER_Id"; } }
    }
}
