using PedidosSSCC;
using PedidosSSCC.Comun;
using System;
using System.Configuration;
using System.Data;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Usuarios : DAL_Base<Usuarios>
    {
        public override object ParsearValorPK(string valorPK) { return valorPK; }

		protected override System.Data.Linq.Table<Usuarios> Tabla
		{
			get { return bd.Usuarios; }
		}

        public static Usuarios ObtenerUsuario(string pstrUsuario)
        {
            return ObtenerUsuario(pstrUsuario, String.Empty, false);
        }

        public static Usuarios ObtenerUsuario(string pstrUsuario, string pstrPass, bool pbolComprobarPass)
        {
            FacturacionInternaDataContext bd = new FacturacionInternaDataContext();

            if (pbolComprobarPass)
            {
                //Realizamos la consulta contra el LDAP
				string lstrPathLDAP = ConfigurationManager.AppSettings["LDAP_PATH"];
                bool lbolResultado = LDAP.AuthenticateUser(pstrUsuario, pstrPass, lstrPathLDAP);
				if(!lbolResultado) return null;
            }

            //Si el trabajador no está activo no le permitimos acceder
            return (from reg in bd.Usuarios where reg.USU_Id == pstrUsuario select reg).FirstOrDefault();
        }

        public Usuarios L_PorIDPersona(int pintIdPersona)
        {
            FacturacionInternaDataContext bd = new FacturacionInternaDataContext();
            return (from reg in bd.Usuarios where reg.USU_PER_Id == pintIdPersona select reg).FirstOrDefault();
        }

        public bool ModificarIdUsuario(string USU_Id_Ant, string USU_Id)
        {
            if (USU_Id_Ant != USU_Id) //Se ha modificado el id de usuario
            {
                FacturacionInternaDataContext bd = new FacturacionInternaDataContext();
                System.Transactions.TransactionScope transaccion = bd.ComenzarTransaccion();
                bd.AsignarTransaccion(transaccion);
                //bd.ExecuteCommand("ALTER TABLE dbo.Usuarios_UnidadesNegocio NOCHECK CONSTRAINT ALL");
                bd.ExecuteCommand("UPDATE dbo.Usuarios SET USU_Id = {0} WHERE USU_Id = {1}", USU_Id, USU_Id_Ant);
                //bd.ExecuteCommand("UPDATE dbo.Usuarios_UnidadesNegocio SET UUN_USU_Id = {0} WHERE UUN_USU_Id = {1}", USU_Id, USU_Id_Ant);
                //bd.ExecuteCommand("ALTER TABLE dbo.Usuarios_UnidadesNegocio CHECK CONSTRAINT ALL");
                bd.ConfirmarTransaccion();
                bd.Dispose();
            }

            return true;
        }

        protected override bool Guardar(Usuarios entidad, int idPersonaModificacion)
		{
            Usuarios item = L_PrimaryKey(entidad.USU_Id);

            if (item == null)
            {
                item = new Usuarios();
                item.USU_Id = entidad.USU_Id;
                item.USU_SPE_Id = entidad.USU_SPE_Id;
                item.USU_PER_Id = entidad.USU_PER_Id;
                item.PER_Id_Modificacion = Sesion.SPersonaId;
                item.FechaAlta = DateTime.Now;

                bd.Usuarios.InsertOnSubmit(item);
            }

            item.USU_SPE_Id = entidad.USU_SPE_Id;
            item.USU_VerTodo = entidad.USU_VerTodo;
            item.FechaModificacion = DateTime.Now;
            item.PER_Id_Modificacion = idPersonaModificacion;

            bd.SubmitChanges();

            return true;
        }
    }
}
