using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Data.Entity;

namespace AccesoDatos
{
    public class DAL_Entes : DAL_Base<Entes>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Entes> Tabla => bd.Entes;

        public List<object> ObtenerEntes()
        {
            return bd.Entes.Select(e => new
            {
                e.ENT_Id,
                e.ENT_Nombre,
                e.ENT_Email,
                e.ENT_TEN_Id,
                e.ENT_OFI_Id,
                e.ENT_EMP_Id,
                e.ENT_EDE_Id,
                NombreTipoEnte = e.TiposEnte != null ? e.TiposEnte.TEN_Nombre : "",
                NombreOficina = e.Oficinas != null ? e.Oficinas.OFI_Nombre : "",
                NombreEmpresa = e.Empresas != null ? e.Empresas.EMP_Nombre : ""
            }).OrderBy(i => i.ENT_Nombre).ToList<object>();
        }

        public bool ExisteEmail(string email)
        {
            return bd.Entes.Any(e => e.ENT_Email == email);
        }

        protected override bool Guardar(Entes entidad, int idPersonaModificacion)
        {
            if (string.IsNullOrWhiteSpace(entidad.ENT_Nombre) || string.IsNullOrWhiteSpace(entidad.ENT_Email))
                return false;

            string nombreNormalizado = entidad.ENT_Nombre.Trim().ToUpper();
            string emailNormalizado = entidad.ENT_Email.Trim().ToUpper();

            var item = L_PrimaryKey(entidad.ENT_Id);

            if (item == null)
            {
                // Nuevo registro: no permitir duplicados en nombre o email
                bool yaExiste = bd.Entes.Any(e =>
                    e.ENT_Nombre.Trim().ToUpper() == nombreNormalizado ||
                    e.ENT_Email.Trim().ToUpper() == emailNormalizado);

                if (yaExiste)
                    return false;

                item = new Entes();
                bd.Entes.InsertOnSubmit(item);
            }
            else
            {
                bool cambiaNombre = item.ENT_Nombre.Trim().ToUpper() != nombreNormalizado;
                bool cambiaEmail = item.ENT_Email.Trim().ToUpper() != emailNormalizado;

                if (cambiaNombre)
                {
                    bool nombreDuplicado = bd.Entes.Any(e =>
                        e.ENT_Id != entidad.ENT_Id &&
                        e.ENT_Nombre.Trim().ToUpper() == nombreNormalizado);

                    if (nombreDuplicado)
                        return false;
                }

                if (cambiaEmail)
                {
                    bool emailDuplicado = bd.Entes.Any(e =>
                        e.ENT_Id != entidad.ENT_Id &&
                        e.ENT_Email.Trim().ToUpper() == emailNormalizado);

                    if (emailDuplicado)
                        return false;
                }
            }

            // Asignación final
            item.ENT_Nombre = entidad.ENT_Nombre.Trim();
            item.ENT_Email = entidad.ENT_Email.Trim();
            item.ENT_TEN_Id = entidad.ENT_TEN_Id;
            item.ENT_OFI_Id = entidad.ENT_OFI_Id;
            item.ENT_EMP_Id = entidad.ENT_EMP_Id;
            item.ENT_EDE_Id = entidad.ENT_EDE_Id;

            bd.SubmitChanges();
            return true;
        }

        public void GuardarEntesBulk(List<Entes> listaEntes)
        {
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["AccesoDatos.Properties.Settings.FacturacionInternaConnectionString"].ConnectionString))
            {
                connection.Open();

                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "Entes";

                    var table = new DataTable();
                    table.Columns.Add("ENT_Nombre", typeof(string));
                    table.Columns.Add("ENT_Email", typeof(string));
                    table.Columns.Add("ENT_EMP_Id", typeof(int)).AllowDBNull = true;
                    table.Columns.Add("ENT_OFI_Id", typeof(int)).AllowDBNull = true;

                    foreach (var ente in listaEntes)
                    {
                        System.Diagnostics.Debug.WriteLine($"{ente.ENT_Nombre} | {ente.ENT_Email} | {ente.ENT_EMP_Id?.ToString() ?? "null"} | {ente.ENT_OFI_Id?.ToString() ?? "null"}");

                        table.Rows.Add(
                            ente.ENT_Nombre,
                            ente.ENT_Email,
                            ente.ENT_EMP_Id.HasValue ? (object)ente.ENT_EMP_Id.Value : DBNull.Value,
                            ente.ENT_OFI_Id.HasValue ? (object)ente.ENT_OFI_Id.Value : DBNull.Value
                        );
                    }

                    bulkCopy.DestinationTableName = "Entes";
                    bulkCopy.ColumnMappings.Add("ENT_Nombre", "ENT_Nombre");
                    bulkCopy.ColumnMappings.Add("ENT_Email", "ENT_Email");
                    bulkCopy.ColumnMappings.Add("ENT_EMP_Id", "ENT_EMP_Id");
                    bulkCopy.ColumnMappings.Add("ENT_OFI_Id", "ENT_OFI_Id");

                    bulkCopy.WriteToServer(table);
                }
            }
        }

        public Entes ObtenerPorEmail(string email)
        {
            var emailLower = email.ToLower();
            return bd.Entes
                .FirstOrDefault(e => e.ENT_Email.ToLower() == emailLower);
        }

        public Entes ObtenerPorNombre(string nombreExacto)
        {
            var nombreExactoLower = nombreExacto.ToLower();
            return bd.Entes
                .FirstOrDefault(e => e.ENT_Nombre.ToLower() == nombreExactoLower);
        }

        public void ActualizarEnte(Entes ente)
        {
            // Buscar el ente en la base de datos usando su ID
            var enteExistente = bd.Entes.FirstOrDefault(e => e.ENT_Id == ente.ENT_Id);

            // Si el ente existe, actualizamos sus propiedades
            if (enteExistente != null)
            {
                enteExistente.ENT_Nombre = ente.ENT_Nombre;
                enteExistente.ENT_Email = ente.ENT_Email;
                enteExistente.ENT_EMP_Id = ente.ENT_EMP_Id;
                enteExistente.ENT_OFI_Id = ente.ENT_OFI_Id;

                // Guardamos los cambios en la base de datos
                bd.SubmitChanges();
            }
        }
    }
}

