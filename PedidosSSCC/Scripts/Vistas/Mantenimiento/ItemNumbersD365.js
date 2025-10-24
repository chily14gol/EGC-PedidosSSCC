function abrirModal(button) { 
    let itemNumber = JSON.parse(button.getAttribute('data-itemNumber'));

    $('#itemNumberId').val(itemNumber.IN3_Id);
    $('#descripcion').val(itemNumber.IN3_Nombre);
    $('#activo').prop('checked', itemNumber.IN3_Activo);

    $('#descripcion').removeClass('is-invalid');

    $('#modalEditar').modal('show');
}

async function eliminarItemNumberAjax(idItemNumber) {
    try {
        const response = await $.ajax({
            url: AppConfig.urls.EliminarItemNumber,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ idItemNumber: idItemNumber }),
            dataType: 'json'
        });

        if (response.success) {
            sessionStorage.removeItem('combo_item_number');
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#table').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast("No se puede eliminar el item number. Tiene relaciones", TipoToast.Warning);
        }
    } catch (error) {
        registrarErrorjQuery(error.status || "", error.message || error);
    }
}

function eliminarItemNumber(button) {
    let itemNumber = JSON.parse(button.getAttribute('data-itemNumber'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar el item number '${itemNumber.IN3_Nombre}'?`,
        onConfirmar: () => eliminarItemNumberAjax(producto.IN3_Id)
    });
}

$(document).ready(function () {
    let tableId = "tablaItemNumbers";
    let storageKey = "datatable_filters_" + tableId;

    VerificarSesionActiva(OpcionMenu.ItemNumbersD365).then(() => {
        establecerFiltros();
    });

    $('#btnNuevo').on('click', function () {
        $('#itemNumberId').val('');
        $('#descripcion').val('');
        $('#activo').prop('checked', false);

        $('#descripcion').removeClass('is-invalid');

        $('#modalEditar').modal('show');
    });

    $('#btnGuardar').on('click', function () {
        let nombreItemNumber = $('#descripcion').val().trim();

        let camposInvalidos = [];

        if (!nombreItemNumber) {
            $('#descripcion').addClass('is-invalid');
            camposInvalidos.push("Descripción");
        } else {
            $('#descripcion').removeClass('is-invalid');
        }

        // Si hay campos inválidos, mostrar mensaje y detener ejecución
        if (camposInvalidos.length > 0) {
            mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
            return;
        }

        let itemNumber = {
            IN3_Id: $('#itemNumberId').val(),
            IN3_Nombre: nombreItemNumber,
            IN3_Activo: $('#activo').is(':checked')
        };

        $.ajax({
            url: AppConfig.urls.GuardarItemNumber,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(itemNumber),
            dataType: 'json'
        })
        .done(function (response) {
            if (response.success) {
                sessionStorage.removeItem('combo_item_number');
                mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                $('#modalEditar').modal('hide');

                let tabla = $('#table').DataTable();
                tabla.ajax.reload(null, false);
            } else {
                mostrarToast("Error al guardar el item number.", TipoToast.Error);
            }
        })
        .fail(function (xhr, status, error) {
            const mensaje = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, mensaje);
        });
    });

    function establecerFiltros() {
        let savedFilters = localStorage.getItem(storageKey);

        if (savedFilters) {
            savedFilters = JSON.parse(savedFilters);
            $("#formBuscar").val(savedFilters.general);
        }

        if (savedFilters?.activo !== undefined) {
            $('#filterActivo').val(savedFilters.activo);
        }

        ObtenerItemNumbersD365();
    }

    function guardarFiltros() {
        let general = $("#formBuscar").val();
        let activo = $('#filterActivo').val();
        let filtroActual = {
            general: general,
            activo: activo
        };

        // Guardar en localStorage
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }

    function ObtenerItemNumbersD365() {
        let columnasConFiltro = [];
        let tablaDatos = inicializarDataTable('#table', {
            ajax: {
                url: AppConfig.urls.ObtenerItemNumbersD365,
                type: 'GET',
                dataSrc: ''
            },
            columns: [
                { data: 'IN3_Id', title: 'Id' },
                { data: 'IN3_Nombre', title: 'Descripción' },
                // Columna oculta con el valor real
                {
                    data: 'IN3_Activo',
                    visible: false, // oculta al usuario
                    searchable: true // permite filtrar por el valor real
                },
                {
                    className: 'dt-center ico-status',
                    data: null,
                    title: 'Activo',
                    orderable: false,
                    render: function (data, type, row, meta) {
                        return `
                            <input type="checkbox" class="form-check-input"
                                id="checkbox-${meta.row}"
                                data-index="${meta.row}"
                                data-id="${row.IN3_Id}" disabled
                                ${row.IN3_Activo ? 'checked' : ''} />
                        `;
                    }
                },
                {
                    className: 'td-btn',
                    data: null,
                    title: '<span class="sReader">Acción</span>',
                    responsivePriority: 2,
                    orderable: false,
                    render: function (data, type, row) {
                        return `
                                <button type="button" class="btn btn-icon btn-editar btn-outline-secondary"
                                        data-itemNumber="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                        onclick="abrirModal(this)">
                                    <i class="bi bi-pencil-square" title="Editar"></i>
                                </button>
                                <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
                                        data-itemNumber="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                        onclick="eliminarItemNumber(this)">
                                    <i class="bi bi-trash" title="Eliminar"></i>
                                </button>`;
                    }
                }
            ],
        }, columnasConFiltro, 'export_itemNumbers');

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

        $('#filterActivo').on('change', function () {
            let val = $(this).val();
            $('#table').DataTable().column(2).search(val).draw(); // Columna 2 = Activo
            guardarFiltros();
        });
    }
});