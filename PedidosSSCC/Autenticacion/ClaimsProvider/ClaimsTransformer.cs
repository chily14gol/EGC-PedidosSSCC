using System;
using System.Linq;
using System.Security.Claims;

namespace PedidosSSCC.Autenticacion.ClaimsProvider
{
    public class ClaimsTransformer : ClaimsAuthenticationManager
    {
        public override ClaimsPrincipal Authenticate(string resourceName, ClaimsPrincipal incomingPrincipal)
        {
            if (incomingPrincipal != null && incomingPrincipal.Identity.IsAuthenticated == true)
            {
                var identity = (ClaimsIdentity)incomingPrincipal.Identity;
                _ = (identity.Name.Contains('\\') && identity.Name.Split('\\').Length > 1) ? identity.Name.Split('\\')[1] : identity.Name;
            }

            return incomingPrincipal;
        }
    }
}
