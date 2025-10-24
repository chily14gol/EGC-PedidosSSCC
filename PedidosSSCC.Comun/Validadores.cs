using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Comun
{
    public class Validadores
    {
        public static bool ValidarEmail(string valor)
        {
            if (String.IsNullOrEmpty(valor))
                return true;

            string ValidationExpression = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
            Match m = Regex.Match(valor, ValidationExpression);

            return m.Success;
        }

        /// <summary> Genera la letra correspondiente a un DNI. </summary>
        /// <param name="dni"> DNI a procesar. </param>
        /// <returns> Letra correspondiente al DNI. </returns>
        private static string LetraNIF(int dni)
        {
            string Correspondencia = "TRWAGMYFPDXBNJZSQVHLCKET";
            return Correspondencia[dni % 23].ToString();
        }

        /// <summary>
        ///  Comprueba si es un NIF válido
        ///  No usar espacios ni separadores para la letra
        ///  Devuelve True si es correcto
        /// </summary>
        /// <param name="valor"></param>
        /// <returns></returns>
        public static bool ValidarNIF(string valor)
        {
            valor = valor.ToUpper(); //Ponemos la letra en mayuscula
            //Comprobamos el formato
            string pattern = @"^[0-9]{8}[A-Z]{1}$";
            Match m = Regex.Match(valor, pattern);

            if (m.Success)
            {
                string letra = valor.Substring(valor.Length - 1); //Cogemos la letra del NIF
                string letraCalculada = LetraNIF(Convert.ToInt32(valor.Substring(0, valor.Length - 1)));

                if (letra == letraCalculada)
                    return true;
                else return false;
            }
            else return false;
        }

        /// <summary>
        ///  Comprueba si es un NIE válido (misma validacion que para NIF pero con la letra X,Y o Z por delante que se sustituye por 0,1 u 2 respectivamente)
        ///  No usar espacios ni separadores para la letra
        ///  Devuelve True si es correcto
        ///  URL Explicacion: http://www.web2.0facil.com/2008/12/02/validar-nuevo-nienif-con-letra-y-inicial/
        /// </summary>
        /// <param name="valor"></param>
        /// <returns></returns>    
        public static bool ValidarNIE(string valor)
        {
            valor = valor.ToUpper(); //Ponemos la letra en mayuscula
            //Comprobamos el formato
            string pattern = @"^[X,Y,Z]{1}[0-9]{7}[A-Z]{1}$";
            Match m = Regex.Match(valor, pattern);

            //Sustituimos la letra inicial por su equivalente numerico
            char prefijo = valor[0];
            switch (prefijo)
            {
                case 'X': valor = "0" + valor.Substring(1);
                    break;
                case 'Y': valor = "1" + valor.Substring(1);
                    break;
                case 'Z': valor = "2" + valor.Substring(1);
                    break;
            }

            if (m.Success)
            {
                string letra = valor.Substring(valor.Length - 1); //Cogemos la letra del NIF
                string letraCalculada = LetraNIF(Convert.ToInt32(valor.Substring(0, valor.Length - 1)));

                if (letra == letraCalculada)
                    return true;
                else return false;
            }
            else return false;
        }


        //Web explicacion CIF:
        //http://club.telepolis.com/jagar1/Economia/Ccif.htm
        //http://www.aulambra.com/ver2.asp?id=139&tipo=
        public static bool ValidarCIF(string valor)
        {
            valor = valor.ToUpper(); //Ponemos las letras en mayuscula
            //Comprobamos el formato
            string pattern = @"^[A,B,C,D,E,F,G,H,J,K,L,M,N,P,Q,R,S,U,V,W]{1}[0-9]{7}[A,B,C,D,E,F,G,H,I,J,0-9]{1}$";
            Match m = Regex.Match(valor, pattern);

            if (m.Success)
            {
                int suma, intValorDigitoControl;

                //1. Sumar los dígitos de la posiciones pares. Suma A 
                suma = (Convert.ToInt32(valor.Substring(2, 1)) + Convert.ToInt32(valor.Substring(4, 1)) + Convert.ToInt32(valor.Substring(6, 1)));

                string temp;
                //2. Para cada uno de los dígitos de la posiciones impares, multiplicarlo por 2 y sumar los dígitos del resultado.
                //Ej.: ( 8 * 2 = 16 --> 1 + 6 = 7 ). Acumular el resultado. Suma B
                //3. Calcular la suma A + suma B = suma C 
                for (int i = 1; i <= 7; i = i + 2)
                {
                    temp = (2 * Convert.ToInt32(valor.Substring(i, 1))).ToString();
                    temp = temp.PadLeft(2, '0');

                    suma = suma + Convert.ToInt32(temp.Substring(0, 1)) + Convert.ToInt32(temp.Substring(1, 1));
                }
                //4. Tomar sólo el dígito de las unidades de C y restárselo a 10. Esta resta nos da SUMA D. 
                intValorDigitoControl = 10 - Convert.ToInt32(suma.ToString().Substring(suma.ToString().Length - 1));

                //Si coindice el digito de control...
                string digitoControl = String.Empty;
                switch (valor.Substring(0, 1))
                {
                    case "N":
                    case "P":
                    case "Q":
                    case "R":
                    case "S":
                    case "W":
                        switch (intValorDigitoControl)
                        {
                            case 1: digitoControl = "A"; break;
                            case 2: digitoControl = "B"; break;
                            case 3: digitoControl = "C"; break;
                            case 4: digitoControl = "D"; break;
                            case 5: digitoControl = "E"; break;
                            case 6: digitoControl = "F"; break;
                            case 7: digitoControl = "G"; break;
                            case 8: digitoControl = "H"; break;
                            case 9: digitoControl = "I"; break;
                            case 10: digitoControl = "J"; break;
                            case 0: digitoControl = "J"; break;
                        }
                        break;
                    default:
                        digitoControl = intValorDigitoControl.ToString(); break;
                }
                //Si sale de 2 digitos cogemos el ultimo (Ej. 10 - 0 = 10 --> digito control 0)
                if (digitoControl.Length > 1)
                    digitoControl = digitoControl.Substring(digitoControl.Length - 1);

                if (digitoControl == valor.Substring(8))
                    return true;
                else return false;
            }
            else return false;
        }
    }

    /// <summary>
    /// Descripción breve de la clase IBAN
    /// http://www.tsql.de/csharp/csharp_iban_validieren_iban_testen_iban_code
    /// </summary>
    public class ValidadorIBAN
    {
        public string IBAN;

        /*
        * CSharp IBAN validieren
        * Der Konstruktor erwartet die Übergabe der zu testenden IBAN
        */
        public ValidadorIBAN(string sIBAN)
        {
            IBAN = sIBAN;
        }

        /*
        * CSharp ISIBAN
        * Liefert True für korrekte IBAN, sonst false
        */
        public bool ISIBAN()
        {
            //Leerzeichen entfernen
            string mysIBAN = IBAN.Replace(" ", "");
            //Eine IBAN hat maximal 34 Stellen
            if (mysIBAN.Length > 34 || mysIBAN.Length < 5)
                return false; //IBAN ist zu lang oder viel zu lang
            else
            {
                string LaenderCode = mysIBAN.Substring(0, 2).ToUpper();
                string Pruefsumme = mysIBAN.Substring(2, 2).ToUpper();
                string BLZ_Konto = mysIBAN.Substring(4).ToUpper();

                if (!IsNumeric(Pruefsumme))
                    return false; //Prüfsumme ist nicht numerisch

                if (!ISLaendercode(LaenderCode))
                    return false; //Ländercode ist ungültig

                //Pruefsumme validieren
                string Umstellung = BLZ_Konto + LaenderCode + "00";
                string Modulus = IBANCleaner(Umstellung);
                if (98 - Modulo(Modulus, 97) != int.Parse(Pruefsumme))
                    return false;  //Prüfsumme ist fehlerhaft 
            }
            return true;
        }

        public void ConvertToIBAN(string bankAccount, string landCode)
        {
            string ibanBase = bankAccount + landCode.Trim() + "00";
            //Convertir las letras a números, de acuerdo a una tabla de conversion E=14, S=28 --> bankAccount + "1428" + "00";
            ibanBase = IBANCleaner(ibanBase);
            int divisor = 97;
            int resto = Modulo(ibanBase, divisor);
            //Diferencia entre 98 y el resto
            string cod = (98 - resto).ToString();
            //Hacemos un trim por si acaso en la cadena quedasen espacios
            cod = cod.Trim().Replace(" ", string.Empty);
            //si el resultado es un sólo dígito, se antepone un 0
            if (cod.Length == 1) cod = "0" + cod;
            IBAN = landCode.Trim() + cod + bankAccount;
        }

        public string getBIC()
        {
            string BICcode = string.Empty;
            if (IBAN != string.Empty)
            {
                /*
                 * SWIFT calculation (p.ej. CAIXESBBXXX)
                 * First 4 characters - bank code (only letters)
                 * Next 2 characters - ISO 3166-1 alpha-2 country code (only letters)
                 * Next 2 characters - location code (letters and digits) (passive participant will have "1" in the second character)
                 * Last 3 characters - branch code, optional ('XXX' for primary office) (letters and digits)
                 * */
            }

            return BICcode;
        }

        /*
         * CSharp Ländercode
         * Test auf korrekten Ländercode nach ISO 3166-1
         */
        private bool ISLaendercode(string code)
        {
            // Der Code muss laut ISO 3166-1 ein 2-stelliger Ländercode aus Buchstaben sein.
            if (code.Length != 2)
                return false;
            else
            {
                code = code.ToUpper();
                string[] Laendercodes = { "AA", "AB", "AC", "AD", "AE", "AF", "AG", "AH", "AI", 
        "AJ", "AK", "AL", "AM", "AN", "AO", "AP", "AQ", "AR", "AS", "AT", "AU", "AV",
        "AW", "AX", "AY", "AZ", "BA", "BB", "BC", "BD", "BE", "BF", "BG", "BH", "BI",
        "BJ", "BK", "BL", "BM", "BN", "BO", "BP", "BQ", "BR", "BS", "BT", "BU", "BV",
        "BW", "BX", "BY", "BZ", "CA", "CB", "CC", "CD", "CE", "CF", "CG", "CH", "CI",
        "CJ", "CK", "CL", "CM", "CN", "CO", "CP", "CQ", "CR", "CS", "CT", "CU", "CV",
        "CW", "CX", "CY", "CZ", "DA", "DB", "DC", "DD", "DE", "DF", "DG", "DH", "DI",
        "DJ", "DK", "DL", "DM", "DN", "DO", "DP", "DQ", "DR", "DS", "DT", "DU", "DV",
        "DW", "DX", "DY", "DZ", "EA", "EB", "EC", "ED", "EE", "EF", "EG", "EH", "EI",
        "EJ", "EK", "EL", "EM", "EN", "EO", "EP", "EQ", "ER", "ES", "ET", "EU", "EV",
        "EW", "EX", "EY", "EZ", "FA", "FB", "FC", "FD", "FE", "FF", "FG", "FH", "FI",
        "FJ", "FK", "FL", "FM", "FN", "FO", "FP", "FQ", "FR", "FS", "FT", "FU", "FV",
        "FW", "FX", "FY", "FZ", "GA", "GB", "GC", "GD", "GE", "GF", "GG", "GH", "GI",
        "GJ", "GK", "GL", "GM", "GN", "GO", "GP", "GQ", "GR", "GS", "GT", "GU", "GV",
        "GW", "GX", "GY", "GZ", "HA", "HB", "HC", "HD", "HE", "HF", "HG", "HH", "HI",
        "HJ", "HK", "HL", "HM", "HN", "HO", "HP", "HQ", "HR", "HS", "HT", "HU", "HV",
        "HW", "HX", "HY", "HZ", "IA", "IB", "IC", "ID", "IE", "IF", "IG", "IH", "II",
        "IJ", "IK", "IL", "IM", "IN", "IO", "IP", "IQ", "IR", "IS", "IT", "IU", "IV",
        "IW", "IX", "IY", "IZ", "JA", "JB", "JC", "JD", "JE", "JF", "JG", "JH", "JI",
        "JJ", "JK", "JL", "JM", "JN", "JO", "JP", "JQ", "JR", "JS", "JT", "JU", "JV",
        "JW", "JX", "JY", "JZ", "KA", "KB", "KC", "KD", "KE", "KF", "KG", "KH", "KI",
        "KJ", "KK", "KL", "KM", "KN", "KO", "KP", "KQ", "KR", "KS", "KT", "KU", "KV",
        "KW", "KX", "KY", "KZ", "LA", "LB", "LC", "LD", "LE", "LF", "LG", "LH", "LI",
        "LJ", "LK", "LL", "LM", "LN", "LO", "LP", "LQ", "LR", "LS", "LT", "LU", "LV",
        "LW", "LX", "LY", "LZ", "MA", "MB", "MC", "MD", "ME", "MF", "MG", "MH", "MI",
        "MJ", "MK", "ML", "MM", "MN", "MO", "MP", "MQ", "MR", "MS", "MT", "MU", "MV",
        "MW", "MX", "MY", "MZ", "NA", "NB", "NC", "ND", "NE", "NF", "NG", "NH", "NI",
        "NJ", "NK", "NL", "NM", "NN", "NO", "NP", "NQ", "NR", "NS", "NT", "NU", "NV",
        "NW", "NX", "NY", "NZ", "OA", "OB", "OC", "OD", "OE", "OF", "OG", "OH", "OI",
        "OJ", "OK", "OL", "OM", "ON", "OO", "OP", "OQ", "OR", "OS", "OT", "OU", "OV",
        "OW", "OX", "OY", "OZ", "PA", "PB", "PC", "PD", "PE", "PF", "PG", "PH", "PI",
        "PJ", "PK", "PL", "PM", "PN", "PO", "PP", "PQ", "PR", "PS", "PT", "PU", "PV",
        "PW", "PX", "PY", "PZ", "QA", "QB", "QC", "QD", "QE", "QF", "QG", "QH", "QI",
        "QJ", "QK", "QL", "QM", "QN", "QO", "QP", "QQ", "QR", "QS", "QT", "QU", "QV",
        "QW", "QX", "QY", "QZ", "RA", "RB", "RC", "RD", "RE", "RF", "RG", "RH", "RI",
        "RJ", "RK", "RL", "RM", "RN", "RO", "RP", "RQ", "RR", "RS", "RT", "RU", "RV",
        "RW", "RX", "RY", "RZ", "SA", "SB", "SC", "SD", "SE", "SF", "SG", "SH", "SI",
        "SJ", "SK", "SL", "SM", "SN", "SO", "SP", "SQ", "SR", "SS", "ST", "SU", "SV",
        "SW", "SX", "SY", "SZ", "TA", "TB", "TC", "TD", "TE", "TF", "TG", "TH", "TI",
        "TJ", "TK", "TL", "TM", "TN", "TO", "TP", "TQ", "TR", "TS", "TT", "TU", "TV",
        "TW", "TX", "TY", "TZ", "UA", "UB", "UC", "UD", "UE", "UF", "UG", "UH", "UI",
        "UJ", "UK", "UL", "UM", "UN", "UO", "UP", "UQ", "UR", "US", "UT", "UU", "UV",
        "UW", "UX", "UY", "UZ", "VA", "VB", "VC", "VD", "VE", "VF", "VG", "VH", "VI",
        "VJ", "VK", "VL", "VM", "VN", "VO", "VP", "VQ", "VR", "VS", "VT", "VU", "VV",
        "VW", "VX", "VY", "VZ", "WA", "WB", "WC", "WD", "WE", "WF", "WG", "WH", "WI",
        "WJ", "WK", "WL", "WM", "WN", "WO", "WP", "WQ", "WR", "WS", "WT", "WU", "WV",
        "WW", "WX", "WY", "WZ", "XA", "XB", "XC", "XD", "XE", "XF", "XG", "XH", "XI",
        "XJ", "XK", "XL", "XM", "XN", "XO", "XP", "XQ", "XR", "XS", "XT", "XU", "XV",
        "XW", "XX", "XY", "XZ", "YA", "YB", "YC", "YD", "YE", "YF", "YG", "YH", "YI",
        "YJ", "YK", "YL", "YM", "YN", "YO", "YP", "YQ", "YR", "YS", "YT", "YU", "YV",
        "YW", "YX", "YY", "YZ", "ZA", "ZB", "ZC", "ZD", "ZE", "ZF", "ZG", "ZH", "ZI",
        "ZJ", "ZK", "ZL", "ZM", "ZN", "ZO", "ZP", "ZQ", "ZR", "ZS", "ZT", "ZU", "ZV",
        "ZW", "ZX", "ZY", "ZZ" };
                if (Array.IndexOf(Laendercodes, code) == -1)
                    return false;
                else
                    return true;
            }
        }

        /*
         * CSharp IBAN Cleaner
         * Buchstaben duch Zahlen ersetzen
         */
        private string IBANCleaner(string sIBAN)
        {
            for (int x = 65; x <= 90; x++)
            {
                int replacewith = x - 64 + 9;
                string replace = ((char)x).ToString();
                sIBAN = sIBAN.Replace(replace, replacewith.ToString());
            }
            return sIBAN;
        }

        /*
         * CSharp Modulo
         * Es war mir bei diesen großen Zahlen mit C# nicht möglich anderes 
         * an eine Mod Ergebnis zu kommen.
         */
        private int Modulo(string sModulus, int iTeiler)
        {
            int iStart, iEnde, iErgebniss, iRestTmp, iBuffer;
            string iRest = "", sErg = "";

            iStart = 0;
            iEnde = 0;

            while (iEnde <= sModulus.Length - 1)
            {
                iBuffer = int.Parse(iRest + sModulus.Substring(iStart, iEnde - iStart + 1));

                if (iBuffer >= iTeiler)
                {
                    iErgebniss = iBuffer / iTeiler;
                    iRestTmp = iBuffer - iErgebniss * iTeiler;
                    iRest = iRestTmp.ToString();

                    sErg = sErg + iErgebniss.ToString();

                    iStart = iEnde + 1;
                    iEnde = iStart;
                }
                else
                {
                    if (sErg != "")
                        sErg = sErg + "0";

                    iEnde = iEnde + 1;
                }
            }

            if (iStart <= sModulus.Length)
                iRest = iRest + sModulus.Substring(iStart);

            return int.Parse(iRest);
        }

        /*
         * Csharp ISNUMERIC
         * Da C# ISNUMERIC als Methode nicht kennt müssen wir selbst ran.
         */
        private bool IsNumeric(string value)
        {
            try
            {
                int.Parse(value);
                return (true);
            }
            catch
            {
                return (false);
            }
        }
    }
}