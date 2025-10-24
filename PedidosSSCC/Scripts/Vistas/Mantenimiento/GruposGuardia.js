function abrirModalGrupo(button) {
    const grupo = JSON.parse(button.getAttribute('data-grupo'));

    $('#grupoId').val(grupo.GRG_Id);
    $('#grupoNombre').val(grupo.GRG_Nombre);
    $('#grupoNombre').removeClass('is-invalid');

    $('#modalGrupoGuardia').modal('show');
}

async function eliminarGrupoAjax(idGrupo) {
    try {
        const response = await $.ajax({
            url: AppConfig.urls.EliminarGrupoGuardia,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ idGrupo }),
            dataType: 'json'
        });

        if (response.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#table').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast("No se puede eliminar el grupo.", TipoToast.Warning);
        }
    } catch (error) {
        registrarErrorjQuery(error.status || "", error.message || error);
    }
}

function eliminarGrupo(button) {
    const grupo = JSON.parse(button.getAttribute('data-grupo'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar el grupo '${grupo.GRG_Nombre}'?`,
        onConfirmar: () => eliminarGrupoAjax(grupo.GRG_Id)
    });
}

$(document).ready(function () {
    let tableId = "tablaGruposGuardia";
    let storageKey = "datatable_filters_" + tableId;

    VerificarSesionActiva(OpcionMenu.GruposGuardia).then(() => {
        establecerFiltros();
    });

    $('#btnNuevo').on('click', function () {
        $('#grupoId').val('');
        $('#grupoNombre').val('');
        $('#grupoNombre').removeClass('is-invalid');
        $('#modalGrupoGuardia').modal('show');
    });

    $('#btnGuardarGrupo').on('click', function () {
        const nombre = $('#grupoNombre').val().trim();
        let camposInvalidos = [];

        if (!nombre) {
            $('#grupoNombre').addClass('is-invalid');
            camposInvalidos.push("Nombre");
        } else {
            $('#grupoNombre').removeClass('is-invalid');
        }

        if (camposInvalidos.length > 0) {
            mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
            return;
        }

        const grupo = {
            GRG_Id: $('#grupoId').val(),
            GRG_Nombre: nombre
        };

        $.ajax({
            url: AppConfig.urls.GuardarGrupoGuardia,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(grupo),
            dataType: 'json'
        })
            .done(function (response) {
                if (response.success) {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                    $('#modalGrupoGuardia').modal('hide');
                    $('#table').DataTable().ajax.reload(null, false);
                } else {
                    mostrarToast("Error al guardar el grupo.", TipoToast.Error);
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
        ObtenerGruposGuardia();
    }

    function guardarFiltros() {
        const filtroActual = {
            general: $("#formBuscar").val()
        };
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }

    function ObtenerGruposGuardia() {
        const tablaDatos = inicializarDataTable('#table', {
            ajax: {
                url: AppConfig.urls.ObtenerGruposGuardia,
                type: 'GET',
                dataSrc: ''
            },
            columns: [
                { data: 'GRG_Id', title: 'Id' },
                { data: 'GRG_Nombre', title: 'Nombre' },
                {
                    className: 'td-btn',
                    data: null,
                    title: '<span class="sReader">Acción</span>',
                    responsivePriority: 2,
                    orderable: false,
                    render: function (data, type, row) {
                        return `
                            <button type="button" class="btn btn-icon btn-editar btn-outline-secondary"
                                    data-grupo="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                    onclick="abrirModalGrupo(this)">
                                <i class="bi bi-pencil-square" title="Editar"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
                                    data-grupo="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                    onclick="eliminarGrupo(this)">
                                <i class="bi bi-trash" title="Eliminar"></i>
                            </button>`;
                    }
                }
            ]
        }, [], 'export_grupos_guardia');

        $(window).resize(() => tablaDatos.columns.adjust().draw());

        $("#formBuscar").on("keyup input", function () {
            tablaDatos.search(this.value, false, false).draw();
            guardarFiltros();
        });
    }
});
