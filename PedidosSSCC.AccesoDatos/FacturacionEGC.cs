using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccesoDatos
{
    partial class Licencias_Excepciones
    {
        [NotMapped]
        public int? LIE_EMP_Id_Original { get; set; }
    }

    partial class Entes_Licencias
    {
        [NotMapped]
        public DateTime? ENL_FechaInicioOriginal { get; set; }

        [NotMapped]
        public int? ENL_LIC_IdOriginal { get; set; }     
    }

    partial class Aplicaciones_Tarifas
    {
        [NotMapped]
        public DateTime? APT_FechaInicioOriginal { get; set; }
    }

    partial class FacturacionInternaDataContext
    {
    }
}