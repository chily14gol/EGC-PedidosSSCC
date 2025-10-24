function editarOficina(button) {
    let oficina = JSON.parse(button.getAttribute('data-oficina'));

    $('#idOficina').val(oficina.OFI_Id);
    $('#nombre').val(oficina.OFI_Nombre);
    $('#nombreDA').val(oficina.OFI_NombreDA);

    $('#nombre').removeClass('is-invalid');

    $('#modalEditarLabel').text('Editar Oficina');
    $('#modalOficina').modal('show');
}

function eliminarOficina(button) {
    let oficina = JSON.parse(button.getAttribute('data-oficina'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar la oficina '${oficina.OFI_Nombre}'?`,
        onConfirmar: function () {
            $.ajax({
                url: AppConfig.urls.EliminarOficina,
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify({ idOficina: oficina.OFI_Id }),
                dataType: 'json'
            })
                .done(function (response) {
                    if (response.success) {
                        mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);

                        let tabla = $('#tableOficinas').DataTable();
                        tabla.ajax.reload(null, false);
                    } else {
                        registrarErrorjQuery(response.status, response.message);
                    }
                })
                .fail(function (xhr, status, error) {
                    const mensaje = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, mensaje);
                });
        }
    });
}

function validarCampoTexto(selector, campo) {
    const val = $(selector).val().trim();
    if (!val) {
        $(selector).addClass('is-invalid');
        return campo;
    }
    $(selector).removeClass('is-invalid');
    return null;
}

const guardarOficina = async () => {
    let idOficina = $('#idOficina').val();
    let nombre = $('#nombre').val().trim();
    let nombreDA = $('#nombreDA').val().trim();

    const camposInvalidos = [
        validarCampoTexto('#nombre', 'Nombre'),
        validarCampoTexto('#nombreDA', 'Nombre DA')
    ].filter(Boolean);

    if (camposInvalidos.length > 0) {
        mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
        return;
    }

    const oficina = {
        OFI_Id: idOficina,
        OFI_Nombre: nombre,
        OFI_NombreDA: nombreDA
    };

    try {
        const response = await $.ajax({
            url: AppConfig.urls.GuardarOficina,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(oficina),
            dataType: 'json'
        });

        if (response.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#modalOficina').modal('hide');

            let tabla = $('#tableOficinas').DataTable();
            tabla.ajax.reload(null, false);
        } else {
            mostrarToast("Ya existe una oficina con el mismo Nombre o Nombre DA", TipoToast.Warning);
        }
    } catch (xhr) {
        const mensaje = obtenerMensajeErrorAjax(xhr);
        registrarErrorjQuery(xhr.status, mensaje);
    }
};

$(document).ready(function () {
    const tableId = "tablaOficinas";
    const storageKey = "datatable_filters_" + tableId;

    VerificarSesionActiva(OpcionMenu.Oficinas).then(() => {
        establecerFiltros();
    });

    function establecerFiltros() {
        let savedFilters = localStorage.getItem(storageKey);
        let hasFilters = false;

        if (savedFilters) {
            savedFilters = JSON.parse(savedFilters);
            $("#formBuscar").val(savedFilters.general);
            $("#filterNombre").val(savedFilters.nombre || "");
            $("#filterNombreDA").val(savedFilters.nombreDA || "");

            let { general, ...otrosFiltros } = savedFilters;
            hasFilters = Object.values(otrosFiltros).some(value => value);
        }

        ObtenerOficinas();

        setTimeout(function () {
            let table = $('#tableOficinas').DataTable();
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
        let filtroActual = {
            general: $("#formBuscar").val(),
            nombre: $('#filterNombre').val(),
            nombreDA: $('#filterNombreDA').val()
        };
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }

    function ObtenerOficinas() {
        let columnasConFiltro = [];
        let tablaDatos = inicializarDataTable('#tableOficinas', {
            ajax: {
                url: AppConfig.urls.ObtenerOficinas,
                type: 'GET',
                dataSrc: '',
                error: function (xhr, status, error) {
                    const mensaje = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, mensaje);
                }
            },
            columns: [
                { data: 'OFI_Nombre', title: 'Nombre' },
                { data: 'OFI_NombreDA', title: 'Nombre DA' },
                {
                    className: 'td-btn',
                    data: null,
                    title: '<span class="sReader">Acción</span>',
                    responsivePriority: 2,
                    orderable: false,
                    render: function (data, type, row) {
                        const json = JSON.stringify(row).replace(/"/g, "&quot;");
                        return `<div class="btn-group" role="group">
                            <button type="button" class="btn btn-icon btn-editar btn-outline-secondary me-2" title="Editar"
                                data-oficina="${json}"
                                onclick="editarOficina(this)">
                                <i class="bi bi-pencil-square"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary" title="Eliminar"
                                data-oficina="${json}"
                                onclick="eliminarOficina(this)">
                                <i class="bi bi-trash"></i>
                            </button>
                        </div>`;
                    }
                }
            ]
        }, columnasConFiltro, 'export_oficinas');

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

    $('#btnNuevaOficina').on('click', function () {
        $('#idOficina').val('-1');
        $('#nombre').val('');
        $('#nombreDA').val('');

        $('#nombre').removeClass('is-invalid');

        $('#modalEditarLabel').text('Agregar Oficina');
        $('#modalOficina').modal('show');
    });

    $('#btnBuscar').on('click', function () {
        let nombre = $('#filterNombre').val();
        let nombreDA = $('#filterNombreDA').val();

        let table = $('#tableOficinas').DataTable();
        table.columns(0).search(nombre).draw();
        table.columns(1).search(nombreDA).draw();

        guardarFiltros();
    });

    $('#modalOficina').modal({
        backdrop: 'static', // Evita que se cierre al hacer clic fuera
        keyboard: false // Evita que se cierre con la tecla "Esc"
    });
});