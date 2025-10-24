function abrirModalContrato(button) {
    const contrato = JSON.parse(button.getAttribute('data-contrato'));

    cargarCombosTarifas(() => {
        $('#idContrato').val(contrato.CCA_Id);
        const fechaInicio = moment(contrato.CCA_FechaInicio).format("YYYY-MM-DD");
        const fechaFin = moment(contrato.CCA_FechaFin).format("YYYY-MM-DD");

        $('#fechaInicio').val(fechaInicio);
        $('#fechaFin').val(fechaFin);

        $('#costeHoraF').val(contrato.CCA_CosteHoraF);
        $('#costeHoraD').val(contrato.CCA_CosteHoraD);
        $('#costeHoraG').val(contrato.CCA_CosteHoraG);
        $('#costeHoraS').val(contrato.CCA_CosteHoraS);
        $('#precioGuardia').val(contrato.CCA_PrecioGuardia);
        $('#tarifaF').val(contrato.CCA_TAR_Id_F);
        $('#tarifaD').val(contrato.CCA_TAR_Id_D);
        $('#tarifaG').val(contrato.CCA_TAR_Id_G);
        $('#tarifaS').val(contrato.CCA_TAR_Id_S);

        $('#modalEditar').modal('show');
    });
}

async function abrirModalExcluidas(button) {
    const contrato = JSON.parse(button.getAttribute('data-contrato'));
    const ccaId = contrato.CCA_Id;

    $('#exCcaId').val(ccaId);
    $('#modalExcluidasLabel').text(`Empresas excluidas de guardia`);

    try {
        mostrarDivCargando();

        // 1) Cargar empresas (para el combo)
        const empresas = await $.ajax({
            url: AppConfig.urls.ObtenerEmpresasCombo,
            type: 'GET',
            dataType: 'json'
        });

        // 2) Cargar excluidas actuales
        const excluidas = await $.ajax({
            url: AppConfig.urls.ObtenerExcluidasGuardia,
            type: 'GET',
            data: { ccaId },
            dataType: 'json'
        });

        // 3) Pintar combo (excluye ya añadidas)
        const yaExcluidasIds = new Set(excluidas.map(e => e.EMP_Id));
        const $sel = $('#selEmpresaExcluida').empty().append('<option value="">Seleccione empresa</option>');
        empresas.forEach(e => {
            if (!yaExcluidasIds.has(e.EMP_Id)) {
                $sel.append(`<option value="${e.EMP_Id}">${e.EMP_Nombre}</option>`);
            }
        });

        // 4) Pintar tabla excluidas
        renderTablaExcluidas(excluidas);

        $('#modalExcluidas').modal('show');
    } catch (xhr) {
        const msg = obtenerMensajeErrorAjax(xhr);
        registrarErrorjQuery(xhr.status, msg);
    } finally {
        ocultarDivCargando();
    }
}

function renderTablaExcluidas(filas) {
    const $tbody = $('#tableExcluidas tbody').empty();
    if (!filas || !filas.length) {
        $tbody.append(`<tr><td colspan="2" class="text-muted">No hay empresas excluidas para este contrato.</td></tr>`);
        return;
    }
    filas.forEach(e => {
        const tr = `
      <tr>
        <td>${e.EMP_Nombre}</td>
        <td class="text-end">
          <button type="button" class="btn btn-sm btn-outline-danger"
                  onclick="eliminarExcluida(${e.CEE_CCA_Id}, ${e.EMP_Id})">
            <i class="bi bi-x-circle"></i> Quitar
          </button>
        </td>
      </tr>`;
        $tbody.append(tr);
    });
}

function cargarCombosTarifas(callback) {
    const tipos = [Tipo.CANTIDAD_FIJA];

    $.ajax({
        url: AppConfig.urls.ObtenerTareasCombo,
        type: 'GET',
        dataType: 'json',
        data: { listaTiposTarea: tipos },
        traditional: true,
        success: function (tareas) {
            const combos = ['#tarifaF', '#tarifaD', '#tarifaG', '#tarifaS'];
            combos.forEach(selector => {
                const $select = $(selector)
                    .empty()
                    .append('<option value="">Seleccione Tarea</option>');
                tareas.forEach(t => {
                    $select.append(`<option value="${t.TAR_Id}">${t.TAR_Nombre}</option>`);
                });
            });
            if (callback) callback();
        },
        error: function (xhr) {
            const msg = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, msg);
        }
    });
}

async function eliminarContratoAjax(idContrato) {
    try {
        const response = await $.ajax({
            url: AppConfig.urls.EliminarContratoCAU,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ idContrato: idContrato }),
            dataType: 'json'
        });

        if (response.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#tableContratosCAU').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast("No se puede eliminar el contrato.", TipoToast.Warning);
        }
    } catch (error) {
        registrarErrorjQuery(error.status || "", error.message || error);
    }
}

function eliminarContrato(button) {
    const contrato = JSON.parse(button.getAttribute('data-contrato'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar el contrato con ID '${contrato.CCA_Id}'?`,
        onConfirmar: () => eliminarContratoAjax(contrato.CCA_Id)
    });
}

async function eliminarExcluida(ccaId, empId) {
    try {
        mostrarDivCargando();

        const resp = await $.ajax({
            url: AppConfig.urls.EliminarExcluidaGuardia,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ ccaId: parseInt(ccaId), empId: parseInt(empId) }),
            dataType: 'json'
        });

        if (!resp.success) {
            mostrarToast(resp.message || 'No se pudo quitar la exclusión.', TipoToast.Warning);
            return;
        }

        // refrescar tabla
        const excluidas = await $.ajax({
            url: AppConfig.urls.ObtenerExcluidasGuardia,
            type: 'GET',
            data: { ccaId },
            dataType: 'json'
        });
        renderTablaExcluidas(excluidas);

        // devolver la empresa al combo
        const empresas = await $.ajax({
            url: AppConfig.urls.ObtenerEmpresasCombo,
            type: 'GET',
            dataType: 'json'
        });
        const idsActuales = new Set(excluidas.map(e => e.EMP_Id));
        const $sel = $('#selEmpresaExcluida').empty().append('<option value="">Seleccione empresa</option>');
        empresas.forEach(e => {
            if (!idsActuales.has(e.EMP_Id)) {
                $sel.append(`<option value="${e.EMP_Id}">${e.EMP_Nombre}</option>`);
            }
        });

        mostrarToast('Exclusión eliminada.', TipoToast.Success);
    } catch (xhr) {
        const msg = obtenerMensajeErrorAjax(xhr);
        registrarErrorjQuery(xhr.status, msg);
    } finally {
        ocultarDivCargando();
    }
}

$(document).ready(function () {
    let tableId = "tableContratosCAU";
    let storageKey = "datatable_filters_" + tableId;

    VerificarSesionActiva(OpcionMenu.ContratosCAU).then(() => {
        establecerFiltros();
    });

    $('#btnNuevo').on('click', function () {
        $('#idContrato').val('');
        $('#fechaInicio').val('');
        $('#fechaFin').val('');
        $('#costeHoraF').val('');
        $('#costeHoraD').val('');
        $('#costeHoraG').val('');
        $('#costeHoraS').val('');
        $('#precioGuardia').val('');

        cargarCombosTarifas(() => {
            $('#modalEditar').modal('show');
        });
    });

    $('#btnGuardarContrato').on('click', function (e) {
        e.preventDefault();

        // 1) Limpiar estilos previos
        const campos = [
            { sel: '#fechaInicio', name: 'Fecha Inicio' },
            { sel: '#fechaFin', name: 'Fecha Fin' },
            { sel: '#costeHoraF', name: 'Coste Hora Fuera de alcance' },
            { sel: '#costeHoraD', name: 'Coste Hora Dentro de alcance' },
            { sel: '#costeHoraG', name: 'Coste Hora Guardia ' },
            { sel: '#costeHoraS', name: 'Coste Hora Software ' },
            { sel: '#precioGuardia', name: 'Precio Guardia' },
            { sel: '#tarifaF', name: 'Tarifa Fuera de alcance' },
            { sel: '#tarifaD', name: 'Tarifa Dentro de alcance' },
            { sel: '#tarifaG', name: 'Tarifa Guardia ' },
            { sel: '#tarifaS', name: 'Tarifa Software ' }
        ];
        campos.forEach(c => $(c.sel).removeClass('is-invalid'));

        // 2) Validar obligatorios
        const invalidos = campos
            .filter(c => !$(c.sel).val() || $(c.sel).val().toString().trim() === '')
            .map(c => {
                $(c.sel).addClass('is-invalid');
                return c.name;
            });

        if (invalidos.length) {
            mostrarToast(
                `Los siguientes campos son obligatorios: ${invalidos.join(', ')}`,
                TipoToast.Warning
            );
            return;
        }

        // 3) Construir objeto contrato
        const contrato = {
            CCA_Id: $('#idContrato').val(),
            CCA_FechaInicio: $('#fechaInicio').val(),
            CCA_FechaFin: $('#fechaFin').val(),
            CCA_CosteHoraF: $('#costeHoraF').val().replace(".", ","),
            CCA_CosteHoraD: $('#costeHoraD').val().replace(".", ","),
            CCA_CosteHoraG: $('#costeHoraG').val().replace(".", ","),
            CCA_CosteHoraS: $('#costeHoraS').val().replace(".", ","),
            CCA_PrecioGuardia: $('#precioGuardia').val().replace(".", ","),
            CCA_TAR_Id_F: $('#tarifaF').val(),
            CCA_TAR_Id_D: $('#tarifaD').val(),
            CCA_TAR_Id_G: $('#tarifaG').val(),
            CCA_TAR_Id_S: $('#tarifaS').val()
        };

        // 4) Enviar al servidor
        $.ajax({
            url: AppConfig.urls.GuardarContratoCAU,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(contrato),
            dataType: 'json'
        })
            .done(function (response) {
                if (response.success) {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                    $('#modalEditar').modal('hide');
                    $('#tableContratosCAU').DataTable().ajax.reload(null, false);
                } else {
                    mostrarToast("Error al guardar el contrato.", TipoToast.Error);
                }
            })
            .fail(function (xhr) {
                registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
            });
    });

    function establecerFiltros() {
        const savedFilters = localStorage.getItem(storageKey);
        if (savedFilters) {
            const filtros = JSON.parse(savedFilters);
            $("#formBuscar").val(filtros.general);
        }
        ObtenerContratosCAU();
    }

    function guardarFiltros() {
        const filtroActual = {
            general: $("#formBuscar").val()
        };
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }

    function ObtenerContratosCAU() {
        const tablaDatos = inicializarDataTable('#tableContratosCAU', {
            ajax: {
                url: AppConfig.urls.ObtenerContratosCAU,
                type: 'GET',
                dataSrc: ''
            },
            columns: [
                {
                    data: 'CCA_FechaInicio',
                    title: 'Fecha Inicio',
                    type: "date", 
                    render: function (data, type, row) {
                        if (!data) return "";

                        let fecha = moment(data); // autodetecta el formato ISO
                        if (!fecha.isValid()) {
                            // Por si acaso viene como texto en español
                            fecha = moment(data, "DD/MM/YYYY HH:mm:ss");
                        }

                        if (!fecha.isValid()) return data;

                        return `<span data-order="${fecha.unix()}">${fecha.format("DD/MM/YYYY")}</span>`;
                    }
                },
                {
                    data: 'CCA_FechaFin',
                    title: 'Fecha Fin',
                    type: "date",
                    render: function (data, type, row) {
                        if (!data) return "";

                        let fecha = moment(data); // autodetecta el formato ISO
                        if (!fecha.isValid()) {
                            // Por si acaso viene como texto en español
                            fecha = moment(data, "DD/MM/YYYY HH:mm:ss");
                        }

                        if (!fecha.isValid()) return data;

                        return `<span data-order="${fecha.unix()}">${fecha.format("DD/MM/YYYY")}</span>`;
                    }
                },
                {
                    data: 'CCA_CosteHoraF',
                    title: 'Coste Hora F',
                    className: 'dt-type-numeric-with-decimal',
                    render: formatMoney
                },
                {
                    data: 'CCA_CosteHoraD',
                    title: 'Coste Hora D',
                    className: 'dt-type-numeric-with-decimal',
                    render: formatMoney
                },
                {
                    data: 'CCA_CosteHoraG',
                    title: 'Coste Hora G',
                    className: 'dt-type-numeric-with-decimal',
                    render: formatMoney
                },
                {
                    data: 'CCA_CosteHoraS',
                    title: 'Coste Hora S',
                    className: 'dt-type-numeric-with-decimal',
                    render: formatMoney
                },
                {
                    data: 'CCA_PrecioGuardia',
                    title: 'Precio Guardia',
                    className: 'dt-type-numeric-with-decimal',
                    render: formatMoney
                },
                {
                    className: 'td-btn',
                    data: null,
                    orderable: false,
                    width: "160px", // <-- dale ancho también aquí
                    render: function (data, type, row) {
                        const contratoJson = JSON.stringify(row).replace(/"/g, "&quot;");
                        return `<div class="btn-group" role="group">
                            <button type="button" class="btn btn-icon btn-outline-secondary me-2 btn-editar"
                                    data-contrato="${contratoJson}"
                                    onclick="abrirModalContrato(this)">
                              <i class="bi bi-pencil-square" title="Editar"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-outline-secondary me-2"
                                    data-contrato="${contratoJson}"
                                    onclick="abrirModalExcluidas(this)">
                              <i class="bi bi-building-dash" title="Excluir empresas de guardia"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-outline-secondary me-2 btn-eliminar"
                                    data-contrato="${contratoJson}"
                                    onclick="eliminarContrato(this)">
                              <i class="bi bi-trash" title="Eliminar"></i>
                            </button></div>`;
                    }
                }
            ]
        }, [], 'export_contratos');

        $(window).resize(() => tablaDatos.columns.adjust().draw());

        $("#formBuscar").on("keyup input", function () {
            tablaDatos.search(this.value, false, false).draw();
            guardarFiltros();
        });
    }

    $('#btnAgregarExcluida').on('click', async function () {
        const ccaId = $('#exCcaId').val();
        const empId = $('#selEmpresaExcluida').val();

        if (!empId) {
            mostrarToast('Seleccione una empresa.', TipoToast.Warning);
            return;
        }

        try {
            mostrarDivCargando();
            const resp = await $.ajax({
                url: AppConfig.urls.AgregarExcluidaGuardia,
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify({ ccaId: parseInt(ccaId), empId: parseInt(empId) }),
                dataType: 'json'
            });

            if (!resp.success) {
                mostrarToast(resp.message || 'No se pudo añadir la exclusión.', TipoToast.Warning);
                return;
            }

            // refrescar tabla y combo
            const excluidas = await $.ajax({
                url: AppConfig.urls.ObtenerExcluidasGuardia,
                type: 'GET',
                data: { ccaId },
                dataType: 'json'
            });
            renderTablaExcluidas(excluidas);

            // quitar del combo la empresa recién añadida
            $(`#selEmpresaExcluida option[value="${empId}"]`).remove();
            $('#selEmpresaExcluida').val('');

            mostrarToast('Empresa excluida de guardia añadida.', TipoToast.Success);
        } catch (xhr) {
            const msg = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, msg);
        } finally {
            ocultarDivCargando();
        }
    });
});
