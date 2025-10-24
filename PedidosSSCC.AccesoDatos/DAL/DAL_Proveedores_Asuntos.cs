using OfficeOpenXml;
using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Transactions;
using static PedidosSSCC.Comun.Constantes;

namespace AccesoDatos
{
    public class DAL_Proveedores_Asuntos : DAL_Base<Proveedores_Asuntos>
    {
        public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

        protected override System.Data.Linq.Table<Proveedores_Asuntos> Tabla => bd.Proveedores_Asuntos;

        protected override bool Guardar(Proveedores_Asuntos entidad, int idPersonaModificacion)
        {
            // No hay validaciones específicas excepto campos requeridos en UI
            var item = L_PrimaryKey(entidad.PAS_Id);
            if (item == null)
            {
                item = new Proveedores_Asuntos
                {
                    PAS_PRV_Id = entidad.PAS_PRV_Id,
                    PAS_Anyo = entidad.PAS_Anyo,
                    PAS_Mes = entidad.PAS_Mes,
                    PAS_CodigoExterno = entidad.PAS_CodigoExterno,
                    PAS_Fecha = entidad.PAS_Fecha,
                    PAS_TKC_Id_GLPI = entidad.PAS_TKC_Id_GLPI,
                    PAS_ENT_Id = entidad.PAS_ENT_Id,
                    PAS_EMP_Id = entidad.PAS_EMP_Id,
                    PAS_Descripcion = entidad.PAS_Descripcion,
                    PAS_Horas = entidad.PAS_Horas,
                    PAS_NumFacturaP = entidad.PAS_NumFacturaP,
                    PAS_Importe = entidad.PAS_Importe,
                    PAS_TAR_Id = entidad.PAS_TAR_Id
                };

                bd.Proveedores_Asuntos.InsertOnSubmit(item);
            }
            else
            {
                item.PAS_PRV_Id = entidad.PAS_PRV_Id;
                item.PAS_Anyo = entidad.PAS_Anyo;
                item.PAS_Mes = entidad.PAS_Mes;
                item.PAS_CodigoExterno = entidad.PAS_CodigoExterno;
                item.PAS_Fecha = entidad.PAS_Fecha;
                item.PAS_TKC_Id_GLPI = entidad.PAS_TKC_Id_GLPI;
                item.PAS_ENT_Id = entidad.PAS_ENT_Id;
                item.PAS_EMP_Id = entidad.PAS_EMP_Id;
                item.PAS_Descripcion = entidad.PAS_Descripcion;
                item.PAS_Horas = entidad.PAS_Horas;
                item.PAS_NumFacturaP = entidad.PAS_NumFacturaP;
                item.PAS_Importe = entidad.PAS_Importe;
                item.PAS_TAR_Id = entidad.PAS_TAR_Id;
            }

            bd.SubmitChanges();
            return true;
        }

        public bool Eliminar(int pasId)
        {
            try
            {
                var item = bd.Proveedores_Asuntos.FirstOrDefault(p => p.PAS_Id == pasId);
                if (item != null)
                {
                    bd.Proveedores_Asuntos.DeleteOnSubmit(item);
                    bd.SubmitChanges();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ImportarExcel(TipoImportAsunto tipoExcel, ExcelWorksheet ws,
            string impFactura, DAL_Proveedores_Asuntos dalAsuntos, List<string> listaErrores,
            ref int insertados, int impProveedor, int impAnyo, int impMes)
        {
            using (var conn = (SqlConnection)bd.Connection)
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                using (var tx = conn.BeginTransaction())
                {
                    bd.Transaction = tx;  // para LINQ-to-SQL

                    // Cargar diccionarios de referencia
                    var listaTickets = new DAL_Tickets().L(false, null).Where(t => t.TKC_Id_GLPI.HasValue).ToDictionary(t => t.TKC_Id_GLPI.Value, t => t);
                    var listaEntes = new DAL_Entes().L(false, null).ToDictionary(e => e.ENT_Id, e => e);
                    var listaEmpresas = new DAL_Empresas().L(false, null).ToDictionary(e => e.EMP_Id, e => e);

                    bool hayErroresBloqueantes = false;
                    switch (tipoExcel)
                    {
                        case TipoImportAsunto.Adur:
                            hayErroresBloqueantes = ProcesarExcel_ADUR(ws, listaTickets, listaEntes, listaEmpresas, impFactura,
                                dalAsuntos, listaErrores, ref insertados, impProveedor, impAnyo, impMes);
                            break;
                        case TipoImportAsunto.Prodware:
                            hayErroresBloqueantes = ProcesarExcel_PRODWARE(ws, listaTickets, listaEntes, listaEmpresas, impFactura,
                                dalAsuntos, listaErrores, ref insertados, impProveedor, impAnyo, impMes);
                            break;
                        case TipoImportAsunto.Optimize:
                            hayErroresBloqueantes = ProcesarExcel_OPTIMIZE(ws, listaTickets, listaEntes, listaEmpresas, impFactura,
                                dalAsuntos, listaErrores, ref insertados, impProveedor, impAnyo, impMes);
                            break;
                        case TipoImportAsunto.Attest:
                            hayErroresBloqueantes = ProcesarExce_ATTEST(ws, listaTickets, listaEntes, listaEmpresas, impFactura,
                                dalAsuntos, listaErrores, ref insertados, impProveedor, impAnyo, impMes);
                            break;
                        default:
                            hayErroresBloqueantes = ProcesarExcel_GENERICO(ws, listaTickets, listaEntes, listaEmpresas, impFactura,
                                dalAsuntos, listaErrores, ref insertados, impProveedor, impAnyo, impMes);
                            break;
                    }

                    if (!hayErroresBloqueantes)
                        tx.Commit();

                    return hayErroresBloqueantes;
                }
            }
        }

        private bool ProcesarExcel_ADUR(ExcelWorksheet ws,
            Dictionary<int, Tickets> listaTickets, Dictionary<int, Entes> listaEntes, Dictionary<int, Empresas> listaEmpresas,
            string numFacturaP, DAL_Proveedores_Asuntos dalAsuntos, List<string> listaErrores,
            ref int insertados, int impProveedor, int impAnyo, int impMes)
        {
            bool isBlocking = false;

            const int FILA_EMPIEZA = 11;
            for (int row = FILA_EMPIEZA; row <= ws.Dimension.End.Row; row++)
            {
                // — Código Externo —
                var codigoExterno = ws.Cells[row, (int)Columnas.A].Text.Trim();

                // — Fecha —
                var fecha = DateTime.TryParse(ws.Cells[row, (int)Columnas.F].Text.Trim(), out var f) ? f : (DateTime?)null;

                if (!String.IsNullOrEmpty(codigoExterno) && fecha != null)
                {
                    // — Extraer GLPI ID de la descripción —
                    var descRaw = ws.Cells[row, (int)Columnas.R].Text.Trim();
                    var descripcion = descRaw.Length > 100 ? descRaw.Substring(0, 100) : descRaw;
                    var match = Regex.Match(descripcion, @"#(\d+)");

                    #region Ticket Id GLPI
                    int glpiId = match.Success && int.TryParse(match.Groups[1].Value, out var g) ? g : 0;
                    int? entidadId = null, empresaId = null;

                    //Solo debe mostrar el mensaje “Ticket GLPI #XXXXX no existe en el sistema.” cuando en la descripción haya un GLPI.
                    //Si no lo hay no debe buscarlo (ni sacar el error)
                    bool ticketEncontrado = true;
                    if (glpiId != 0)
                    {
                        if (listaTickets.TryGetValue(glpiId, out var ticket))
                            entidadId = ticket.TKC_ENT_Id_Solicitante;
                        else
                        {
                            ticketEncontrado = false;
                            listaErrores.Add($"Fila {row}: Ticket GLPI #{glpiId} no existe en el sistema.");
                        }
                    }
                    #endregion

                    #region Determinar empresa
                    string empresaNombreDA = ws.Cells[row, (int)Columnas.AB].Text.Trim();
                    if (string.IsNullOrEmpty(empresaNombreDA) && entidadId.HasValue)
                    {
                        if (listaEntes.TryGetValue(entidadId.Value, out var ente) && ente.ENT_EMP_Id.HasValue)
                            empresaId = ente.ENT_EMP_Id.Value;
                    }
                    else if (!string.IsNullOrEmpty(empresaNombreDA))
                    {
                        var empresaMatch = listaEmpresas.Values.FirstOrDefault(e =>
                            string.Equals(e.EMP_NombreDA, empresaNombreDA, StringComparison.OrdinalIgnoreCase));
                        if (empresaMatch != null)
                            empresaId = empresaMatch.EMP_Id;
                        else
                        {
                            // **Empresa no existe** → error bloqueante
                            listaErrores.Add($"Fila {row}: Empresa '{empresaNombreDA}' no existe en el sistema.");
                            isBlocking = true;
                        }
                    }
                    //Solo debe dar el error “No se pudo determinar la empresa.” cuando se especifique una empresa y no seamos capaces de localizarla.
                    //else
                    //{
                    //    // Sin empresa y sin ente → lo tratamos como bloqueante
                    //    listaErrores.Add($"Fila {row}: No se pudo determinar la empresa.");
                    //    isBlocking = true;
                    //}
                    #endregion

                    // — Horas —
                    var horas = decimal.TryParse(ws.Cells[row, (int)Columnas.J].Text.Trim(), out var hval) ? hval : 0m;

                    if (!isBlocking)
                    {
                        var obj = new Proveedores_Asuntos
                        {
                            PAS_PRV_Id = impProveedor,
                            PAS_Anyo = impAnyo,
                            PAS_Mes = impMes,
                            PAS_NumFacturaP = numFacturaP,
                            PAS_CodigoExterno = codigoExterno,
                            PAS_ENT_Id = entidadId,
                            PAS_EMP_Id = empresaId,
                            PAS_Descripcion = descripcion,
                            PAS_Horas = horas
                        };

                        if (ticketEncontrado)
                            obj.PAS_TKC_Id_GLPI = glpiId != 0 ? (int?)glpiId : null;

                        if (fecha.HasValue)
                            obj.PAS_Fecha = fecha.Value;

                        if (dalAsuntos.G(obj, Sesion.SPersonaId))
                            insertados++;
                    }
                }
            }

            return isBlocking;
        }

        private bool ProcesarExcel_PRODWARE(ExcelWorksheet ws,
            Dictionary<int, Tickets> listaTickets, Dictionary<int, Entes> listaEntes, Dictionary<int, Empresas> listaEmpresas,
            string numFacturaP, DAL_Proveedores_Asuntos dalAsuntos, List<string> listaErrores,
            ref int insertados, int impProveedor, int impAnyo, int impMes)
        {
            bool isBlocking = false;

            // Validar contrato vigente
            var dalContrato = new DAL_Proveedores_ContratosSoporte();
            DateTime hoy = DateTime.Today;
            var contrato = dalContrato.L(false, c =>
                    c.PVC_PRV_Id == impProveedor &&
                    c.PVC_FechaInicio <= hoy &&
                    (c.PVC_FechaFin == null || c.PVC_FechaFin >= hoy))
                .OrderByDescending(c => c.PVC_FechaInicio)
                .FirstOrDefault();

            if (contrato == null)
            {
                var dalProv = new DAL_Proveedores();
                var objProv = dalProv.L_PrimaryKey(impProveedor);
                if (objProv != null)
                    listaErrores.Add($"No hay contrato vigente para el proveedor {objProv.PRV_Nombre}.");

                isBlocking = true;
                return isBlocking;
            }

            var dalReparto = new DAL_Proveedores_ContratosSoporte_Reparto();
            var repartos = dalReparto.L(false, r => r.PVR_PVC_Id == contrato.PVC_Id).ToList();

            const int FILA_EMPIEZA = 2;
            for (int row = FILA_EMPIEZA; row <= ws.Dimension.End.Row; row++)
            {
                // — Descripción —
                const int MAX_LEN = 100;
                var descripcion = ws.Cells[row, (int)Columnas.G].Text.Trim();
                if (descripcion.Length > MAX_LEN)
                    descripcion = descripcion.Substring(0, MAX_LEN);

                // — Código Externo —
                var codigoExterno = ws.Cells[row, (int)Columnas.F].Text.Trim();

                #region Fecha
                string textoFecha = ws.Cells[row, (int)Columnas.A].Text.Trim();
                DateTime? fecha = null;
                if (!string.IsNullOrEmpty(textoFecha))
                {
                    var formatos = new[] { "MMM-yy", "MMM–yy" };
                    textoFecha = textoFecha.Replace('\u2013', '-').Replace('\u2014', '-')
                        .Trim();

                    var ci = new CultureInfo("es-ES");
                    var dtfi = (DateTimeFormatInfo)ci.DateTimeFormat.Clone();

                    if (!DateTime.TryParseExact(textoFecha, formatos, dtfi, DateTimeStyles.None, out var f))
                    {
                        listaErrores.Add($"Fila {row}: Fecha '{textoFecha}' con formato inválido.");
                    }
                    else
                        fecha = f;
                }
                #endregion

                #region Empresas a procesar
                string empresaNombreDA = ws.Cells[row, (int)Columnas.B].Text.Trim();
                var empresasAProcesar = new List<int>();

                if (string.Equals(empresaNombreDA, "GENERAL", StringComparison.OrdinalIgnoreCase))
                {
                    if (!repartos.Any())
                    {
                        listaErrores.Add($"Fila {row}: Contrato {contrato.PVC_Id} sin repartos definidos.");
                        isBlocking = true;
                    }
                    else
                    {
                        empresasAProcesar.AddRange(repartos.Select(r => r.PVR_EMP_Id));
                    }
                }
                else if (!string.IsNullOrEmpty(empresaNombreDA))
                {
                    var empresaMatch = listaEmpresas.Values.FirstOrDefault(e =>
                        string.Equals(e.EMP_NombreDA, empresaNombreDA, StringComparison.OrdinalIgnoreCase));
                    if (empresaMatch == null)
                    {
                        listaErrores.Add($"Fila {row}: Empresa '{empresaNombreDA}' no existe.");
                        isBlocking = true;
                    }
                    else
                        empresasAProcesar.Add(empresaMatch.EMP_Id);
                }
                else
                {
                    // salto de fila en blanco
                    continue;
                }
                #endregion

                // — Importe base —
                var importeBase = decimal.TryParse(ws.Cells[row, (int)Columnas.H].Text.Trim(), out var tmp) ? tmp : 0m;

                // Tarea
                var dicTareas = new DAL_Tareas().L(false, null).ToDictionary(e => e.TAR_Id);
                var tarea = ws.Cells[row, (int)Columnas.C].Text.Trim();
                int idTarea = dicTareas.FirstOrDefault(x => x.Value.TAR_Nombre.Equals(tarea, StringComparison.OrdinalIgnoreCase)).Key;

                if (idTarea == 0)
                {
                    listaErrores.Add($"Fila {row}: Tarea {tarea} no existe.");
                    isBlocking = true;
                }

                if (!isBlocking)
                {
                    foreach (var empId in empresasAProcesar)
                    {
                        var repartoItem = dalReparto.L_PrimaryKey($"{contrato.PVC_Id}|{empId}");
                        var porcentaje = repartoItem != null ? repartoItem.PVR_Porcentaje : 100m;
                        var importe = importeBase * porcentaje / 100;

                        var obj = new Proveedores_Asuntos
                        {
                            PAS_PRV_Id = impProveedor,
                            PAS_Anyo = impAnyo,
                            PAS_Mes = impMes,
                            PAS_NumFacturaP = numFacturaP,
                            PAS_CodigoExterno = codigoExterno,
                            PAS_EMP_Id = empId,
                            PAS_Descripcion = descripcion,
                            PAS_Importe = importe,
                            PAS_TAR_Id = idTarea
                        };

                        if (fecha.HasValue)
                            obj.PAS_Fecha = fecha.Value;

                        if (dalAsuntos.G(obj, Sesion.SPersonaId))
                            insertados++;
                    }
                }
            }

            return isBlocking;
        }

        private bool ProcesarExcel_OPTIMIZE(ExcelWorksheet ws,
            Dictionary<int, Tickets> listaTickets, Dictionary<int, Entes> listaEntes, Dictionary<int, Empresas> listaEmpresas,
            string numFacturaP, DAL_Proveedores_Asuntos dalAsuntos, List<string> listaErrores,
            ref int insertados, int impProveedor, int impAnyo, int impMes)
        {
            bool isBlocking = false;

            const int FILA_EMPIEZA = 7;
            for (int row = FILA_EMPIEZA; row <= ws.Dimension.End.Row; row++)
            {
                // -- Si está en blanco completo, saltar --
                var cFecha = ws.Cells[row, (int)Columnas.A].Text.Trim();
                var cCod = ws.Cells[row, (int)Columnas.C].Text.Trim();
                var cDesc = ws.Cells[row, (int)Columnas.E].Text.Trim();
                var empresaNombreDA = ws.Cells[row, (int)Columnas.J].Text.Trim();

                if (string.IsNullOrEmpty(cFecha) && string.IsNullOrEmpty(cCod)
                    && string.IsNullOrEmpty(cDesc) && string.IsNullOrEmpty(empresaNombreDA))
                    continue;

                // — Validar y parsear —
                if (!DateTime.TryParse(cFecha, out var fecha))
                    listaErrores.Add($"Fila {row}: Fecha '{cFecha}' inválida."); // no bloqueante

                var codigoExterno = cCod;
                var descripcion = cDesc.Length > 100 ? cDesc.Substring(0, 100) : cDesc;

                #region Detearminar empresa
                int? empresaId = null;
                if (!string.IsNullOrEmpty(empresaNombreDA))
                {
                    var empresaMatch = listaEmpresas.Values.FirstOrDefault(e =>
                        string.Equals(e.EMP_NombreDA, empresaNombreDA, StringComparison.OrdinalIgnoreCase));
                    if (empresaMatch == null)
                    {
                        listaErrores.Add($"Fila {row}: Empresa '{empresaNombreDA}' no existe.");
                        isBlocking = true;
                    }
                    else
                        empresaId = empresaMatch.EMP_Id;
                }
                else
                {
                    listaErrores.Add($"Fila {row}: Falta empresa.");
                    isBlocking = true;
                }
                #endregion

                // — Horas —
                var horas = decimal.TryParse(ws.Cells[row, (int)Columnas.I].Text.Trim(), out var h) ? h : 0m;

                if (!isBlocking)
                {
                    var obj = new Proveedores_Asuntos
                    {
                        PAS_PRV_Id = impProveedor,
                        PAS_Anyo = impAnyo,
                        PAS_Mes = impMes,
                        PAS_NumFacturaP = numFacturaP,
                        PAS_CodigoExterno = codigoExterno,
                        PAS_Fecha = fecha,
                        PAS_EMP_Id = empresaId,
                        PAS_Descripcion = descripcion,
                        PAS_Horas = horas
                    };

                    if (dalAsuntos.G(obj, Sesion.SPersonaId))
                        insertados++;
                }
            }

            return isBlocking;
        }

        private bool ProcesarExce_ATTEST(ExcelWorksheet ws,
            Dictionary<int, Tickets> listaTickets, Dictionary<int, Entes> listaEntes, Dictionary<int, Empresas> listaEmpresas,
            string numFacturaP, DAL_Proveedores_Asuntos dalAsuntos, List<string> listaErrores,
            ref int insertados, int impProveedor, int impAnyo, int impMes)
        {
            bool isBlocking = false;

            const int FILA_EMPIEZA = 2;
            for (int row = FILA_EMPIEZA; row <= ws.Dimension.End.Row; row++)
            {
                // — Descripción —
                var descRaw = ws.Cells[row, (int)Columnas.L].Text.Trim();
                var descripcion = descRaw.Length > 100 ? descRaw.Substring(0, 100) : descRaw;

                // — Código Externo —
                var codigoExterno = ws.Cells[row, (int)Columnas.K].Text.Trim();

                #region Ticket Id GLPI
                int glpiId = int.TryParse(ws.Cells[row, (int)Columnas.J].Text.Trim(), out var g) ? g : 0;
                int? entidadId = null;

                //Solo debe mostrar el mensaje “Ticket GLPI #XXXXX no existe en el sistema.” cuando en la descripción haya un GLPI.
                //Si no lo hay no debe buscarlo (ni sacar el error)
                bool ticketEncontrado = true;
                if (glpiId != 0)
                {
                    if (listaTickets.TryGetValue(glpiId, out var ticket))
                        entidadId = ticket.TKC_ENT_Id_Solicitante;
                    else
                    {
                        ticketEncontrado = false;
                        listaErrores.Add($"Fila {row}: Ticket GLPI #{glpiId} no existe en el sistema.");
                    }
                }
                #endregion

                #region Empresa
                string empresaNombreDA = ws.Cells[row, (int)Columnas.A].Text.Trim();
                int? empresaId = null;
                if (!string.IsNullOrEmpty(empresaNombreDA))
                {
                    var empresaMatch = listaEmpresas.Values.FirstOrDefault(e =>
                        string.Equals(e.EMP_NombreDA, empresaNombreDA, StringComparison.OrdinalIgnoreCase));
                    if (empresaMatch == null)
                    {
                        listaErrores.Add($"Fila {row}: Empresa '{empresaNombreDA}' no existe.");
                        isBlocking = true;
                    }
                    else
                    {
                        empresaId = empresaMatch.EMP_Id;
                    }
                }
                else if (entidadId.HasValue && listaEntes.TryGetValue(entidadId.Value, out var ente)
                         && ente.ENT_EMP_Id.HasValue)
                {
                    empresaId = ente.ENT_EMP_Id.Value;
                }
                else
                {
                    listaErrores.Add($"Fila {row}: No se pudo determinar la empresa.");
                    isBlocking = true;
                }
                #endregion

                // — Horas —
                var horas = decimal.TryParse(ws.Cells[row, (int)Columnas.F].Text.Trim(), out var h) ? h : 0m;

                // — Insertar o acumular errores —
                if (!isBlocking)
                {
                    var obj = new Proveedores_Asuntos
                    {
                        PAS_PRV_Id = impProveedor,
                        PAS_Anyo = impAnyo,
                        PAS_Mes = impMes,
                        PAS_NumFacturaP = numFacturaP,
                        PAS_CodigoExterno = codigoExterno,
                        PAS_EMP_Id = empresaId,
                        PAS_Descripcion = descripcion,
                        PAS_Horas = horas
                    };

                    if (ticketEncontrado)
                        obj.PAS_TKC_Id_GLPI = glpiId != 0 ? (int?)glpiId : null;

                    if (dalAsuntos.G(obj, Sesion.SPersonaId))
                        insertados++;
                }
            }

            return isBlocking;
        }

        private bool ProcesarExcel_GENERICO(ExcelWorksheet ws,
            Dictionary<int, Tickets> listaTickets, Dictionary<int, Entes> listaEntes, Dictionary<int, Empresas> listaEmpresas,
            string numFacturaP, DAL_Proveedores_Asuntos dalAsuntos, List<string> errores,
            ref int insertados, int impProveedor, int impAnyo, int impMes)
        {
            for (int row = 11; row <= ws.Dimension.End.Row; row++)
            {
                // Extraer GLPI de la descripción
                var descripcion = ws.Cells[row, (int)Columnas.R].Text.Trim();
                var match = Regex.Match(descripcion, @"#(\d+)");
                int glpiId = match.Success && int.TryParse(match.Groups[1].Value, out var g) ? g : 0;

                // Si había GLPI en Excel pero no existe en el diccionario => error
                if (glpiId != 0 && !listaTickets.ContainsKey(glpiId))
                {
                    errores.Add($"Fila {row}: Ticket GLPI #{glpiId} no existe en el sistema.");
                    // no hacemos continue aquí para seguir validando resto de filas
                    //continue;
                }

                // Resto de campos
                var codigoExterno = ws.Cells[row, (int)Columnas.A].Text.Trim();
                var fecha = DateTime.TryParse(ws.Cells[row, (int)Columnas.F].Text.Trim(), out var f) ? f : (DateTime?)null;

                // Determinar entidad y empresa
                string empresaNombreDA = ws.Cells[row, (int)Columnas.AB].Text.Trim();

                int? entidadId = null;
                int? empresaId = null;

                if (glpiId != 0)
                {
                    var ticket = listaTickets[glpiId];
                    entidadId = ticket.TKC_ENT_Id_Solicitante;

                    // si la empresa no viene en la hoja, probar la del ente

                    if (string.IsNullOrEmpty(empresaNombreDA) && entidadId.HasValue)
                    {
                        if (listaEntes.TryGetValue(entidadId.Value, out var enteObj) && enteObj.ENT_EMP_Id.HasValue)
                            empresaId = enteObj.ENT_EMP_Id.Value;
                    }
                    else if (int.TryParse(empresaNombreDA, out var eVal) && eVal != 0)
                    {
                        empresaId = eVal;
                    }
                }

                if (!empresaId.HasValue)
                {
                    errores.Add($"Fila {row}: Empresa '{empresaNombreDA}' no se a encontrado.");
                }
                else
                {
                    var horas = decimal.TryParse(ws.Cells[row, 10].Text.Trim(), out var hval) ? hval : 0m;

                    var objAsunto = new Proveedores_Asuntos
                    {
                        PAS_PRV_Id = impProveedor,
                        PAS_Anyo = impAnyo,
                        PAS_Mes = impMes,
                        PAS_NumFacturaP = numFacturaP,
                        PAS_CodigoExterno = codigoExterno,
                        PAS_Fecha = fecha ?? DateTime.MinValue,
                        PAS_TKC_Id_GLPI = (glpiId != 0 ? (int?)glpiId : null),
                        PAS_ENT_Id = entidadId,
                        PAS_EMP_Id = empresaId,
                        PAS_Descripcion = String.Empty,
                        PAS_Horas = horas
                    };

                    // Si no hubo error de GLPI en esta fila, intentar grabar
                    if (dalAsuntos.G(objAsunto, Sesion.SPersonaId))
                        insertados++;
                }
            }

            return false;
        }
    }
}