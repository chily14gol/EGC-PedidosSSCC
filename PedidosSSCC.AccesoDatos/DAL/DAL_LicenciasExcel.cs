using AccesoDatos;
using System.Collections.Generic;
using System.Linq;
using System;
using OfficeOpenXml;
using static PedidosSSCC.Comun.Constantes;
using System.Transactions;

public class ResultadoProcesamiento
{
    public string Email { get; set; }
    public string Nombre { get; set; }
    public string Licencia { get; set; }
    public string Resultado { get; set; }
}

public class DAL_LicenciasExcel : DAL_Base<Licencias>
{
    public override object ParsearValorPK(string valorPK) => int.Parse(valorPK);

    protected override System.Data.Linq.Table<Licencias> Tabla => bd.Licencias;

    protected override bool Guardar(Licencias entidad, int idPersonaModificacion)
    {
        return true;
    }

    public List<ResultadoProcesamiento> ProcesarLicencias(ExcelWorksheet ws)
    {
        var resultados = new List<ResultadoProcesamiento>();

        void UpsertResult(string email, string nombre, string licencia, string resultadoText)
        {
            var existente = resultados
                .FirstOrDefault(r =>
                    r.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                    r.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase) &&
                    r.Licencia.Equals(licencia, StringComparison.OrdinalIgnoreCase));

            if (existente != null)
            {
                existente.Resultado = string.IsNullOrEmpty(existente.Resultado)
                    ? resultadoText
                    : $"{existente.Resultado}; {resultadoText}";
            }
            else
            {
                resultados.Add(new ResultadoProcesamiento
                {
                    Email = email,
                    Nombre = nombre,
                    Licencia = licencia,
                    Resultado = resultadoText
                });
            }
        }

        var blockingResults = new HashSet<string> {
        "Entidad no encontrada",
        "Licencia no encontrada",
        "Licencias incompatibles",
        "Licencias incompatibles (hermanas)"
    };

        // --- 1) Precaches ---
        var incompatibles = bd.Licencias_Incompatibles
            .Select(x => new { x.LIL_LIC_Id1, x.LIL_LIC_Id2 })
            .ToList();

        var licPorNombre = bd.Licencias
            .ToList()
            .Select(l => new { l.LIC_Id, Key = (l.LIC_NombreMS ?? "").Trim() })
            .ToDictionary(x => x.Key, x => x.LIC_Id, StringComparer.OrdinalIgnoreCase);

        var licTipo = bd.Licencias_TiposEnte
            .ToLookup(x => x.LTE_TEN_Id, x => x.LTE_LIC_Id);

        var licPadre = bd.Licencias
            .ToList()
            .Where(l => l.LIC_LIC_Id_Padre != null)
            .Select(l => new { Child = l.LIC_Id, Parent = l.LIC_LIC_Id_Padre.Value })
            .ToDictionary(x => x.Child, x => x.Parent);

        Func<int, int> GetRoot = id => licPadre.TryGetValue(id, out var p) ? p : id;

        var entPorId = bd.Entes.ToDictionary(e => e.ENT_Id);
        var licNombrePorId = bd.Licencias.ToDictionary(l => l.LIC_Id, l => (l.LIC_NombreMS ?? l.LIC_Nombre).Trim());
        var licMSIds = new HashSet<int>(bd.Licencias
            .Where(l => l.LIC_NombreMS != null && l.LIC_NombreMS.Trim() != "")
            .Select(l => l.LIC_Id));

        // --- 2) Primera pasada: VALIDACIONES ---
        var filasValidas = new List<(int ENT_Id, string email, string ENT_Nombre, List<int> LicIds)>();
        int totalRows = ws.Dimension.End.Row;

        for (int row = 2; row <= totalRows; row++)
        {
            var rowErrors = new List<ResultadoProcesamiento>();

            string email = ws.Cells[row, (int)Columnas.C].Text.Trim();
            string licTexto = ws.Cells[row, (int)Columnas.S].Text.Trim();
            if (string.IsNullOrEmpty(email))
                continue;

            var ente = bd.Entes.FirstOrDefault(e => e.ENT_Email == email);
            if (ente == null)
            {
                rowErrors.Add(new ResultadoProcesamiento
                {
                    Email = email,
                    Nombre = "",
                    Licencia = "",
                    Resultado = "Entidad no encontrada"
                });
            }
            else
            {
                string nomEnt = ente.ENT_Nombre;

                var nombresLic = licTexto
                    .Split(new[] { '+', ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0);

                var licIds = new List<int>();
                foreach (var nom in nombresLic)
                {
                    if (!licPorNombre.TryGetValue(nom, out var idLic))
                    {
                        rowErrors.Add(new ResultadoProcesamiento
                        {
                            Email = email,
                            Nombre = nomEnt,
                            Licencia = nom,
                            Resultado = "Licencia no encontrada"
                        });
                    }
                    else
                    {
                        licIds.Add(idLic);
                    }
                }

                if (licIds.Any())
                {
                    // 2.1 Hermanas
                    var gruposPorPadre = licIds
                        .GroupBy(id => GetRoot(id))
                        .Where(g => g.Count() > 1);

                    if (gruposPorPadre.Any())
                    {
                        var primera = gruposPorPadre.First().ToList();
                        var nombresHermanas = primera
                            .Select(id => bd.Licencias.First(l => l.LIC_Id == id).LIC_NombreMS.Trim());

                        rowErrors.Add(new ResultadoProcesamiento
                        {
                            Email = email,
                            Nombre = nomEnt,
                            Licencia = string.Join(" + ", nombresHermanas),
                            Resultado = "Licencias incompatibles (hermanas)"
                        });
                    }

                    // 2.2 Incompatibles
                    var effective = licIds.Select(GetRoot).Distinct().ToList();
                    foreach (var par in incompatibles)
                    {
                        if (effective.Contains(par.LIL_LIC_Id1) &&
                            effective.Contains(par.LIL_LIC_Id2))
                        {
                            var nom1 = bd.Licencias.First(l => l.LIC_Id == par.LIL_LIC_Id1).LIC_NombreMS.Trim();
                            var nom2 = bd.Licencias.First(l => l.LIC_Id == par.LIL_LIC_Id2).LIC_NombreMS.Trim();
                            rowErrors.Add(new ResultadoProcesamiento
                            {
                                Email = email,
                                Nombre = nomEnt,
                                Licencia = $"{nom1} + {nom2}",
                                Resultado = "Licencias incompatibles"
                            });
                        }
                    }

                    // Tipo de ente (warning)
                    if (!ente.ENT_TEN_Id.HasValue)
                    {
                        rowErrors.Add(new ResultadoProcesamiento
                        {
                            Email = email,
                            Nombre = nomEnt,
                            Licencia = "",
                            Resultado = "Entidad sin tipo de entidad"
                        });
                    }
                    else
                    {
                        var validas = licTipo[ente.ENT_TEN_Id.Value];
                        foreach (var idLic in licIds)
                        {
                            if (!validas.Contains(idLic))
                            {
                                var nomLic = bd.Licencias.First(l => l.LIC_Id == idLic).LIC_NombreMS.Trim();
                                rowErrors.Add(new ResultadoProcesamiento
                                {
                                    Email = email,
                                    Nombre = nomEnt,
                                    Licencia = nomLic,
                                    Resultado = "Licencia no corresponde al tipo de entidad"
                                });
                            }
                        }
                    }

                    var rowBlocking = rowErrors.Where(r => blockingResults.Contains(r.Resultado)).ToList();
                    resultados.AddRange(rowErrors);

                    if (!rowBlocking.Any())
                        filasValidas.Add((ente.ENT_Id, ente.ENT_Email, nomEnt, licIds));
                }
            }

            resultados.AddRange(rowErrors);
        }

        bool hayBloqueantesGlobales = resultados.Any(r => blockingResults.Contains(r.Resultado));

        if (!hayBloqueantesGlobales)
        {
            // --- 3) Segunda pasada: aplicar cambios ---
            using (var tran = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                // 3.1 Altas y bajas para entidades que sí aparecen
                foreach (var (ENT_Id, emailEnt, nomEnt, licIds) in filasValidas)
                {
                    var vigentes = bd.Entes_Licencias
                        .Where(el => el.ENL_ENT_Id == ENT_Id && el.ENL_FechaFin == null)
                        .ToList();

                    // ALTAS
                    foreach (var idLic in licIds)
                    {
                        bool ya = vigentes.Any(el => el.ENL_LIC_Id == idLic);
                        var nomLic = licNombrePorId[idLic];
                        if (ya)
                            UpsertResult(emailEnt, nomEnt, nomLic, "Licencia ya vigente");
                        else
                        {
                            bd.Entes_Licencias.InsertOnSubmit(new Entes_Licencias
                            {
                                ENL_ENT_Id = ENT_Id,
                                ENL_LIC_Id = idLic,
                                ENL_FechaInicio = DateTime.Today,
                                ENL_FechaFin = null
                            });
                            UpsertResult(emailEnt, nomEnt, nomLic, "Licencia asignada");
                        }
                    }

                    // BAJAS dentro de esa entidad
                    var restantes = bd.Entes_Licencias
                        .Where(el => el.ENL_ENT_Id == ENT_Id && el.ENL_FechaFin == null)
                        .ToList();

                    foreach (var enl in restantes)
                    {
                        if (!licIds.Contains(enl.ENL_LIC_Id))
                        {
                            enl.ENL_FechaFin = DateTime.Today;
                            UpsertResult(emailEnt, nomEnt, licNombrePorId[enl.ENL_LIC_Id], "Licencia desasignada");
                        }
                    }

                    bd.SubmitChanges();
                }

                // 3.2 Bajas globales de licencias MS para ENTIDADES que no aparecen en el Excel
                var entIdsExcel = new HashSet<int>(filasValidas.Select(f => f.ENT_Id));

                var enlFuera = bd.Entes_Licencias
                    .ToList()
                    .Where(el => el.ENL_FechaFin == null
                                 && licMSIds.Contains(el.ENL_LIC_Id)
                                 && !entIdsExcel.Contains(el.ENL_ENT_Id))
                    .ToList();

                foreach (var enl in enlFuera)
                {
                    enl.ENL_FechaFin = DateTime.Today;

                    var ent = entPorId.TryGetValue(enl.ENL_ENT_Id, out var e) ? e : null;
                    var email = ent?.ENT_Email ?? "";
                    var nombreEnt = ent?.ENT_Nombre ?? "";
                    var nomLic = licNombrePorId[enl.ENL_LIC_Id];

                    UpsertResult(email, nombreEnt, nomLic, "Licencia desasignada (no aparece en Excel)");
                }

                bd.SubmitChanges();
                tran.Complete();
            }
        }

        return resultados;
    }
}