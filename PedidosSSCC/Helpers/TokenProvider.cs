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
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
                return certs.Count > 0 ? certs[0] : null;
            }
        }

        public static async Task<string> GetAccessTokenAsync(string endpoint)
        {
            var clientId = ConfigurationManager.AppSettings["SP_ClientId"];
            var tenantId = ConfigurationManager.AppSettings["SP_TenantId"];
            var thumbprint = ConfigurationManager.AppSettings["SP_Certificado"];

            using (var certificate = GetCertificateByThumbprint(thumbprint)) // aquí certPath es el valor de SP_Certificado
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithTenantId(tenantId)
                    .WithCertificate(certificate)
                    .Build();

                var token = await app
                    .AcquireTokenForClient(new[] { $"{endpoint.TrimEnd('/')}/.default" })
                    .ExecuteAsync();

                return token.AccessToken;
            }
        }
    }
}
