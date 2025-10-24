using System;

namespace AccesoDatos
{
    public class DAL_UnidadesTarea_Idioma: DAL_Base<UnidadesTarea_Idioma>
	{
        public override object ParsearValorPK(string valorPK) { return valorPK; }

		protected override System.Data.Linq.Table<UnidadesTarea_Idioma> Tabla
		{
			get
			{
				return bd.UnidadesTarea_Idioma;
			}
		}
		public override string ComboText { get { return "UTI_Nombre"; } }
		public override string ComboValue { get { return "UTI_UTA_Id"; } }

		protected override bool L_Params(UnidadesTarea_Idioma reg)
		{
			return reg.UTI_IDI_Id == 1;
		}

		protected override bool Guardar(UnidadesTarea_Idioma entidad, int idPersonaModificacion)
		{
			throw new NotImplementedException();
		}

		protected override bool Borrar(object valorPK)
		{
			throw new NotImplementedException();
		}
	}
}
