using System;
using System.Collections.Generic;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Proyectos_Partes_Horas : DAL_Base<Proyectos_Partes_Horas>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);
        protected override System.Data.Linq.Table<Proyectos_Partes_Horas> Tabla => bd.Proyectos_Partes_Horas;

        protected override bool Guardar(Proyectos_Partes_Horas entidad, int idPersonaModificacion)
        {
            bd.SubmitChanges();
            return true;
        }

        /// <summary>
        /// Obtiene todas las horas de una línea de parte (PPA_Id).
        /// </summary>
        public List<Proyectos_Partes_Horas> ObtenerPorParte(int ppaId)
        {
            return Tabla
                .Where(h => h.PPH_PPA_Id == ppaId)
                .ToList();
        }

        /// <summary>
        /// Inserta o actualiza la hora de un día para una línea de parte.
        /// Si existe un registro con misma (PPA_Id + Fecha), lo actualiza; si no, lo inserta.
        /// </summary>
        public bool GuardarHora(int ppaId, DateTime fecha, decimal horas, int idPersonaModificacion)
        {
            try
            {
                var original = Tabla.SingleOrDefault(h => h.PPH_PPA_Id == ppaId && h.PPH_Fecha == fecha);
                if (original == null)
                {
                    var nueva = new Proyectos_Partes_Horas
                    {
                        PPH_PPA_Id = ppaId,
                        PPH_Fecha = fecha,
                        PPH_Horas = horas
                    };
                    Tabla.InsertOnSubmit(nueva);
                }
                else
                {
                    original.PPH_Horas = horas;
                }
                bd.SubmitChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Elimina todas las horas asociadas a una línea de parte (antes de eliminar la línea).
        /// </summary>
        public void EliminarPorParte(int ppaId)
        {
            var hijos = Tabla.Where(h => h.PPH_PPA_Id == ppaId);
            if (hijos.Any())
            {
                Tabla.DeleteAllOnSubmit(hijos);
                bd.SubmitChanges();
            }
        }
    }
}
