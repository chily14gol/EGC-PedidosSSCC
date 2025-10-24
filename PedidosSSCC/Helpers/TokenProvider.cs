using Microsoft.Identity.Client;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PedidosSSCC.Helpers
{
    public static class TokenProvider
    {
        private static X509Certificate2 GetCertificateByThumbprint(string thumbprint)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                return null;
            }

            string normalizedThumbprint = thumbprint.Replace(" ", string.Empty).ToUpperInvariant();

            foreach (var location in new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine })
            {
                using (var store = new X509Store(StoreName.My, location))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var certs = store.Certificates.Find(X509FindType.FindByThumbprint, normalizedThumbprint, validOnly: false);
                    if (certs.Count > 0)
                    {
                        // Creamos una copia para evitar que se libere al cerrar el almacén.
                        return new X509Certificate2(certs[0]);
                    }
                }
            }

            return null;
        }

        public static async Task<string> GetAccessTokenAsync(string endpoint)
        {
            var clientId = ConfigurationManager.AppSettings["SP_ClientId"];
            var tenantId = ConfigurationManager.AppSettings["SP_TenantId"];
            var thumbprint = ConfigurationManager.AppSettings["SP_Certificado"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ConfigurationErrorsException("Faltan los valores de configuración de Azure AD para SharePoint.");
            }

            using (var certificate = GetCertificateByThumbprint(thumbprint))
            {
                if (certificate == null)
                {
                    throw new ConfigurationErrorsException("No se ha encontrado el certificado configurado para autenticarse contra SharePoint.");
                }

                var app = ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithTenantId(tenantId)
                    .WithCertificate(certificate)
                    .Build();

                var token = await app
                    .AcquireTokenForClient(new[] { $"{endpoint.TrimEnd('/')}/.default" })
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                return token.AccessToken;
            }
        }
    }
}
