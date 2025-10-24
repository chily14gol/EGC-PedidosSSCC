using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AccesoDatos
{
    public class DAL_Seguridad_Opciones : DAL_Base<Seguridad_Opciones>
    {
        public override object ParsearValorPK(string valorPK) { return valorPK; }

		protected override System.Data.Linq.Table<Seguridad_Opciones> Tabla
		{
			get { return bd.Seguridad_Opciones; }
		}

		protected override Expression<Func<Seguridad_Opciones, bool>> ObtenerPrefiltroPrimaryKey(object valorPK)
		{
			string valor = valorPK.ToString();
			Expression<Func<Seguridad_Opciones, bool>> retorno = p => p.SOP_Id == valor;
			return retorno;
		}

        protected override bool Guardar(Seguridad_Opciones entidad, int idPersonaModificacion)
		{
			Seguridad_Opciones item = L_PrimaryKey(entidad.SOP_Id);
			if (item == null)
			{
				item = new Seguridad_Opciones();
		
				item.FechaAlta = DateTime.Now;
				item.SOP_Id = entidad.SOP_Id;
                item.SOP_Nombre = entidad.SOP_Nombre;
                bd.Seguridad_Opciones.InsertOnSubmit(item);
			}

			item.FechaModificacion = DateTime.Now;
			item.PER_Id_Modificacion = idPersonaModificacion;

			bd.SubmitChanges();
			return true;
		}

	}
}
