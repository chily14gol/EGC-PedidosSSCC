function abrirModalProveedor(button) {
    const proveedor = JSON.parse(button.getAttribute('data-proveedor'));

    $('#proveedorId').val(proveedor.PRV_Id);
    $('#proveedorCIF').val(proveedor.PRV_CIF);
    $('#proveedorNombre').val(proveedor.PRV_Nombre);
    $('#proveedorActivo').prop('checked', proveedor.PRV_Activo);
    $('#proveedorCIF, #proveedorNombre').removeClass('is-invalid');
    $('#proveedorTarea').val(proveedor.PRV_TAR_Id_Soporte || '');
    $('#proveedorPlantilla').val(proveedor.PRV_PlantillaExcel || '');

    $('#modalProveedor').modal('show');
}

function abrirModalContrato(button) {
    const contrato = JSON.parse(button.getAttribute('data-proveedor'));
    const proveedorId = contrato.PRV_Id;

    limpiarModalContrato();
    $('#contratoProveedorId').val(proveedorId);

    // Mostrar el modal y cargar la tabla solo cuando esté completamente visible
    $('#modalContratos').off('shown.bs.modal').on('shown.bs.modal', function () {
        cargarContratosProveedor(proveedorId);
    });

    $('#modalContratos').modal('show');
}
function limpiarModalContrato() {
    $('#contratoId').val('');
    $('#contratoFechaInicio').val('');
    $('#contratoFechaFin').val('');
    $('#contratoPrecioHora').val('');
    $('#contratoHoras').val('');
    $('#tableReparto tbody').empty();
}
function mostrarZonaAlta() {
    $('#zonaAltaContrato').slideDown();
    $('#btnNuevoContrato').hide();
}
function ocultarZonaAlta() {
    $('#zonaAltaContrato').slideUp();
    $('#btnNuevoContrato').show();
    limpiarModalContrato();
}
function editarContrato(button) {
    const contrato = JSON.parse(button.getAttribute('data-contrato'));

    $('#contratoId').val(contrato.PVC_Id);
    $('#contratoProveedorId').val(contrato.PVC_PRV_Id);
    $('#contratoFechaInicio').val(parseDotNetDateToDDMMYYYY(contrato.PVC_FechaInicio));
    $('#contratoFechaFin').val(parseDotNetDateToDDMMYYYY(contrato.PVC_FechaFin));
    $('#contratoPrecioHora').val(contrato.PVC_PrecioHora);
    $('#contratoHoras').val(contrato.PVC_HorasContratadas);
    mostrarZonaAlta();
    cargarReparto(contrato.PVC_Id);
    $('#modalContratos').modal('show');
}

async function cargarReparto(contratoId) {
    // asume que creas esta ruta en tu controlador:
    // [HttpGet] ActionResult ObtenerRepartoContrato(int contratoId)
    const reparto = await $.getJSON(AppConfig.urls.ObtenerRepartoContrato, { contratoId });
    const $tb = $('#tableReparto tbody').empty();
    reparto.forEach(r => {
        $tb.append(`
              <tr>
                <td>
                  <select class="form-select form-select-sm selectEmpresa">
                    ${opcionesEmpresas}
                  </select>
                </td>
                <td>
                    <div class="input-group input-group-sm">
                        <input type="number" class="form-control form-control-sm inputPorcentaje"
                         min="0" max="100" step="0.01" value="${r.PVR_Porcentaje}" />
                       <span class="input-group-text">%</span>
                    </div>
                </td>
                <td class="text-center">
                  <button type="button"
                          class="btn btn-sm btn-outline-danger btnEliminarReparto">
                    <i class="bi bi-trash"></i>
                  </button>
                </td>
              </tr>`);
        // fijamos la empresa seleccionada
        $tb.find('tr:last .selectEmpresa').val(r.PVR_EMP_Id);
    });
}
function eliminarContrato(idContrato) {
    mostrarAlertaConfirmacion({
        titulo: `¿Eliminar contrato?`,
        onConfirmar: () => {
            $.post(AppConfig.urls.EliminarContratoSoporte, { idContrato: idContrato }, resp => {
                if (resp.success) {
                    ocultarZonaAlta();
                    $('#tableContratos').DataTable().ajax.reload(null, false);
                }
            }, 'json');
        }
    });
}
function cargarContratosProveedor(proveedorId) {
    const tabla = $('#tableContratos').DataTable({
        destroy: true,
        ajax: {
            url: AppConfig.urls.ObtenerContratoSoporte,
            type: 'GET',
            dataSrc: function (json) {
                // Filtramos solo los del proveedor seleccionado
                return json.filter(c => c.PVC_PRV_Id === proveedorId);
            },
            error: function (xhr) {
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
            }
        },
        columns: [
            { data: 'PVC_FechaInicio', render: formatDateToDDMMYYYY_Proveedores },
            { data: 'PVC_FechaFin', render: formatDateToDDMMYYYY_Proveedores },
            {
                data: 'PVC_PrecioHora',
                className: 'text-end',
                render: formatMoney
            },
            {
                data: 'PVC_HorasContratadas',
                className: 'text-end'
            },
            {
                data: null,
                className: 'td-btn',
                orderable: false,
                render: function (data, type, row) {
                    return `<div class="btn-group" role="group">
                            <button type="button" class="btn btn-icon btn-editar me-2"
                                data-contrato="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                onclick="editarContrato(this)">
                                <i class="bi bi-pencil-square" title="Editar"></i>
                            </button>
                            <button type="button" class="btn btn-icon me-2" onclick="eliminarContrato(${row.PVC_Id})">
                                <i class="bi bi-trash"></i>
                            </button>
                        </div>`;
                }
            }
        ],
        paging: false,
        searching: false,
        info: false,
        ordering: false,
        dom: 't'
    });
}

async function eliminarProveedorAjax(idProveedor) {
    try {
        const response = await $.ajax({
            url: AppConfig.urls.EliminarProveedor,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ idProveedor }),
            dataType: 'json'
        });

        if (response.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#table').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast("No se puede eliminar el proveedor.", TipoToast.Warning);
        }
    } catch (error) {
        registrarErrorjQuery(error.status || "", error.message || error);
    }
}
function eliminarProveedor(button) {
    const proveedor = JSON.parse(button.getAttribute('data-proveedor'));

    mostrarAlertaConfirmacion({
        titulo: `¿Eliminar el proveedor '${proveedor.PRV_Nombre}'?`,
        onConfirmar: () => eliminarProveedorAjax(proveedor.PRV_Id)
    });
}

let opcionesEmpresas = '';

$(document).ready(function () {
    VerificarSesionActiva(OpcionMenu.Proveedores).then(() => {
        cargarComboTareas();
        cargarComboEmpresas();
        ObtenerProveedores();
    });
    function ObtenerProveedores() {
        const tablaDatos = inicializarDataTable('#table', {
            ajax: {
                url: AppConfig.urls.ObtenerProveedores,
                type: 'GET',
                dataSrc: ''
            },
            columns: [
                { data: 'PRV_Id', title: 'Id' },
                { data: 'PRV_CIF', title: 'CIF' },
                { data: 'PRV_Nombre', title: 'Nombre' },
                {
                    data: 'PRV_Activo',
                    title: 'Activo',
                    render: function (data) {
                        return data ? 'Sí' : 'No';
                    }
                },
                {
                    className: 'td-btn',
                    data: null,
                    title: '<span class="sReader">Acción</span>',
                    orderable: false,
                    // render de la última columna
                    render: function (data, type, row) {
                        return `
                            <div class="btn-group" role="group">
                              <button type="button"
                                      class="btn btn-icon btn-editar btn-outline-secondary me-2"
                                      data-proveedor="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                      onclick="abrirModalProveedor(this)">
                                <i class="bi bi-pencil-square" title="Editar"></i>
                              </button>
                              <button type="button"
                                      class="btn btn-icon btn-contratos btn-outline-secondary me-2"
                                      data-proveedor="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                      onclick="abrirModalContrato(this)">
                                <i class="bi bi-journal-text" title="Contratos"></i>
                              </button>
                              <button type="button"
                                      class="btn btn-icon btn-eliminar btn-outline-secondary me-2"
                                      data-proveedor="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                      onclick="eliminarProveedor(this)">
                                <i class="bi bi-trash" title="Eliminar"></i>
                              </button>
                            </div>`;
                    }

                }
            ]
        }, [], 'export_proveedores');

        $(window).resize(() => tablaDatos.columns.adjust().draw());

        $("#formBuscar").on("keyup input", function () {
            tablaDatos.search(this.value, false, false).draw();
        });
    }

    async function cargarComboTareas() {
        try {
            const tipos = [Tipo.CANTIDAD_FIJA];

            $.ajax({
                url: AppConfig.urls.ObtenerComboTareas,
                type: 'GET',
                dataType: 'json',
                data: { listaTiposTarea: tipos },
                traditional: true,
                success: function (tareas) {
                    const combos = ['#proveedorTarea'];
                    combos.forEach(selector => {
                        const $select = $(selector)
                            .empty()
                            .append('<option value="">Seleccione Tarea</option>');
                        tareas.forEach(t => {
                            $select.append(`<option value="${t.TAR_Id}">${t.TAR_Nombre}</option>`);
                        });
                    });
                },
                error: function (xhr) {
                    const msg = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, msg);
                }
            });
        } catch (error) {
            registrarErrorjQuery('GET', 'Error al cargar tareas');
        }
    }

    async function cargarComboEmpresas() {
        try {
            const lst = await $.getJSON(AppConfig.urls.ObtenerEmpresasCombo);
            setOpcionesEmpresas(lst);
        } catch (error) {
            registrarErrorjQuery('GET', 'Error al cargar empresas');
        }
    }
    function setOpcionesEmpresas(lista) {
        opcionesEmpresas = '<option value="">Seleccione empresa</option>';
        lista.forEach(e => {
            opcionesEmpresas += `<option value="${e.EMP_Id}">${e.EMP_Nombre}</option>`;
        });
    }

    $('#btnNuevo').on('click', function () {
        $('#proveedorId').val('');
        $('#proveedorCIF').val('');
        $('#proveedorNombre').val('');
        $('#proveedorActivo').prop('checked', true);
        $('#proveedorCIF, #proveedorNombre').removeClass('is-invalid');
        $('#proveedorTarea').val('');
        $('#proveedorPlantilla').val('');
        $('#modalProveedor').modal('show');
    });

    $('#btnGuardarProveedor').on('click', function () {
        const cif = $('#proveedorCIF').val().trim();
        const nombre = $('#proveedorNombre').val().trim();
        const activo = $('#proveedorActivo').is(':checked');
        let camposInvalidos = [];

        if (!cif) {
            $('#proveedorCIF').addClass('is-invalid');
            camposInvalidos.push("CIF");
        } else {
            $('#proveedorCIF').removeClass('is-invalid');
        }

        if (!nombre) {
            $('#proveedorNombre').addClass('is-invalid');
            camposInvalidos.push("Nombre");
        } else {
            $('#proveedorNombre').removeClass('is-invalid');
        }

        if (camposInvalidos.length > 0) {
            mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
            return;
        }

        const proveedor = {
            PRV_Id: $('#proveedorId').val(),
            PRV_CIF: cif,
            PRV_Nombre: nombre,
            PRV_Activo: activo,
            PRV_TAR_Id_Soporte: $('#proveedorTarea').val(),
            PRV_PlantillaExcel: $('#proveedorPlantilla').val()
        };

        $.ajax({
            url: AppConfig.urls.GuardarProveedor,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(proveedor),
            dataType: 'json'
        })
            .done(function (response) {
                if (response.success) {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                    $('#modalProveedor').modal('hide');
                    $('#table').DataTable().ajax.reload(null, false);
                } else {
                    mostrarToast("Error al guardar el proveedor.", TipoToast.Error);
                }
            })
            .fail(function (xhr) {
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
            });
    });

    $('#btnAgregarReparto').on('click', function () {
        const nuevaFila = `
            <tr>
              <td>
                <select class="form-select form-select-sm selectEmpresa">
                  ${opcionesEmpresas}
                </select>
              </td>
              <td>
               <div class="input-group input-group-sm">
                <input type="number"
                       class="form-control form-control-sm inputPorcentaje"
                       min="0" max="100" step="0.01" />
                              <span class="input-group-text">%</span>
                    </div>
              </td>
              <td class="text-center">
                <button type="button"
                        class="btn btn-sm btn-outline-danger btnEliminarReparto">
                  <i class="bi bi-trash"></i>
                </button>
              </td>
            </tr>`;
        $('#tableReparto tbody').append(nuevaFila);
    });

    $('#tableReparto').on('click', '.btnEliminarReparto', function () {
        $(this).closest('tr').remove();
    });

    $('#btnGuardarContrato').on('click', function () {
        const inicio = $('#contratoFechaInicio').val(),
            fin = $('#contratoFechaFin').val(),
            precioHora = $('#contratoPrecioHora').val().trim();

        let camposInvalidos = [];
        if (!inicio) {
            $('#contratoFechaInicio').addClass('is-invalid');
            camposInvalidos.push("Fecha Inicio");
        } else {
            $('#contratoFechaInicio').removeClass('is-invalid');
        }

        if (!precioHora) {
            $('#contratoPrecioHora').addClass('is-invalid');
            camposInvalidos.push("Precio Hora");
        } else {
            $('#contratoPrecioHora').removeClass('is-invalid');
        }

        if (camposInvalidos.length > 0) {
            mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
            return;
        }

        if (fin && new Date(fin) < new Date(inicio)) {
            $('#contratoFechaFin').addClass('is-invalid');
            mostrarToast("Fecha Fin no puede ser anterior a Fecha Inicio.", TipoToast.Warning);
            return;
        }
        $('#contratoFechaFin').removeClass('is-invalid');

        // Antes de enviar, construimos el array de reparto:
        const reparto = [];
        $('#tableReparto tbody tr').each(function () {
            const empId = $(this).find('.selectEmpresa').val();
            const pct = $(this).find('.inputPorcentaje').val();
            if (empId) {
                // parseFloat(pct) devuelve NaN si pct=="" o no es numérico;
                // NaN es falsy, así que el || 0 convierte cualquier NaN o valor falsy a 0
                const porcentaje = parseFloat(pct) || 0;

                reparto.push({
                    EMP_Id: parseInt(empId, 10),
                    Porcentaje: porcentaje
                });
            }
        })

        // —— Validación: sólo si hay alguna empresa en reparto —— 
        if (reparto.length > 0) {
            const suma = reparto.reduce((acc, r) => acc + r.Porcentaje, 0);
            // permitimos un pequeño margen de error:
            if (Math.abs(suma - 100) > 0.01) {
                mostrarToast(
                    `La suma de porcentajes debe ser 100% (ahora es ${suma.toFixed(2)}%).`,
                    TipoToast.Warning
                );
                return;
            }
        }

        const contrato = {
            PVC_Id: $('#contratoId').val(),
            PVC_PRV_Id: $('#contratoProveedorId').val(),
            PVC_FechaInicio: formatDateToDDMMYYYY(inicio),
            PVC_FechaFin: fin ? formatDateToDDMMYYYY(fin) : null,
            PVC_PrecioHora: parseFloat(precioHora),
            PVC_HorasContratadas: parseInt($('#contratoHoras').val() || 0),
        };

        const objProvContrato = {
            objContrato: contrato,
            Reparto: reparto
        };

        $.ajax({
            url: AppConfig.urls.GuardarContratoSoporte,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(objProvContrato),
            dataType: 'json'
        })
            .done(function (response) {
                if (response.success) {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                    ocultarZonaAlta();
                    $('#tableContratos').DataTable().ajax.reload(null, false);
                    limpiarModalContrato();
                } else {
                    mostrarToast(response.message, TipoToast.Warning);
                }
            })
            .fail(function (xhr) {
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
            });
    });

    $('#btnNuevoContrato').on('click', function () {
        mostrarZonaAlta();
        $('#modalContratosLabel').text('Nuevo Contrato');
    });

    $('#btnCancelarContrato').on('click', function () {
        ocultarZonaAlta();
    });

    $('#modalContratos').on('show.bs.modal', function () {
        ocultarZonaAlta();
    });
});