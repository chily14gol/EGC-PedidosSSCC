async function editarEnte(button) {
    const ente = JSON.parse(button.getAttribute('data-ente'));

    // 1️⃣ Fijamos primero el select de empresa
    $('#idEnte').val(ente.ENT_Id);
    $('#empresa').val(ente.ENT_EMP_Id);

    // 2️⃣ Esperamos a que se carguen los departamentos de esa empresa
    await cargarComboDepartamentosEmpresa(ente.ENT_EMP_Id);

    // 3️⃣ Ahora sí seleccionamos el departamento del ente
    $('#departamento').val(ente.ENT_EDE_Id);

    // 4️⃣ El resto de campos
    $('#nombre').val(ente.ENT_Nombre);
    $('#email').val(ente.ENT_Email);
    $('#tipoEnte').val(ente.ENT_TEN_Id);
    $('#oficina').val(ente.ENT_OFI_Id);

    $('#modalEditarLabel').text('Editar Entidad');
    $('#modalEditar').modal('show');
}

async function cargarComboDepartamentosEmpresa(idEmpresa) {
    try {
        const datos = await $.ajax({
            url: AppConfig.urls.ObtenerDepartamentosEmpresa,
            type: 'GET',
            contentType: 'application/json',
            data: { idEmpresa },
            dataType: 'json'
        });

        const $select = $('#departamento');
        $select.empty().append('<option value="">Seleccione Departamento</option>');

        datos.forEach(item => {
            $select.append(`<option value="${item.EDE_Id}">${item.EDE_Nombre}</option>`);
        });
    } catch (xhr) {
        const mensaje = obtenerMensajeErrorAjax(xhr);
        registrarErrorjQuery(xhr.status, mensaje);
    }
}
function eliminarEnte(button) {
    const ente = JSON.parse(button.getAttribute('data-ente'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar el ente '${ente.ENT_Nombre}'?`,
        onConfirmar: () => {
            $.ajax({
                url: AppConfig.urls.EliminarEnte,
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify({ idEnte: ente.ENT_Id }),
                dataType: 'json'
            })
                .done(response => {
                    if (response.success) {
                        mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                        $('#table').DataTable().ajax.reload(null, false);
                    } else {
                        mostrarToast('No se puede eliminar. Tiene licencias asociadas', TipoToast.Warning);
                    }
                })
                .fail((xhr, status, error) => {
                    const mensaje = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, mensaje);
                });
        }
    });
}

const guardarEnte = async () => {
    let id = $('#idEnte').val();
    let nombre = $('#nombre').val();
    let email = $('#email').val();
    let tipoEnte = $('#tipoEnte').val();
    let empresa = $('#empresa').val();
    let oficina = $('#oficina').val();
    let departamento = $('#departamento').val();

    let camposInvalidos = [];

    if (!nombre) {
        $('#nombre').addClass('is-invalid');
        camposInvalidos.push("Nombre");
    } else {
        $('#nombre').removeClass('is-invalid');
    }

    if (!email) {
        $('#email').addClass('is-invalid');
        camposInvalidos.push("Email");
    } else {
        $('#email').removeClass('is-invalid');
    }

    if (!tipoEnte) {
        $('#tipoEnte').addClass('is-invalid');
        camposInvalidos.push("Tipo Entidad");
    } else {
        $('#tipoEnte').removeClass('is-invalid');
    }

    if (camposInvalidos.length > 0) {
        mostrarToast(AppConfig.mensajes.camposObligatorios + camposInvalidos.join(", "), TipoToast.Warning);
        return;
    }

    let ente = {
        ENT_Id: id,
        ENT_Nombre: nombre,
        ENT_Email: email,
        ENT_TEN_Id: tipoEnte,
        ENT_EMP_Id: empresa,
        ENT_OFI_Id: oficina,
        ENT_EDE_Id: departamento
    };

    try {
        const response = await $.ajax({
            url: AppConfig.urls.GuardarEnte,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(ente),
            dataType: 'json'
        });

        if (response.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#modalEditar').modal('hide');
            $('#table').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast("Ya existe una entidad con el nombre o el email", TipoToast.Warning);
        }
    } catch (xhr) {
        const mensaje = obtenerMensajeErrorAjax(xhr);
        registrarErrorjQuery(xhr.status, mensaje);
    }
};

//----------------
// Aplicaciones
//----------------
function verAplicaciones(button) {
    let obj = JSON.parse(button.getAttribute('data-ente'));

    $('#idEnteAplicacion').val(obj.ENT_Id);
    $('#aplicaciones').val('');
    $('#fechaInicio').val('');
    $('#fechaFin').val('');

    ObtenerAplicaciones(obj.ENT_Id);
    $('#modalAplicaciones').modal('show');
}
function editarAplicacion(button) {
    let obj = JSON.parse(button.getAttribute('data-ente'));

    $('#idAplicacionOriginal').val(obj.ENL_APP_Id);
    $('#aplicaciones').val(obj.ENL_APP_Id);
    $('#fechaInicioOriginal').val(obj.FechaInicio);
    $('#fechaInicio').val(formatDateInputForDateField(obj.FechaInicio));
    $('#fechaFin').val(formatDateInputForDateField(obj.FechaFin));

    $('#modalAplicaciones').modal('show');
}
function eliminarAplicacion(button) {
    setTimeout(() => {
        let obj = JSON.parse(button.getAttribute('data-app'));

        mostrarAlertaConfirmacion({
            titulo: `¿Eliminar asignación de '${obj.NombreAplicacion}'?`,
            backdrop: false,
            onConfirmar: function () {
                $.ajax({
                    url: AppConfig.urls.EliminarAplicacionEnte,
                    type: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    data: JSON.stringify({
                        idAplicacion: obj.ENL_APP_Id,
                        idEnte: obj.ENL_ENT_Id,
                        fechaInicio: toISODate(obj.ENL_FechaInicio)
                    }),
                    dataType: 'json'
                })
                    .done(function (response) {
                        if (response.success) {
                            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                            let tabla = $('#tableAplicaciones').DataTable();
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
function ObtenerAplicaciones(idEnte) {
    let tabla = inicializarDataTable('#tableAplicaciones', {
        paging: false,
        searching: false,
        info: false,
        ordering: false,
        dom: 't',
        ajax: {
            url: AppConfig.urls.ObtenerAplicacionesPorEntidad,
            type: 'GET',
            data: { idEnte: idEnte },
            dataSrc: ''
        },
        rowId: row => `fila-app-${row.ENL_APP_Id}`,
        columns: [
            { data: 'NombreAplicacion', title: 'Aplicación' },
            { data: 'ENL_FechaInicio', render: formatDateToDDMMYYYY, title: 'Fecha Inicio' },
            { data: 'ENL_FechaFin', render: formatDateToDDMMYYYY, title: 'Fecha Fin' },
            {
                className: 'td-btn',
                data: null,
                title: '',
                orderable: false,
                render: (data, type, row) => {
                    const js = JSON.stringify(row).replace(/"/g, '&quot;');
                    return `
                          <button class="btn btn-icon btn-eliminar" data-app="${js}" onclick="eliminarAplicacion(this)">
                            <i class="bi bi-trash" title="Eliminar"></i>
                          </button>`;
                }
            }
        ]
    });
    $(window).resize(() => tabla.columns.adjust().draw());
}

const guardarAplicacion = async () => {
    const enteId = $('#idEnteAplicacion').val(),
        orig = $('#idAplicacionOriginal').val(),
        appId = $('#aplicaciones').val(),
        inicio = $('#fechaInicioApp').val(),
        fin = $('#fechaFinApp').val();

    let invalid = [];
    if (!appId) { $('#aplicaciones').addClass('is-invalid'); invalid.push('Aplicación'); }
    else $('#aplicaciones').removeClass('is-invalid');

    if (!inicio) { $('#fechaInicioApp').addClass('is-invalid'); invalid.push('Fecha Inicio'); }
    else $('#fechaInicioApp').removeClass('is-invalid');

    if (invalid.length) {
        mostrarToast(AppConfig.mensajes.camposObligatorios + invalid.join(', '), TipoToast.Warning);
        return;
    }

    if (fin && new Date(fin) < new Date(inicio)) {
        $('#fechaFinApp').addClass('is-invalid');
        mostrarToast("Fecha Fin no puede ser anterior a Fecha Inicio.", TipoToast.Warning);
        return;
    }
    $('#fechaFin').removeClass('is-invalid');

    const dto = {
        ENL_ENT_Id: parseInt(enteId),
        ENL_APP_Id: parseInt(appId),  
        ENL_FechaInicio: formatDateToDDMMYYYY(inicio),
        ENL_FechaFin: fin ? formatDateToDDMMYYYY(fin) : null
    };
    if (orig) dto.ENL_APP_IdOriginal = parseInt(orig);

    try {
        const resp = await $.ajax({
            url: AppConfig.urls.GuardarAplicacionEnte,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(dto),
            dataType: 'json'
        });
        if (resp.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#idAplicacionOriginal, #aplicaciones, #fechaInicio, #fechaFin').val('');
            $('#tableAplicaciones').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast(resp.message || "Error al guardar.", TipoToast.Warning);
        }
    } catch (xhr) {
        registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
    }
}

//----------------
// Licencias
//----------------
function verLicencias(button) {
    let obj = JSON.parse(button.getAttribute('data-ente'));
    $('#idEnteLicencias').val(obj.ENT_Id);
    $('#licencias, #fechaInicio, #fechaFin').val('');
    ObtenerLicencias(obj.ENT_Id);
    $('#modalLicencias').modal('show');
}
function ObtenerLicencias(idEnte) {
    let columnasConFiltro = [];
    let tablaDatos = inicializarDataTable('#tableLicencias', {
        paging: false,
        searching: false,
        info: false,
        ordering: false,
        dom: 't',
        ajax: {
            url: AppConfig.urls.ObtenerLicenciasPorEnte,
            type: 'GET',
            dataSrc: function (json) {
                return json;
            },
            data: { idEnte: idEnte },
            dataType: 'json',
        },
        rowId: function (row) {
            return `fila-${row.EntidadId}-${row.LicenciaId}-${row.FechaInicio?.substring(0, 10)}`;
        },
        columns: [
            {
                data: 'NombreLicencia',
                title: 'Licencia',
                render: function (data) {
                    return `<span title="${data}">${data}</span>`; // ← tooltip si es largo
                }
            },
            {
                data: 'FechaInicio',
                title: 'Fecha Inicio',
                render: formatDateToDDMMYYYY
            },
            {
                data: 'FechaFin',
                title: 'Fecha Fin',
                render: formatDateToDDMMYYYY
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
                                data-licencia="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                onclick="eliminarLicencia(this)">
                                <i class="bi bi-trash" title="Eliminar"></i>
                            </button>`;
                }
            }
        ]
    }, columnasConFiltro, 'export_licencias');

    $(window).resize(function () {
        tablaDatos.columns.adjust().draw();
    });
}
function eliminarLicencia(button) {
    setTimeout(() => {
        let obj = JSON.parse(button.getAttribute('data-licencia'));
        const fechaFormateada = formatDateInputForDateField(obj.FechaInicio);

        mostrarAlertaConfirmacion({
            titulo: `¿Estás seguro de que deseas eliminar la licencia de '${obj.NombreLicencia}' con fecha de inicio '${fechaFormateada}'?`,
            backdrop: false,
            onConfirmar: function () {
                $.ajax({
                    url: AppConfig.urls.EliminarLicencia,
                    type: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    data: JSON.stringify({
                        idEnte: obj.EntidadId,
                        idEnteLicencia: obj.LicenciaId,
                        fechaInicio: toISODate(obj.FechaInicio)
                    }),
                    dataType: 'json'
                })
                    .done(function (response) {
                        if (response.success) {
                            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                            let tabla = $('#tableLicencias').DataTable();
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

const guardarLicencia = async () => {
    let idEnte = $('#idEnteLicencias').val();
    let idLicenciaOriginal = $('#idLicenciaOriginal').val();
    let idLicencia = $('#licencias').val();
    let fechaInicioOriginal = $('#fechaInicioOriginal').val();
    let fechaInicio = $('#fechaInicio').val();
    let fechaFin = $('#fechaFin').val();

    let camposInvalidos = [];

    if (!idLicencia) {
        $('#licencias').addClass('is-invalid');
        camposInvalidos.push("Licencia");
    } else {
        $('#licencias').removeClass('is-invalid');
    }

    if (!fechaInicio) {
        $('#fechaInicio').addClass('is-invalid');
        camposInvalidos.push("Fecha Inicio");
    } else {
        $('#fechaInicio').removeClass('is-invalid');
    }

    if (camposInvalidos.length > 0) {
        mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
        return;
    }

    // Validar que Fecha Fin >= Fecha Inicio
    if (fechaFin && fechaInicio) {
        const inicio = new Date(fechaInicio);
        const fin = new Date(fechaFin);

        if (fin < inicio) {
            $('#fechaFin').addClass('is-invalid');
            mostrarToast("La Fecha Fin no puede ser anterior a la Fecha Inicio.", TipoToast.Warning);
            return;
        } else {
            $('#fechaFin').removeClass('is-invalid');
        }
    }

    let enteLicencia = {
        ENL_ENT_Id: idEnte,
        ENL_LIC_IdOriginal: idLicenciaOriginal,
        ENL_LIC_Id: idLicencia,
        ENL_FechaInicioOriginal: fechaInicioOriginal ? formatDateToDDMMYYYY(fechaInicioOriginal) : null,
        ENL_FechaInicio: fechaInicio ? formatDateToDDMMYYYY(fechaInicio) : null,
        ENL_FechaFin: fechaFin ? formatDateToDDMMYYYY(fechaFin) : null
    };

    try {
        const response = await $.ajax({
            url: AppConfig.urls.GuardarLicencia,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(enteLicencia),
            dataType: 'json'
        });

        if (response.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#idLicenciaOriginal').val('');
            $('#licencias').val('');
            $('#fechaInicioOriginal').val('');
            $('#fechaInicio').val('');
            $('#fechaFin').val('');

            const filaEditadaId = `fila-${idEnte}-${idLicencia}-${fechaInicioOriginal ? formatDateInputForDateField(fechaInicioOriginal) : formatDateInputForDateField(fechaInicio)}`;
            sessionStorage.setItem("ultimaFilaEditada", filaEditadaId);

            let tabla = $('#tableLicencias').DataTable();
            tabla.ajax.reload(function () {
                resaltarFilaPorId(sessionStorage.getItem("ultimaFilaEditada"));
            }, false);
        } else {
            // Si el servidor envía message, lo mostramos
            const texto = response.message || "No se pudo guardar la licencia";
            mostrarToast(texto, TipoToast.Warning);
        }
    } catch (xhr) {
        // Capturamos ApplicationException (500) y mostramos su mensaje
        let msg = obtenerMensajeErrorAjax(xhr);
        if (xhr.responseJSON?.message) {
            msg = xhr.responseJSON.message;
        }
        registrarErrorjQuery(xhr.status, msg);
    }
}

// ——————————————
// Licencias Anuales
// ——————————————
function verLicenciasAnuales(btn) {
    const lic = JSON.parse(btn.getAttribute('data-ente'));
    $('#idEnteLicenciasAnuales').val(lic.ENT_Id);
    $('#licenciasAnual, #fechaInicioAnual, #fechaFinAnual, #contrato').val('');
    ObtenerLicenciasAnual(lic.ENT_Id);
    $('#modalLicenciasAnuales').modal('show');
};
function ObtenerLicenciasAnual(idEnte) {
    let columnasConFiltro = [];
    const tbl = inicializarDataTable('#tableEntesLicAnual', {
        paging: false, searching: false, info: false, ordering: false, dom: 't',
        ajax: {
            url: AppConfig.urls.ObtenerLicenciasAnualPorEnte,
            type: 'GET',
            data: { idEnte: idEnte },
            dataSrc: ''
        },
        columns: [
            {
                data: 'NombreLicenciaAnual',
                title: 'Licencia Anual',
                render: function (data) {
                    return `<span title="${data}">${data}</span>`; // ← tooltip si es largo
                }
            },
            {
                data: 'FechaInicio',
                title: 'Fecha Inicio',
                render: formatDateToDDMMYYYY
            },
            {
                data: 'FechaFin',
                title: 'Fecha Fin',
                render: formatDateToDDMMYYYY
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
                                data-licencia="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                onclick="eliminarLicenciaAnual(this)">
                                <i class="bi bi-trash" title="Eliminar"></i>
                            </button>`;
                }
            }
        ]
    }, columnasConFiltro, 'export_licencias_anual');
    $(window).resize(() => tbl.columns.adjust().draw());
}
function eliminarLicenciaAnual(button) {
    const obj = JSON.parse(button.getAttribute('data-licencia'));
    const fechaFormateada = formatDateInputForDateField(obj.FechaInicio);

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar la licencia de '${obj.NombreLicenciaAnual}' con fecha de inicio '${fechaFormateada}'?`,
        backdrop: false,
        onConfirmar: () => {
            $.ajax({
                url: AppConfig.urls.EliminarLicenciaAnual,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    idEnte: obj.EntidadId,
                    idLicenciaAnual: obj.LicenciaAnualId,
                    fechaInicio: toISODate(obj.FechaInicio)
                }),
                dataType: 'json'
            })
                .done(resp => {
                    if (resp.success)
                        $('#tableEntesLicAnual').DataTable().ajax.reload(null, false);
                    else
                        mostrarToast(resp.message, TipoToast.Warning);
                })
                .fail(function (xhr, status, error) {
                    const mensaje = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, mensaje);
                });
        }
    });
};

const guardarLicenciaAnual = async () => {
    let idEnte = $('#idEnteLicenciasAnuales').val();
    let idLicenciasAnual = $('#licenciasAnual').val();
    let fechaInicio = $('#fechaInicioAnual').val();
    let fechaFin = $('#fechaFinAnual').val();
    let idContrato = $('#contrato').val();

    let camposInvalidos = [];

    if (!idLicenciasAnual) {
        $('#licenciasAnual').addClass('is-invalid');
        camposInvalidos.push("Licencia Anual");
    } else {
        $('#licenciasAnual').removeClass('is-invalid');
    }

    if (!fechaInicio) {
        $('#fechaInicioAnual').addClass('is-invalid');
        camposInvalidos.push("Fecha Inicio");
    } else {
        $('#fechaInicioAnual').removeClass('is-invalid');
    }

    if (!fechaFin) {
        $('#fechaFinAnual').addClass('is-invalid');
        camposInvalidos.push("Fecha Inicio");
    } else {
        $('#fechaFinAnual').removeClass('is-invalid');
    }

    if (!idContrato) {
        $('#contrato').addClass('is-invalid');
        camposInvalidos.push("Contrato");
    } else {
        $('#contrato').removeClass('is-invalid');
    }

    if (camposInvalidos.length > 0) {
        mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
        return;
    }

    // Validar que Fecha Fin >= Fecha Inicio
    if (fechaFin && fechaInicio) {
        const inicio = new Date(fechaInicio);
        const fin = new Date(fechaFin);

        if (fin < inicio) {
            $('#fechaFinAnual').addClass('is-invalid');
            mostrarToast("La Fecha Fin no puede ser anterior a la Fecha Inicio.", TipoToast.Warning);
            return;
        } else {
            $('#fechaFinAnual').removeClass('is-invalid');
        }
    }

    const dto = {
        ELA_LAN_Id: idLicenciasAnual,
        ELA_ENT_Id: parseInt(idEnte, 10),
        ELA_FechaInicio: formatDateToDDMMYYYY(fechaInicio),
        ELA_FechaFin: formatDateToDDMMYYYY(fechaFin),
        ELA_CLA_Id: idContrato
    };

    const resp = await $.ajax({
        url: AppConfig.urls.GuardarLicenciaAnual,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(dto),
        dataType: 'json'
    });
    if (resp.success) {
        mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
        $('#formEntesLicAnual')[0].reset();
        $('#tableEntesLicAnual').DataTable().ajax.reload(null, false);
    } else {
        mostrarToast(resp.message, TipoToast.Warning);
    }
};

$(document).ready(function () {
    const storageKey = Filtros.Entidades;

    initEntes();

    async function initEntes() {
        try {
            await VerificarSesionActiva(OpcionMenu.Entes);
            await ObtenerEntes();
            await establecerFiltros();
            ocultarDivCargando();

            cargarComboTiposEnte();
            cargarComboOficinas();
            cargarComboEmpresas();
            cargarComboAplicaciones();
            cargarComboLicencias();
            cargarComboLicenciasAnual();
            cargarComboContratosLicenciasAnuales();
        } catch (xhr) {
            const mensaje = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, mensaje);
        }
    }

    async function establecerFiltros() {
        let savedFilters = localStorage.getItem(storageKey);
        let hasFilters = false;

        if (savedFilters) {
            savedFilters = JSON.parse(savedFilters);
            $("#formBuscar").val(savedFilters.general);
            $("#filterNombre").val(savedFilters.nombre || "");
            $("#filterEmail").val(savedFilters.email || "");
            $("#filterTipoEntidad").val(savedFilters.tipoEntidad || "");
            $("#filterEmpresa").val(savedFilters.empresa || "");
            $("#filterOficina").val(savedFilters.oficina || "");
            $("#filterSinEmpresa").prop("checked", !!savedFilters.sinEmpresa);
            $("#filterEmpresa").prop("disabled", !!savedFilters.sinEmpresa);

            let { general, ...otrosFiltros } = savedFilters;
            hasFilters = Object.values(otrosFiltros).some(value => value);
        }

        setTimeout(() => {
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
        let filtroActual = {
            general: $("#formBuscar").val(),
            nombre: $("#filterNombre").val(),
            email: $("#filterEmail").val(),
            tipoEntidad: $("#filterTipoEntidad").val(),
            empresa: $("#filterEmpresa").val(),
            oficina: $("#filterOficina").val(),
            sinEmpresa: $("#filterSinEmpresa").is(":checked") 
        };
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }

    async function ObtenerEntes() {
        return new Promise((resolve) => {
            let columnasConFiltro = [];
            let tablaDatos = inicializarDataTable('#table', {
                ajax: {
                    url: AppConfig.urls.ObtenerEntes,
                    type: 'GET',
                    dataSrc: '',
                    complete: function () {
                        resolve(); // ✅ se resuelve cuando termina la carga AJAX
                    }
                },
                columns: [
                    { data: 'ENT_Nombre', title: 'Nombre' },
                    { data: 'ENT_Email', title: 'Email' },
                    { data: 'NombreTipoEnte', title: 'Tipo de Ente' },
                    { data: 'NombreEmpresa', title: 'Empresa', render: (data) => data ?? '' },
                    { data: 'NombreOficina', title: 'Oficina' },
                    {
                        className: 'td-btn',
                        data: null,
                        title: '<span class="sReader">Acción</span>',
                        responsivePriority: 2,
                        orderable: false,
                        render: function (data, type, row) {
                            return `<div class="btn-group" role="group">
                            <button type="button" class="btn btn-icon btn-editar btn-outline-secondary me-2"
                                data-ente="${JSON.stringify(row).replace(/"/g, "&quot;")}" onclick="editarEnte(this)">
                                <i class="bi bi-pencil-square" title="Editar"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-verAplicaciones btn-outline-secondary me-2" title="Aplicaciones"
                                data-ente="${JSON.stringify(row).replace(/"/g, "&quot;")}" onclick="verAplicaciones(this)">
                                <i class="bi bi-grid-3x3-gap"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-verLicencias btn-outline-secondary me-2" title="Licencias"
                                data-ente="${JSON.stringify(row).replace(/"/g, "&quot;")}" onclick="verLicencias(this)">
                                <i class="bi bi-file-earmark-lock"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-verLicencias btn-outline-secondary me-2" title="Licencias Anuales"
                                data-ente="${JSON.stringify(row).replace(/"/g, "&quot;")}" onclick="verLicenciasAnuales(this)">
                                <i class="bi bi-calendar2-range"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary me-2"
                                data-ente="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                onclick="eliminarEnte(this)">
                                <i class="bi bi-trash" title="Eliminar"></i>
                            </button>
                            </div>`;
                        }
                    }
                ]
            }, columnasConFiltro, 'export_entes');

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
        });
    }

    async function cargarComboTiposEnte() {
        try {
            const datos = await $.ajax({
                url: AppConfig.urls.ObtenerTiposEnte,
                type: 'GET',
                dataType: 'json'
            });

            const $select = $('#tipoEnte');
            $select.empty().append('<option value="">Seleccione Tipo Ente</option>');

            datos.forEach(item => {
                $select.append(`<option value="${item.TEN_Id}">${item.TEN_Nombre}</option>`);
            });
        } catch (xhr) {
            const mensaje = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, mensaje);
        }
    }

    async function cargarComboOficinas() {
        try {
            const datos = await $.ajax({
                url: AppConfig.urls.ObtenerOficinas,
                type: 'GET',
                dataType: 'json'
            });

            const $select = $('#oficina');
            $select.empty().append('<option value="">Seleccione Oficina</option>');

            datos.forEach(item => {
                $select.append(`<option value="${item.OFI_Id}">${item.OFI_Nombre}</option>`);
            });
        } catch (xhr) {
            const mensaje = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, mensaje);
        }
    }

    async function cargarComboEmpresas() {
        try {
            const datos = await $.ajax({
                url: AppConfig.urls.ObtenerComboEmpresas,
                type: 'GET',
                dataType: 'json'
            });

            const $select = $('#empresa');
            $select.empty().append('<option value="">Seleccione Empresa</option>');

            datos.forEach(item => {
                $select.append(`<option value="${item.EMP_Id}">${item.EMP_Nombre}</option>`);
            });
        } catch (xhr) {
            const mensaje = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, mensaje);
        }
    }

    async function cargarComboAplicaciones() {
        try {
            const datos = await $.ajax({
                url: AppConfig.urls.ObtenerAplicaciones,
                type: 'GET',
                dataType: 'json'
            });

            const $select = $('#aplicaciones');
            $select.empty().append('<option value="">Seleccione Aplicación</option>');

            datos.forEach(item => {
                $select.append(`<option value="${item.APP_Id}">${item.APP_Nombre}</option>`);
            });
        } catch (xhr) {
            const mensaje = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, mensaje);
        }
    }

    async function cargarComboLicencias() {
        try {
            const datos = await $.ajax({
                url: AppConfig.urls.ObtenerComboLicencias,
                type: 'GET',
                dataType: 'json'
            });

            const $select = $('#licencias');
            $select.empty().append('<option value="">Seleccione Licencia</option>');

            datos.forEach(item => {
                $select.append(`<option value="${item.LIC_Id}">${item.LIC_Nombre}</option>`);
            });
        } catch (xhr) {
            const mensaje = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, mensaje);
        }
    }

    async function cargarComboLicenciasAnual() {
        try {
            const datos = await $.ajax({
                url: AppConfig.urls.ObtenerComboLicenciasAnual,
                type: 'GET',
                dataType: 'json'
            });

            const $select = $('#licenciasAnual');
            $select.empty().append('<option value="">Seleccione Licencia Anual</option>');

            datos.forEach(item => {
                $select.append(`<option value="${item.LAN_Id}">${item.LAN_Nombre}</option>`);
            });
        } catch (xhr) {
            const mensaje = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, mensaje);
        }
    }

    async function cargarComboContratosLicenciasAnuales() {
        try {
            const datos = await $.ajax({
                url: AppConfig.urls.ObtenerContratosLicenciasAnuales,
                type: 'GET',
                dataType: 'json'
            });
            const $sel = $('#contrato')
                .empty()
                .append('<option value="">Seleccione contrato</option>');
            datos.forEach(c => {
                $sel.append(`<option value="${c.CLA_Id}">${c.Display}</option>`);
            });
        } catch (xhr) {
            console.error('Error cargando contratos:', xhr);
            mostrarToast('No se pudieron cargar los contratos', TipoToast.Error);
        }
    }

    $('#btnNuevo').on('click', function () {
        $('#idEnte').val('-1');
        $('#nombre').val('');
        $('#email').val('');
        $('#tipoEnte').val('');
        $('#empresa').val('');
        $('#oficina').val('');
        $('#departamento').val('');

        $('#modalEditarLabel').text('Nueva Entidad');
        $('#modalEditar').modal('show');
    });

    $('#btnBuscar').on('click', function () {
        let nombre = $('#filterNombre').val();
        let email = $('#filterEmail').val();
        let tipoEntidad = $('#filterTipoEntidad').val();
        let empresa = $('#filterEmpresa').val();
        let oficina = $('#filterOficina').val();
        let sinEmpresa = $('#filterSinEmpresa').is(':checked');

        let table = $('#table').DataTable();
        table.columns(0).search(nombre).draw();
        table.columns(1).search(email).draw();
        table.columns(2).search(tipoEntidad).draw();
        if (sinEmpresa) {
            table.columns(3).search('^\\s*$', true, false); // 👈 celdas vacías
        } else {
            table.columns(3).search(empresa, false, false);
        }
        table.columns(4).search(oficina).draw();

        guardarFiltros();
    });

    // Cuando el usuario cambie de Empresa, recargamos Departamentos
    $('#empresa').on('change', async function () {
        const idEmpresa = $(this).val();
        if (idEmpresa) {
            await cargarComboDepartamentosEmpresa(idEmpresa);
        } else {
            // Si no hay empresa seleccionada, dejamos el select de departamentos vacío
            $('#departamento')
                .empty()
                .append('<option value="">Seleccione Departamento</option>');
        }
    });

    $('#filterSinEmpresa').on('change', function () {
        const checked = $(this).is(':checked');
        $('#filterEmpresa').prop('disabled', checked);
        if (checked) {
            $('#filterEmpresa').val(''); // limpia si se marca
        }
    });
});