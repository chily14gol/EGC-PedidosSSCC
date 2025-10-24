using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_ItemNumbersD365 : DAL_Base<ItemNumbersD365>
	{
		public DAL_ItemNumbersD365() : base() { }
		public DAL_ItemNumbersD365(TransactionScope transaccion) : base(transaccion) {}

		protected override System.Data.Linq.Table<ItemNumbersD365> Tabla
		{
			get { return bd.ItemNumbersD365; }
		}

		public override string ComboText { get { return "IN3_Nombre"; } }
		public override string ComboValue { get { return "IN3_Id"; } }
        public override List<ItemNumbersD365> ObtenerCombo(object valorFK, bool sinFiltrar = false, bool registroVacio = true, object valorSeleccionado = null, string pstrCampoOrdenar = null)
        {
            List<ItemNumbersD365> lstRetorno;
            lstRetorno = base.ObtenerCombo(valorFK, sinFiltrar, registroVacio, valorSeleccionado, pstrCampoOrdenar);

            return lstRetorno.Where(r => r.IN3_Activo || r.IN3_Id.Equals(valorSeleccionado) || r.IN3_Id <= 0).ToList(); //Sacamos solo los activos, el valor seleccionado y el registro vacio si lo hay
        }

        protected override bool Guardar(ItemNumbersD365 entidad, int idPersonaModificacion)
        {
            bool retorno = false;

            ItemNumbersD365 item = L_PrimaryKey(entidad.IN3_Id);

            if (item == null)
            {
                item = new ItemNumbersD365();
                // Propiedades que solo se asignan en el alta
                int? intId = (from reg in bd.ItemNumbersD365 select (int?)reg.IN3_Id).Max();
                item.IN3_Id = (intId != null) ? intId.Value + 1 : 1;
                item.FechaAlta = DateTime.Now;
                bd.ItemNumbersD365.InsertOnSubmit(item);
            }
            else
            {
                // Propiedades que solo se asignan una vez dado de alta, en updates
            }
            item.FechaModificacion = DateTime.Now;
            item.PER_Id_Modificacion = idPersonaModificacion;

            item.IN3_Nombre = entidad.IN3_Nombre;
            item.IN3_Activo = entidad.IN3_Activo;

            bd.SubmitChanges();
            entidad.IN3_Id = item.IN3_Id; //Asignamos el ID autonumerico al objeto "origen" para devolverlo en el caso de un alta.try

			retorno = true;

			return retorno;
		}
    }
}
