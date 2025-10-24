using System;

namespace AccesoDatos
{
    public class DAL_Departamentos : DAL_Base<Departamentos>
    {
        public override object ParsearValorPK(string valorPK) { return valorPK; }

        protected override System.Data.Linq.Table<Departamentos> Tabla
        {
            get { return bd.Departamentos; }
        }

        protected override bool Guardar(Departamentos entidad, int idPersonaModificacion)
        {
            Departamentos item = L_PrimaryKey(entidad.DEP_Id);

            if (item == null)
            {
                item = new Departamentos();
                item.FechaAlta = DateTime.Now;
                bd.Departamentos.InsertOnSubmit(item);
            }

            item.DEP_Codigo = entidad.DEP_Codigo;
            item.DEP_Nombre = entidad.DEP_Nombre;
            item.DEP_CodigoD365 = entidad.DEP_CodigoD365;
            item.DEP_PER_Id_Responsable = entidad.DEP_PER_Id_Responsable;
            item.PER_Id_Modificacion = idPersonaModificacion;
            item.FechaModificacion = DateTime.Now;

            bd.SubmitChanges();

            return true;
        }
    }
}