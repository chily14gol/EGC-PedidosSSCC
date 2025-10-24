using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Transactions;

namespace AccesoDatos
{
	public class DAL_EnlacesContables : DAL_Base<EnlacesContables>
	{
		protected override System.Data.Linq.Table<EnlacesContables> Tabla
		{
			get { return bd.EnlacesContables; }
		}

        public override List<EnlacesContables> L(bool sinFiltrar = false, System.Linq.Expressions.Expression<Func<EnlacesContables, bool>> preFiltro = null)
        {
            return base.L(sinFiltrar, preFiltro).OrderByDescending(r => r.FechaAlta).ToList();
        }

        public EnlacesContables ObtenerUltimoEnlace()
        {
            return (from reg in bd.EnlacesContables orderby reg.ECO_Fecha descending select reg).FirstOrDefault();
        }

        protected override bool Guardar(EnlacesContables entidad, int idPersonaModificacion)
		{
			EnlacesContables item = L_PrimaryKey(entidad.ValorPK);
			if(item == null)
			{
				item = new EnlacesContables();
				Tabla.InsertOnSubmit(item);
                item.FechaAlta = DateTime.Now;
				// Propiedades que solo se asignan en el alta
			}
			else
			{
				// Codigo usado solo en updates
			}
			item.ECO_Fecha = entidad.ECO_Fecha;
			item.ECO_PER_Id = entidad.ECO_PER_Id;
            item.ECO_Documento = entidad.ECO_Documento;
            item.ECO_DocumentoBytes = entidad.ECO_DocumentoBytes;
            
			bd.SubmitChanges();

            entidad.ECO_Id = item.ECO_Id; //Para usarlo a posteriori

			return true;
		}

        public bool AsignarEnlaceContablaAPedidos(EnlacesContables entidad, List<Facturas> lstPedidos, int idPersonaModificacion)
        {
            using (TransactionScope transaccion = ComenzarTransaccion())
            {
                int[] arrFAC_Id = lstPedidos.Select(r => r.FAC_Id).ToArray();
                List<Facturas> lstPedidosActualizar = (from fac in bd.Facturas where arrFAC_Id.Contains(fac.FAC_Id) select fac).ToList();

                foreach (Facturas objPedido in lstPedidosActualizar)
                {
                    objPedido.FAC_ECO_Id = entidad.ECO_Id;
                    objPedido.FechaModificacion = DateTime.Now;
                }
                bd.SubmitChanges();

                bd.ConfirmarTransaccion();
                return true;
            }
        }

        public bool AsociarDocumento(int pintECO_Id, Binary parrDocumentoBytes)
        {
            EnlacesContables enlace = (from reg in bd.EnlacesContables where reg.ECO_Id.Equals(pintECO_Id) select reg).FirstOrDefault();
            if (enlace != null)
            {
                enlace.ECO_DocumentoBytes = parrDocumentoBytes;
                bd.SubmitChanges();
                return true;
            }
            else return false;
        }
    }
}
