function abrirModalTipo(button) {
    const tipo = JSON.parse(button.getAttribute('data-tipo'));

    $('#tipoId').val(tipo.TTK_Id);
    $('#tipoNombre').val(tipo.TTK_Nombre);
    $('#tipoNombre').removeClass('is-invalid');

    $('#modalTipoTicket').modal('show');
}

async function eliminarTipoAjax(idTipo) {
    try {
        const response = await $.ajax({
            url: AppConfig.urls.EliminarTipoTicket,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ idTipo: idTipo }),
            dataType: 'json'
        });

        if (response.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#table').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast("No se puede eliminar el tipo.", TipoToast.Warning);
        }
    } catch (error) {
        registrarErrorjQuery(error.status || "", error.message || error);
    }
}

function eliminarTipo(button) {
    const tipo = JSON.parse(button.getAttribute('data-tipo'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar el tipo '${tipo.TTK_Nombre}'?`,
        onConfirmar: () => eliminarTipoAjax(tipo.TTK_Id)
    });
}

$(document).ready(function () {
    let tableId = "tablaTipos";
    let storageKey = "datatable_filters_" + tableId;

    VerificarSesionActiva(OpcionMenu.TiposTicket).then(() => {
        establecerFiltros();
    });

    $('#btnNuevo').on('click', function () {
        $('#tipoId').val('');
        $('#tipoNombre').val('');
        $('#tipoNombre').removeClass('is-invalid');
        $('#modalTipoTicket').modal('show');
    });

    $('#btnGuardarTipo').on('click', function () {
        const nombre = $('#tipoNombre').val().trim();
        if (!nombre) {
            $('#tipoNombre').addClass('is-invalid');
            mostrarToast("Campo obligatorio sin rellenar", TipoToast.Warning);
            return;
        }

        const tipo = {
            TTK_Id: $('#tipoId').val(),
            TTK_Nombre: nombre
        };

        $.ajax({
            url: AppConfig.urls.GuardarTipoTicket,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(tipo),
            dataType: 'json'
        })
            .done(function (response) {
                if (response.success) {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                    $('#modalTipoTicket').modal('hide');
                    $('#table').DataTable().ajax.reload(null, false);
                } else {
                    mostrarToast("Error al guardar el tipo.", TipoToast.Error);
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
        ObtenerTiposTicket();
    }

    function guardarFiltros() {
        const filtroActual = {
            general: $("#formBuscar").val()
        };
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }

    function ObtenerTiposTicket() {
        const tablaDatos = inicializarDataTable('#table', {
            ajax: {
                url: AppConfig.urls.ObtenerTiposTicket,
                type: 'GET',
                dataSrc: ''
            },
            columns: [
                { data: 'TTK_Id', title: 'Id' },
                { data: 'TTK_Nombre', title: 'Nombre' },
                {
                    className: 'td-btn',
                    data: null,
                    title: '<span class="sReader">Acción</span>',
                    responsivePriority: 2,
                    orderable: false,
                    render: function (data, type, row) {
                        return `
                            <button type="button" class="btn btn-icon btn-editar btn-outline-secondary"
                                    data-tipo="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                    onclick="abrirModalTipo(this)">
                                <i class="bi bi-pencil-square" title="Editar"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
                                    data-tipo="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                    onclick="eliminarTipo(this)">
                                <i class="bi bi-trash" title="Eliminar"></i>
                            </button>`;
                    }
                }
            ]
        }, [], 'export_tipos');

        $(window).resize(() => tablaDatos.columns.adjust().draw());

        $("#formBuscar").on("keyup input", function () {
            tablaDatos.search(this.value, false, false).draw();
            guardarFiltros();
        });
    }
});
