function abrirModalValidacion(button) {
    const validacion = JSON.parse(button.getAttribute('data-validacion'));

    $('#validacionId').val(validacion.VTK_Id);
    $('#validacionNombre').val(validacion.VTK_Nombre);
    $('#validacionNombre').removeClass('is-invalid');

    $('#modalValidacionTicket').modal('show');
}

async function eliminarValidacionAjax(idValidacion) {
    try {
        const response = await $.ajax({
            url: AppConfig.urls.EliminarValidacionTicket,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ idValidacion }),
            dataType: 'json'
        });

        if (response.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#table').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast("No se puede eliminar la validación.", TipoToast.Warning);
        }
    } catch (error) {
        registrarErrorjQuery(error.status || "", error.message || error);
    }
}

function eliminarValidacion(button) {
    const validacion = JSON.parse(button.getAttribute('data-validacion'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar la validación '${validacion.VTK_Nombre}'?`,
        onConfirmar: () => eliminarValidacionAjax(validacion.VTK_Id)
    });
}

$(document).ready(function () {
    let tableId = "tablaValidacionesTicket";
    let storageKey = "datatable_filters_" + tableId;

    VerificarSesionActiva(OpcionMenu.ValidacionesTicket).then(() => {
        establecerFiltros();
    });

    $('#btnNuevo').on('click', function () {
        $('#validacionId').val('');
        $('#validacionNombre').val('');
        $('#validacionNombre').removeClass('is-invalid');
        $('#modalValidacionTicket').modal('show');
    });

    $('#btnGuardarValidacion').on('click', function () {
        const nombre = $('#validacionNombre').val().trim();
        let camposInvalidos = [];

        if (!nombre) {
            $('#validacionNombre').addClass('is-invalid');
            camposInvalidos.push("Nombre");
        } else {
            $('#validacionNombre').removeClass('is-invalid');
        }

        if (camposInvalidos.length > 0) {
            mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
            return;
        }

        const validacion = {
            VTK_Id: $('#validacionId').val(),
            VTK_Nombre: nombre
        };

        $.ajax({
            url: AppConfig.urls.GuardarValidacionTicket,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(validacion),
            dataType: 'json'
        })
            .done(function (response) {
                if (response.success) {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                    $('#modalValidacionTicket').modal('hide');
                    $('#table').DataTable().ajax.reload(null, false);
                } else {
                    mostrarToast("Error al guardar la validación.", TipoToast.Error);
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
        ObtenerValidacionesTicket();
    }

    function guardarFiltros() {
        const filtroActual = {
            general: $("#formBuscar").val()
        };
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }

    function ObtenerValidacionesTicket() {
        const tablaDatos = inicializarDataTable('#table', {
            ajax: {
                url: AppConfig.urls.ObtenerValidacionesTicket,
                type: 'GET',
                dataSrc: ''
            },
            columns: [
                { data: 'VTK_Id', title: 'Id' },
                { data: 'VTK_Nombre', title: 'Nombre' },
                {
                    className: 'td-btn',
                    data: null,
                    title: '<span class="sReader">Acción</span>',
                    responsivePriority: 2,
                    orderable: false,
                    render: function (data, type, row) {
                        return `
                            <button type="button" class="btn btn-icon btn-editar btn-outline-secondary"
                                    data-validacion="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                    onclick="abrirModalValidacion(this)">
                                <i class="bi bi-pencil-square" title="Editar"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
                                    data-validacion="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                    onclick="eliminarValidacion(this)">
                                <i class="bi bi-trash" title="Eliminar"></i>
                            </button>`;
                    }
                }
            ]
        }, [], 'export_validaciones_ticket');

        $(window).resize(() => tablaDatos.columns.adjust().draw());

        $("#formBuscar").on("keyup input", function () {
            tablaDatos.search(this.value, false, false).draw();
            guardarFiltros();
        });
    }
});
