function verTarifas(button) {
    const obj = JSON.parse(button.getAttribute('data-licencia'));
    $('#idLicenciaTarifas').val(obj.LIC_Id);

    // Cargo la tabla de tarifas antes de mostrar nada
    ObtenerTarifas(obj.LIC_Id);

    // Preparo y limpio el formulario
    limpiarFormularioTarifa();

    // Oculto el formulario y muestro el modal
    $('#formTarifas').hide();
    $('#modalTarifas').modal('show');

    // Muestro el botón de "Nueva tarifa"
    $('#btnNuevaTarifa').show();
}

function verExcepciones(button) {
    let obj = JSON.parse(button.getAttribute('data-licencia'));
    limpiarFormularioExcepcion();
    InicializarSelectLicenciasReemplazo();
    ObtenerExcepciones(obj.LIC_Id);
    $('#idLicenciaExcepciones').val(obj.LIC_Id);
    $('#modalExcepciones').modal('show');
}

function limpiarFormularioTarifa() {
    // Fechas
    $('#fechaInicio, #fechaFin').val('').prop('disabled', false);
    // Precios
    $('#precioUnitarioSW, #precioUnitarioAntivirus, #precioUnitarioBackup').val('');
    // El formulario, por defecto, oculto
    $('#formTarifas').hide();
    // Ocultar botón guardar (si usas ID en vez de onclick inline)
    $('#btnGuardarTarifa').hide();
}

function limpiarFormularioExcepcion() {
    $('#idEmpresaOriginal').val('');
    $('#licenciasReemplazo').val(null).trigger('change');
    $('#licenciasReemplazo').prop('disabled', true);
    limpiarCampos(['#empresa', '#correcion']);
}

function ObtenerTarifas(idLicencia) {
    let columnasConFiltro = [];
    let tablaDatos = inicializarDataTable('#tableTarifas', {
        paging: false,
        searching: false,
        info: false,
        ordering: false,
        dom: 't',
        ajax: {
            url: AppConfig.urls.ObtenerTarifas,
            type: 'GET',
            dataSrc: function (json) {
                return json;
            },
            data: { licenciaId: idLicencia },
            dataType: 'json',
        },
        rowId: function (row) {
            return `fila-${row.LIT_LIC_Id}-${row.LIT_FechaInicio?.substring(0, 10)}`;
        },
        columns: [
            { data: 'LIT_FechaInicio', title: 'Fecha Inicio', render: formatDateToDDMMYYYY },
            { data: 'LIT_FechaFin', title: 'Fecha Fin', render: formatDateToDDMMYYYY },
            {
                data: 'LIT_PrecioUnitarioSW',
                title: 'Importe SW',
                className: 'dt-type-numeric-with-decimal',
                render: function (data) {
                    return formatMoney(data);
                }
            },
            {
                data: 'LIT_PrecioUnitarioAntivirus',
                title: 'Importe AV',
                className: 'dt-type-numeric-with-decimal',
                render: function (data) {
                    return formatMoney(data);
                }
            },
            {
                data: 'LIT_PrecioUnitarioBackup',
                title: 'Importe BK',
                className: 'dt-type-numeric-with-decimal',
                render: function (data) {
                    return formatMoney(data);
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
                        <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
                            data-tarifa="${JSON.stringify(row).replace(/"/g, "&quot;")}" onclick="eliminarTarifa(this)">
                            <i class="bi bi-trash" title="Eliminar"></i>
                        </button>`;
                }
            }
        ]
    }, columnasConFiltro, 'export_tarifas');

    $(window).resize(function () {
        tablaDatos.columns.adjust().draw();
    });
}

function ObtenerExcepciones(idLicencia) {
    let columnasConFiltro = [];
    let tablaDatos = inicializarDataTable('#tableExcepciones', {
        paging: false,         // ❌ sin paginación
        searching: false,      // ❌ sin buscador
        info: false,           // ❌ sin texto de "Mostrando X de Y"
        ordering: false,       // ❌ sin ordenación
        dom: 't',              // solo la tabla (sin controles extra)
        ajax: {
            url: AppConfig.urls.ObtenerExcepciones,
            type: 'GET',
            dataSrc: '',
            data: { licenciaId: idLicencia },
            dataType: 'json',
        },
        rowId: function (row) {
            return `fila-${row.LIE_LIC_Id}-${row.LIE_EMP_Id}`;
        },
        columns: [
            { data: 'EmpresaNombre', title: 'Empresa' },
            { data: 'LIE_CorreccionFacturacion', title: 'Corrección Facturación' },
            {
                data: 'LicenciasReemplazoNombres', // o 'LicenciasReemplazoTexto' si usaste string
                title: 'Licencias Reemplazo',
                render: function (data) {
                    if (!data) return '';
                    if (Array.isArray(data)) return data.join(", ");
                    return data;
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
                                data-excepcion="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                onclick="editarExcepcion(this)">
                                <i class="bi bi-pencil-square" title="Editar"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
                                data-excepcion="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                onclick="eliminarExcepcion(this)">
                                <i class="bi bi-trash" title="Eliminar"></i>
                            </button>`;
                }
            }
        ]
    }, columnasConFiltro, 'export_excepciones');

    $(window).resize(function () {
        tablaDatos.columns.adjust().draw();
    });
}

function editarLicencia(button) {
    let licencia = JSON.parse(button.getAttribute('data-licencia'));

    $('#idLicencia').val(licencia.LIC_Id);
    $('#nombre').val(licencia.LIC_Nombre);
    $('#grupo').val(licencia.LIC_MaximoGrupo);
    $('#padre').val(licencia.LIC_LIC_Id_Padre);
    $('#tareaSW').val(licencia.LIC_TAR_Id_SW || '');
    $('#tareaAntivirus').val(licencia.LIC_TAR_Id_Antivirus || '');
    $('#tareaBackup').val(licencia.LIC_TAR_Id_Backup || '');
    $('#nombreMS').val(licencia.LIC_NombreMS || '');
    $('#gestionado').prop('checked', licencia.LIC_Gestionado === true);

    $('#modalEditarLabel').text('Editar Licencia');
    $('#modalEditar').modal('show');

    InicializarSelectTiposEnte();
    cargarComboTiposEnte(licencia.LIC_Id);

    InicializarSelectLicenciasIncompatibles();
    cargarComboLicenciasIncompatibles(licencia.LIC_Id);

    const isHija = licencia.LIC_LIC_Id_Padre != null;
    $('#licenciasIncompatibles').prop('disabled', isHija);
    $('#tiposEnte').prop('disabled', isHija);
}

function editarTarifa(button) {
    let obj = JSON.parse(button.getAttribute('data-tarifa'));
    $('#fechaInicio').val(formatDateInputForDateField(obj.LIT_FechaInicio)).prop('disabled', true);
    $('#fechaFin').val(formatDateInputForDateField(obj.LIT_FechaFin)).prop('disabled', false);

    $('#precioUnitarioSW').val(obj.LIT_PrecioUnitarioSW != null ? obj.LIT_PrecioUnitarioSW.toString().replace('.', ',') : '');
    $('#precioUnitarioAntivirus').val(obj.LIT_PrecioUnitarioAntivirus != null ? obj.LIT_PrecioUnitarioAntivirus.toString().replace('.', ',') : '');
    $('#precioUnitarioBackup').val(obj.LIT_PrecioUnitarioBackup != null ? obj.LIT_PrecioUnitarioBackup.toString().replace('.', ',') : '');

    $('#btnNuevaTarifa').hide();
    $('#btnCancelarTarifa').show();
    $('#btnGuardarTarifa').show();
    $('#formTarifas').slideDown();
}

function editarExcepcion(button) {
    let obj = JSON.parse(button.getAttribute('data-excepcion'));

    $('#licenciasReemplazo').prop('disabled', false); // Activar el select
    InicializarSelectLicenciasReemplazo();
    cargarComboLicenciasReemplazo(obj.LIE_LIC_Id, obj.LIE_EMP_Id);

    $('#idLicenciaExcepciones').val(obj.LIE_LIC_Id);
    $('#idEmpresaOriginal').val(obj.LIE_EMP_Id);
    $('#empresa').val(obj.LIE_EMP_Id);
    $('#correcion').val(obj.LIE_CorreccionFacturacion.toString().replace(".", ","));
}

function editarLineaEsfuerzo(button) {
    const obj = JSON.parse(button.getAttribute('data-esfuerzo'));
    $('#idConceptoEditar').val(obj.LEE_Id);
    $('#empresaEsfuerzo').val(obj.LEE_EMP_Id);
    $('#anyoEsfuerzo').val(obj.LEE_Anyo);
    $('#mesEsfuerzo').val(obj.LEE_Mes);
    $('#unidadesEsfuerzo').val(obj.LEE_Unidades);
    $('#importeUnitarioEsfuerzo').val(obj.LEE_ImporteUnitario);
}

function editarPedidoEsfuerzo(btn) {
    const obj = JSON.parse(btn.getAttribute('data-pedido'));
    $('#fleId').val(obj.FLE_Id);
    $('#pedidoEsfuerzo').val(obj.FLE_FAC_Id);
}

async function guardarLicencia() {
    let tiposSeleccionados = obtenerTiposEnteSeleccionados();
    let licenciasIncompatibles = obtenerLicenciasIncompatiblesSeleccionados();

    let licencia = {
        LIC_Id: $('#idLicencia').val(),
        LIC_Nombre: $('#nombre').val(),
        LIC_NombreMS: $('#nombreMS').val(),
        LIC_MaximoGrupo: $('#grupo').val(),
        LIC_LIC_Id_Padre: $('#padre').val(),
        LIC_TAR_Id_SW: $('#tareaSW').val(),
        LIC_TAR_Id_Antivirus: $('#tareaAntivirus').val(),
        LIC_TAR_Id_Backup: $('#tareaBackup').val(),
        TiposEnte: tiposSeleccionados,
        LicenciasIncompatibles: licenciasIncompatibles,
        LIC_Gestionado: $('#gestionado').is(':checked')
    };

    try {
        const response = await $.ajax({
            url: AppConfig.urls.guardarLicencia,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(licencia),
            dataType: 'json'
        });

        if (response.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#modalEditar').modal('hide');
            $('#table').DataTable().ajax.reload(null, false);
        } else {
            registrarErrorjQuery(response.status, response.message);
        }
    } catch (xhr) {
        const mensaje = obtenerMensajeErrorAjax(xhr);
        registrarErrorjQuery(xhr.status, mensaje);
    }
}

const cancelarTarifa = async () => {
    $('#btnNuevaTarifa').show();
    $('#btnCancelarTarifa').hide();
    $('#btnGuardarTarifa').hide();
    $('#formTarifas').hide();
};

const guardarExcepcion = async () => {
    let idLicencia = $('#idLicenciaExcepciones').val();
    let idEmpresaOriginal = $('#idEmpresaOriginal').val();
    let idEmpresa = $('#empresa').val();
    let correcion = $('#correcion').val();
    let licenciasReemplazoSeleccionadas = obtenerLicenciasReemplazoSeleccionadas();

    let camposInvalidos = [];
    validarCampo('#empresa', 'Empresa', camposInvalidos);
    validarCampo('#correcion', 'Correción', camposInvalidos);

    if (camposInvalidos.length > 0) {
        mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
        return;
    }

    let excepcion = {
        LIE_LIC_Id: idLicencia,
        LIE_EMP_Id_Original: idEmpresaOriginal ? idEmpresaOriginal : null,
        LIE_EMP_Id: idEmpresa,
        LIE_CorreccionFacturacion: correcion,
        LicenciasReemplazo: licenciasReemplazoSeleccionadas
    };

    const response = await $.ajax({
        url: AppConfig.urls.GuardarExcepcion,
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify(excepcion),
        dataType: 'json'
    });

    if (response.success) {
        limpiarFormularioExcepcion();
        mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);

        const filaEditadaId = `fila-${idLicencia}-${idEmpresa}`;
        sessionStorage.setItem("ultimaFilaEditada", filaEditadaId);

        let tabla = $('#tableExcepciones').DataTable();
        tabla.ajax.reload(function () {
            resaltarFilaPorId(sessionStorage.getItem("ultimaFilaEditada"));
        }, false);
    } else {
        limpiarFormularioExcepcion();
        mostrarToast("Ya existe una excepción para esta empresa y licencia.", TipoToast.Warning);
    }
}

function eliminarLicencia(button) {
    let obj = JSON.parse(button.getAttribute('data-licencia'));

    let licencia = {
        LIC_Id: obj.LIC_Id,
        LIC_Nombre: obj.LIC_Nombre,
        LIC_MaximoGrupo: obj.LIC_MaximoGrupo,
        LIC_LIC_Id_Padre: obj.LIC_LIC_Id_Padre
    };

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar la licencia '${licencia.LIC_Nombre}'?`,
        onConfirmar: function () {
            $.ajax({
                url: AppConfig.urls.eliminarLicencia,
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify(licencia),
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

function eliminarTarifa(button) {
    setTimeout(() => {
        let obj = JSON.parse(button.getAttribute('data-tarifa'));

        const fechaFormateada = formatDateInputForDateField(obj.LIT_FechaInicio);

        mostrarAlertaConfirmacion({
            titulo: `¿Eliminar tarifa con fecha de inicio ${formatDateToDDMMYYYY(fechaFormateada)}?`,
            backdrop: false,
            onConfirmar: function () {
                $.ajax({
                    url: AppConfig.urls.EliminarTarifa,
                    type: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    data: JSON.stringify({
                        idLicencia: obj.LIT_LIC_Id,
                        fechaInicio: toISODate(obj.LIT_FechaInicio)
                    }),
                    dataType: 'json'
                })
                    .done(function (response) {
                        if (response.success) {
                            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                            let tabla = $('#tableTarifas').DataTable();
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
    }, 300);
}

function eliminarExcepcion(button) {
    setTimeout(() => {
        let obj = JSON.parse(button.getAttribute('data-excepcion'));

        mostrarAlertaConfirmacion({
            titulo: `¿Estás seguro de que deseas eliminar la excepción de '${obj.EmpresaNombre}'?`,
            backdrop: false,
            onConfirmar: function () {
                $.ajax({
                    url: AppConfig.urls.EliminarExcepcion,
                    type: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    data: JSON.stringify({
                        idLicencia: obj.LIE_LIC_Id,
                        idEmpresa: obj.LIE_EMP_Id
                    }),
                    dataType: 'json'
                })
                    .done(function (response) {
                        if (response.success) {
                            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                            let tabla = $('#tableExcepciones').DataTable();
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
    }, 300);
}

function InicializarSelectTiposEnte() {
    if (!$.fn.select2) {
        console.error("Select2 no está disponible");
        return;
    }

    let $select = $('#tiposEnte');
    if ($select.length === 0) {
        console.error("El select #tiposEnte no está en el DOM");
        return;
    }

    $select.select2({
        placeholder: "",
        allowClear: true,
        multiple: true,
        dropdownParent: $('#modalEditar'), // o el modal donde esté el select
        width: '100%'
    });
}

function InicializarSelectLicenciasIncompatibles() {
    if (!$.fn.select2) {
        console.error("Select2 no está disponible");
        return;
    }

    let $select = $('#licenciasIncompatibles');
    if ($select.length === 0) {
        console.error("El select #licenciasIncompatibles no está en el DOM");
        return;
    }

    $select.select2({
        placeholder: "",
        allowClear: true,
        multiple: true,
        dropdownParent: $('#modalEditar'), // o el modal donde esté el select
        width: '100%'
    });
}


async function cargarComboTiposEnte(idLicencia) {
    try {
        const response = await fetch(`${AppConfig.urls.ObtenerComboTiposEnte}?idLicencia=${idLicencia}`, {
            method: 'GET',
            headers: { 'Content-Type': 'application/json; charset=utf-8' }
        });

        const data = await response.json();
        const $select = $('#tiposEnte');
        $select.empty();

        data.forEach(item => {
            const option = new Option(item.text, item.id, item.selected, item.selected);
            $select.append(option);
        });

        $select.trigger('change');
    } catch (error) {
        registrarErrorjQuery('fetch', error.message);
    }
}

async function cargarComboLicenciasIncompatibles(idLicencia) {
    try {
        const response = await fetch(`${AppConfig.urls.ObtenerLicenciasIncompatiblesCombo}?idLicencia=${idLicencia}`, {
            method: 'GET',
            headers: { 'Content-Type': 'application/json; charset=utf-8' }
        });

        const data = await response.json();
        const $select = $('#licenciasIncompatibles');
        $select.empty();

        data.forEach(item => {
            const option = new Option(item.text, item.id, item.selected, item.selected);
            $select.append(option);
        });

        $select.trigger('change');
    } catch (error) {
        registrarErrorjQuery('fetch', error.message);
    }
}

function obtenerTiposEnteSeleccionados() {
    return $('#tiposEnte').val();
}

function obtenerLicenciasIncompatiblesSeleccionados() {
    return $('#licenciasIncompatibles').val();
}

function InicializarSelectLicenciasReemplazo() {
    if (!$.fn.select2) {
        console.error("Select2 no está disponible");
        return;
    }

    let $select = $('#licenciasReemplazo');
    if ($select.length === 0) {
        console.error("El select #licenciasReemplazo no está en el DOM");
        return;
    }

    $select.select2({
        placeholder: "Seleccione licencias...",
        allowClear: true,
        multiple: true,
        dropdownParent: $('#modalExcepciones'), // o el modal donde esté el select
        width: '100%'
    });
}

async function cargarComboLicenciasReemplazo(idLicencia, idEmpresa) {
    try {
        const response = await fetch(`${AppConfig.urls.ObtenerComboLicenciasReemplazo}?idLicencia=${idLicencia}&idEmpresa=${idEmpresa}`, {
            method: 'GET',
            headers: { 'Content-Type': 'application/json; charset=utf-8' }
        });

        const data = await response.json();
        const $select = $('#licenciasReemplazo');
        $select.empty();

        // Añadir todas las opciones
        data.forEach(item => {
            const option = new Option(item.text, item.id, false, false); // aún no seleccionamos aquí
            $select.append(option);
        });

        // Establecer los valores seleccionados
        const valoresSeleccionados = data.filter(i => i.selected).map(i => i.id);
        $select.val(valoresSeleccionados).trigger('change');
    } catch (error) {
        registrarErrorjQuery('fetch', error.message);
    }
}

function obtenerLicenciasReemplazoSeleccionadas() {
    return $('#licenciasReemplazo').val();
}

function verEntesLicencias(button) {
    const lic = JSON.parse(button.getAttribute('data-licencia'));
    $('#idLicenciaEntes').val(lic.LIC_Id);
    limpiarFormularioEntes();

    // ocultar form y botones
    $('#formEntesLicencias').hide();
    $('#btnGuardarEnteLicencia').hide();
    $('#btnCancelarEnte').hide();
    $('#btnNuevoEnte').show();

    cargarComboEntes();
    ObtenerEntesLicencias(lic.LIC_Id);
    $('#modalEntesLicencias').modal('show');
}

async function cargarComboEntes() {
    try {
        const response = await $.ajax({
            url: AppConfig.urls.ObtenerEntidadesCombo, // ya existe en MantenimientoController
            type: 'GET', dataType: 'json'
        });
        const $sel = $('#selectEnte').empty().append('<option value="">Seleccione…</option>');
        response.forEach(e => $sel.append(
            `<option value="${e.ENT_Id}">${e.ENT_Nombre}</option>`
        ));
    } catch (xhr) {
        console.error(xhr);
    }
}

//function ObtenerEntesLicencias(idLicencia) {
//    inicializarDataTable('#tableEntesLicencias', {
//        paging: false,
//        searching: false,
//        info: false,
//        ordering: false,
//        dom: 't',
//        ajax: {
//            url: AppConfig.urls.ObtenerEntesLicencias,
//            type: 'GET',
//            data: { idLicencia: idLicencia },   // ojo: el parámetro que espera tu acción
//            dataSrc: ''
//        },
//        columns: [
//            { data: 'NombreEntidad', title: 'Entidad' },
//            {
//                data: 'FechaInicio',
//                title: 'Fecha Inicio',
//                render: formatDateToDDMMYYYY
//            },
//            {
//                data: 'FechaFin',
//                title: 'Fecha Inicio',
//                render: formatDateToDDMMYYYY
//            },
//            {
//                className: 'td-btn',
//                data: null,
//                title: '<span class="sReader">Acción</span>',
//                orderable: false,
//                render: (data, _, row) => `
//                      <button class="btn btn-icon btn-eliminar btn-outline-secondary"
//                              data-ente="${JSON.stringify(row)}"
//                              onclick="eliminarEnteLicencia(this)">
//                        <i class="bi bi-trash" title="Eliminar"></i>
//                      </button>`
//            }
//        ]
//    });
//}

function ObtenerEntesLicencias(idLicencia) {
    if ($.fn.dataTable.isDataTable('#tableEntesLicencias')) {
        $('#tableEntesLicencias').DataTable().destroy();
    }

    const tabla = $('#tableEntesLicencias').DataTable({
        ajax: {
            url: AppConfig.urls.ObtenerEntesLicencias,
            type: 'GET',
            data: { idLicencia: idLicencia },   // ojo: el parámetro que espera tu acción
            dataSrc: ''
        },
        columns: [
            { data: 'NombreEntidad', title: 'Entidad' },
            { data: 'EmpresaNombre', title: 'Empresa' },
            {
                data: 'FechaInicio',
                title: 'Fecha Inicio',
                render: formatDateToDDMMYYYY
            },
            {
                data: 'FechaFin',
                title: 'Fecha Inicio',
                render: formatDateToDDMMYYYY
            },
            {
                className: 'td-btn',
                data: null,
                title: '<span class="sReader">Acción</span>',
                orderable: false,
                render: (data, _, row) => `
                                  <button class="btn btn-icon btn-eliminar btn-outline-secondary"
                                          data-ente="${JSON.stringify(row)}"
                                          onclick="eliminarEnteLicencia(this)">
                                    <i class="bi bi-trash" title="Eliminar"></i>
                                  </button>`
            }
        ],
        paging: false,
        searching: true,
        info: false,
        ordering: false,
        dom: 'Bt',
        buttons: [
            {
                extend: 'excelHtml5',
                text: 'Exportar a Excel',
                titleAttr: 'Exportar a Excel',
                className: 'btn btn-sm btn-success',         // estilo Bootstrap
                title: `EntesLicencia_${new Date().toISOString().slice(0, 10)}`,
                exportOptions: { columns: [0, 1, 2] }
            }
        ]
    });

    // Colocamos el contenedor de botones en la toolbar, a la derecha del filtro
    tabla.buttons().container().appendTo('#toolbarEntesButtons');

    // Ajuste columnas al redimensionar
    $(window).off('resize.dtEntes').on('resize.dtEntes', () => tabla.columns.adjust().draw());
}

function limpiarFormularioEntes() {
    $('#selectEnte').val('');
    $('#fechaInicioEnte').val('');
}

function eliminarEnteLicencia(button) {
    const obj = JSON.parse(button.getAttribute('data-ente'));
    mostrarAlertaConfirmacion({
        titulo: `¿Eliminar asociación con ${obj.NombreEntidad}?`,
        onConfirmar: async () => {
            try {
                const res = await $.ajax({
                    url: AppConfig.urls.EliminarEnteLicencia,
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({
                        idEnte: obj.EntidadId,
                        idEnteLicencia: obj.LicenciaId,
                        fechaInicio: obj.FechaInicio
                    }),
                    dataType: 'json'
                });
                if (res.success) {
                    mostrarToast('Eliminado', TipoToast.Success);
                    $('#tableEntesLicencias').DataTable().ajax.reload(null, false);
                } else {
                    mostrarToast(res.message || 'Error al eliminar', TipoToast.Error);
                }
            } catch (xhr) {
                console.error(xhr);
            }
        }
    });
}

function limpiarFormularioEntes() {
    $('#selectEnte').val('');
    $('#fechaInicioEnte').val('');
    $('#fechaFinEnte').val('');
}