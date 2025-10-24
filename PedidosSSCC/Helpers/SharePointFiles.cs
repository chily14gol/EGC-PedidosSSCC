using Microsoft.SharePoint.Client;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PedidosSSCC.Helpers
{
    public static class SharePointFiles
    {
        public static async Task<string> ObtenerFicherosSharepoint(string siteURL, string siteFolder, string nombreExcel)
        {
            try
            {
                // Token para SharePoint
                var resource = new Uri(siteURL).GetLeftPart(UriPartial.Authority);
                var token = await TokenProvider.GetAccessTokenAsync(resource);

                using (var ctx = new ClientContext(siteURL))
                {
                    ctx.ExecutingWebRequest += (s, e) =>
                    {
                        e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + token;
                    };

                    // Carpeta origen (relativa al sitio)
                    var srcFolder = ctx.Web.GetFolderByServerRelativeUrl(siteFolder);

                    // Cargamos solo archivos de la carpeta
                    ctx.Load(srcFolder, f => f.Files);
                    ctx.ExecuteQuery();

                    var file = srcFolder.Files.FirstOrDefault(f => Path.GetFileName(f.Name)
                        .Equals(nombreExcel, StringComparison.OrdinalIgnoreCase));

                    string rutaTemp = ConfigurationManager.AppSettings["PathTemporales"] + @"\" + nombreExcel;

                    using (var fileStream = System.IO.File.Create(rutaTemp))
                    {
                        ClientResult<Stream> streamResult = file.OpenBinaryStream();
                        ctx.ExecuteQuery();
                        streamResult.Value.CopyTo(fileStream);
                    }

                    return rutaTemp;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error SharePoint: {ex.Message}");
            }

            return null;
        }
    }
}
