function abrirModalEstado(button) { 
    const estado = JSON.parse(button.getAttribute('data-estado'));

    $('#estadoId').val(estado.ETK_Id);
    $('#estadoNombre').val(estado.ETK_Nombre);
    $('#estadoNombre').removeClass('is-invalid');

    $('#modalEstadoTicket').modal('show');
}

async function eliminarEstadoAjax(idEstado) {
    try {
        const response = await $.ajax({
            url: AppConfig.urls.EliminarEstadoTicket,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ idEstado: idEstado }),
            dataType: 'json'
        });

        if (response.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#tablaEstados').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast("No se puede eliminar el estado.", TipoToast.Warning);
        }
    } catch (error) {
        registrarErrorjQuery(error.status || "", error.message || error);
    }
}

function eliminarEstado(button) {
    const estado = JSON.parse(button.getAttribute('data-estado'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar el estado '${estado.ETK_Nombre}'?`,
        onConfirmar: () => eliminarEstadoAjax(estado.ETK_Id)
    });
}

$(document).ready(function () {
    let tableId = "tablaEstados";
    let storageKey = "datatable_filters_" + tableId;

    VerificarSesionActiva(OpcionMenu.EstadosTicket).then(() => {
        establecerFiltros();
    });

    $('#btnNuevo').on('click', function () {
        $('#estadoId').val('');
        $('#estadoNombre').val('');
        $('#estadoNombre').removeClass('is-invalid');
        $('#modalEstadoTicket').modal('show');
    });

    $('#btnGuardarEstado').on('click', function () {
        const nombre = $('#estadoNombre').val().trim();
        let camposInvalidos = [];

        if (!nombre) {
            $('#estadoNombre').addClass('is-invalid');
            camposInvalidos.push("Nombre");
        } else {
            $('#estadoNombre').removeClass('is-invalid');
        }

        if (camposInvalidos.length > 0) {
            mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
            return;
        }

        const estado = {
            ETK_Id: $('#estadoId').val(),
            ETK_Nombre: nombre
        };

        $.ajax({
            url: AppConfig.urls.GuardarEstadoTicket,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(estado),
            dataType: 'json'
        })
            .done(function (response) {
                if (response.success) {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                    $('#modalEstadoTicket').modal('hide');
                    const tableSelector = `#${tableId}`;
                    $(tableSelector).DataTable().ajax.reload(null, false);
                } else {
                    mostrarToast(response.message || "Error al guardar el estado ticket.", TipoToast.Error);
                }
            })
            .fail(function (xhr) {
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
            });
    });

    function establecerFiltros() {
        const savedFilters = localStorage.getItem(storageKey);
        if (savedFilters) {
            const filtros = JSON.parse(savedFilters);
            $("#formBuscar").val(filtros.general);
        }
        ObtenerEstadosTicket();
    }

    function guardarFiltros() {
        const filtroActual = {
            general: $("#formBuscar").val()
        };
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }

    function ObtenerEstadosTicket() {
        const tableSelector = `#${tableId}`;
        const tablaDatos = inicializarDataTable(tableSelector, {
            ajax: {
                url: AppConfig.urls.ObtenerEstadosTicket,
                type: 'GET',
                dataSrc: ''
            },
            columns: [
                { data: 'ETK_Id', title: 'Id' },
                { data: 'ETK_Nombre', title: 'Nombre' },
                {
                    className: 'td-btn',
                    data: null,
                    title: '<span class="sReader">Acción</span>',
                    responsivePriority: 2,
                    orderable: false,
                    render: function (data, type, row) {
                        return `
                            <button type="button" class="btn btn-icon btn-editar btn-outline-secondary"
                                    data-estado="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                    onclick="abrirModalEstado(this)">
                                <i class="bi bi-pencil-square" title="Editar"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
                                    data-estado="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                    onclick="eliminarEstado(this)">
                                <i class="bi bi-trash" title="Eliminar"></i>
                            </button>`;
                    }
                }
            ]
        }, [], 'export_estados');

        $(window).resize(() => tablaDatos.columns.adjust().draw());

        $("#formBuscar").on("keyup input", function () {
            tablaDatos.search(this.value, false, false).draw();
            guardarFiltros();
        });
    }

    $('#modalEstadoTicket').on('hidden.bs.modal', function () {
        $('#estadoId').val('');
        $('#estadoNombre').val('');
        $('#estadoNombre').removeClass('is-invalid');
    });

    $('#modalEstadoTicket').modal({
        backdrop: 'static', // Evita que se cierre al hacer clic fuera
        keyboard: false // Evita que se cierre con la tecla "Esc"
    });
});
