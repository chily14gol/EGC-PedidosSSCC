function abrirModal(button) {
    let producto = JSON.parse(button.getAttribute('data-producto'));

    $('#productoId').val(producto.PR3_Id);
    $('#productoNombre').val(producto.PR3_Nombre);
    $('#productoActivo').prop('checked', producto.PR3_Activo);
    $('#productoNombre').removeClass('is-invalid');

    $('#modalProducto').modal('show');
}

async function eliminarProductoAjax(idProducto) {
    try {
        const response = await $.ajax({
            url: AppConfig.urls.EliminarProducto,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ idProducto: idProducto }),
            dataType: 'json'
        });

        if (response.success) {
            sessionStorage.removeItem('combo_productos_d365');
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#table').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast("No se puede eliminar el producto. Tiene relaciones", TipoToast.Warning);
        }
    } catch (error) {
        registrarErrorjQuery(error.status || "", error.message || error);
    }
}

function eliminarProducto(button) {
    let producto = JSON.parse(button.getAttribute('data-producto'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar el producto '${producto.PR3_Nombre}'?`,
        onConfirmar: () => eliminarProductoAjax(producto.PR3_Id)
    });
}

$(document).ready(function () {
    let tableId = "tablaProductos";
    let storageKey = "datatable_filters_" + tableId;

    VerificarSesionActiva(OpcionMenu.ProductosD365).then(() => {
        establecerFiltros();
    });

    $('#btnNuevo').on('click', function () {
        $('#productoId').val('');
        $('#productoNombre').val('');
        $('#productoActivo').prop('checked', false);

        $('#productoNombre').removeClass('is-invalid');

        $('#modalProducto').modal('show');
    });

    $('#btnGuardar').on('click', function () {
        let nombreProducto = $('#productoNombre').val().trim();

        let camposInvalidos = [];

        if (!nombreProducto) {
            $('#productoNombre').addClass('is-invalid');
            camposInvalidos.push("Descripción");
        } else {
            $('#productoNombre').removeClass('is-invalid');
        }

        // Si hay campos inválidos, mostrar mensaje y detener ejecución
        if (camposInvalidos.length > 0) {
            mostrarToast(`@Resources.Toast_CamposObligatoriosGeneral`, TipoToast.Warning);
            return;
        }

        let producto = {
            PR3_Id: $('#productoId').val(),
            PR3_Nombre: nombreProducto,
            PR3_Activo: $('#productoActivo').is(':checked')
        };

        $.ajax({
            url: AppConfig.urls.GuardarProducto,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(producto),
            dataType: 'json'
        })
            .done(function (response) {
                if (response.success) {
                    sessionStorage.removeItem('combo_productos_d365');
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                    $('#modalProducto').modal('hide');

                    let tabla = $('#table').DataTable();
                    tabla.ajax.reload(null, false);
                } else {
                    mostrarToast(response.message || "Error al guardar", TipoToast.Error);
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

        ObtenerProductosD365();
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

    function ObtenerProductosD365() {
        let columnasConFiltro = [1, 2];
        let tablaDatos = inicializarDataTable('#table', {
            ajax: {
                url: AppConfig.urls.ObtenerProductosD365,
                type: 'GET',
                dataSrc: ''
            },
            columns: [
                { data: 'PR3_Id', title: 'Id' },
                { data: 'PR3_Nombre', title: 'Descripción' },
                // Columna oculta con el valor real
                {
                    data: 'PR3_Activo',
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
                                data-id="${row.PR3_Id}" disabled
                                ${row.PR3_Activo ? 'checked' : ''} />
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
                                    data-producto="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                    data-*
                                    onclick="abrirModal(this)">
                                <i class="bi bi-pencil-square" title="Editar"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
                                    data-producto="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                    onclick="eliminarProducto(this)">
                                <i class="bi bi-trash" title="Eliminar"></i>
                            </button>`;
                    }
                }
            ],
        }, columnasConFiltro, 'export_productos');

        $(window).resize(function () {
            tablaDatos.columns.adjust().draw();
        });

        $("#formBuscar").on("keyup input", function () {
            tablaDatos.search(this.value, false, false).draw();
            //tablaDatos.responsive.rebuild();
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