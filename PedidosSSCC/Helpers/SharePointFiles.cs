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
                if (string.IsNullOrWhiteSpace(siteURL))
                {
                    throw new ArgumentException("La URL del sitio de SharePoint no puede estar vacía.", nameof(siteURL));
                }

                if (string.IsNullOrWhiteSpace(siteFolder))
                {
                    throw new ArgumentException("La ruta de la carpeta de SharePoint no puede estar vacía.", nameof(siteFolder));
                }

                if (string.IsNullOrWhiteSpace(nombreExcel))
                {
                    throw new ArgumentException("El nombre del fichero de SharePoint no puede estar vacío.", nameof(nombreExcel));
                }

                if (!Uri.TryCreate(siteURL, UriKind.Absolute, out var siteUri))
                {
                    throw new UriFormatException($"La URL de SharePoint '{siteURL}' no es válida.");
                }

                // Token para SharePoint
                var resource = siteUri.GetLeftPart(UriPartial.Authority);
                var token = await TokenProvider.GetAccessTokenAsync(resource).ConfigureAwait(false);

                using (var ctx = new ClientContext(siteUri.AbsoluteUri))
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

                    if (file == null)
                    {
                        throw new FileNotFoundException($"No se ha encontrado el fichero '{nombreExcel}' en SharePoint.");
                    }

                    string rutaBase = ConfigurationManager.AppSettings["PathTemporales"];
                    if (string.IsNullOrWhiteSpace(rutaBase))
                    {
                        throw new ConfigurationErrorsException("No se ha configurado la ruta para ficheros temporales (PathTemporales).");
                    }

                    string rutaTemp = Path.Combine(rutaBase, nombreExcel);
                    Directory.CreateDirectory(Path.GetDirectoryName(rutaTemp));

                    using (var fileStream = File.Create(rutaTemp))
                    {
                        ClientResult<Stream> streamResult = file.OpenBinaryStream();
                        ctx.ExecuteQuery();
                        using (var dataStream = streamResult.Value)
                        {
                            dataStream.CopyTo(fileStream);
                        }
                    }

                    return rutaTemp;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error SharePoint: {ex}");
                throw;
            }
        }
    }
}
