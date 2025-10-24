using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Tickets : DAL_Base<Tickets>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Tickets> Tabla => bd.Tickets;

        protected override bool Guardar(Tickets objTicket, int idPersonaModificacion)
        {
            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    // Comprobar duplicado por título
                    var duplicado = bd.Tickets
                        .Any(t => t.TKC_Id_GLPI == objTicket.TKC_Id_GLPI && t.TKC_Id != objTicket.TKC_Id);

                    if (duplicado)
                    {
                        MensajeErrorEspecifico = "Ya existe un ticket con ese ID GLPI.";
                        return false;
                    }

                    // Buscar el ticket en el mismo contexto
                    var item = bd.Tickets.FirstOrDefault(t => t.TKC_Id == objTicket.TKC_Id);

                    bool esNuevo = item == null;

                    if (esNuevo)
                    {
                        item = new Tickets();
                        bd.Tickets.InsertOnSubmit(item);
                    }

                    // Asignar todos los campos (nuevos o modificados)
                    item.TKC_Titulo = objTicket.TKC_Titulo;
                    item.TKC_GrupoAsignado = objTicket.TKC_GrupoAsignado;
                    item.TKC_Categoria = objTicket.TKC_Categoria;
                    item.TKC_Ubicacion = objTicket.TKC_Ubicacion;
                    item.TKC_Duracion = objTicket.TKC_Duracion;
                    item.TKC_Descripcion = objTicket.TKC_Descripcion;
                    item.TKC_ETK_Id = objTicket.TKC_ETK_Id;
                    item.TKC_TTK_Id = objTicket.TKC_TTK_Id;
                    item.TKC_ENT_Id_Solicitante = objTicket.TKC_ENT_Id_Solicitante;
                    item.TKC_VTK_Id = objTicket.TKC_VTK_Id;
                    item.TKC_ProveedorAsignado = objTicket.TKC_ProveedorAsignado;
                    item.TKC_GrupoCargo = objTicket.TKC_GrupoCargo;
                    item.TKC_OTK_Id = objTicket.TKC_OTK_Id;
                    item.TKC_FechaApertura = objTicket.TKC_FechaApertura;
                    item.TKC_FechaResolucion = objTicket.TKC_FechaResolucion;
                    item.TKC_CTK_Id = objTicket.TKC_CTK_Id;
                    if (objTicket.TKC_Id_GLPI.HasValue)
                        item.TKC_Id_GLPI = objTicket.TKC_Id_GLPI;

                    // Guardar
                    bd.SubmitChanges();

                    return true;
                }
            }
            catch (Exception ex)
            {
                MensajeErrorEspecifico = ex.Message;
                return false;
            }
        }

        public bool Eliminar(Tickets entidad)
        {
            try
            {
                // Eliminar la Tickets
                var objEliminar = bd.Tickets.FirstOrDefault(l => l.TKC_Id == entidad.TKC_Id);

                if (objEliminar != null)
                {
                    bd.Tickets.DeleteOnSubmit(objEliminar);
                }

                // Confirmar cambios
                bd.SubmitChanges();
                return true;
            }
            catch (Exception ex)
            {
                // Loguea si quieres: Logger.Error(ex);
                return false;
            }
        }
    }
}



