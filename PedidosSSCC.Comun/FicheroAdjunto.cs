using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Comun
{
    public class FicheroAdjunto
    {
        public FicheroAdjunto(string pstrNombre, byte[] parrContenido)
        {
            strNombre = pstrNombre;
            arrContenido = parrContenido;
        }

        string strNombre = String.Empty;
        public string Nombre
        {
            get
            {
                return strNombre;
            }
        }

        byte[] arrContenido = null;
        public byte[] Contenido
        {
            get
            {
                return arrContenido;
            }
        }

        public string Size
        {
            get
            {
                if (arrContenido != null)
                {
                    double ldblSize = arrContenido.Length / 1024;
                    return Math.Round(ldblSize) + " KB";
                }
                else return "0 KB";
            }
        }
    }
}
