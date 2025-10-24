async function editarEmpresa(button) {
    let empresa = JSON.parse(button.getAttribute('data-empresa'));

    $('#idEmpresa').val(empresa.EMP_Id);
    $('#nombre').val(empresa.EMP_Nombre).prop('disabled', false);
    $('#nombreDA').val(empresa.EMP_NombreDA).prop('disabled', false);
    $('#razonSocial').val(empresa.EMP_RazonSocial).prop('disabled', false);
    $('#cif').val(empresa.EMP_CIF).prop('disabled', false);
    $('#direccion').val(empresa.EMP_Direccion).prop('disabled', false);
    $('#aprobador').val(empresa.EMP_PER_Id_AprobadorDefault).prop('disabled', false);
    $('#lineaNegocio').val(empresa.EMP_LNE_Id).prop('disabled', false);
    $('#codigoAPIKA').val(empresa.EMP_CodigoAPIKA).prop('disabled', false);
    $('#codigoD365').val(empresa.EMP_CodigoD365).prop('disabled', false);
    $('#formaPagoAPIKA').val(empresa.EMP_FPA_CodigoAPIKA).prop('disabled', false);
    $('#formaPagoCodigoD365').val(empresa.EMP_FPA_D365).prop('disabled', false);
    $('#tipoCliente').val(empresa.EMP_TipoCliente).prop('disabled', false);
    $('#grupoD365').val(empresa.EMP_EGrupoD365).prop('disabled', false);
    $('#empresaFacturar').val(empresa.EMP_EmpresaFacturar_Id);
    $('#excluidaGuardia').prop('checked', empresa.EMP_ExcluidaGuardia === true);
    $('#facturaVDF').val(empresa.EMP_NumFacturaVDF).prop('disabled', false);

    await CargarComboEmpresaAprobadores(empresa.EMP_Id);
    await CargarComboGruposGuardia();
    quitarMarcasError();

    $('#grupoGuardia').val(empresa.EMP_GRG_Id || '').prop('disabled', false);
    $('#modalEditarLabel').text('Editar Empresa');
    $('#modalEditar').modal('show');
}

const eliminarEmpresa = (button) => {
    const empresa = JSON.parse(button.getAttribute('data-empresa'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar la empresa '${empresa.EMP_Nombre}'?`,
        onConfirmar: async () => {
            try {
                const response = await fetch(AppConfig.urls.EliminarEmpresa, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json; charset=utf-8'
                    },
                    body: JSON.stringify({ idEmpresa: empresa.EMP_Id })
                });

                const data = await response.json();

                if (data.success) {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                    const tabla = $('#table').DataTable();
                    tabla.ajax.reload(null, false);
                } else {
                    mostrarToast("No se puede eliminar la empresa. Tiene pedidos asociados", TipoToast.Warning);
                }

            } catch (error) {
                registrarErrorjQuery("Error", error.message || error);
            }
        }
    });
};
function guardarEmpresa() {
    let campos = [
        "nombre", "nombreDA", "razonSocial", "cif", "aprobador", "lineaNegocio",
        "codigoAPIKA", "codigoD365", "formaPagoAPIKA", "formaPagoCodigoD365",
        "tipoCliente", "grupoD365"
    ];

    let data = {};
    let camposInvalidos = [];

    campos.forEach(id => {
        const valor = $(`#${id}`).val();
        data[id] = valor;

        if (!valor) {
            $(`#${id}`).addClass('is-invalid');
            camposInvalidos.push(id);
        } else {
            $(`#${id}`).removeClass('is-invalid');
        }
    });

    if (camposInvalidos.length > 0) {
        mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
        return;
    }

    let objEmpresa = {
        EMP_Id: $('#idEmpresa').val(),
        EMP_Nombre: data.nombre,
        EMP_NombreDA: data.nombreDA,
        EMP_RazonSocial: data.razonSocial,
        EMP_Direccion: $('#direccion').val(),
        EMP_CIF: data.cif,
        EMP_PER_Id_AprobadorDefault: data.aprobador,
        EMP_LNE_Id: data.lineaNegocio,
        EMP_CodigoAPIKA: data.codigoAPIKA,
        EMP_CodigoD365: data.codigoD365,
        EMP_FPA_CodigoAPIKA: data.formaPagoAPIKA,
        EMP_FPA_D365: data.formaPagoCodigoD365,
        EMP_TipoCliente: data.tipoCliente,
        EMP_EGrupoD365: data.grupoD365,
        EMP_EmpresaFacturar_Id: $('#empresaFacturar').val(),
        EMP_ExcluidaGuardia: $('#excluidaGuardia').is(':checked'),
        EMP_GRG_Id: $('#grupoGuardia').val() || null,
        EMP_NumFacturaVDF: $('#facturaVDF').val()
    };

    let sendData = {
        empresa: objEmpresa,
        idsAprobadores: $('#aprobadoresAdicionales').val() || []
    };

    $.ajax({
        url: AppConfig.urls.GuardarEmpresa,
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify(sendData),
        dataType: 'json'
    })
        .done(function (response) {
            if (response.success) {
                mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                $('#modalEditar').modal('hide');
                $('#table').DataTable().ajax.reload(null, false);
            } else {
                registrarErrorjQuery("", null);
            }
        })
        .fail(function (xhr, status, error) {
            const mensaje = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, mensaje);
        });
}

async function CargarComboEmpresaAprobadores(idEmpresa) {
    try {
        const params = new URLSearchParams({ idEmpresa });

        const response = await fetch(`${AppConfig.urls.ObtenerComboEmpresaAprobadores}?${params}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json; charset=utf-8'
            }
        });

        const data = await response.json();

        const $select = $('#aprobadoresAdicionales');
        $select.empty();

        data.forEach(item => {
            const option = new Option(item.ApellidosNombre, item.EMA_PER_Id, true, true);
            $select.append(option);
        });

        $select.trigger('change'); // actualizar Select2
    } catch (error) {
        registrarErrorjQuery('fetch', error.message);
    }
}

async function CargarComboGruposGuardia() {
    try {
        const data = await $.ajax({
            url: AppConfig.urls.ObtenerComboGruposGuardia,
            type: 'GET',
            dataType: 'json'
        });

        const $select = $('#grupoGuardia');
        $select.empty().append('<option value="">Seleccione un grupo</option>');
        data.forEach(item => {
            $select.append(`<option value="${item.GRG_Id}">${item.GRG_Nombre}</option>`);
        });
    } catch (error) {
        registrarErrorjQuery('fetch', error.message);
    }
}
function quitarMarcasError() {
    $('#nombre').removeClass('is-invalid');
    $('#razonSocial').removeClass('is-invalid');
    $('#cif').removeClass('is-invalid');
    $('#aprobador').removeClass('is-invalid');
    $('#lineaNegocio').removeClass('is-invalid');
    $('#codigoAPIKA').removeClass('is-invalid');
    $('#codigoD365').removeClass('is-invalid');
    $('#formaPagoAPIKA').removeClass('is-invalid');
    $('#formaPagoCodigoD365').removeClass('is-invalid');
    $('#tipoCliente').removeClass('is-invalid');
    $('#grupoD365').removeClass('is-invalid');
    $('#error-cif').addClass('d-none');
    $('#facturaVDF').removeClass('is-invalid');
}

let currentDeptoId = null;

async function abrirModalDepartamentos(idEmpresa, nombreEmpresa) {
    currentEmpresaId = idEmpresa;
    $('#dep-empresa-nombre').text(nombreEmpresa);
    ocultarPanelEdicion();
    await cargarDepartamentos();
    $('#modalDepartamentos').modal('show');
}
function mostrarPanelEdicion(id, nombre) {
    currentDeptoId = id || null;
    $('#txtDeptoNombre').val(nombre || '');
    $('#panelDeptoEdicion').slideDown();
    $('#panelDeptoNuevoBtn').hide();
}
function ocultarPanelEdicion() {
    currentDeptoId = null;
    $('#txtDeptoNombre').val('');
    $('#panelDeptoEdicion').slideUp();
    $('#panelDeptoNuevoBtn').show();
}

async function cargarDepartamentos() {
    // Destruye y vuelve a crear la tabla cada vez
    const tabla = inicializarDataTable('#tblDepartamentos', {
        paging: false,
        searching: false,
        info: false,
        ordering: false,
        dom: 't',
        ajax: {
            url: AppConfig.urls.ObtenerDepartamentosEmpresa,
            type: 'GET',
            data: function () {
                return { idEmpresa: currentEmpresaId };
            },
            dataSrc: ''
        },
        columns: [
            {
                data: 'EDE_Nombre',
                title: 'Nombre'
            },
            {
                data: null,
                className: 'td-btn text-end',
                orderable: false,
                title: '<span class="sReader">Acción</span>',
                render: function (data, type, row) {
                    // row.EDE_Id y row.EDE_Nombre
                    const nombreEsc = row.EDE_Nombre.replace(/'/g, "\\'");
                    return `
            <div class="btn-group" role="group">
              <button
                class="btn btn-sm btn-warning"
                title="Editar"
                onclick="mostrarPanelEdicion(${row.EDE_Id}, '${nombreEsc}')">
                <i class="bi bi-pencil"></i>
              </button>
              <button
                class="btn btn-sm btn-danger"
                title="Eliminar"
                onclick="eliminarDepartamento(${row.EDE_Id})">
                <i class="bi bi-trash"></i>
              </button>
            </div>`;
                }
            }
        ]
    }, [], 'export_departamentos');

    // Al cambiar tamaño, reajusta
    $(window).resize(() => tabla.columns.adjust().draw());
}

function eliminarDepartamento(id) {
    mostrarAlertaConfirmacion({
        titulo: '¿Eliminar departamento?',
        onConfirmar: async () => {
            try {
                const resp = await $.ajax({
                    url: AppConfig.urls.EliminarDepartamentoEmpresa,
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ id })
                });
                if (resp.success) {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                    cargarDepartamentos();
                } else {
                    mostrarToast(resp.message, TipoToast.Warning);
                }
            } catch (e) {
                console.error(e);
            }
        }
    });
}

$(document).ready(function () {
    let storageKey = Filtros.Empresas;

    async function CargarComboPersonas() {
        try {
            let response = await $.ajax({
                url: AppConfig.urls.ObtenerPersonas,
                type: 'GET',
                dataType: 'json'
            });

            let $select = $('#aprobador');
            if ($select.length === 0) {
                console.error("El select #aprobador no está en el DOM");
                return;
            }

            $select.empty().append('<option value="">Seleccione una persona</option>');

            response.forEach(item => {
                $select.append(`<option value="${item.PER_Id}">${item.ApellidosNombre}</option>`);
            });

            return response;
        } catch (error) {
            console.error('Error al cargar personas:', error);
            throw error;
        }
    }

    async function CargarComboLineasNegocio() {
        try {
            const data = await $.ajax({
                url: AppConfig.urls.ObtenerComboLineasNegocio,
                type: 'GET',
                dataType: 'json'
            });

            const $select = $('#lineaNegocio');
            $select.empty();
            $select.append('<option value="">Seleccione una línea de negocio</option>');

            data.forEach(item => {
                $select.append(`<option value="${item.LNI_LNE_Id}">${item.LNI_Nombre}</option>`);
            });

            return data;
        } catch (error) {
            registrarErrorjQuery(error.status || 'error', error.message || error);
            throw error;
        }
    }

    async function CargarComboEmpresasFacturar() {
        try {
            const data = await $.ajax({
                url: AppConfig.urls.ObtenerComboEmpresas,
                type: 'GET',
                dataType: 'json'
            });

            const $select = $('#empresaFacturar');
            $select.empty();
            $select.append('<option value="">Seleccione una empresa</option>');

            data.forEach(item => {
                $select.append(`<option value="${item.EMP_Id}">${item.EMP_Nombre}</option>`);
            });

            return data;
        } catch (error) {
            registrarErrorjQuery(error.status || 'error', error.message || error);
            throw error;
        }
    }

    async function inicializarCombos() {
        try {
            await CargarComboPersonas();
            await CargarComboLineasNegocio();
            await CargarComboEmpresasFacturar();

            establecerFiltros();
            InicializarSelect2();
        } catch (error) {
            console.error("Error al cargar combos:", error);
        }
    }

    VerificarSesionActiva(OpcionMenu.Empresas).then(() => {
        inicializarCombos();
    });
    function InicializarSelect2() {
        if (!$.fn.select2) {
            console.error("Select2 no está disponible");
            return;
        }

        let $select = $('#aprobadoresAdicionales');
        if ($select.length === 0) {
            console.error("El select #aprobadoresAdicionales no está en el DOM");
            return;
        }

        $select.select2({
            placeholder: "Seleccione aprobadores...",
            allowClear: true,
            tags: true,
            dropdownParent: $('#modalEditar'),
            width: '100%',
            ajax: {
                url: AppConfig.urls.ObtenerPersonas,
                dataType: 'json',
                delay: 250,
                processResults: function (data) {
                    return {
                        results: data.map(item => ({ id: item.PER_Id, text: item.ApellidosNombre }))
                    };
                },
                cache: true
            }
        });
    }
    function establecerFiltros() {
        // Recuperar filtros previos si existen
        let savedFilters = localStorage.getItem(storageKey);
        let hasFilters = false;

        if (savedFilters) {
            savedFilters = JSON.parse(savedFilters);
            $("#formBuscar").val(savedFilters.general);
            $("#filterNombre").val(savedFilters.nombre || "");
            $("#filterNombreDA").val(savedFilters.nombreDA || "");
            $("#filterRazonSocial").val(savedFilters.razonSocial || "");
            $("#filterCIF").val(savedFilters.cif || "");
            $("#filterDireccion").val(savedFilters.direccion || "");
            $("#filterLineaNegocio").val(savedFilters.lineaNegocio || "");

            // Comprobar si al menos un filtro tiene valor
            let { general, ...otrosFiltros } = savedFilters;
            hasFilters = Object.values(otrosFiltros).some(value => value !== "" && value !== null && value !== undefined);
        }

        ObtenerEmpresas();

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
        let nombre = $("#filterNombre").val();
        let nombreDA = $("#filterNombreDA").val();
        let razonSocial = $('#filterRazonSocial').val();
        let cif = $('#filterCIF').val();
        let direccion = $('#filterDireccion').val();
        let lineaNegocio = $('#filterLineaNegocio').val();

        let filtroActual = {
            general: general,
            nombre: nombre,
            nombreDA: nombreDA,
            razonSocial: razonSocial,
            cif: cif,
            direccion: direccion,
            lineaNegocio: lineaNegocio
        };

        // Guardar en localStorage
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }
    function ObtenerEmpresas() {
        let columnasConFiltro = [];
        let tablaDatos = inicializarDataTable('#table', {
            ajax: {
                url: AppConfig.urls.ObtenerEmpresas,
                type: 'GET',
                dataSrc: ''
            },
            columns: [
                { data: 'EMP_Nombre', title: 'Nombre' },
                { data: 'EMP_NombreDA', title: 'Nombre Directorio Activo' },
                { data: 'EMP_RazonSocial', title: 'Razón Social' },
                { data: 'EMP_CIF', title: 'CIF' },
                { data: 'EMP_Direccion', title: 'Dirección' },
                { data: 'LineaNegocioNombre', title: 'Línea Negocio' },
                {
                    className: 'td-btn',
                    data: null,
                    title: '<span class="sReader">Acción</span>',
                    responsivePriority: 2,
                    orderable: false,
                    render: function (data, type, row) {
                        const jsRow = JSON.stringify(row).replace(/"/g, '&quot;');
                        return `<div class="btn-group" role="group">
                            <button class="btn btn-icon me-2 btn-editar"
                                    data-empresa="${jsRow}"
                                    onclick="editarEmpresa(this)">
                                <i class="bi bi-pencil-square" title="Editar"></i>
                            </button>
                            <button class="btn btn-icon me-2 btn-dept"
                                    data-empresa="${jsRow}"
                                    onclick="abrirModalDepartamentos(${row.EMP_Id}, '${htmlEscape(row.EMP_Nombre)}')"> 
                               <i class="bi bi-folder" title="Departamentos"></i>
                            </button>
                            <button class="btn btn-icon me-2 btn-eliminar"
                                data-empresa="${jsRow}"
                                onclick="eliminarEmpresa(this)">
                                <i class="bi bi-trash" title="Eliminar"></i>
                            </button>
                            </div>`;
                    }
                }
            ]
        }, columnasConFiltro, 'export_empresas');

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
        $('#idEmpresa').val('');
        $('#nombre').val('').prop('disabled', false);
        $('#nombreDA').val('').prop('disabled', false);
        $('#razonSocial').val('').prop('disabled', false);
        $('#cif').val('').prop('disabled', false);
        $('#direccion').val('').prop('disabled', false);
        $('#aprobador').val('').prop('disabled', false);
        $('#lineaNegocio').val('').prop('disabled', false);
        $('#codigoAPIKA').val('').prop('disabled', false);
        $('#codigoD365').val('').prop('disabled', false);
        $('#formaPagoAPIKA').val('').prop('disabled', false);
        $('#formaPagoCodigoD365').val('').prop('disabled', false);
        $('#tipoCliente').val('').prop('disabled', false);
        $('#grupoD365').val('').prop('disabled', false);
        $('#empresaFacturar').val('').prop('disabled', false);
        $('#aprobadoresAdicionales').val(null).prop('disabled', false).trigger('change');
        $('#excluidaGuardia').prop('checked', false);
        $('#grupoGuardia').val('').prop('disabled', false);
        $('#facturaVDF').val('').prop('disabled', false);

        quitarMarcasError();

        $('#modalEditarLabel').text('Agregar Empresa'); // Cambiar título del modal
        $('#modalEditar').modal('show'); // Mostrar modal
    });

    $('#btnBuscar').on('click', function () {
        let nombre = $('#filterNombre').val();
        let nombreDA = $('#filterNombreDA').val();
        let razonSocial = $('#filterRazonSocial').val();
        let cif = $('#filterCIF').val();
        let direccion = $('#filterDireccion').val();
        let lineaNegocio = $('#filterLineaNegocio').val();

        let table = $('#table').DataTable();
        table.columns(0).search(nombre).draw();
        table.columns(1).search(nombreDA).draw();
        table.columns(2).search(razonSocial).draw();
        table.columns(3).search(cif).draw();
        table.columns(4).search(direccion).draw();
        table.columns(5).search(lineaNegocio).draw();

        guardarFiltros();
    });

    $('#btnAddDepartamento').on('click', () => {
        mostrarPanelEdicion(0, '');
    });

    $('#btnCancelarDepto').on('click', () => {
        ocultarPanelEdicion();
    });

    $('#btnGuardarDepto').on('click', async () => {
        const nombre = $('#txtDeptoNombre').val().trim();
        if (!nombre) {
            alert('El nombre no puede quedar vacío.');
            return;
        }
        const payload = {
            EDE_Id: currentDeptoId || 0,
            EDE_EMP_Id: currentEmpresaId,
            EDE_Nombre: nombre
        };
        try {
            const resp = await $.ajax({
                url: AppConfig.urls.GuardarDepartamentoEmpresa,
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(payload),
                dataType: 'json'
            });
            if (resp.success) {
                mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                ocultarPanelEdicion();
                cargarDepartamentos();
            } else {
                mostrarToast(resp.message || 'Error al guardar.', TipoToast.Warning);
            }
        } catch (e) {
            console.error(e);
        }
    });
});