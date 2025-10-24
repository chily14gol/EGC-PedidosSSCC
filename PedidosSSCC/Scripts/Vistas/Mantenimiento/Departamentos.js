function editarDepartamento(button) {
    resetCamposModal();

    let departamento = JSON.parse(button.getAttribute('data-departamento'));

    $('#idDepartamento').val(departamento.DEP_Id); // Guardamos el ID original (para edición)
    $('#codigo').val(departamento.DEP_Codigo).prop('disabled', false);
    $('#nombre').val(departamento.DEP_Nombre).prop('disabled', false);
    $('#responsable').val(departamento.DEP_PER_Id_Responsable).prop('disabled', false);
    $('#codigoD365').val(departamento.DEP_CodigoD365).prop('disabled', false);

    $('#modalEditarLabel').text('Editar Departamento'); // Cambiar título
    $('#modalEditar').modal('show');
}

function resetCamposModal() {
    $('#codigo, #nombre, #responsable, #codigoD365')
        .val('')
        .prop('disabled', false)
        .removeClass('is-invalid');
}

function eliminarDepartamento(button) {
    let departamento = JSON.parse(button.getAttribute('data-departamento'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar el departamento '${departamento.DEP_Codigo}-${departamento.DEP_Nombre}'?`,
        onConfirmar: function () {
            $.ajax({
                url: AppConfig.urls.eliminarDepartamento,
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify({ idDepartamento: departamento.DEP_Id }),
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
                .fail(function (xhr, status, error) {
                    const mensaje = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, mensaje);
                });

        }
    });
}

const guardarDepartamentoAsync = async () => {
    let idDepartamento = $('#idDepartamento').val();
    let codigo = $('#codigo').val();
    let nombre = $('#nombre').val();
    let idResponsable = $('#responsable').val();
    let codigoD365 = $('#codigoD365').val();

    let camposInvalidos = [];
    validarCampo('#codigo', 'Código', camposInvalidos);
    validarCampo('#nombre', 'Nombre', camposInvalidos);
    validarCampo('#responsable', 'Responsable', camposInvalidos);
    validarCampo('#codigoD365', 'Código D365', camposInvalidos);

    if (camposInvalidos.length > 0) {
        mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
        return;
    }

    let tabla = $('#table').DataTable();
    let depExiste = false;

    tabla.rows().every(function () {
        let data = this.data();

        //Si es el mismo departamento en edición, ignorarlo
        if (data.DEP_Id != idDepartamento && data.DEP_Codigo.toLowerCase() === codigo.toLowerCase()) {
            depExiste = true;
            return false;
        }
    });

    if (depExiste) {
        $('#codigo').addClass('is-invalid');
        mostrarToast("El código ya existe en el sistema.", TipoToast.Error);
        return;
    } else {
        $('#codigo').removeClass('is-invalid');
    }

    let departamento = {
        DEP_Id: idDepartamento,
        DEP_Codigo: codigo,
        DEP_Nombre: nombre,
        DEP_PER_Id_Responsable: idResponsable,
        DEP_CodigoD365: codigoD365
    };

    try {
        const response = await $.ajax({
            url: AppConfig.urls.guardarDepartamento,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(departamento),
            dataType: 'json'
        });

        if (response.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#modalEditar').modal('hide');

            let tabla = $('#table').DataTable();
            tabla.ajax.reload(null, false);
        } else {
            registrarErrorjQuery(response.status, response.message);
        }
    } catch (error) {
        const mensaje = obtenerMensajeErrorAjax(error);
        registrarErrorjQuery(error.status || 500, mensaje);
    }
}

$(document).ready(function () {
    let tableId = "tablaDepartamentos";
    let storageKey = "datatable_filters_" + tableId;

    VerificarSesionActiva(OpcionMenu.Departamentos).then(() => {
        Promise.all([CargarComboPersonas()])
            .then(() => {
                establecerFiltros();
            })
            .catch((error) => {
                console.error('Error al cargar los datos:', error);
            });
    });

    function CargarComboPersonas() {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: AppConfig.urls.obtenerPersonas,
                type: 'GET',
                dataType: 'json',
                success: function (data) {
                    let $select = $('#responsable');
                    $select.empty();

                    $.each(data, function (index, item) {
                        $select.append('<option value="' + item.PER_Id + '">' + item.ApellidosNombre + '</option>');
                    });

                    resolve();
                },
                error: function (xhr, status, error) {
                    const mensaje = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, mensaje);
                    reject();
                }
            });
        });
    }

    function establecerFiltros() {
        // Recuperar filtros previos si existen
        let savedFilters = localStorage.getItem(storageKey);
        let hasFilters = false;

        if (savedFilters) {
            savedFilters = JSON.parse(savedFilters);
            $("#formBuscar").val(savedFilters.general);
            $("#filterCodigo").val(savedFilters.codigo || "");
            $("#filterNombre").val(savedFilters.nombre || "");
            $("#filterResponsable").val(savedFilters.responsable || "");
            $("#filterCodigoD365").val(savedFilters.codigoD365 || "");

            // Comprobar si al menos un filtro tiene valor
            let { general, ...otrosFiltros } = savedFilters;
            hasFilters = Object.values(otrosFiltros).some(value => value !== "" && value !== null && value !== undefined);
        }

        cargarTablaDepartamentos();

        setTimeout(function () {
            let table = $('#table').DataTable();
            if (savedFilters?.general) {
                table.search(savedFilters.general, false, false).draw();
            }
        }, 200);

        // Si hay filtros aplicados, mostrar el div de filtros
        if (hasFilters) {
            $("#btnLimpiar").show();
            $(".table-filter").addClass('advance');
            $('#btnAvanzado').html(`Ocultar Filtros`); // Cambiar icono y texto
        }
    }

    function guardarFiltros() {
        let general = $("#formBuscar").val();
        let codigo = $("#filterCodigo").val();
        let nombre = $('#filterNombre').val();
        let responsable = $('#filterResponsable').val();
        let codigoD365 = $('#filterCodigoD365').val();

        let filtroActual = {
            general: general,
            codigo: codigo,
            nombre: nombre,
            responsable: responsable,
            codigoD365: codigoD365
        };

        // Guardar en localStorage
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }

    function cargarTablaDepartamentos() {
        let columnasConFiltro = [];
        let tablaDatos = inicializarDataTable('#table', {
            ajax: {
                url: AppConfig.urls.obtenerDepartamentos,
                type: 'GET',
                dataSrc: ''
            },
            columns: [
                //{ data: 'DEP_Id', title: 'Id' },
                { data: 'DEP_Codigo', title: 'Código' },
                { data: 'DEP_Nombre', title: 'Nombre' },
                { data: 'NombreResponsable', title: 'Responsable' },
                { data: 'DEP_CodigoD365', title: 'Código D365' },
                {
                    className: 'td-btn',
                    data: null,
                    title: '<span class="sReader">Acción</span>',
                    responsivePriority: 2,
                    orderable: false,
                    render: function (data, type, row) {
                        return `
                                    <button type="button" class="btn btn-icon btn-editar btn-outline-secondary"
                                        data-departamento="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                        onclick="editarDepartamento(this)">
                                        <i class="bi bi-pencil-square" title="Editar"></i>
                                    </button>
                                    <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
                                            data-departamento="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                            onclick="eliminarDepartamento(this)">
                                        <i class="bi bi-trash" title="Eliminar"></i>
                                    </button>`;
                    }
                }
            ]
        }, columnasConFiltro, 'export_departamentos');

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
        $('#idDepartamento').val('-1');
        $('#codigo').val('').prop('disabled', false);
        $('#nombre').val('').prop('disabled', false);
        $('#responsable').val('').prop('disabled', false);
        $('#codigoD365').val('').prop('disabled', false);

        resetCamposModal();

        $('#modalEditarLabel').text('Agregar Departamento'); // Cambiar título del modal
        $('#modalEditar').modal('show'); // Mostrar modal
    });

    $('#btnBuscar').on('click', function () {
        let codigo = $('#filterCodigo').val();
        let nombre = $('#filterNombre').val();
        let responsable = $('#filterResponsable').val();
        let codigoD365 = $('#filterCodigoD365').val();

        let table = $('#table').DataTable();
        table.columns(0).search(codigo).draw();
        table.columns(1).search(nombre).draw();
        table.columns(2).search(responsable).draw();
        table.columns(3).search(codigoD365).draw();

        guardarFiltros();
    });
});