using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Tareas_Empresas_LineasEsfuerzo : DAL_Base<Tareas_Empresas_LineasEsfuerzo>
	{
		public DAL_Tareas_Empresas_LineasEsfuerzo() : base() { }

		public DAL_Tareas_Empresas_LineasEsfuerzo(TransactionScope transaccion) : base(transaccion) {}

        protected const int MargenFacturaP = 1; //Damos el margen de 1€ arriba o abajo

        protected override System.Data.Linq.Table<Tareas_Empresas_LineasEsfuerzo> Tabla
		{
			get { return bd.Tareas_Empresas_LineasEsfuerzo; }
		}

        public enum TipoClaveExterna
        {
            Pendientes = 1,
            SeleccionablesFactura = 2,
            SeleccionadosFactura = 3,
            AnyoMes = 4,
            TareaEmpresaAnyoMes = 5
        }

        public override bool ConsultarMedianteClaveExterna { get { return true; } }
        
        private TipoClaveExterna _modoConsultaClaveExterna;

        public TipoClaveExterna ModoConsultaClaveExterna
        {
            get { return _modoConsultaClaveExterna; }
            set { _modoConsultaClaveExterna = value; }
        }

        public override Tareas_Empresas_LineasEsfuerzo L_PrimaryKey(object valorPK, bool sinFiltrar = false)
        {
            Tareas_Empresas_LineasEsfuerzo retorno = get_PrimaryKey(bd != null ? bd : new FacturacionInternaDataContext(), int.Parse(valorPK.ToString()));

            if (retorno == null)
                return retorno;

            return retorno;
        }

        public override List<Tareas_Empresas_LineasEsfuerzo> L_ClaveExterna(object valorFK, Expression<Func<Tareas_Empresas_LineasEsfuerzo, bool>> preFiltro = null)
        {
            List<Tareas_Empresas_LineasEsfuerzo> retorno = new List<Tareas_Empresas_LineasEsfuerzo>();

            switch (ModoConsultaClaveExterna)
            {
                case TipoClaveExterna.SeleccionablesFactura:
                    string[] arrFK = valorFK.ToString().Split(Constantes.SeparadorPK);
                    int? lintCFA_Id = arrFK[2].ToInt();
                    //Obtenemos las lineas de esfuerzo de esa empresa y ese concepto de facturacion (si se ha seleccionado):
                    retorno = (from reg in bd.Tareas_Empresas_LineasEsfuerzo where (!lintCFA_Id.HasValue || reg.Tareas_Empresas.Tareas.TAR_CFA_Id.Equals(lintCFA_Id)) && reg.TLE_EMP_Id.Equals(arrFK[0].ToInt()) && (reg.TLE_ESO_Id.Equals((int)Constantes.EstadosSolicitud.PendienteAprobacion) || reg.TLE_ESO_Id.Equals((int)Constantes.EstadosSolicitud.Aprobado)) select reg).ToList();

                    //Obtenemos las lineas de esfuerzo usadas en otras facturas:
                    List<int> lstLineasUtilizadas = (from reg in bd.Facturas_Tareas_LineasEsfuerzo where !reg.FLE_FAC_Id.Equals(arrFK[1].ToInt()) select reg.FLE_TLE_Id).ToList();

                    //Nos quedamos con las no utilizadas:
                    retorno = retorno.Where(r => !lstLineasUtilizadas.Contains(r.TLE_Id)).ToList();
                    break;
                case TipoClaveExterna.SeleccionadosFactura:
                    //Obtenemos las lineas de esfuerzo de esa factura:
                    retorno = (from reg in bd.Tareas_Empresas_LineasEsfuerzo
                               join fle in bd.Facturas_Tareas_LineasEsfuerzo on reg.TLE_Id equals fle.FLE_TLE_Id
                               where fle.FLE_FAC_Id.Equals(valorFK)
                               select reg).ToList();
                    break;
                case TipoClaveExterna.AnyoMes:
                    string[] arrFK_AnyoMes = valorFK.ToString().Split(Constantes.SeparadorPK);
                    //Obtenemos las lineas de esfuerzo de ese año/mes:
                    retorno = (from reg in bd.Tareas_Empresas_LineasEsfuerzo where reg.TLE_Anyo.Equals(arrFK_AnyoMes[0].ToInt()) && reg.TLE_Mes.Equals(arrFK_AnyoMes[1].ToInt()) select reg).ToList();
                    break;
                case TipoClaveExterna.TareaEmpresaAnyoMes:
                    string[] arrFK_TareaEmpresaAnyoMes = valorFK.ToString().Split(Constantes.SeparadorPK);
                    //Obtenemos las lineas de esfuerzo de ese año/mes:
                    retorno = (from reg in bd.Tareas_Empresas_LineasEsfuerzo
                               where reg.TLE_TAR_Id.Equals(arrFK_TareaEmpresaAnyoMes[0].ToInt())
                               && reg.TLE_EMP_Id.Equals(arrFK_TareaEmpresaAnyoMes[1].ToInt()) && reg.TLE_Anyo.Equals(arrFK_TareaEmpresaAnyoMes[2].ToInt()) && reg.TLE_Mes.Equals(arrFK_TareaEmpresaAnyoMes[3].ToInt())
                               select reg).ToList();
                    break;
                case TipoClaveExterna.Pendientes:
                default:
                    retorno = (from reg in bd.Tareas_Empresas_LineasEsfuerzo where reg.TLE_PER_Id_Aprobador.Equals(valorFK) && reg.TLE_ESO_Id.Equals((int)Constantes.EstadosSolicitud.PendienteAprobacion) select reg).ToList();
                    break;
            }

            return retorno;
        }

        public override List<Tareas_Empresas_LineasEsfuerzo> L(bool sinFiltrar = false, Expression<Func<Tareas_Empresas_LineasEsfuerzo, bool>> preFiltro = null)
        {
            List<Tareas_Empresas_LineasEsfuerzo> retorno;
            
            if (preFiltro == null)
                retorno = get_L(bd != null ? bd : new FacturacionInternaDataContext()).ToList();
            else 
                retorno = (from reg in bd.Tareas_Empresas_LineasEsfuerzo
                    .Where(preFiltro) select reg)
                    .ToList();

            if (!sinFiltrar)
                retorno = retorno.Where(c => c.PuedeAccederRegistro).ToList();

            return retorno;
        }

        public override bool ValidacionesGuardarEspecificas(ref Tareas_Empresas_LineasEsfuerzo objConcepto)
        {
            DAL_Tareas_Empresas dalTEM = new DAL_Tareas_Empresas();
            Tareas_Empresas tareaEmpresa = dalTEM.L_PrimaryKey(String.Format("{1}{0}{2}{0}{3}", Constantes.SeparadorPK, 
                objConcepto.TLE_TAR_Id, objConcepto.TLE_EMP_Id, objConcepto.TLE_Anyo));

            if (tareaEmpresa != null)
            {
                int idConcepto = objConcepto.TLE_Id;
                int idTarea = objConcepto.TLE_TAR_Id;
                int idEmpresa = objConcepto.TLE_EMP_Id;
                int anyo = objConcepto.TLE_Anyo;
                
                //Comprobamos que no se supere el nº de horas, de unidades o el presupuesto definido a nivel de empresa-linea de esfuerzo. Solo avisamos pero lo permitimos
                decimal? ldecCantidadTotal = (from tle in bd.Tareas_Empresas_LineasEsfuerzo where tle.TLE_EMP_Id.Equals(idEmpresa) 
                                              && tle.TLE_TAR_Id.Equals(idTarea) && tle.TLE_Anyo.Equals(anyo) && !tle.TLE_Id.Equals(idConcepto) select tle).Sum(r => (decimal?)r.TLE_Cantidad);
                
                ldecCantidadTotal = ldecCantidadTotal.HasValue ? ldecCantidadTotal.Value + objConcepto.TLE_Cantidad : objConcepto.TLE_Cantidad;

                switch (tareaEmpresa.Tareas.TAR_TTA_Id)
                {
                    case (int)Constantes.TipoTarea.PorHoras:
                    case (int)Constantes.TipoTarea.PorUnidades:
                        if (ldecCantidadTotal > tareaEmpresa.TEM_Elementos)
                        {
                            objConcepto.MensajeErrorEspecifico = Resources.Resource.alertCantidadTareaEmpresaSuperada;
                        }
                        break;
                    case (int)Constantes.TipoTarea.CantidadFija:
                        if (ldecCantidadTotal > tareaEmpresa.TEM_Presupuesto)
                        {
                            objConcepto.MensajeErrorEspecifico = Resources.Resource.alertPresupuestoTareaEmpresaSuperado;
                        }
                        break;
                }
            }

            return true;
        }

        protected override bool Guardar(Tareas_Empresas_LineasEsfuerzo objConcepto, int idPersonaModificacion)
        {
            bool retorno = false;
            Tareas_Empresas_LineasEsfuerzo item;

            if (objConcepto.TLE_Id > 0)
            {
                item = L_PrimaryKey(objConcepto.TLE_Id);
            }
            else
            {
                item = new Tareas_Empresas_LineasEsfuerzo();
                item.FechaAlta = DateTime.Now;
                bd.Tareas_Empresas_LineasEsfuerzo.InsertOnSubmit(item);
            }

            item.TLE_TAR_Id = objConcepto.TLE_TAR_Id;
            item.TLE_EMP_Id = objConcepto.TLE_EMP_Id;
            item.TLE_Anyo = objConcepto.TLE_Anyo;
            item.TLE_Mes = objConcepto.TLE_Mes;
            item.TLE_Cantidad = objConcepto.TLE_Cantidad;
            item.TLE_Descripcion = objConcepto.TLE_Descripcion;
            item.TLE_ESO_Id = objConcepto.TLE_ESO_Id;
            item.TLE_Inversion = objConcepto.TLE_Inversion;
            item.TLE_FAP_Id = objConcepto.TLE_FAP_Id;

            // Si está pendiente de aprobación, asignar aprobador
            if (objConcepto.TLE_ESO_Id == (int)Constantes.EstadosSolicitud.PendienteAprobacion)
            {
                DAL_Empresas_Aprobadores dal = new DAL_Empresas_Aprobadores
                {
                    ModoConsultaClaveExterna = DAL_Empresas_Aprobadores.TipoClaveExterna.Empresa
                };

                // Obtener lista de aprobadores
                //List<Empresas_Aprobadores> listaAprobadores = dal.L_ClaveExterna(objConcepto.TLE_EMP_Id);

                //DAL_Empresas dalEmp = new DAL_Empresas();
                //Empresas objEmpresa = dalEmp.L_PrimaryKey(objConcepto.TLE_EMP_Id);

                //// Asignar el aprobador por defecto si la empresa existe
                //if (objEmpresa != null)
                //{
                //    // Verificar si Session.IdPersona está en la lista de aprobadores
                //    bool existeAprobador = listaAprobadores.Any(aprobador => aprobador.EMA_PER_Id == Sesion.SPersonaId);

                //    // Si existe en la lista, usa Session.IdPersona; si no, usa el aprobador por defecto
                //    item.TLE_PER_Id_Aprobador = existeAprobador ? Sesion.SPersonaId : objEmpresa.EMP_PER_Id_AprobadorDefault;
                //}

                DAL_Tareas_Empresas dalTarEmp = new DAL_Tareas_Empresas();
                string clave = objConcepto.TLE_TAR_Id + "|" + objConcepto.TLE_EMP_Id + "|" + objConcepto.TLE_Anyo;
                Tareas_Empresas objTareaEmpresa = dalTarEmp.L_PrimaryKey(clave);

                if (objTareaEmpresa != null)
                    item.TLE_PER_Id_Aprobador = objTareaEmpresa.TEM_PER_Id_Aprobador;

                item.TLE_FechaAprobacion = null;
                item.TLE_ComentarioAprobacion = objConcepto.TLE_ComentarioAprobacion;
            }

            item.FechaModificacion = DateTime.Now;
            item.PER_Id_Modificacion = idPersonaModificacion;

            bd.SubmitChanges();
            objConcepto.TLE_Id = item.TLE_Id;

            retorno = true;

            return retorno;
        }

        public override bool D(object valorPK)
        {
            Tareas_Empresas_LineasEsfuerzo tle = L_PrimaryKey(valorPK, sinFiltrar: true);
            bool lbolRetorno = base.D(valorPK);

            return lbolRetorno;
        }

        public IQueryable<Tareas_Empresas_LineasEsfuerzo> Leer(Expression<Func<Tareas_Empresas_LineasEsfuerzo, bool>> preFiltro = null)
        {
            var query = bd.Tareas_Empresas_LineasEsfuerzo
                .Include(t => t.EstadosSolicitud)
                .Where(tle => bd.Tareas.Any(t => t.TAR_Id == tle.TLE_TAR_Id))
                .AsNoTracking();

            if (preFiltro != null)
                query = query.Where(preFiltro);

            return query;
        }

        public bool AprobarConcepto(int idConcepto, Constantes.EstadosSolicitud estado, string comentario)
        {
            Tareas_Empresas_LineasEsfuerzo item = L_PrimaryKey(idConcepto);
            item.TLE_ESO_Id = (int)estado;
            item.TLE_FechaAprobacion = DateTime.Now;
            item.TLE_ComentarioAprobacion = comentario;
            item.FechaModificacion = DateTime.Now;

            bd.SubmitChanges();

            bool retorno = true;
            return retorno;
        }

        #region "CompiledQuerys"
        public static Func<FacturacionInternaDataContext, IEnumerable<Tareas_Empresas_LineasEsfuerzo>>
            get_L = CompiledQuery.Compile<FacturacionInternaDataContext, IEnumerable<Tareas_Empresas_LineasEsfuerzo>>((FacturacionInternaDataContext bd)
                => (from reg in bd.Tareas_Empresas_LineasEsfuerzo
                    select reg));

        public static Func<FacturacionInternaDataContext, int, Tareas_Empresas_LineasEsfuerzo>
            get_PrimaryKey = CompiledQuery.Compile<FacturacionInternaDataContext, int, Tareas_Empresas_LineasEsfuerzo>((FacturacionInternaDataContext bd, int valorFK)
                => (from reg in bd.Tareas_Empresas_LineasEsfuerzo
                    where reg.TLE_Id.Equals(valorFK)
                    select reg).FirstOrDefault());
        #endregion
    }
}
