using System;
using System.Collections.Generic;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_LineasNegocio_Idioma : DAL_Base<LineasNegocio_Idioma>
    {
        public override object ParsearValorPK(string valorPK) { return valorPK; }

        protected override System.Data.Linq.Table<LineasNegocio_Idioma> Tabla
        {
            get { return bd.LineasNegocio_Idioma; }
        }

        protected override bool Guardar(LineasNegocio_Idioma entidad, int idPersonaModificacion)
        {
            //Usuarios item = L_PrimaryKey(entidad.USU_Id);
            //if (item == null)
            //{
            //    item = new Usuarios();
            //    // Propiedades que solo se asignan en el alta
            //    item.FechaAlta = DateTime.Now;
            //    item.USU_Id = entidad.USU_Id;
            //    item.USU_PER_Id = entidad.USU_PER_Id;
            //    bd.Usuarios.InsertOnSubmit(item);
            //}
            //else
            //{
            //}
            //item.FechaModificacion = DateTime.Now;
            //item.PER_Id_Modificacion = idPersonaModificacion;

            //item.USU_SPE_Id = entidad.USU_SPE_Id;
            //item.USU_VerTodo = entidad.USU_VerTodo;


            ////Si han puesto algun email lo actualizamos
            //if (!String.IsNullOrEmpty(entidad.PER_Email_Actualizar))
            //{
            //    if (item.PersonaUsuario != null)
            //        item.PersonaUsuario.PER_Email = entidad.PER_Email_Actualizar;
            //    else
            //    {
            //        DAL_Personas dalPER = new DAL_Personas();
            //        Personas persona = dalPER.L_PrimaryKey(entidad.USU_PER_Id);
            //        if (persona != null)
            //        {
            //            persona.PER_Email = entidad.PER_Email_Actualizar;
            //            dalPER.G(persona, idPersonaModificacion);
            //        }
            //    }
            //}

            //bd.SubmitChanges();

            return true;
        }
    }
}
