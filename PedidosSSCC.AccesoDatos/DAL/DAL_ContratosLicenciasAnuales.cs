using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace AccesoDatos
{
    public class DAL_ContratosLicenciasAnuales : DAL_Base<ContratosLicenciasAnuales>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<ContratosLicenciasAnuales> Tabla => bd.ContratosLicenciasAnuales;

        protected override bool Guardar(ContratosLicenciasAnuales obj, int idPersonaModificacion)
        {
            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                MensajeErrorEspecifico = ex.Message;
                return false;
            }
        }

        public bool GuardarContrato(ContratosLicenciasAnuales obj, out string mensaje)
        {
            try
            {
                mensaje = null;

                using (var bd = new FacturacionInternaDataContext())
                {
                    // Valores a comparar
                    var proveedorId = obj.CLA_PRV_Id;
                    var inicioNuevo = obj.CLA_FechaInicio;
                    var finNuevo = obj.CLA_FechaFin;

                    // Validación de solapamiento: Si estamos editando, excluimos el propio obj.CLA_Id
                    bool haySolapamiento = bd.ContratosLicenciasAnuales
                        .Where(c => c.CLA_PRV_Id == proveedorId
                                 && (obj.CLA_Id == 0 || c.CLA_Id != obj.CLA_Id))
                        .Any(c =>
                            c.CLA_FechaInicio <= finNuevo &&
                            c.CLA_FechaFin >= inicioNuevo
                        );
                    if (haySolapamiento)
                    {
                        mensaje = "Ya existe un contrato para ese proveedor cuyas fechas se solapan.";
                        return false;
                    }

                    if (obj.CLA_Id == 0)
                    {
                        // Inserto el contrato y obtengo su ID
                        bd.ContratosLicenciasAnuales.InsertOnSubmit(obj);
                        bd.SubmitChanges();  // ahora obj.CLA_Id tiene el valor generado

                        //Busco el contrato anterior de este proveedor
                        var prev = bd.ContratosLicenciasAnuales
                                     .Where(c =>
                                         c.CLA_PRV_Id == obj.CLA_PRV_Id
                                         && c.CLA_FechaFin < obj.CLA_FechaInicio
                                     )
                                     .OrderByDescending(c => c.CLA_FechaFin)
                                     .FirstOrDefault();

                        if (prev != null)
                        {
                            // Y cuya LicenciaAnual pertenezca al mismo proveedor (LAN_PRV_Id)
                            var asignPrevias = (
                                from e in bd.Entes_LicenciasAnuales
                                join l in bd.LicenciasAnuales
                                    on e.ELA_LAN_Id equals l.LAN_Id
                                where e.ELA_CLA_Id == prev.CLA_Id
                                   && l.LAN_PRV_Id == obj.CLA_PRV_Id
                                   // <-- sólo aquellas cuya fecha de inicio y fin estén dentro del contrato anterior
                                   && e.ELA_FechaInicio >= prev.CLA_FechaInicio
                                   && e.ELA_FechaFin <= prev.CLA_FechaFin
                                select e
                            ).ToList();

                            // Las clono para el nuevo contrato
                            foreach (var e in asignPrevias)
                            {
                                var copia = new Entes_LicenciasAnuales
                                {
                                    ELA_ENT_Id = e.ELA_ENT_Id,
                                    ELA_LAN_Id = e.ELA_LAN_Id,
                                    ELA_FechaInicio = obj.CLA_FechaInicio,
                                    ELA_FechaFin = obj.CLA_FechaFin,
                                    ELA_CLA_Id = obj.CLA_Id,
                                    //ELA_Facturada = false //Al crear un contrato nuevo, cuando copias las licencias asignadas al contrato anterior no se debe copiar el campo "facturada". Ese debe ser falso.
                                };
                                bd.Entes_LicenciasAnuales.InsertOnSubmit(copia);
                            }

                            // Clonar Tarifas del contrato anterior
                            var tarifasPrevias = bd.ContratosLicenciasAnuales_Tarifas
                                .Where(t => t.CLT_CLA_Id == prev.CLA_Id)
                                .ToList();

                            foreach (var t in tarifasPrevias)
                            {
                                var tCopia = new ContratosLicenciasAnuales_Tarifas
                                {
                                    CLT_CLA_Id = obj.CLA_Id,       // nuevo contrato
                                    CLT_LAN_Id = t.CLT_LAN_Id,     // misma licencia anual
                                    CLT_ImporteAnual = t.CLT_ImporteAnual,
                                    //CLT_NumLicencias = t.CLT_NumLicencias
                                };
                                bd.ContratosLicenciasAnuales_Tarifas.InsertOnSubmit(tCopia);
                            }

                            //Guardo los clones
                            bd.SubmitChanges();
                        }
                    }

                    else
                    {
                        // Actualización de uno existente
                        var existente = bd.ContratosLicenciasAnuales
                                          .FirstOrDefault(a => a.CLA_Id == obj.CLA_Id);
                        if (existente == null)
                        {
                            mensaje = "No se encontró el contrato a actualizar.";
                            return false;
                        }

                        existente.CLA_PRV_Id = obj.CLA_PRV_Id;
                        existente.CLA_FechaInicio = obj.CLA_FechaInicio;
                        existente.CLA_FechaFin = obj.CLA_FechaFin;
                    }

                    bd.SubmitChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
                return false;
            }
        }

        public bool EliminarContrato(int idContrato, out string mensaje)
        {
            mensaje = null;

            try
            {
                using (var bd = new FacturacionInternaDataContext())
                {
                    // 1) Cargar el contrato
                    var contrato = bd.ContratosLicenciasAnuales
                                     .FirstOrDefault(c => c.CLA_Id == idContrato);
                    if (contrato == null)
                    {
                        mensaje = "Contrato no encontrado.";
                        return false;
                    }

                    // Verificar que ninguna de las (Entidad, Licencia) de este contrato
                    // ya exista en la tabla de Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales
                    var qryEntes = bd.Entes_LicenciasAnuales.Where(e => e.ELA_CLA_Id == idContrato);

                    bool hayFacturadas = (
                        from t in bd.Tareas_Empresas_LineasEsfuerzo_LicenciasAnuales
                        join e in qryEntes
                          on new { ent = t.TCL_ENT_Id, lan = t.TCL_LAN_Id }
                         equals new { ent = e.ELA_ENT_Id, lan = e.ELA_LAN_Id }
                        select t
                    ).Any();
                    if (hayFacturadas)
                    {
                        mensaje = "No se puede eliminar el contrato porque ya tiene operaciones asociadas en Tareas.";
                        return false;
                    }

                    // 2) Marcar para borrado todas las filas de Entes_LicenciasAnuales
                    var entes = bd.Entes_LicenciasAnuales.Where(e => e.ELA_CLA_Id == idContrato);
                    bd.Entes_LicenciasAnuales.DeleteAllOnSubmit(entes);

                    // 3) Marcar para borrado todas las filas de ContratosLicenciasAnuales_Tarifas
                    var tarifas = bd.ContratosLicenciasAnuales_Tarifas.Where(t => t.CLT_CLA_Id == idContrato);
                    bd.ContratosLicenciasAnuales_Tarifas.DeleteAllOnSubmit(tarifas);

                    // 4) Ejecutar primero el borrado de hijos
                    bd.SubmitChanges();

                    // 5) Ahora que ya no quedan hijos, marcar y borrar el propio contrato
                    bd.ContratosLicenciasAnuales.DeleteOnSubmit(contrato);
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
    }
}