using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_ProductosD365 : DAL_Base<ProductosD365>
	{
		public DAL_ProductosD365() : base() { }
		public DAL_ProductosD365(TransactionScope transaccion) : base(transaccion) {}

		protected override System.Data.Linq.Table<ProductosD365> Tabla
		{
			get { return bd.ProductosD365; }
		}

		public override string ComboText { get { return "PR3_Nombre"; } }
		public override string ComboValue { get { return "PR3_Id"; } }

        public override List<ProductosD365> ObtenerCombo(object valorFK, bool sinFiltrar = false, bool registroVacio = true, object valorSeleccionado = null, string pstrCampoOrdenar = null)
        {
            List<ProductosD365> lstRetorno;
            lstRetorno = base.ObtenerCombo(valorFK, sinFiltrar, registroVacio, valorSeleccionado, pstrCampoOrdenar);
         
            return lstRetorno.Where(r => r.PR3_Activo || r.PR3_Id.Equals(valorSeleccionado) || r.PR3_Id <= 0).ToList(); //Sacamos solo los activos, el valor seleccionado y el registro vacio si lo hay
        }

        protected override bool Guardar(ProductosD365 entidad, int idPersonaModificacion)
        {
            bool retorno = false;

            ProductosD365 item = L_PrimaryKey(entidad.PR3_Id);

            if (item == null)
            {
                item = new ProductosD365();
                // Propiedades que solo se asignan en el alta
                int? intId = (from reg in bd.ProductosD365 select (int?)reg.PR3_Id).Max();
                item.PR3_Id = (intId != null) ? intId.Value + 1 : 1;
                item.FechaAlta = DateTime.Now;
                bd.ProductosD365.InsertOnSubmit(item);
            }
            else
            {
                // Propiedades que solo se asignan una vez dado de alta, en updates
            }
            item.FechaModificacion = DateTime.Now;
            item.PER_Id_Modificacion = idPersonaModificacion;

            item.PR3_Nombre = entidad.PR3_Nombre;
            item.PR3_Activo = entidad.PR3_Activo;

            bd.SubmitChanges();
            entidad.PR3_Id = item.PR3_Id; //Asignamos el ID autonumerico al objeto "origen" para devolverlo en el caso de un alta.try

			retorno = true;

			return retorno;
		}
    }
}
