using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Facturas : DAL_Base<Facturas>
	{
		public DAL_Facturas() : base() { }
		public DAL_Facturas(TransactionScope transaccion) : base(transaccion) {}

		protected override System.Data.Linq.Table<Facturas> Tabla
		{
			get { return bd.Facturas; }
		}

        public enum TipoClaveExterna
        {
            Pendientes = 1
        }

        public bool VerPedidos = true;

        public override bool ConsultarMedianteClaveExterna { get { return true; } }

        private TipoClaveExterna _modoConsultaClaveExterna;

        public TipoClaveExterna ModoConsultaClaveExterna
        {
            get { return _modoConsultaClaveExterna; }
            set { _modoConsultaClaveExterna = value; }
        }

        public string GenerarNumPedido(DateTime pdtFechaFactura)
        {
            //PVE03-SCAANNNN siendo "AA" los 2 últimos dígitos del año y "NNNN" el auto numérico anual 
            string lstrPrefijo = "PVE03-SC" + pdtFechaFactura.ToString("yy");

            string strNumFactura = (from fac in bd.Facturas
                                    where fac.FAC_NumFactura.StartsWith(lstrPrefijo) && fac.FAC_Pedido.Equals(VerPedidos)
                                    select fac.FAC_NumFactura).Max();

            int lintSecuencial;
            if (strNumFactura == null)
                lintSecuencial = 1;
            else
            {
                int.TryParse(strNumFactura.Replace(lstrPrefijo, String.Empty), out lintSecuencial);
                lintSecuencial++;
            }

            return lstrPrefijo + lintSecuencial.ToString().PadLeft(4, '0');
        }

        public override List<Facturas> L(bool sinFiltrar = false, Expression<Func<Facturas, bool>> preFiltro = null)
        {
            List<Facturas> retorno;

            if (preFiltro == null)
                retorno = get_L(bd != null ? bd : new FacturacionInternaDataContext(), VerPedidos).ToList();
            else
                retorno = (from reg in bd.Facturas.Where(preFiltro) select reg).ToList();

            if (!sinFiltrar)
                retorno = retorno.Where(c => c.PuedeAccederRegistro).ToList();

            return retorno;
        }

        public override List<Facturas> L_ClaveExterna(object valorFK, Expression<Func<Facturas, bool>> preFiltro = null)
        {
            List<Facturas> retorno = new List<Facturas>();

            switch (ModoConsultaClaveExterna)
            {
                case TipoClaveExterna.Pendientes:
                default:
                    retorno = (from reg in bd.Facturas where reg.FAC_PER_Id_Aprobador.Equals(valorFK) && reg.FAC_ESO_Id.Equals((int)Constantes.EstadosSolicitud.PendienteAprobacion) select reg).ToList();
                    break;
            }

            return retorno;
        }

        public override Facturas L_PrimaryKey(object valorPK, bool sinFiltrar = false)
        {
            Facturas retorno = get_PrimaryKey(bd ?? new FacturacionInternaDataContext(), int.Parse(valorPK.ToString()));
			if(retorno == null) return retorno;
            return retorno;
        }

        public List<Facturas> L_EnlaceContable(DateTime pdtFechaEnlace)
        {
            List<Facturas> retorno = (from reg in bd.Facturas 
                                      where reg.FAC_ESO_Id.Equals((int)Constantes.EstadosSolicitud.Aprobado) && 
                                            !reg.FAC_ECO_Id.HasValue && reg.FAC_FechaEmision <= pdtFechaEnlace &&
                                            reg.FAC_ImporteTotal > 0 //No hacemos enlace de los pedidos con importe negativo (no incluimos los abonos)
                                      select reg).ToList();
            return retorno;
        }

        protected override bool Guardar(Facturas entidad, int idPersonaModificacion)
        {
            bool retorno = false;

            Facturas item = L_PrimaryKey(entidad.FAC_Id);

            if (item == null)
            {
                item = new Facturas();
                item.FechaAlta = DateTime.Now;
                bd.Facturas.InsertOnSubmit(item);

                //Rellenamos la direccion con la de la empresa cliente:
                DAL_Empresas dalEMP = new DAL_Empresas();
                item.FAC_Direccion = dalEMP.L_PrimaryKey(entidad.FAC_EMP_Id, sinFiltrar: true).EMP_Direccion;
            }
            else
            {
                item.FAC_Direccion = entidad.FAC_Direccion;
            }

            item.FechaModificacion = DateTime.Now;
            item.PER_Id_Modificacion = idPersonaModificacion;

            item.FAC_EMP_Id = entidad.FAC_EMP_Id;
            item.FAC_CFA_Id = entidad.FAC_CFA_Id;
            item.FAC_NumFactura = entidad.FAC_NumFactura;
            item.FAC_EMP_Id_Facturar = entidad.FAC_EMP_Id_Facturar;
            item.FAC_Contacto = entidad.FAC_Contacto;
            item.FAC_FechaEmision = entidad.FAC_FechaEmision;
            item.FAC_FechaVencimiento = entidad.FAC_FechaVencimiento;
            item.FAC_Expediente = entidad.FAC_Expediente;
            item.FAC_Documento = entidad.FAC_Documento;
            item.FAC_DocumentoBytes = entidad.FAC_DocumentoBytes;
            item.FAC_IVATotal = entidad.FAC_IVATotal;
            item.FAC_ImporteTotal = entidad.FAC_ImporteTotal;
            item.FAC_ESO_Id = entidad.FAC_ESO_Id;
            item.FAC_RequiereAprobacion = entidad.FAC_RequiereAprobacion;
            item.FAC_PER_Id_Aprobador = entidad.FAC_PER_Id_Aprobador;
            item.FAC_FechaAprobacion = entidad.FAC_FechaAprobacion;
            item.FAC_ComentarioAprobacion = entidad.FAC_ComentarioAprobacion;
            item.FAC_Pedido = entidad.FAC_Pedido;

            bd.Facturas_Tareas_LineasEsfuerzo.DeleteAllOnSubmit(item.Facturas_Tareas_LineasEsfuerzo);
            item.Facturas_Tareas_LineasEsfuerzo.AddRange(entidad.Facturas_Tareas_LineasEsfuerzo);

            bd.SubmitChanges();
            entidad.FAC_Id = item.FAC_Id; //Asignamos el ID autonumerico al objeto "origen" para devolverlo en el caso de un alta.try

			retorno = true;

			return retorno;
		}

        public bool EditarPedido(Facturas objPedido, int idPersonaModificacion)
        {
            bool retorno = false;

            Facturas item = L_PrimaryKey(objPedido.FAC_Id);

            item.FechaModificacion = DateTime.Now;
            item.PER_Id_Modificacion = idPersonaModificacion;
            item.FAC_EMP_Id_Facturar = objPedido.FAC_EMP_Id_Facturar;
            item.FAC_Contacto = objPedido.FAC_Contacto;
            item.FAC_Expediente = objPedido.FAC_Expediente;
            item.FAC_RequiereAprobacion = objPedido.FAC_RequiereAprobacion;
            item.FAC_Direccion = objPedido.FAC_Direccion;
            item.FAC_ImporteTotal = objPedido.FAC_ImporteTotal;

            bd.SubmitChanges();

            retorno = true;

            return retorno;
        }

        public bool Solicitar(int pintFAC_Id, int pintPER_Id_Solicitante)
        {
            //Cambiamos el estado y asignamos el aprobador
            Facturas factura = L_PrimaryKey(pintFAC_Id, sinFiltrar: true);
            DAL_Empresas dalEMP = new DAL_Empresas();
            Empresas empresa = dalEMP.L_PrimaryKey(factura.FAC_EMP_Id, sinFiltrar: true);
            factura.FAC_PER_Id_Aprobador = empresa.EMP_PER_Id_AprobadorDefault;
            factura.FAC_ESO_Id = (int)Constantes.EstadosSolicitud.PendienteAprobacion;
            factura.PER_Id_Modificacion = pintPER_Id_Solicitante;
            factura.FechaModificacion = DateTime.Now;
            bd.SubmitChanges();

            return true;
        }

        public bool AprobarRechazar(Facturas objPedido, Constantes.EstadosSolicitud estadoSolicitud, string pstrComentario, int pintPER_Id_Aprobador)
        {
            objPedido.FAC_ESO_Id = (int)estadoSolicitud;
            objPedido.FAC_PER_Id_Aprobador = pintPER_Id_Aprobador;
            objPedido.FAC_FechaAprobacion = DateTime.Now;
            objPedido.FAC_ComentarioAprobacion = pstrComentario;
            objPedido.FechaModificacion = DateTime.Now;

            bd.SubmitChanges();

            return true;
        }

        #region "CompiledQuerys"
        public static Func<FacturacionInternaDataContext, bool, IEnumerable<Facturas>>
            get_L = CompiledQuery.Compile<FacturacionInternaDataContext, bool, IEnumerable<Facturas>>((FacturacionInternaDataContext bd, bool VerPedidos)
                => (from reg in bd.Facturas where reg.FAC_Pedido.Equals(VerPedidos)
                    select reg));

        public static new Func<FacturacionInternaDataContext, int, Facturas>
            get_PrimaryKey = CompiledQuery.Compile<FacturacionInternaDataContext, int, Facturas>((FacturacionInternaDataContext bd, int valorFK)
                => (from reg in bd.Facturas
                    where reg.FAC_Id.Equals(valorFK)
                    select reg).FirstOrDefault());
        #endregion
    }
}
