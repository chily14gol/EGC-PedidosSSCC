using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Oficinas : DAL_Base<Oficinas>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Oficinas> Tabla => bd.Oficinas;

        protected override bool Guardar(Oficinas entidad, int idPersonaModificacion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entidad.OFI_Nombre) || string.IsNullOrWhiteSpace(entidad.OFI_NombreDA))
                {
                    return false;
                }

                if (entidad.OFI_Id <= 0)
                {
                    // Alta nueva — comprobar duplicados
                    bool yaExiste = bd.Oficinas.Any(o =>
                        o.OFI_Nombre == entidad.OFI_Nombre || o.OFI_NombreDA == entidad.OFI_NombreDA);

                    if (yaExiste)
                        return false;

                    bd.Oficinas.InsertOnSubmit(entidad);
                }
                else
                {
                    // Edición — buscar la oficina
                    var oficinaExistente = bd.Oficinas.FirstOrDefault(o => o.OFI_Id == entidad.OFI_Id);
                    if (oficinaExistente == null)
                        return false;

                    // Solo comprobamos duplicados si se cambian los valores
                    bool nombreModificado = oficinaExistente.OFI_Nombre != entidad.OFI_Nombre;
                    bool nombreDAModificado = oficinaExistente.OFI_NombreDA != entidad.OFI_NombreDA;

                    if (nombreModificado || nombreDAModificado)
                    {
                        bool yaExiste = bd.Oficinas.Any(o =>
                            o.OFI_Id != entidad.OFI_Id &&
                            (o.OFI_Nombre == entidad.OFI_Nombre || o.OFI_NombreDA == entidad.OFI_NombreDA));

                        if (yaExiste)
                            return false;
                    }

                    // Actualizar datos
                    oficinaExistente.OFI_Nombre = entidad.OFI_Nombre;
                    oficinaExistente.OFI_NombreDA = entidad.OFI_NombreDA;
                }

                bd.SubmitChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public int? ObtenerIdPorNombre(string nombreOficina)
        {
            var oficina = bd.Oficinas.FirstOrDefault(o => o.OFI_Nombre == nombreOficina);
            return oficina?.OFI_Id;
        }
    }
}

