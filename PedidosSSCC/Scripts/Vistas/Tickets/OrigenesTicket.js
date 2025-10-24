function abrirModalOrigen(button) { 
    const origen = JSON.parse(button.getAttribute('data-origen'));

    $('#origenId').val(origen.OTK_Id);
    $('#origenNombre').val(origen.OTK_Nombre);
    $('#origenNombre').removeClass('is-invalid');

    $('#modalOrigenTicket').modal('show');
}

async function eliminarOrigenAjax(idOrigen) {
    try {
        const response = await $.ajax({
            url: AppConfig.urls.EliminarOrigenTicket,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ idOrigen }),
            dataType: 'json'
        });

        if (response.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#table').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast("No se puede eliminar el origen.", TipoToast.Warning);
        }
    } catch (error) {
        registrarErrorjQuery(error.status || "", error.message || error);
    }
}

function eliminarOrigen(button) {
    const origen = JSON.parse(button.getAttribute('data-origen'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar el origen '${origen.OTK_Nombre}'?`,
        onConfirmar: () => eliminarOrigenAjax(origen.OTK_Id)
    });
}

$(document).ready(function () {
    const tableId = "tablaOrigenes";
    const storageKey = "datatable_filters_" + tableId;

    VerificarSesionActiva(OpcionMenu.OrigenesTicket).then(() => {
        establecerFiltros();
    });

    $('#btnNuevo').on('click', function () {
        $('#origenId').val('');
        $('#origenNombre').val('');
        $('#origenNombre').removeClass('is-invalid');
        $('#modalOrigenTicket').modal('show');
    });

    $('#btnGuardarOrigen').on('click', function () {
        const nombre = $('#origenNombre').val().trim();
        let camposInvalidos = [];

        if (!nombre) {
            $('#origenNombre').addClass('is-invalid');
            camposInvalidos.push("Nombre");
        } else {
            $('#origenNombre').removeClass('is-invalid');
        }

        if (camposInvalidos.length > 0) {
            mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
            return;
        }

        const origen = {
            OTK_Id: $('#origenId').val(),
            OTK_Nombre: nombre
        };

        $.ajax({
            url: AppConfig.urls.GuardarOrigenTicket,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(origen),
            dataType: 'json'
        })
            .done(function (response) {
                if (response.success) {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                    $('#modalOrigenTicket').modal('hide');
                    $('#table').DataTable().ajax.reload(null, false);
                } else {
                    mostrarToast("Error al guardar el origen.", TipoToast.Error);
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
        ObtenerOrigenesTicket();
    }

    function guardarFiltros() {
        const filtroActual = {
            general: $("#formBuscar").val()
        };
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }

    function ObtenerOrigenesTicket() {
        const tablaDatos = inicializarDataTable('#table', {
            ajax: {
                url: AppConfig.urls.ObtenerOrigenesTicket,
                type: 'GET',
                dataSrc: ''
            },
            columns: [
                { data: 'OTK_Id', title: 'Id' },
                { data: 'OTK_Nombre', title: 'Nombre' },
                {
                    className: 'td-btn',
                    data: null,
                    title: '<span class="sReader">Acción</span>',
                    responsivePriority: 2,
                    orderable: false,
                    render: function (data, type, row) {
                        return `
                            <button type="button" class="btn btn-icon btn-editar btn-outline-secondary"
                                    data-origen="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                    onclick="abrirModalOrigen(this)">
                                <i class="bi bi-pencil-square" title="Editar"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
                                    data-origen="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                    onclick="eliminarOrigen(this)">
                                <i class="bi bi-trash" title="Eliminar"></i>
                            </button>`;
                    }
                }
            ]
        }, [], 'export_origenes');

        $(window).resize(() => tablaDatos.columns.adjust().draw());

        $("#formBuscar").on("keyup input", function () {
            tablaDatos.search(this.value, false, false).draw();
            guardarFiltros();
        });
    }

    $('#modalOrigenTicket').modal({
        backdrop: 'static', // Evita que se cierre al hacer clic fuera
        keyboard: false // Evita que se cierre con la tecla "Esc"
    });
});
