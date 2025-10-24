using AccesoDatos;
using Comun;
using System;
using System.Collections.Generic;
using System.Web.Security;

namespace PedidosSSCC.Autenticacion.Model
{
    public class ExcelD365RoleProvider : RoleProvider
    {
        private FacturacionInternaDataContext _db = new FacturacionInternaDataContext();
        public override string ApplicationName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();
        }

        public override string[] GetRolesForUser(string username)
        {
            string usernameSplitted = Utilidades.ObtenerUsuarioSinDominio(username);

            //Obtenemos los roles del usuario
            List<string> userRoles = null; // RolService.ObtenerRolesUsuario(usernameSplitted);

            return userRoles.ToArray();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            throw new NotImplementedException();
        }
    }
}