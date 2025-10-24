using System;

namespace PedidosSSCC.Comun
{
    public class Constantes
    {
        public enum EstadosSolicitud
        {
            SinSolicitar = 1,
            PendienteAprobacion = 2,
            Aprobado = 3,
            Rechazado = 4
        }

        public enum TipoTarea
        {
            PorHoras = 1,
            PorUnidades = 2,
            CantidadFija = 3
        }

        public enum TareaUnidades
        {
            Horas = 1,
            JornadasMeta4 = 2,
            Licencias = 3,
            Nominas = 4
        }

        public enum Configuracion
        {
            AnioConcepto = 1
        }

        public enum Modulo
        {
            General = 0,
            Facturacion = 1,
            Mantenimiento = 2
        }

        public enum TipoClienteEnlace
        {
            Peninsular = 1,
            Extranjero = 2
        }

        public enum Columnas
        {
            A = 1,
            B = 2,
            C = 3,
            D = 4,
            E = 5,
            F = 6,
            G = 7,
            H = 8,
            I = 9,
            J = 10,
            K = 11,
            L = 12,
            M = 13,
            N = 14,
            O = 15,
            P = 16,
            Q = 17,
            R = 18,
            S = 19,
            T = 20,
            U = 21,
            V = 22,
            W = 23,
            X = 24,
            Y = 25,
            Z = 26,
            AA = 27,
            AB = 28,
            AC = 29,
            AD = 30,
            AE = 31,
            AF = 32,
            AG = 33,
            AH = 34,
            AI = 35,
            AJ = 36
        }

        public enum TipoImportAsunto
        {
            Generico = 0,
            Adur = 1,
            Prodware = 2,
            Optimize = 3,
            Attest = 4
        }

        public enum EmpresaExcluyenteConceptos
        {
            EGC = 3 //ERHARDT GESTION CORPORATIVA S.L.
        }

        public struct TipoCache {
            public const string Pedidos = "Pedidos_";
            public const string Conceptos = "Conceptos_";
            public const string Tareas = "Tareas_";
        }

        public struct SeguridadOpciones
        {
            public const string Inicio = "0";

            public const string Facturacion = "1";
            public const string Facturacion_Tareas = "1.1";
            public const string Facturacion_Tareas_DatosGenerales = "1.1.1";
            public const string Facturacion_Tareas_EdicionPresupuesto = "1.1.2";
            public const string Facturacion_LineasEsfuerzo = "1.2";
            public const string Facturacion_Pedidos = "1.3";
            public const string Facturacion_Enlace = "1.4";
            
            public const string Mantenimiento = "2";
            public const string Mantenimiento_Seguridad_Usuarios = "2.1.1";
            public const string Mantenimiento_Seguridad_Perfiles = "2.1.2"; 
            public const string Mantenimiento_Seguridad_Configuracion = "2.2.1";
            public const string Mantenimiento_Seguridad_ProductosD365 = "2.2.2";
            public const string Mantenimiento_Seguridad_ItemNumbersD365 = "2.2.3";
            public const string Mantenimiento_Seguridad_Proveedores = "2.5.1";
            public const string Mantenimiento_Proyectos = "2.6";
        }

		public const char SeparadorPK = '|';
        public const char SeparadorDictionary = '_';
		public const char SeparadorMultiseleccionRadGrid = '~';
        public const string SaltoLineaMensaje = "\r\n";
        public const decimal RatioCompensacionHoras_PorDefecto = 1.75m;
    }
}