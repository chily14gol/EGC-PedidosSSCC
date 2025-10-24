using System;
using System.Linq;

namespace AccesoDatos
{
    public class DAL_Telefonia : DAL_Base<Telefonia>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Telefonia> Tabla => bd.Telefonia;

        protected override bool Guardar(Telefonia objTelefonia, int idPersonaModificacion)
        {
            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    // 1) Comprobar duplicado: mismo Año, Mes, Tipo, Empresa y Teléfono, distinto PK
                    var duplicado = bd.Telefonia
                        .Any(t =>
                            t.TFN_Anyo == objTelefonia.TFN_Anyo &&
                            t.TFN_Mes == objTelefonia.TFN_Mes &&
                            t.TFN_Tipo == objTelefonia.TFN_Tipo &&
                            t.TFN_EMP_Id == objTelefonia.TFN_EMP_Id &&
                            t.TFN_Telefono == objTelefonia.TFN_Telefono &&
                            t.TFN_Extension == objTelefonia.TFN_Extension &&
                            t.TFN_TipoCuota == objTelefonia.TFN_TipoCuota &&
                            t.TFN_Id != objTelefonia.TFN_Id
                        );

                    if (duplicado)
                    {
                        MensajeErrorEspecifico = "Ya existe un registro de telefonía para ese año, mes, tipo, empresa, teléfono, extensión y tipo de couta";
                        return false;
                    }

                    // 2) Buscar registro existente
                    var item = bd.Telefonia
                        .FirstOrDefault(t => t.TFN_Id == objTelefonia.TFN_Id);

                    bool esNuevo = item == null;
                    if (esNuevo)
                    {
                        item = new Telefonia();
                        bd.Telefonia.InsertOnSubmit(item);
                    }

                    // 3) Asignar campos
                    item.TFN_Anyo = objTelefonia.TFN_Anyo;
                    item.TFN_Mes = objTelefonia.TFN_Mes;
                    item.TFN_Tipo = objTelefonia.TFN_Tipo;
                    item.TFN_EMP_Id = objTelefonia.TFN_EMP_Id;
                    item.TFN_Planta_EMP_Id = objTelefonia.TFN_Planta_EMP_Id;
                    item.TFN_Planta_Departamento = objTelefonia.TFN_Planta_Departamento;
                    item.TFN_Planta_Sede = objTelefonia.TFN_Planta_Sede;
                    item.TFN_Planta_Uso = objTelefonia.TFN_Planta_Uso;
                    item.TFN_Ciclo = objTelefonia.TFN_Ciclo;
                    item.TFN_NumFactura = objTelefonia.TFN_NumFactura;
                    item.TFN_NumCuenta = objTelefonia.TFN_NumCuenta;
                    item.TFN_Categoria = objTelefonia.TFN_Categoria;
                    item.TFN_Telefono = objTelefonia.TFN_Telefono;
                    item.TFN_Extension = objTelefonia.TFN_Extension;
                    item.TFN_TipoCuota = objTelefonia.TFN_TipoCuota;
                    item.TFN_Bytes = objTelefonia.TFN_Bytes;
                    item.TFN_Importe = objTelefonia.TFN_Importe;
                    item.TFN_FechaInicio = objTelefonia.TFN_FechaInicio;
                    item.TFN_FechaFin = objTelefonia.TFN_FechaFin;

                    // 4) Guardar cambios
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

        public bool Eliminar(Telefonia entidad)
        {
            try
            {
                // Eliminar la Tickets
                var objEliminar = bd.Telefonia.FirstOrDefault(l => l.TFN_Id == entidad.TFN_Id);

                if (objEliminar != null)
                {
                    bd.Telefonia.DeleteOnSubmit(objEliminar);
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
