using System;

using System.DirectoryServices;

namespace PedidosSSCC
{
    public class LDAP
    {
        public static bool AuthenticateUser(string username, string password, string LdapPath)
        {
			DirectoryEntry entry = new DirectoryEntry(LdapPath, username, password);
            try
            {
                // Bind to the native AdsObject to force authentication.
                Object obj = entry.NativeObject;
            }
            catch
            {
                return false;
            }
            return true;
        }

    }
}
