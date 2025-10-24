function editarTipo(button) {
    let tipo = JSON.parse(button.getAttribute('data-tipo'));

    $('#idTipo').val(tipo.TEN_Id);
    $('#nombre').val(tipo.TEN_Nombre);

    $('#nombre').removeClass('is-invalid');

    $('#modalEditarLabel').text('Editar Tipo de Ente');
    $('#modalEditar').modal('show');
}

function eliminarTipo(button) {
    let tipo = JSON.parse(button.getAttribute('data-tipo'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar el tipo '${tipo.TEN_Nombre}'?`,
        onConfirmar: function () {
            $.ajax({
                url: AppConfig.urls.EliminarTipoEnte,
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify({ idTipoEnte: tipo.TEN_Id }),
                dataType: 'json'
            })
                .done(function (response) {
                    if (response.success) {
                        mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                        let tabla = $('#table').DataTable();
                        tabla.ajax.reload(null, false);
                    } else {
                        registrarErrorjQuery(response.status, response.message);
                    }
                })
                .fail(function (xhr) {
                    const mensaje = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, mensaje);
                });
        }
    });
}

const guardarTipo = async () => {
    let id = $('#idTipo').val();
    let nombre = $('#nombre').val().trim();

    if (!nombre) {
        $('#nombre').addClass('is-invalid');
        mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
        return;
    }
    $('#nombre').removeClass('is-invalid');

    const tipo = {
        TEN_Id: id,
        TEN_Nombre: nombre
    };

    try {
        const response = await $.ajax({
            url: AppConfig.urls.GuardarTipoEnte,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(tipo),
            dataType: 'json'
        });

        if (response.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#modalEditar').modal('hide');
            let tabla = $('#table').DataTable();
            tabla.ajax.reload(null, false);
        } else {
            mostrarToast("Ya existe un tipo con ese nombre", TipoToast.Warning);
        }
    } catch (xhr) {
        const mensaje = obtenerMensajeErrorAjax(xhr);
        registrarErrorjQuery(xhr.status, mensaje);
    }
};

$(document).ready(function () {
    const storageKey = Filtros.TiposEnte;

    VerificarSesionActiva(OpcionMenu.TiposEnte).then(() => {
        establecerFiltros();
    });

    function establecerFiltros() {
        let savedFilters = localStorage.getItem(storageKey);
        let hasFilters = false;

        if (savedFilters) {
            savedFilters = JSON.parse(savedFilters);
            $("#formBuscar").val(savedFilters.general);
            $("#filterNombre").val(savedFilters.nombre || "");

            let { general, ...otrosFiltros } = savedFilters;
            hasFilters = Object.values(otrosFiltros).some(value => value);
        }

        ObtenerTipos();

        setTimeout(function () {
            let table = $('#table').DataTable();
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

    function guardarFiltros() {
        const filtroActual = {
            general: $("#formBuscar").val(),
            nombre: $('#filterNombre').val(),
        };
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }

    function ObtenerTipos() {
        let columnasConFiltro = [];
        let tablaDatos = inicializarDataTable('#table', {
            ajax: {
                url: AppConfig.urls.ObtenerTiposEnte,
                type: 'GET',
                dataSrc: '',
                error: function (xhr) {
                    const mensaje = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, mensaje);
                }
            },
            columns: [
                { data: 'TEN_Nombre', title: 'Nombre' },
                {
                    className: 'td-btn',
                    data: null,
                    title: '<span class="sReader">Acción</span>',
                    orderable: false,
                    render: function (data, type, row) {
                        return `
                        <div class="btn-group" role="group">
                            <button type="button" class="btn btn-icon btn-editar btn-outline-secondary me-2" title="Editar"
                                data-tipo="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                onclick="editarTipo(this)">
                                <i class="bi bi-pencil-square"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary" title="Eliminar"
                                data-tipo="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                onclick="eliminarTipo(this)">
                                <i class="bi bi-trash"></i>
                            </button>
                        </div>`;
                    }
                }
            ]
        }, columnasConFiltro, 'export_tiposente');

        $(window).resize(function () {
            tablaDatos.columns.adjust().draw();
        });

        $('#formBuscar').on('keyup', function () {
            tablaDatos.search(this.value, false, false).draw();
            guardarFiltros();
        });
    }

    $('#btnNuevo').on('click', function () {
        $('#idTipo').val('-1');
        $('#nombre').val('');
        $('#nombre').removeClass('is-invalid');
        $('#modalEditarLabel').text('Nuevo Tipo de Ente');
        $('#modalEditar').modal('show');
    });

    $('#btnBuscar').on('click', function () {
        let nombre = $('#filterNombre').val();

        let table = $('#table').DataTable();
        table.columns(0).search(nombre).draw();

        guardarFiltros();
    });
});
