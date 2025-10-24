function editarConfiguracion(button) {
    let config = JSON.parse(button.getAttribute('data-config')); //Test smart 4

    $('#idConfiguracion').val(config.CFG_Id);
    $('#descripcion').val(config.CFG_Descripcion).prop('disabled', false);
    $('#valor').val(config.CFG_Valor).prop('disabled', false);

    $('#descripcion').removeClass('is-invalid');
    $('#valor').removeClass('is-invalid');

    $('#modalEditarLabel').text('Editar Configuración');
    $('#modalEditar').modal('show');
}

function eliminarConfiguracion(button) {
    let config = JSON.parse(button.getAttribute('data-config'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar la configuración '${config.CFG_Descripcion}'?`,
        onConfirmar: async () => {
            try {
                const response = await $.ajax({
                    url: AppConfig.urls.eliminarConfiguracion,
                    type: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    data: JSON.stringify({ idConfiguracion: config.CFG_Id }),
                    dataType: 'json'
                });

                if (response.success) {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);

                    let tabla = $('#table').DataTable();
                    tabla.ajax.reload(null, false);
                } else {
                    registrarErrorjQuery(response.status, response.message);
                }

            } catch (error) {
                registrarErrorjQuery(error.status || 'Error', error.message || error);
            }
        }
    });
}

const guardarConfiguracion = async () => {
    try {
        let idConfiguracion = $('#idConfiguracion').val();
        let descripcion = $('#descripcion').val();
        let valor = $('#valor').val();

        let camposInvalidos = [];

        if (!descripcion) {
            $('#descripcion').addClass('is-invalid');
            camposInvalidos.push("descripcion");
        } else {
            $('#descripcion').removeClass('is-invalid');
        }

        if (!valor) {
            $('#valor').addClass('is-invalid');
            camposInvalidos.push("valor");
        } else {
            $('#valor').removeClass('is-invalid');
        }

        if (camposInvalidos.length > 0) {
            mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
            return;
        }

        // Si idConfiguracion es 1, verificar que 'valor' sea un número entero
        if (idConfiguracion == "1" && (!/^\d+$/.test(valor))) {
            mostrarToast("El valor debe ser un año.", TipoToast.Warning);
            return;
        }

        let tabla = $('#table').DataTable();
        let configExiste = false;

        tabla.rows().every(function () {
            let data = this.data();

            //Si es la misma config en edición, ignorarlo
            if (data.CFG_Id != idConfiguracion && data.CFG_Descripcion.toLowerCase() === descripcion.toLowerCase()) {
                configExiste = true;
                return false;
            }
        });

        if (configExiste) {
            $('#descripcion').addClass('is-invalid');
            mostrarToast("La descripción ya existe en el sistema.", TipoToast.Error);
            return;
        } else {
            $('#descripcion').removeClass('is-invalid');
        }

        let config = {
            CFG_Id: (idConfiguracion == "" ? 0 : idConfiguracion),
            CFG_Descripcion: descripcion,
            CFG_Valor: valor
        };

        const response = await $.ajax({
            url: AppConfig.urls.guardarConfiguracion,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(config),
            dataType: 'json'
        });

        if (response.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#modalEditar').modal('hide');

            const tabla = $('#table').DataTable();
            tabla.ajax.reload(null, false);
        } else {
            registrarErrorjQuery(response.status, response.message);
        }

    } catch (error) {
        registrarErrorjQuery("Error", error.message || error);
    }
};

$(document).ready(function () {
    VerificarSesionActiva(OpcionMenu.Configuraciones).then(() => {
        ObtenerConfiguraciones();
    });

    function ObtenerConfiguraciones() {
        let columnasConFiltro = [];
        let tablaDatos = inicializarDataTable('#table', {
            ajax: {
                url: AppConfig.urls.obtenerConfiguraciones,
                type: 'GET',
                dataSrc: ''
            },
            columns: [
                { data: 'CFG_Id', title: 'Id' },
                { data: 'CFG_Descripcion', title: 'Descripción' },
                { data: 'CFG_Valor', title: 'Valor' },
                {
                    className: 'td-btn',
                    data: null,
                    title: '<span class="sReader">Acción</span>',
                    responsivePriority: 2,
                    orderable: false,
                    render: function (data, type, row) {
                        return `
                                        <button type="button" class="btn btn-icon btn-editar btn-outline-secondary"
                                            data-config="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                            onclick="editarConfiguracion(this)">
                                            <i class="bi bi-pencil-square" title="Editar"></i>
                                        </button>
                                        <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
                                            data-config="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                            onclick="eliminarConfiguracion(this)">
                                            <i class="bi bi-trash" title="Eliminar"></i>
                                        </button>`;
                    }
                }
            ]
        }, columnasConFiltro, 'export_config');

        $(window).resize(function () {
            tablaDatos.columns.adjust().draw();
        });

        $("#formBuscar").on("keyup input", function () {
            tablaDatos.search(this.value, false, false).draw();
            tablaDatos.responsive.rebuild();
            tablaDatos.responsive.recalc();
            tablaDatos.columns.adjust();
            tablaDatos.draw(false);
            guardarFiltros();
        });
    }

    $('#btnNuevo').on('click', function () {
        $('#idConfiguracion').val('');
        $('#descripcion').val('').prop('disabled', false);
        $('#valor').val('').prop('disabled', false);

        $('#descripcion').removeClass('is-invalid');
        $('#valor').removeClass('is-invalid');

        $('#modalEditarLabel').text('Agregar Configuración');
        $('#modalEditar').modal('show');
    });
});
