var ticketSelect;

$(document).ready(function () {
    VerificarSesionActiva(OpcionMenu.ProveedoresAsuntos).then(() => {
        // Inicializa combos y tabla
        cargarComboProveedores('#pasProveedor', false);
        cargarComboProveedores('#impProveedor', true);
        cargarComboEntidades();
        cargarComboEmpresas();
        cargarComboTareas();
        //cargarComboTicketsGLPI();
        inicializarTablaAsuntos();
    });

    $('#btnNuevoAsunto').on('click', () => {
        limpiarModalAsunto();
        $('#modalAsuntoLabel').text('Añadir Asunto');
        const modalEl = document.getElementById('modalAsunto');
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    });

    $('#btnGuardarAsunto').on('click', guardarAsunto);

    $('#btnImportarExcel').on('click', () => {
        limpiarImportar();
        cargarPeriodoFacturacion();

        const modalEl = document.getElementById('modalImportar');
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    });

    $('#btnProcesarExcel').on('click', function () {
        const proveedor = $('#impProveedor').val();
        const anyo = $('#impAnyo').val();
        const mes = $('#impMes').val();
        const facturaVal = $('#impFactura').val();
        const fileInput = $('#impFile')[0];
        const file = fileInput.files[0];

        let camposInvalidos = [];
        if (!proveedor) {
            $('#impProveedor').addClass('is-invalid');
            camposInvalidos.push('Proveedor');
        } else $('#impProveedor').removeClass('is-invalid');

        if (!file) {
            $(fileInput).addClass('is-invalid');
            camposInvalidos.push('Archivo Excel');
        } else $(fileInput).removeClass('is-invalid');

        if (camposInvalidos.length) {
            mostrarToast(
                AppConfig.mensajes.camposObligatorios + camposInvalidos.join(', '),
                TipoToast.Warning
            );
            return;
        }

        bootstrap.Modal.getOrCreateInstance('#modalImportar').hide();
        mostrarDivCargando();

        const formData = new FormData();
        formData.append('impProveedor', proveedor);
        formData.append('impAnyo', anyo);
        formData.append('impMes', mes);
        formData.append('impFactura', facturaVal);
        formData.append('impFile', file);

        $.ajax({
            url: AppConfig.urls.ImportarAsuntosExcel,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success) {
                    ocultarDivCargando();
                    //Swal.fire({
                    //    toast: true,
                    //    position: 'top-end',
                    //    icon: 'success',
                    //    title: 'Archivo importado correctamente',
                    //    showConfirmButton: false,
                    //    timer: 2000
                    //});
                    mostrarToast("Archivo importado correctamente.", TipoToast.Success);

                    setTimeout(function () {
                        mostrarDivCargando();
                        $('#tableAsuntos').DataTable().destroy();
                        inicializarTablaAsuntos();
                    }, 200);
                } else {
                    ocultarDivCargando();

                    if (response.errores) {
                        var descargarArchivo = true;
                        var mensaje = "";
                        if (response.hayErroresBloqueantes) {
                            if (response.errores && response.errores.length === 1) {
                                const err = Array.isArray(response.errores[0])
                                    ? response.errores[0][0]
                                    : response.errores[0];

                                if (typeof err === "string" &&
                                    err.toLowerCase().includes("no hay contrato vigente para el proveedor")) {
                                    mensaje = err;
                                    descargarArchivo = false;
                                } else {
                                    mensaje = "La importación NO se ha realizado. Consulte el fichero generado.";
                                }
                            }
                            else
                                mensaje = "La importación NO se ha realizado. Consulte el fichero generado."           
                        }
                        else {
                            mensaje = "La importación SI se ha realizado (" + response.insertados + " registros) con algunos errores. Consulte el fichero generado.";
                        }

                        if (response.hayErroresBloqueantes) {
                            Swal.fire({
                                title: "Error al importar",
                                text: mensaje,
                                icon: "error",
                                confirmButtonText: "Cerrar",
                                confirmButtonColor: "#d33"
                            }).then((result) => {
                                if (result.isConfirmed) {
                                    setTimeout(function () {
                                        mostrarDivCargando();
                                        $('#tableAsuntos').DataTable().destroy();
                                        inicializarTablaAsuntos();
                                    }, 200);
                                }
                            });
                        }
                        else {
                            Swal.fire({
                                title: "Importación realizada con errores",
                                text: mensaje,
                                icon: "warning",
                                confirmButtonText: "Cerrar",
                                confirmButtonColor: "#d33"
                            }).then((result) => {
                                if (result.isConfirmed) {
                                    setTimeout(function () {
                                        mostrarDivCargando();
                                        $('#tableAsuntos').DataTable().destroy();
                                        inicializarTablaAsuntos();
                                    }, 200);
                                }
                            });
                        }

                        if (response.fileUrl && descargarArchivo) {
                            // Si hay un archivo de errores, descargarlo automáticamente
                            let downloadLink = document.createElement("a");
                            downloadLink.href = response.fileUrl;
                            downloadLink.download = "Errores_Importacion.xlsx";
                            document.body.appendChild(downloadLink);
                            downloadLink.click();
                            document.body.removeChild(downloadLink);
                        }
                    }
                    else {
                        if (response.excepcion) {
                            Swal.fire({
                                title: "Error al importar",
                                text: response.excepcion,
                                icon: "error",
                                confirmButtonText: "Cerrar",
                                confirmButtonColor: "#d33"
                            }).then((result) => {
                                if (result.isConfirmed) {
                                    setTimeout(function () {
                                        mostrarDivCargando();
                                        $('#tableAsuntos').DataTable().destroy();
                                        inicializarTablaAsuntos();
                                    }, 200);
                                }
                            });
                        }
                    }
                }
            },
            error: function (xhr, status, error) {
                ocultarDivCargando();
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
            }
        });
    });

    $('#btnPrevisulizarConceptosAsuntos').click(async () => {     
        mostrarDivCargando();

        try {
            // 1) Llamamos a PrevisulizarConceptosAsuntos
            const response = await $.ajax({
                url: window.AppConfig.urls.previsulizarConceptosAsuntos,
                type: 'POST',
                dataType: 'json',
                contentType: 'application/json; charset=utf-8'
            });
            ocultarDivCargando();

            if (!response.success) {
                await Swal.fire({
                    icon: 'error',
                    title: 'Error al previsualizar',
                    html: `<div class="text-start">${response.mensaje}</div>`,
                    confirmButtonText: 'Entendido'
                });
                return;
            }

            // 2) Extraemos preview y posibles errores
            const { partesPreview, partesErrors } = response;

            // 3) Si no hay preview ni errores, avisamos y salimos
            if ((!partesPreview || partesPreview.length === 0) && (!partesErrors || partesErrors.length)) {
                await Swal.fire({
                    title: "Nada que Previsualizar",
                    html: `<p>No hay conceptos de asuntos pendientes.</p>`,
                    icon: "info",
                    confirmButtonText: "Entendido",
                    width: 500
                });
                return;
            }

            // 4) Construimos el HTML agrupado por usuario
            let htmlPartes = "";
            if (partesErrors && partesErrors.length) {
                htmlPartes = `
                  <div class="alert alert-warning">
                    <ul>
                      ${partesErrors.map(e => `<li>${e}</li>`).join("")}
                    </ul>
                  </div>
                `;
            }

            if (!partesPreview || partesPreview.length === 0) {
                htmlPartes = `<p>No hay asuntos validos para previsualizar.</p>`;
            } else {
                htmlPartes += generarResumenPorAsunto(partesPreview);
            }

            // 5) Mostramos un Swal con confirmación “Generar”
            const { isConfirmed } = await Swal.fire({
                title: "Previsualizar Conceptos de Asuntos",
                html: `<div class="small text-start">
                         ${htmlPartes}
                       </div>`,
                width: '80%',
                showCancelButton: true,
                cancelButtonText: "Cerrar",
                confirmButtonText: "Generar Conceptos",
                icon: "info"
            });

            // 6) Si el usuario pulsa “Generar Conceptos”, llamamos al endpoint de generar
            if (isConfirmed) {
                await generarConceptosAsuntos();
            }

        } catch (err) {
            ocultarDivCargando();
            registrarErrorjQuery(err.status, err.message);
        }
    });

    // 1) Selecciona el <select> y crea la instancia
    const el = document.getElementById('pasTicket');
    ticketSelect = new Choices(el, {
        removeItemButton: true,
        allowHTML: false,
        placeholderValue: 'Buscar ticket GLPI…',
        searchPlaceholderValue: 'Teclea para buscar',
        shouldSort: false,
        noResultsText: 'No hay resultados',
        loadingText: 'Cargando…',
        searchEnabled: true,
        searchChoices: false,  // que NO filtre localmente
        searchFloor: 1         // busca desde 1 carácter
    });

    // 2) Escucha el evento de búsqueda nativo de Choices
    el.addEventListener('search', async (e) => {
        const value = e.detail.value;
        if (!value) {
            ticketSelect.clearChoices();
            return;
        }

        // muestra un “Cargando…”
        ticketSelect.clearChoices();
        ticketSelect.setChoices(
            [{ value: '', label: ticketSelect.config.loadingText, disabled: true }],
            'value', 'label', true
        );

        try {
            const resp = await fetch(
                `${AppConfig.urls.ObtenerTicketsGLPICombo}?q=${encodeURIComponent(value)}`
            );
            const data = await resp.json();  // { results: […] }

            // vuelca los resultados remotos
            ticketSelect.clearChoices();
            ticketSelect.setChoices(
                data.results.map(t => ({ value: t.id, label: t.text })),
                'value', 'label', true
            );
        } catch (err) {
            console.error(err);
        }
    });
});
function inicializarTablaAsuntos() {
    const tablaDatos = inicializarDataTable('#tableAsuntos', {
        ajax: {
            url: AppConfig.urls.ObtenerAsuntos,
            type: 'GET',
            dataSrc: ''
        },
        columns: [
            { data: 'ProveedorNombre', title: 'Proveedor' },
            { data: 'PAS_Anyo', title: 'Año' },
            { data: 'PAS_Mes', title: 'Mes' },
            { data: 'PAS_CodigoExterno', title: 'Código Externo' },
            { data: 'PAS_Fecha', title: 'Fecha', render: formatDateToDDMMYYYY },
            { data: 'TicketTitulo', title: 'Ticket GLPI' },
            { data: 'EntidadNombre', title: 'Entidad' },
            { data: 'EmpresaNombre', title: 'Empresa' },
            { data: 'TareaNombre', title: 'Tarea' },
            { data: 'PAS_Descripcion', title: 'Descripción' },
            { data: 'PAS_Horas', title: 'Horas', className: 'text-end dt-type-numeric-with-decimal' },
            { data: 'PAS_Importe', title: 'Importe', className: 'text-end', render: formatMoney },
            { data: 'PAS_NumFacturaP', title: 'Nº Factura' },
            {
                data: null, title: '<span class="sReader">Acción</span>', orderable: false, className: 'td-btn',
                render: function (data, type, row) {
                    const jsRow = JSON.stringify(row).replace(/"/g, "&quot;");
                    return `
                            <button class="btn btn-icon btn-editar btn-outline-secondary me-2" data-asunto="${jsRow}" onclick="editarAsunto(this)">
                                <i class="bi bi-pencil-square"></i>
                            </button>
                            <button class="btn btn-icon btn-eliminar btn-outline-secondary" data-asunto="${jsRow}" onclick="eliminarAsunto(this)">
                                <i class="bi bi-trash"></i>
                            </button>`;
                }
            }
        ]
    }, [], 'export_asuntos');

    $(window).resize(() => tablaDatos.columns.adjust().draw());

    $("#formBuscarAsunto").on("keyup", function () {
        tablaDatos.search(this.value, false, false).draw();
    });
}

async function cargarComboProveedores(selectCombo, conSoporte) {
    let data;
    if (conSoporte)
        data = await $.get(AppConfig.urls.ObtenerProveedoresSoporte);
    else
        data = await $.get(AppConfig.urls.ObtenerProveedores);
    const sel = $(selectCombo).empty().append('<option value="">Seleccione Proveedor</option>');
    data.forEach(x => sel.append(`<option value="${x.PRV_Id}">${x.PRV_Nombre}</option>`));
}

async function cargarComboEntidades() {
    const data = await $.get(AppConfig.urls.ObtenerComboEntidades);
    const sel = $('#pasEntidad').empty().append('<option value="">Seleccione Entidad</option>');
    data.forEach(x => sel.append(`<option value="${x.ENT_Id}">${x.ENT_Nombre}</option>`));

    const $pasEntidad = $('#pasEntidad');
    if ($pasEntidad.hasClass('select2-hidden-accessible')) {
        $pasEntidad.select2('destroy');
    }
    $pasEntidad.select2({
        placeholder: 'Seleccione Entidad',
        allowClear: true,
        width: '100%',
        dropdownParent: $('#modalAsunto')
    });
}

async function cargarComboEmpresas() {
    const data = await $.get(AppConfig.urls.ObtenerComboEmpresas);
    const sel = $('#pasEmpresa').empty().append('<option value="">Seleccione Empresa</option>');
    data.forEach(x => sel.append(`<option value="${x.EMP_Id}">${x.EMP_Nombre}</option>`));
}

async function cargarComboTicketsGLPI() {
    const data = await $.get(AppConfig.urls.ObtenerTicketsGLPICombo);
    const sel = $('#pasTicket').empty().append('<option value="">Seleccione Ticket GLPI</option>');
    data.forEach(x => sel.append(`<option value="${x.TKC_Id_GLPI}">${x.TKC_Titulo}</option>`));
}

async function cargarPeriodoFacturacion() {
    const data = await $.get(AppConfig.urls.ObtenerPeriodoFacturacion);
    $('#impAnyo').val(data.Anyo);
    $('#impMes').val(data.Mes);
}

async function cargarComboTareas() {
    const data = await $.get(AppConfig.urls.ObtenerTareasCombo);
    const sel = $('#pasTarea').empty().append('<option value="">Seleccione Tarea</option>');
    data.forEach(x => sel.append(`<option value="${x.TAR_Id}">${x.TAR_Nombre}</option>`));

    const $pasTarea = $('#pasTarea');
    if ($pasTarea.hasClass('select2-hidden-accessible')) {
        $pasTarea.select2('destroy');
    }
    $pasTarea.select2({
        placeholder: 'Seleccione Tarea',
        allowClear: true,
        width: '100%',
        dropdownParent: $('#modalAsunto')
    });
}

function limpiarModalAsunto() {
    $('#pasId, #pasAnyo, #pasMes, #pasCodigoExterno, #pasFecha, #pasDescripcion, #pasHoras, #pasImporte, #pasNumFactura').val('');
    $('#pasEntidad, #pasEmpresa, #pasTicket').val('');

    ticketSelect.clearChoices();      // elimina todas las opciones del desplegable
    ticketSelect.clearStore();        // vacía el almacenamiento interno de Choices.js
    ticketSelect.clearInput();        // limpia el texto del input

    // 2️⃣ Vuelve a cargar solo la opción placeholder
    ticketSelect.setChoices(
        [{ value: '', label: 'Seleccione Ticket GLPI', disabled: true }],
        'value',
        'label',
        true
    );

    $('#pasTarea').val(null).trigger('change');
    $('#modalAsunto .is-invalid').removeClass('is-invalid');
}
function editarAsunto(btn) {
    limpiarModalAsunto();

    const a = JSON.parse(btn.getAttribute('data-asunto'));
    $('#pasId').val(a.PAS_Id);
    $('#pasProveedor').val(a.PAS_PRV_Id || '');
    $('#pasAnyo').val(a.PAS_Anyo);
    $('#pasMes').val(a.PAS_Mes);
    $('#pasCodigoExterno').val(a.PAS_CodigoExterno);
    $('#pasFecha').val(formatDateInputForDateField(a.PAS_Fecha));

    if (a.PAS_TKC_Id_GLPI) {
        ticketSelect.setChoices(
            [
                {
                    value: a.PAS_TKC_Id_GLPI,
                    label: "#" + a.PAS_TKC_Id_GLPI + ' - ' + a.TicketTitulo,   // asegúrate de tener el título en el objeto
                    selected: true,
                    disabled: false
                }
            ],
            'value',
            'label',
            true   // replaceChoices = true
        );
    }

    const $sol = $('#pasEntidad');
    $sol.val(a.PAS_ENT_Id || '');
    $sol.trigger('change');

    $('#pasEmpresa').val(a.PAS_EMP_Id || '');
    $('#pasDescripcion').val(a.PAS_Descripcion);
    $('#pasHoras').val(a.PAS_Horas);
    $('#pasImporte').val(a.PAS_Importe);
    $('#pasNumFactura').val(a.PAS_NumFacturaP);
    $('#pasTarea').val(a.PAS_TAR_Id || '').trigger('change');

    $('#modalAsuntoLabel').text('Editar Asunto');
    $('#modalAsunto').modal('show');
}

async function guardarAsunto() {
    let inv = [];
    validarCampo('#pasProveedor', 'Proveedor', inv);
    validarCampo('#pasAnyo', 'Año', inv);
    validarCampo('#pasMes', 'Mes', inv);
    if (inv.length) {
        mostrarToast(AppConfig.mensajes.camposObligatorios + inv.join(', '), TipoToast.Warning);
        return;
    }

    const dto = {
        PAS_Id: $('#pasId').val(),
        PAS_PRV_Id: $('#pasProveedor').val(),
        PAS_Anyo: $('#pasAnyo').val(),
        PAS_Mes: $('#pasMes').val(),
        PAS_CodigoExterno: $('#pasCodigoExterno').val(),
        PAS_Fecha: $('#pasFecha').val(),
        PAS_TKC_Id_GLPI: $('#pasTicket').val(),
        PAS_ENT_Id: $('#pasEntidad').val(),
        PAS_EMP_Id: $('#pasEmpresa').val(),
        PAS_TAR_Id: $('#pasTarea').val() || null,
        PAS_Descripcion: $('#pasDescripcion').val(),
        PAS_Horas: parseFloat($('#pasHoras').val()),
        PAS_Importe: parseFloat($('#pasImporte').val()),
        PAS_NumFacturaP: $('#pasNumFactura').val()
    };

    try {
        const resp = await $.ajax({
            url: AppConfig.urls.GuardarAsunto,
            type: 'POST', contentType: 'application/json', data: JSON.stringify(dto), dataType: 'json'
        });
        if (resp.success) {
            mostrarToast('Guardado', TipoToast.Success);
            $('#modalAsunto').modal('hide');
            $('#tableAsuntos').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast('Error al guardar', TipoToast.Error);
        }
    } catch (xhr) {
        const msg = obtenerMensajeErrorAjax(xhr);
        registrarErrorjQuery(xhr.status, msg);
    }
}
function eliminarAsunto(btn) {
    const a = JSON.parse(btn.getAttribute('data-asunto'));
    mostrarAlertaConfirmacion({
        titulo: `¿Eliminar asunto '${a.PAS_Descripcion}'?`, onConfirmar: async () => {
            try {
                const r = await $.ajax({
                    url: AppConfig.urls.EliminarAsunto,
                    type: 'POST', contentType: 'application/json', data: JSON.stringify({ id: a.PAS_Id }), dataType: 'json'
                });
                if (r.success) {
                    mostrarToast('Eliminado', TipoToast.Success);
                    $('#tableAsuntos').DataTable().ajax.reload(null, false);
                } else {
                    mostrarToast('No se pudo eliminar', TipoToast.Warning);
                }
            } catch (err) {
                registrarErrorjQuery(err.status || '', err.message || err);
            }
        }
    });
}
function limpiarImportar() {
    $('#formImportar')[0].reset();
    $('#impFactura').val('');
}
function generarResumenPorAsunto(preview) {
    let html = '';
    preview.forEach(emp => {
        html += `
    <div class="card mb-4 shadow-sm">
      <div class="card-header text-white d-flex justify-content-between" style="background-color:#0092ff">
        <div>
          <strong>${emp.NombreEmpresa}</strong>
          <small class="text-light ms-3">${emp.Anyo}/${String(emp.Mes).padStart(2, '0')}</small>
        </div>
        <div>
          <span class="badge bg-light text-dark fs-6">Total Horas: ${emp.TotalHoras.toFixed(2)}</span>
          <span class="badge bg-light text-dark ms-2 fs-6">Importe: ${formatMoney(emp.TotalImporte)}</span>
        </div>
      </div>
      <div class="card-body p-2">`;

        // Por cada proveedor
        emp.Proveedores.forEach(prov => {
            html += `
      <div class="mb-3">
        <h6>Proveedor → ${prov.NombreProveedor}
          <strong class="text-muted">(Horas Contratadas: ${prov.HorasContratadas.toFixed(2)}, Precio/Hora: ${formatMoney(prov.PrecioHoraContrato)})</strong>
        </h6>`;

            // **Aquí pintamos los errores de este proveedor, si los hay**
            if (prov.ListaErrores && prov.ListaErrores.length) {
                html += `
        <div class="alert alert-warning">
          <ul class="mb-2" style="margin-bottom: 0px !important;">
            ${prov.ListaErrores.map(err => `<li>${err}</li>`).join('')}
          </ul>
        </div>`;
            }

            html += `
        <table class="table table-sm table-striped mb-2">
          <thead class="table-light">
            <tr>
              <th class="text-end">Fecha</th>
              <th class="text-end">Asunto</th>
              <th class="text-end">Entidad</th>
              <th class="text-end">Ticket GLPI</th>
              <th class="text-end">Horas/Importe</th>
            </tr>
          </thead>
          <tbody>`;

            prov.Asuntos.forEach(a => {
                const fecha = parseDotNetDateToDDMMYYYY(a.Fecha);
                const ticketHtml = a.IdTicket ? `#${a.IdTicket}` : '';
                html += `
            <tr>
              <td class="text-end">${fecha}</td>
              <td class="text-end">${a.Descripcion}</td>
              <td class="text-end">${a.EntidadNombre}</td>
              <td class="text-end">${ticketHtml}</td>
              <td class="text-end">${a.Horas > 0 ? `${a.Horas.toFixed(2)} h` : `${a.Importe.toFixed(2)} €`}</td>
            </tr>`;

                if (a.Mensaje) {
                    html += `
            <tr>
              <td colspan="3" class="ps-3"><em>${a.Mensaje}</em></td>
            </tr>`;
                }
            });

            html += `
          </tbody>
        </table>
      </div>`;
        });

        html += `
      </div>
    </div>`;
    });

    return html;
}

async function generarConceptosAsuntos() {
    mostrarDivCargando();
    try {
        const response = await $.ajax({
            url: AppConfig.urls.GenerarConceptosAsuntos,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json'
        });
        ocultarDivCargando();

        if (response.success) {
            const htmlResultado = response.resultados.length
                ? `<ul class="text-start">
             ${response.resultados.map(r =>
                    `<li><strong>${r.EmpresaNombre}</strong>: ${r.ConceptosCreados} concepto(s)</li>`
                ).join('')}
           </ul>`
                : `<p>No se ha generado ningún concepto.</p>`;

            await Swal.fire({
                title: 'Conceptos generados correctamente',
                html: htmlResultado,
                icon: 'success',
                confirmButtonText: 'Aceptar',
                width: '50%'
            });

            // Recarga de la tabla de asuntos (u otra que quieras actualizar)
            $('#tableAsuntos').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast(response.mensaje || 'Error al generar conceptos.', TipoToast.Error);
        }
    } catch (err) {
        ocultarDivCargando();
        const msg = obtenerMensajeErrorAjax(err);
        registrarErrorjQuery(err.status || '', msg);
    }
}