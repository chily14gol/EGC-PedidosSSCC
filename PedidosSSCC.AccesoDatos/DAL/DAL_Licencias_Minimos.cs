using System;
using System.Collections.Generic;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Licencias_Minimos : DAL_Base<Licencias_Minimos>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Licencias_Minimos> Tabla => bd.Licencias_Minimos;

        protected override bool Guardar(Licencias_Minimos entidad, int idPersonaModificacion)
        {
            bd.Licencias_Minimos.InsertOnSubmit(entidad);
            bd.SubmitChanges();
            return true;
        }

        public List<MinimoDTO> ObtenerMinimosPorLicencia(int licenciaId)
        {
            // Accedemos al DataContext: “bd” (inherente en DAL_Base)
            var query = from lm in bd.Licencias_Minimos
                        join emp in bd.Empresas on lm.LEM_EMP_Id equals emp.EMP_Id
                        where lm.LEM_LIC_Id == licenciaId
                        select new MinimoDTO
                        {
                            LEM_LIC_Id = lm.LEM_LIC_Id,
                            LEM_EMP_Id = lm.LEM_EMP_Id,
                            LEM_MinimoFacturar = lm.LEM_MinimoFacturar,
                            EmpresaNombre = emp.EMP_Nombre
                        };

            return query.ToList();
        }
        public bool GuardarOModificar(Licencias_Minimos entidad, int idPersonaModificacion)
        {
            // Intentamos buscar si ya existe
            var existente = bd.Licencias_Minimos
                .SingleOrDefault(x => x.LEM_LIC_Id == entidad.LEM_LIC_Id && x.LEM_EMP_Id == entidad.LEM_EMP_Id);

            if (existente == null)
            {
                // Alta
                entidad.LEM_LIC_Id = entidad.LEM_LIC_Id;
                entidad.LEM_EMP_Id = entidad.LEM_EMP_Id;
                entidad.LEM_MinimoFacturar = entidad.LEM_MinimoFacturar;
                bd.Licencias_Minimos.InsertOnSubmit(entidad);
            }
            else
            {
                // Edición
                existente.LEM_MinimoFacturar = entidad.LEM_MinimoFacturar;
                // (no necesitamos asignar PK porque ya es la misma)
            }
            bd.SubmitChanges();
            return true;
        }

        public bool Eliminar(int licId, int empId)
        {
            var aEliminar = bd.Licencias_Minimos
                .SingleOrDefault(x => x.LEM_LIC_Id == licId && x.LEM_EMP_Id == empId);
            if (aEliminar != null)
            {
                bd.Licencias_Minimos.DeleteOnSubmit(aEliminar);
                bd.SubmitChanges();
                return true;
            }
            return false;
        }
    }

    public class MinimoDTO
    {
        public int LEM_LIC_Id { get; set; }
        public int LEM_EMP_Id { get; set; }
        public int LEM_MinimoFacturar { get; set; }
        public string EmpresaNombre { get; set; }
    }
}