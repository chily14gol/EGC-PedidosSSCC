using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Mail;

namespace Comun
{
    public class Utilidades
    {
        #region Emails
		public static bool EnviarEmail(string pstrFromDireccion, string pstrFromNombre, string pstrTo, string pstrAsunto, string pstrTextoMensaje)
		{
			string[] larrTo = { pstrTo };
			string[] larrCC = new string[0];
			string[] larrCCO = new string[0];
			return EnviarEmail(pstrFromDireccion, pstrFromNombre, larrTo, larrCC, larrCCO, pstrAsunto, pstrTextoMensaje, new List<FicheroAdjunto>());
		}

		public static bool EnviarEmail(string pstrFromDireccion, string pstrFromNombre, string[] parrTo, string[] parrCC, string[] parrCCO, string pstrAsunto, string pstrTextoMensaje, List<FicheroAdjunto> plstFicherosAdjuntos)
		{
			try
			{
				MailMessage msg = new MailMessage();

				foreach (string pstrTo in parrTo)
				{
					if (pstrTo != String.Empty)
						msg.To.Add(new MailAddress(pstrTo));
				}
				foreach (string pstrCC in parrCC)
				{
					if (pstrCC != String.Empty)
						msg.CC.Add(new MailAddress(pstrCC));
				}
				foreach (string pstrCCO in parrCCO)
				{
					if (pstrCCO != String.Empty)
						msg.Bcc.Add(new MailAddress(pstrCCO));
				}

				msg.From = new MailAddress(pstrFromDireccion, pstrFromNombre);

				foreach (FicheroAdjunto ficheroAdjunto in plstFicherosAdjuntos)
				{
					Attachment data = new Attachment(new MemoryStream(ficheroAdjunto.Contenido), ficheroAdjunto.Nombre);
					msg.Attachments.Add(data);
				}

				msg.Subject = pstrAsunto;
				msg.Body = pstrTextoMensaje;
				msg.IsBodyHtml = true;

				SmtpClient clienteSmtp = new SmtpClient(ConfigurationManager.AppSettings["EmailSMTPServer"]);

				//Si requiere autenticación:
				if (ConfigurationManager.AppSettings["EmailSMTPUser"] != String.Empty)
					clienteSmtp.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["EmailSMTPUser"], ConfigurationManager.AppSettings["EmailSMTPPass"]);

				clienteSmtp.Send(msg);
				return true;
			}
			catch (Exception ex)
			{
				//PageBase.RegistrarError(ex);
				throw ex;
			}
		}
		#endregion

        public static string ObtenerUsuarioSinDominio(string pstrUsuarioCompleto)
        {
            return (pstrUsuarioCompleto.Contains("\\") && pstrUsuarioCompleto.Split('\\').Length > 1) ? pstrUsuarioCompleto.Split('\\')[1] : pstrUsuarioCompleto;
        }
    }
}
