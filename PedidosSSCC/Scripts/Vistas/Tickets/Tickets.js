let storageKey = Filtros.Tickets;

// Filtro personalizado para rango de Fecha Apertura
$.fn.dataTable.ext.search.push(function (settings, data) {
    // Solo aplicamos al table de tickets
    if (!settings.nTable.id || settings.nTable.id !== 'tablaTickets') {
        return true;
    }

    const min = $('#filterFechaDesde').val();
    const max = $('#filterFechaHasta').val();
    const fechaApertura = data[7]; // Fecha Apertura formateada 'DD/MM/YYYY HH:mm'

    if (!fechaApertura) return true;

    const fecha = moment(fechaApertura, 'DD/MM/YYYY HH:mm').toDate();
    let desdeOk = true, hastaOk = true;

    if (min) {
        const d = new Date(min);
        desdeOk = fecha >= d;
    }
    if (max) {
        const h = new Date(max);
        h.setHours(23, 59, 59, 999);
        hastaOk = fecha <= h;
    }

    return desdeOk && hastaOk;
});

async function eliminarTicket(button) {
    const ticket = JSON.parse(button.getAttribute('data-ticket'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar el ticket '${ticket.TKC_Titulo}'?`,
        onConfirmar: async function () {
            try {
                const response = await $.ajax({
                    url: AppConfig.urls.EliminarTicket,
                    type: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    data: JSON.stringify({ idTicket: ticket.TKC_Id }),
                    dataType: 'json'
                });

                if (response.success) {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                    const tabla = $('#tablaTickets').DataTable();
                    tabla.ajax.reload(null, false);
                } else {
                    mostrarToast("No se puede eliminar el ticket.", TipoToast.Warning);
                }
            } catch (error) {
                registrarErrorjQuery(error.status || 'error', error.message || error);
            }
        }
    });
}

function generarBotonesTicket(row) {
    let jsonRaw = JSON.stringify(row);
    let jsonEscapado = jsonRaw.replace(/"/g, "&quot;");

    return `
        <button type="button"
                class="btn btn-icon btn-editar btn-outline-secondary"
                data-ticket="${jsonEscapado}"
                onclick="abrirModalTicket(this)">
            <i class="bi bi-eye-fill" title="Ver"></i>
        </button>
        <button type="button"
                class="btn btn-icon btn-eliminar btn-outline-secondary"
                data-ticket="${jsonEscapado}"
                onclick="eliminarTicket(this)">
            <i class="bi bi-trash" title="Eliminar"></i>
        </button>`;
}

function abrirModalTicket(button) {
    const data = JSON.parse(button.getAttribute('data-ticket'));

    $('#TKC_Id').val(data.TKC_Id);
    $('#TKC_Id_GLPI').val(data.TKC_Id_GLPI || '');
    $('#TKC_Titulo').val(data.TKC_Titulo);
    $('#TKC_GrupoAsignado').val(data.TKC_GrupoAsignado);
    $('#TKC_Categoria').val(data.TKC_Categoria);
    $('#TKC_CTK_Id').val(data.TKC_CTK_Id || '');
    $('#TKC_Ubicacion').val(data.TKC_Ubicacion);
    $('#TKC_Duracion').val(data.TKC_Duracion);
    $('#TKC_Descripcion').val(data.TKC_Descripcion);

    // NUEVO: cargar también los campos ocultos si no se editan en el modal
    $('#TKC_ETK_Id').val(data.TKC_ETK_Id || '');
    $('#TKC_TTK_Id').val(data.TKC_TTK_Id || '');
    $('#TKC_OTK_Id').val(data.TKC_OTK_Id || '');

    const $sol = $('#TKC_ENT_Id_Solicitante');
    $sol.val(data.TKC_ENT_Id_Solicitante || '');
    $sol.trigger('change');

    $('#TKC_ProveedorAsignado').val(data.TKC_ProveedorAsignado || '');
    $('#TKC_GrupoCargo').val(data.TKC_GrupoCargo || '');
    $('#TKC_VTK_Id').val(data.TKC_VTK_Id || '');

    const fechaApertura = moment(data.TKC_FechaApertura).format("YYYY-MM-DD");
    const fechaResolucion = moment(data.TKC_FechaResolucion).format("YYYY-MM-DD");
    $('#TKC_FechaApertura').val(fechaApertura);
    $('#TKC_FechaResolucion').val(fechaResolucion);

    $('#modalTicketLabel').text('Modificación de Ticket');
    $('#modalTicket').modal('show');
}

function limpiarModalTicket() {
    $('#formTicket')[0].reset();
    $('#TKC_Id').val('');
}

function guardarFiltros() {
    let general = $("#formBuscar").val();
    let titulo = $('#filterTitulo').val();
    let grupoAsignado = $('#filterGrupoAsignado').val();
    let categoria = $('#filterCategoria').val();
    let ubicacion = $('#filterUbicacion').val();
    let empresa = $('#filterEmpresa').val();
    let sinEmpresa = $('#filterSinEmpresa').is(':checked');

    let filtroActual = { general, titulo, grupoAsignado, categoria, ubicacion, empresa, sinEmpresa };
    localStorage.setItem(storageKey, JSON.stringify(filtroActual));
}

let tablaDatos;

async function ObtenerTickets() {
    let columnasConFiltro = [];
    tablaDatos = inicializarDataTable("#tablaTickets", {
        ajax: {
            url: AppConfig.urls.ObtenerTickets,
            type: 'GET',
            dataSrc: '',
            dataType: "json"
        },
        columns: [
            { data: 'TKC_Titulo', title: 'Título' },
            { data: 'TKC_Id_GLPI', title: 'GLPI' },
            { data: 'TKC_GrupoAsignado', title: 'Grupo Asignado' },
            { data: 'TKC_Categoria', title: 'Categoría' },
            { data: 'TKC_Ubicacion', title: 'Ubicación' },
            {
                data: 'TKC_Duracion',
                title: 'Duración',
                render: function (data) {
                    if (!data || isNaN(data)) return '';
                    let minutos = parseInt(data, 10);
                    const horas = Math.floor(minutos / 60);
                    const mins = minutos % 60;
                    // Por ejemplo “1h 30m” o “45m” si < 60
                    return horas > 0
                        ? `${horas}h ${mins}m`
                        : `${mins}m`;
                }
            },
            { data: 'EMP_Nombre', title: 'Empresa', render: (data) => data ?? '' },
            {
                data: 'TKC_FechaApertura',
                title: 'Fecha Apertura',
                render: function (data) {
                    // Si 'data' llega como cadena ISO o Date, moment lo formatea:
                    return data ? moment(data).format('DD/MM/YYYY HH:mm') : '';
                }
            },
            {
                data: 'TKC_FechaResolucion',
                title: 'Fecha Resolución',
                render: function (data) {
                    return data ? moment(data).format('DD/MM/YYYY HH:mm') : '';
                }
            },
            {
                className: 'td-btn',
                data: null,
                title: '<span class="sReader">Acción</span>',
                responsivePriority: 2,
                orderable: false,
                render: function (data, type, row) {
                    return generarBotonesTicket(data);
                }
            }
        ]
    }, columnasConFiltro, 'export_tickets');

    $(window).resize(() => tablaDatos.columns.adjust().draw());

    $("#formBuscar").on("keyup input", function () {
        tablaDatos.search(this.value, false, false).draw();
        guardarFiltros();
    });

    return tablaDatos;
}

async function poblarSelect(idSelect, url, idCampo, textoCampo) {
    try {
        const datos = await $.getJSON(url);
        const $select = $('#' + idSelect);
        $select.empty();
        $select.append(`<option value="">-- Selecciona --</option>`);
        datos.forEach(x => {
            $select.append(`<option value="${x[idCampo]}">${x[textoCampo]}</option>`);
        });
    } catch (err) {
        console.error(`Error al cargar ${idSelect}`, err);
    }
}

$(document).ready(async function () {
    await VerificarSesionActiva(OpcionMenu.Tickets);
    await establecerFiltros();

    await poblarSelect('TKC_ETK_Id', AppConfig.urls.ObtenerEstadosTicket, 'ETK_Id', 'ETK_Nombre');
    await poblarSelect('TKC_TTK_Id', AppConfig.urls.ObtenerTiposTicket, 'TTK_Id', 'TTK_Nombre');
    await poblarSelect('TKC_OTK_Id', AppConfig.urls.ObtenerOrigenesTicket, 'OTK_Id', 'OTK_Nombre');
    await poblarSelect('TKC_VTK_Id', AppConfig.urls.ObtenerValidacionesTicket, 'VTK_Id', 'VTK_Nombre');
    await poblarSelect('TKC_ENT_Id_Solicitante', AppConfig.urls.ObtenerEntes, 'ENT_Id', 'ENT_Nombre');
    await poblarSelect('filterEmpresa', AppConfig.urls.ObtenerComboEmpresas, 'EMP_Id', 'EMP_Nombre');

    const $solicitante = $('#TKC_ENT_Id_Solicitante');
    if ($solicitante.hasClass('select2-hidden-accessible')) {
        $solicitante.select2('destroy');
    }
    $solicitante.select2({
        placeholder: 'Seleccione solicitante',
        allowClear: true,
        width: '100%',
        dropdownParent: $('#modalTicket')
    });

    await poblarSelect('TKC_CTK_Id', AppConfig.urls.ObtenerCategoriasTicket, 'CTK_Id', 'CTK_Nombre');

    async function establecerFiltros() {
        let savedFilters = localStorage.getItem(storageKey);
        let hasFilters = false;

        if (savedFilters) {
            savedFilters = JSON.parse(savedFilters);
            $("#formBuscar").val(savedFilters.general);
            $("#filterTitulo").val(savedFilters.titulo || "");
            $("#filterGrupoAsignado").val(savedFilters.grupoAsignado || "");
            $("#filterCategoria").val(savedFilters.categoria || "");
            $("#filterUbicacion").val(savedFilters.ubicacion || "");
            $("#filterEmpresa").val(savedFilters.empresa || "");
            $("#filterSinEmpresa").prop("checked", !!savedFilters.sinEmpresa);
            $("#filterEmpresa").prop("disabled", !!savedFilters.sinEmpresa);

            let { general, ...otrosFiltros } = savedFilters;
            hasFilters = Object.values(otrosFiltros).some(v => v !== "" && v !== null && v !== undefined);
        }

        await ObtenerTickets();

        setTimeout(() => {
            let table = $('#tablaTickets').DataTable();
            if (savedFilters?.general) {
                table.search(savedFilters.general, false, false).draw();
            }
        }, 200);

        if (hasFilters) {
            $("#btnLimpiar").show();
            $(".table-filter").addClass('advance');
            $('#btnAvanzado').html(`Ocultar Filtros`);
        }
    }

    $('#btnBuscar').on('click', function () {
        const titulo = $('#filterTitulo').val();
        const grupo = $('#filterGrupoAsignado').val();
        const categoria = $('#filterCategoria').val();
        const ubicacion = $('#filterUbicacion').val();

        const sel = $('#filterEmpresa option:selected');
        const empresaTx = sel.length ? sel.text().trim() : '';
        const sinEmpresa = $('#filterSinEmpresa').is(':checked');

        const table = $('#tablaTickets').DataTable();

        table.column(0).search(titulo);     // Título
        table.column(2).search(grupo);      // Grupo Asignado
        table.column(3).search(categoria);  // Categoría
        table.column(4).search(ubicacion);  // Ubicación

        // Empresa: si “Sin empresa” => regex vacío; si no => búsqueda exacta por texto
        if (sinEmpresa) {
            table.column(6).search('^\\s*$', true, false);   // ← SOLO vacías
        } else if (empresaTx && empresaTx !== '-- Selecciona --') {
            const rx = '^' + $.fn.dataTable.util.escapeRegex(empresaTx) + '$';
            table.column(6).search(rx, true, false);
        } else {
            table.column(6).search('');
        }

        table.draw();
        guardarFiltros();
    });

    // Aplicar rango de fechas al cambiar inputs
    $('#filterFechaDesde, #filterFechaHasta, #filterEmpresa').on('change', function () {
        $('#tablaTickets').DataTable().draw();
    });

    function validarFormularioTicket() {
        // Define aquí los campos obligatorios y su etiqueta para el mensaje
        const campos = [
            { id: 'TKC_Titulo', label: 'Título' },
            { id: 'TKC_GrupoAsignado', label: 'Grupo Asignado' },
            { id: 'TKC_ETK_Id', label: 'Estado' },
            { id: 'TKC_TTK_Id', label: 'Tipo' },
            { id: 'TKC_OTK_Id', label: 'Origen' },
            { id: 'TKC_VTK_Id', label: 'Validación' },
            { id: 'TKC_CTK_Id', label: 'Categoría Calculada' }
        ];

        let faltan = [];

        campos.forEach(c => {
            const $el = $(`#${c.id}`);
            const val = $el.val();
            if (!val || val.toString().trim() === '') {
                // marcar como inválido
                $el.addClass('is-invalid');
                faltan.push(c.label);
            } else {
                $el.removeClass('is-invalid');
            }
        });

        if (faltan.length > 0) {
            mostrarToast(AppConfig.mensajes.camposObligatorios + `${faltan.join(', ')}`, TipoToast.Warning);
            return false;
        }

        return true;
    }

    $('#btnGuardarTicket').click(async () => {
        // 2.1) validamos antes de nada
        if (!validarFormularioTicket()) return;

        // 2.2) construimos el objeto a enviar
        const ticket = {
            TKC_Id: $('#TKC_Id').val(),
            TKC_Id_GLPI: $('#TKC_Id_GLPI').val(),
            TKC_Titulo: $('#TKC_Titulo').val(),
            TKC_GrupoAsignado: $('#TKC_GrupoAsignado').val(),
            TKC_Categoria: $('#TKC_Categoria').val(),
            TKC_Ubicacion: $('#TKC_Ubicacion').val(),
            TKC_Duracion: $('#TKC_Duracion').val(),
            TKC_Descripcion: $('#TKC_Descripcion').val(),
            TKC_ETK_Id: $('#TKC_ETK_Id').val(),
            TKC_TTK_Id: $('#TKC_TTK_Id').val(),
            TKC_OTK_Id: $('#TKC_OTK_Id').val(),
            TKC_ENT_Id_Solicitante: $('#TKC_ENT_Id_Solicitante').val(),
            TKC_ProveedorAsignado: $('#TKC_ProveedorAsignado').val(),
            TKC_GrupoCargo: $('#TKC_GrupoCargo').val(),
            TKC_VTK_Id: $('#TKC_VTK_Id').val(),
            TKC_FechaApertura: $('#TKC_FechaApertura').val(),
            TKC_FechaResolucion: $('#TKC_FechaResolucion').val(),
            TKC_CTK_Id: $('#TKC_CTK_Id').val()
        };

        try {
            const response = await $.ajax({
                url: AppConfig.urls.GuardarTicket,
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify(ticket),
                dataType: 'json'
            });

            if (response.success) {
                // cerramos modal y recargamos tabla
                $('#modalTicket').modal('hide');

                const tabla = await ObtenerTickets();
                tabla.ajax.reload(null, false);

                mostrarToast("Guardado con éxito", TipoToast.Success);
            } else {
                mostrarToast(response.message || "Error al guardar", TipoToast.Error);
            }
        } catch (error) {
            const mensaje = obtenerMensajeErrorAjax(error);
            registrarErrorjQuery(error.status || '', mensaje);
            mostrarToast("Error al conectar con el servidor", TipoToast.Error);
        }
    });

    $('#btnNuevo').on('click', function () {
        limpiarModalTicket();
        $('#modalTicketLabel').text('Alta de Ticket');
        $('#modalTicket').modal('show');
    });

    $('#btnDescargarPlantilla').click(function () {
        window.location.href = AppConfig.urls.DescargarPlantillaTickets;
    });

    $('#btnImportarExcel').on('click', function () {
        $('#archivoExcel').val('');
        $("#modalImportar").modal("show");
    });

    $('#btnSubirProcesar').click(async function (e) {
        e.preventDefault();

        let archivo = $('#archivoExcel')[0].files[0];

        if (!archivo) {
            mostrarToast("Por favor, seleccione un archivo.", TipoToast.Error);
            return;
        }

        let formData = new FormData();
        formData.append("archivoExcel", archivo);

        $("#modalImportar").modal('hide');
        mostrarDivCargando();

        try {
            const response = await $.ajax({
                url: AppConfig.urls.ImportarExcelTickets,
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false
            });

            if (response.success) {
                ocultarDivCargando();
                mostrarToast("Archivo importado correctamente.", TipoToast.Success);
                $('#tablaTickets').DataTable().ajax.reload(null, false);
            } else {
                ocultarDivCargando();
                mostrarToast(response.message || "Error durante la importación.", TipoToast.Error);

                if (response.fileUrl) {
                    let link = document.createElement('a');
                    link.href = response.fileUrl;
                    link.download = "Errores_Importacion.xlsx";
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                }
                $('#tablaTickets').DataTable().ajax.reload(null, false);
            }
        } catch (error) {
            const mensaje = obtenerMensajeErrorAjax(error);
            registrarErrorjQuery(error.status || '', mensaje);
        }
    });

    $('#modalTicket').modal({
        backdrop: 'static', // Evita que se cierre al hacer clic fuera
        keyboard: false // Evita que se cierre con la tecla "Esc"
    });

    $('#filterSinEmpresa').on('change', function () {
        const checked = $(this).is(':checked');
        $('#filterEmpresa').prop('disabled', checked);
        if (checked) {
            $('#filterEmpresa').val('');
        }
    });

    $('#btnPrevisulizarConceptos').click(async () => {
        mostrarDivCargando();
        try {
            const response = await $.ajax({
                url: AppConfig.urls.PrevisualizarConceptos,
                type: 'POST',
                dataType: 'json',
                contentType: 'application/json; charset=utf-8'
            });
            ocultarDivCargando();

            if (!response.success) {
                await Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: 'Se produjo un error procesando la previsualización.'
                });
                return;
            }

            const soporte = response.soporte ?? [];
            const soporteWarnings = response.soporteErrors ?? [];
            const soporteBlocking = response.soporteBlockingErrors ?? [];

            const htmlAll = `
          <div class="small text-start">
            <h5>Soporte CAU</h5>
            ${renderSoporteSection(soporte, soporteWarnings, soporteBlocking)}
          </div>`;

            const hasConcepts = soporte.length > 0;
            const hasBlocking = soporteBlocking.length > 0;

            if (hasBlocking) {
                // ❌ Bloqueantes → botón único "Aceptar", sin cierre fuera ni con ESC
                await Swal.fire({
                    title: "Errores bloqueantes",
                    html: htmlAll,
                    width: '70%',
                    icon: 'error',
                    confirmButtonText: 'Aceptar'
                });
            } else {
                // ✅ Sin bloqueantes → permitir generar conceptos
                const { isConfirmed } = await Swal.fire({
                    title: "¿Deseas generar los conceptos?",
                    html: htmlAll,
                    width: '70%',
                    showCancelButton: hasConcepts,
                    cancelButtonText: hasConcepts ? "No, cancelar" : "Cerrar",
                    showConfirmButton: hasConcepts,
                    confirmButtonText: "Sí, generar"
                });

                if (isConfirmed) {
                    await ejecutarGeneracionConceptos();
                }
            }
        } catch (err) {
            ocultarDivCargando();
            registrarErrorjQuery(err.status, err.message);
        }
    });

});

function renderSoporteSection(preview, warnings = [], blocking = []) {
    let html = "";

    // ⚠️ Bloqueantes (rojo)
    if (blocking.length) {
        html += `<div class="alert alert-danger">
      <strong>Errores bloqueantes</strong>
      <ul>${blocking.map(e => `<li>${e}</li>`).join("")}</ul>
    </div>`;
    }

    // ⚠️ Warnings (amarillo)
    if (warnings.length) {
        html += `<div class="alert alert-warning">
      <ul>${warnings.map(e => `<li>${e}</li>`).join("")}</ul>
    </div>`;
    }

    // Resumen (como Licencias)
    if (!preview || !preview.length) {
        html += `<p>No hay conceptos de soporte pendientes.</p>`;
    } else {
        html += generarResumenConceptosSoportePrev(preview);
    }

    return html;
}

function generarResumenConceptosSoportePrev(preview) {
    if (!preview || !preview.length) {
        return "<p>No hay conceptos de soporte pendientes.</p>";
    }

    let html = "<div class='small text-start'>";

    preview.forEach(dto => {
        html += `
      <div style="
        border:1px solid #e0e0e0;
        border-radius:8px;
        padding:10px;
        margin-bottom:12px;">
        <div class="fw-semibold mb-2" style="color:#001f3f;">
          Categoría: ${dto.CategoriaNombre}
        </div>
        <table style="width:100%;border-collapse:collapse;font-size:0.9em">
          <thead>
            <tr style="background:#f0f0f0">
              <th style="padding:6px;border:1px solid #ddd;text-align:left">Empresa</th>
              <th style="padding:6px;border:1px solid #ddd;text-align:center">Periodo</th>
              <th style="padding:6px;border:1px solid #ddd;text-align:right">Importe</th>
            </tr>
          </thead>
          <tbody>
    `;

        dto.Filas.forEach(r => {
            html += `
        <tr>
          <td style="padding:6px;border:1px solid #eee">${r.EmpresaNombre}</td>
          <td style="padding:6px;border:1px solid #eee;text-align:center">${r.Mes}/${r.Anyo}</td>
          <td style="padding:6px;border:1px solid #eee;text-align:right">${formatMoney(r.ImporteTotal)}</td>
        </tr>
      `;
        });

        html += `
          </tbody>
        </table>
      </div>
    `;
    });

    html += "</div>";
    return html;
}

async function ejecutarGeneracionConceptos() {
    mostrarDivCargando();
    try {
        const response = await $.ajax({
            url: AppConfig.urls.GenerarTodosConceptos,
            type: 'POST',
            dataType: 'json'
        });
        ocultarDivCargando();

        // — Si devolvió errores bloqueantes —
        if (response.soporteBlockingErrors?.length) {
            await Swal.fire({
                icon: 'error',
                title: 'Errores bloqueantes',
                html: `<ul style="text-align:left">
                  ${response.soporteBlockingErrors.map(e => `<li>${e}</li>`).join('')}
                </ul>`,
                confirmButtonText: 'Aceptar',
                allowOutsideClick: false,
                allowEscapeKey: false
            });
            return;
        }

        // — Si devolvió errores de validación al persistir —
        if (response.errors?.length) {
            await Swal.fire({
                icon: 'error',
                title: 'Errores generando conceptos',
                html: `<ul style="text-align:left">
                 ${response.errors.map(e => `<li>${e}</li>`).join('')}
               </ul>`,
                confirmButtonText: 'Entendido'
            });
            return;
        }

        // — Si success=false por otro motivo —
        if (!response.success) {
            mostrarErrorPedidos();
            return;
        }

        // — Éxito: mostrar resumen de conceptos creados —
        const htmlFin = `
      <div style="text-align:left">
        <h5>Conceptos de Soporte</h5>
        ${generarResumenConceptosSoporteFin(response.soporte)}
      </div>
    `;
        await Swal.fire({
            title: "Proceso Completado",
            html: htmlFin,
            icon: "success",
            confirmButtonText: 'Aceptar'
        });

        // refrescar tabla
        const tabla = $('#table').DataTable();
        tabla.destroy();
        mostrarDivCargando()
        $('#table tbody').empty();
        await ObtenerTickets();
        ocultarDivCargando();

    } catch (err) {
        ocultarDivCargando();
        registrarErrorjQuery(err.status, err.message);
    }
}

function generarResumenConceptosSoporteFin(resultado) {
    if (!resultado || resultado.length === 0) {
        return "<ul><li><strong>No se ha generado ningún concepto de soporte</strong></li></ul>";
    }
    let mensaje = "<ul style='text-align: left;'>";
    resultado.forEach(empresa => {
        mensaje += `<li><strong>${empresa.EmpresaNombre}</strong>: ${empresa.ConceptosCreados} concepto(s) generados</li>`;
    });
    mensaje += "</ul>";
    return mensaje;
}
