$(function () {
    initContratos();

    async function initContratos() {
        await VerificarSesionActiva(OpcionMenu.ContratosLicenciasAnuales);
        await loadContratos();
        ocultarDivCargando();
        await cargarComboEntidades();
        await cargarComboProveedores();
    }

    // — Listado de Contratos —
    async function loadContratos() {
        return new Promise(res => {
            const tbl = inicializarDataTable('#tableContratos', {
                ajax: {
                    url: AppConfig.urls.ObtenerContratos,
                    type: 'GET',
                    dataSrc: ''
                },
                columns: [
                    { data: 'ProveedorNombre', title: 'Proveedor' },
                    { data: 'CLA_FechaInicio', title: 'Fecha Inicio', render: formatDateToDDMMYYYY },
                    { data: 'CLA_FechaFin', title: 'Fecha Fin', render: formatDateToDDMMYYYY },
                    {
                        className: 'td-btn', orderable: false, data: null,
                        render: (_, __, r) => {
                            const js = JSON.stringify(r).replace(/"/g, '&quot;');
                            return `
                            <div class="btn-group" role="group">
                                <button class="btn btn-icon me-2" onclick="verTarifasContrato(this)" data-row="${js}">
                                  <i class="bi bi-currency-dollar" title="Tarifas"></i>
                                </button>
                                <button class="btn btn-icon btn-entes me-2" data-row="${js}" onclick="viewEntesLicencia(this)">
                                    <i class="bi bi-people" title="Entidades"></i>
                                </button>
                                <button class="btn btn-icon me-2" onclick="eliminarContrato(this)" data-row="${js}">
                                  <i class="bi bi-trash" title="Eliminar"></i>
                                </button>
                              </div>`;
                        }
                    }
                ]
            }, [], 'export_contratos');

            $('#btnNuevoContrato').click(() => {
                $('#claId, #claProv, #claInicio, #claFin').val('');
                $('#modalContratoLabel').text('Nuevo Contrato');
                $('#modalContrato').modal('show');
            });

            $(window).resize(() => tbl.columns.adjust().draw());
            res();
        });
    }

    window.editarContrato = btn => {
        const r = JSON.parse(btn.getAttribute('data-row'));
        $('#claId').val(r.CLA_Id);
        $('#claProv').val(r.CLA_PRV_Id);
        $('#claInicio').val(formatDateInputForDateField(r.CLA_FechaInicio));
        $('#claFin').val(r.CLA_FechaFin ? formatDateInputForDateField(r.CLA_FechaFin) : '');
        $('#modalContratoLabel').text('Editar Contrato');
        $('#modalContrato').modal('show');
    };

    window.eliminarContrato = btn => {
        const r = JSON.parse(btn.getAttribute('data-row'));
        mostrarAlertaConfirmacion({
            titulo: `¿Eliminar contrato con proveedor '${r.ProveedorNombre}'?`,
            onConfirmar: () => {
                $.ajax({
                    url: AppConfig.urls.EliminarContrato,
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ idContrato: r.CLA_Id }),
                    dataType: 'json'
                })
                    .done(resp => {
                        if (resp.success) {
                            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                            $('#tableContratos').DataTable().ajax.reload(null, false);
                        } else {
                            mostrarToast(resp.message, TipoToast.Warning);
                        }
                    })
                    .fail(xhr => registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr)));
            }
        });
    };

    window.guardarContrato = async () => {
        const id = parseInt($('#claId').val(), 10);
        const idProv = $('#claProv').val().trim();
        const inicio = $('#claInicio').val().trim();
        const fin = $('#claFin').val().trim();

        let invalidos = [];

        if (!idProv) { $('#claProv').addClass('is-invalid'); invalidos.push('Proveedor'); }
        else $('#claProv').removeClass('is-invalid')
        if (!inicio) { $('#claInicio').addClass('is-invalid'); invalidos.push('Fecha Inicio'); }
        else $('#claInicio').removeClass('is-invalid')
        if (!fin) { $('#claFin').addClass('is-invalid'); invalidos.push('Fecha Fin'); }
        else $('#claFin').removeClass('is-invalid');

        if (invalidos.length) {
            mostrarToast(AppConfig.mensajes.camposObligatorios + invalidos.join(', '), TipoToast.Warning);
            return;
        }

        if (fin && new Date(fin) < new Date(inicio)) {
            $('#claFin').addClass('is-invalid');
            mostrarToast("Fecha Fin no puede ser anterior a Fecha Inicio.", TipoToast.Warning);
            return;
        }
        $('#claFin').removeClass('is-invalid');

        const dto = {
            CLA_Id: id,
            CLA_PRV_Id: idProv,
            CLA_FechaInicio: inicio,
            CLA_FechaFin: fin
        };
        const resp = await $.ajax({
            url: AppConfig.urls.GuardarContrato,
            type: 'POST', contentType: 'application/json',
            data: JSON.stringify(dto), dataType: 'json'
        });
        if (resp.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#modalContrato').modal('hide');
            $('#tableContratos').DataTable().ajax.reload();
        } else {
            mostrarToast(resp.message || "Error al guardar.", TipoToast.Warning);
        }
    };

    // ——————————————
    // Tarifas de un Contrato
    // ——————————————
    $('#btnNuevaTarifa').click(() => {
        $('#selectLicencia, #cltImporte').val('');
        $('#btnNuevaTarifa').hide();
        $('#btnCancelarTarifa').show();
        $('#btnGuardarTarifa').show();
        $('#formTarifaContrato').slideDown();
    });

    $('#btnCancelarTarifa').on('click', function () {
        cancelarTarifa();
    });

    function cancelarTarifa() {
        $('#btnNuevaTarifa').show();
        $('#btnCancelarTarifa').hide();
        $('#btnGuardarTarifa').hide();
        $('#formTarifaContrato').slideUp();
    }

    window.verTarifasContrato = btn => {
        const r = JSON.parse(btn.getAttribute('data-row'));
        $('#cltClaId').val(r.CLA_Id);
        $('#cltImporte, #cltNumLic').val('');

        cargarComboLicenciasTarifas(r.CLA_PRV_Id);
        loadTarifasContrato(r.CLA_Id);

        $('#formTarifaContrato').hide();
        $('#btnNuevaTarifa').show();
        $('#btnGuardarTarifa').hide();
        $('#modalTarifasContrato').modal('show');
    };
    function loadTarifasContrato(id) {
        const tbl = inicializarDataTable('#tableTarifasContrato', {
            paging: false,
            searching: false,
            info: false,
            ordering: false,
            dom: 't',
            ajax: {
                url: AppConfig.urls.ObtenerTarifasContrato,
                type: 'GET',
                data: { idContrato: id },
                dataSrc: ''
            },
            columns: [
                { data: 'LicenciaNombre', title: 'Licencia', className: 'text-end' },
                { data: 'CLT_ImporteAnual', title: 'Importe Anual', className: 'text-center', render: v => formatMoney(v) },
                {
                    className: 'td-btn', orderable: false, data: null,
                    render: (_, __, r) => {
                        const js = JSON.stringify(r).replace(/"/g, '&quot;');
                        return `
                        <button class="btn btn-icon btn-editar me-2" data-row="${js}" onclick="editarTarifaContrato(this)">
                          <i class="bi bi-pencil-square"></i>
                        </button>
                        <button class="btn btn-icon" onclick="eliminarTarifaContrato(this)" data-row="${js}">
                          <i class="bi bi-trash"></i>
                        </button>`;
                    }
                }
            ]
        });
        $(window).resize(() => tbl.columns.adjust().draw());
    }

    window.editarTarifaContrato = function (btn) {
        const o = JSON.parse(btn.getAttribute('data-row'));
        $('#selectLicencia').val(o.CLT_LAN_Id);
        $('#cltImporte').val(o.CLT_ImporteAnual);

        $('#btnNuevaTarifa').hide();
        $('#btnCancelarTarifa').show();
        $('#btnGuardarTarifa').show();
        $('#formTarifaContrato').slideDown();
    };

    window.eliminarTarifaContrato = btn => {
        const r = JSON.parse(btn.getAttribute('data-row'));
        mostrarAlertaConfirmacion({
            titulo: `¿Eliminar tarifa ${formatMoney(r.CLT_ImporteAnual)}?`,
            onConfirmar: () => {
                $.ajax({
                    url: AppConfig.urls.EliminarTarifaContrato,
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({
                        idContrato: r.CLT_CLA_Id,
                        idLicencia: r.CLT_LAN_Id
                    }),
                    dataType: 'json'
                })
                    .done(resp => {
                        if (resp.success) {
                            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                            $('#tableTarifasContrato').DataTable().ajax.reload(null, false);
                        } else {
                            mostrarToast(resp.message, TipoToast.Warning);
                        }
                    })
                    .fail(xhr => registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr)));
            }
        });
    };

    window.guardarTarifaContrato = async () => {
        const licencia = $('#selectLicencia').val().trim();
        const importe = $('#cltImporte').val().trim();

        let invalidos = [];

        if (!licencia) { $('#selectLicencia').addClass('is-invalid'); invalidos.push('Licencia'); }
        else $('#selectLicencia').removeClass('is-invalid');
        if (!importe) { $('#cltImporte').addClass('is-invalid'); invalidos.push('Importe'); }
        else $('#cltImporte').removeClass('is-invalid');

        if (invalidos.length) {
            mostrarToast(AppConfig.mensajes.camposObligatorios + invalidos.join(', '), TipoToast.Warning);
            return;
        }

        const dto = {
            CLT_CLA_Id: parseInt($('#cltClaId').val(), 10),
            CLT_LAN_Id: parseInt($('#selectLicencia').val()),
            CLT_ImporteAnual: parseFloat($('#cltImporte').val())
        };
        const resp = await $.ajax({
            url: AppConfig.urls.GuardarTarifaContrato,
            type: 'POST', contentType: 'application/json',
            data: JSON.stringify(dto), dataType: 'json'
        });
        if (resp.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            cancelarTarifa();
            loadTarifasContrato(dto.CLT_CLA_Id);
        }
    };

    // ——————————————
    // Entidades ↔ Contrato
    // ——————————————
    // helper para yyyy-MM-dd sin tocar UTC
    function formatDateForInput(d) {
        const yyyy = d.getFullYear();
        const mm = String(d.getMonth() + 1).padStart(2, '0');
        const dd = String(d.getDate()).padStart(2, '0');
        return `${yyyy}-${mm}-${dd}`;
    }

    $('#btnNuevaEnte').click(() => {
        // 1) Recuperamos fechas del contrato
        const rawInicio = $('#btnNuevaEnte').data('inicioContrato');
        const rawFin = $('#btnNuevaEnte').data('finContrato');

        const inicioContrato = new Date(rawInicio);
        const finContrato = rawFin ? new Date(rawFin) : null;

        const hoy = new Date();
        let defaultInicio, defaultFin;

        if (finContrato && hoy > finContrato) {
            // hoy superó el fin del contrato
            defaultInicio = finContrato;
            defaultFin = finContrato;
        } else {
            // inicio no antes de hoy ni antes del contrato
            defaultInicio = hoy > inicioContrato ? hoy : inicioContrato;
            // fin = finContrato (si existe) o igual al inicio
            defaultFin = finContrato || defaultInicio;
        }

        // 5) Usamos la función local en vez de toISOString()
        $('#elaFechaInicio').val(formatDateForInput(defaultInicio));
        $('#elaFechaFin').val(formatDateForInput(defaultFin));
        $('#elaEntId').val(null).trigger('change');
        $('#elaLicencias').val('');

        // 6) Mostrar formulario
        $('#btnNuevaEnte').hide();
        $('#btnCancelarEnte').show();
        $('#btnGuardarEnte').show();
        $('#formEntes').slideDown();
    });

    $('#btnCancelarEnte').on('click', function () {
        cancelarEnte();
    });
    function cancelarEnte() {
        $('#elaEntId').val(null).trigger('change');
        $('#btnNuevaEnte').show();
        $('#btnCancelarEnte').hide();
        $('#btnGuardarEnte').hide();
        $('#formEntes').slideUp();
    }
    function loadEntesLicencia(id) {
        const tbl = inicializarDataTable('#tableEntes', {
            paging: false,
            searching: false,
            info: false,
            ordering: false,
            dom: 't',
            ajax: {
                url: AppConfig.urls.ObtenerEntesPorLicenciaAnual,
                type: 'GET',
                data: { idContrato: id },
                dataSrc: ''
            },
            columns: [
                { data: 'NombreEntidad', title: 'Entidad' },
                { data: 'NombreLicencia', title: 'Licencia' },
                { data: 'ELA_FechaInicio', title: 'Fecha Inicio', render: formatDateToDDMMYYYY },
                { data: 'ELA_FechaFin', title: 'Fecha Fin', render: formatDateToDDMMYYYY },
                {
                    className: 'td-btn',
                    data: null,
                    orderable: false,
                    responsivePriority: 1,
                    render: (_, __, row) => {
                        const js = JSON.stringify(row).replace(/"/g, '&quot;');
                        const btnEdit = `
                          <button class="btn btn-icon btn-editar me-2"
                                  data-row="${js}"
                                  onclick="editEnteLicencia(this)">
                            <i class="bi bi-pencil-square" title="Editar"></i>
                          </button>`;

                        // Si ya está facturada, mostramos un icono deshabilitado
                        let btnDelete;
                        if (row.ELA_Facturada) {
                            btnDelete = `
                                <button class="btn btn-icon btn-eliminar" disabled
                                        title="No se puede eliminar: ya facturada">
                                  <i class="bi bi-trash"></i>
                                </button>`;
                        } else {
                            btnDelete = `
                                <button class="btn btn-icon btn-eliminar"
                                        data-row="${js}"
                                        onclick="delEnteLicencia(this)">
                                  <i class="bi bi-trash" title="Eliminar"></i>
                                </button>`;
                        }

                        return btnEdit + btnDelete;
                    }
                }
            ]
        }, [], 'entes');
        $(window).resize(() => tbl.columns.adjust().draw());
    }

    window.viewEntesLicencia = function (btn) {
        const r = JSON.parse(btn.getAttribute('data-row'));
        $('#contratoId').val(r.CLA_Id);

        // Guardamos las fechas crudas del contrato para usar luego
        $('#btnNuevaEnte')
            .data('inicioContrato', r.CLA_FechaInicio)
            .data('finContrato', r.CLA_FechaFin);

        $('#elaEntId, #elaFechaInicio, #elaFechaFin').val('');

        cargarComboLicenciasEntes(r.CLA_PRV_Id);
        loadEntesLicencia(r.CLA_Id);

        $('#formEntes').hide();
        $('#btnNuevaEnte').show();
        $('#btnGuardarEnte').hide();
        $('#modalEntes').modal('show');
    };

    window.editEnteLicencia = function (btn) {
        const o = JSON.parse(btn.getAttribute('data-row'));

        // 1) Si no existe la <option> en el <select>, la creamos
        if ($('#elaEntId option[value="' + o.ELA_ENT_Id + '"]').length === 0) {
            // Usamos el nombre que ya traes en `o.NombreEntidad`
            const nueva = new Option(o.NombreEntidad, o.ELA_ENT_Id, true, true);
            $('#elaEntId').append(nueva);
        }

        // 2) Ahora sí, ponemos el valor y avisamos a Select2
        $('#elaEntId')
            .val(o.ELA_ENT_Id)
            .trigger('change');  

        $('#elaLicencias').val(o.ELA_LAN_Id);
        $('#elaFechaInicio').val(formatDateInputForDateField(o.ELA_FechaInicio));
        $('#elaFechaFin').val(o.ELA_FechaFin ? formatDateInputForDateField(o.ELA_FechaFin) : '');

        $('#btnNuevaEnte').hide();
        $('#btnCancelarEnte').show();
        $('#btnGuardarEnte').show();
        $('#formEntes').slideDown();
        $('#modalEntes').modal('show');
    };

    window.delEnteLicencia = function (btn) {
        const o = JSON.parse(btn.getAttribute('data-row'));
        mostrarAlertaConfirmacion({
            titulo: `¿Eliminar entidad '${o.NombreEntidad}'?`,
            onConfirmar: () => {
                $.ajax({
                    url: AppConfig.urls.EliminarEnteLicenciaAnual,
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({
                        idEntidad: o.ELA_ENT_Id,
                        idLicenciaAnual: o.ELA_LAN_Id,
                        idContrato: o.ELA_CLA_Id
                    }),
                    dataType: 'json'
                })
                    .done(resp => {
                        if (resp.success) {
                            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                            $('#tableEntes').DataTable().ajax.reload(null, false);
                        } else {
                            mostrarToast(resp.message, TipoToast.Warning);
                        }
                    })
                    .fail(xhr => registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr)));
            }
        });
    };
    function stripTime(d) {
        return new Date(d.getFullYear(), d.getMonth(), d.getDate());
    }

    window.guardarEnteLicencia = async () => {
        const contratoId = parseInt($('#contratoId').val(), 10);
        const entidadId = parseInt($('#elaEntId').val(), 10);
        const licenciaId = parseInt($('#elaLicencias').val(), 10);
        const inicio = $('#elaFechaInicio').val();
        const fin = $('#elaFechaFin').val();

        const contratoInicioRaw = $('#btnNuevaEnte').data('inicioContrato');
        const contratoFinRaw = $('#btnNuevaEnte').data('finContrato');

        const contratoInicio = stripTime(new Date(contratoInicioRaw));
        const contratoFin = contratoFinRaw ? stripTime(new Date(contratoFinRaw)) : null;

        // ————— Validación adicional de rango del contrato —————
        const fechaInicio = stripTime(new Date(inicio));
        const fechaFinObj = stripTime(new Date(fin));

        if (fechaInicio < contratoInicio || (contratoFin && fechaInicio > contratoFin)) {
            $('#elaFechaInicio').addClass('is-invalid');
            mostrarToast(
                `La Fecha Inicio debe estar entre ${formatDateToDDMMYYYY(contratoInicioRaw)} ` +
                (contratoFinRaw ? `y ${formatDateToDDMMYYYY(contratoFinRaw)}.` : `en adelante.`),
                TipoToast.Warning
            );
            return;
        }
        $('#elaFechaInicio').removeClass('is-invalid');

        if (fechaFinObj < contratoInicio || (contratoFin && fechaFinObj > contratoFin)) {
            $('#elaFechaFin').addClass('is-invalid');
            mostrarToast(
                `La Fecha Fin debe estar entre ${formatDateToDDMMYYYY(contratoInicioRaw)} ` +
                (contratoFinRaw ? `y ${formatDateToDDMMYYYY(contratoFinRaw)}.` : `en adelante.`),
                TipoToast.Warning
            );
            return;
        }
        $('#elaFechaFin').removeClass('is-invalid');

        let invalid = [];
        if (!entidadId) { $('#elaEntId').addClass('is-invalid'); invalid.push('Entidad'); }
        else { $('#elaEntId').removeClass('is-invalid'); }
        if (!licenciaId) { $('#elaLicencias').addClass('is-invalid'); invalid.push('Licencia'); }
        else { $('#elaLicencias').removeClass('is-invalid'); }
        if (!inicio) { $('#elaFechaInicio').addClass('is-invalid'); invalid.push('Fecha Inicio'); }
        else { $('#elaFechaInicio').removeClass('is-invalid'); }
        if (!fin) { $('#elaFechaFin').addClass('is-invalid'); invalid.push('Fecha Fin'); }
        else { $('#elaFechaFin').removeClass('is-invalid'); }

        if (invalid.length) {
            mostrarToast(AppConfig.mensajes.camposObligatorios + invalid.join(', '), TipoToast.Warning);
            return;
        }

        if (fin && new Date(fin) < new Date(inicio)) {
            $('#elaFechaFin').addClass('is-invalid');
            mostrarToast('Fecha Fin no puede ser anterior a Fecha Inicio.', TipoToast.Warning);
            return;
        }
        $('#elaFechaFin').removeClass('is-invalid');

        const dto = {
            ELA_ENT_Id: entidadId,
            ELA_LAN_Id: licenciaId,
            ELA_CLA_Id: contratoId,
            ELA_FechaInicio: formatDateToDDMMYYYY(inicio),
            ELA_FechaFin: formatDateToDDMMYYYY(fin)
        };

        const resp = await $.ajax({
            url: AppConfig.urls.GuardarEnteLicenciaAnual,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(dto),
            dataType: 'json'
        });

        if (resp.success) {
            cancelarEnte();
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#tableEntes').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast(resp.message, TipoToast.Warning);
        }
    };

    async function cargarComboEntidades() {
        try {
            const datos = await $.ajax({ url: AppConfig.urls.ObtenerEntes, type: 'GET', dataType: 'json' });
            const $sel = $('#elaEntId')
                .empty()
                .append('<option value="">Seleccione entidad</option>');

            datos.forEach(i =>
                $sel.append(`<option value="${i.ENT_Id}">${i.ENT_Nombre}</option>`)
            );

            // Aquí inicializamos select2 para búsqueda, con dropdown dentro del modal
            if ($sel.hasClass('select2-hidden-accessible')) {
                $sel.select2('destroy');
            }

            $sel.select2({
                placeholder: 'Seleccione entidad',
                allowClear: true,
                width: '100%',
                dropdownParent: $('#modalEntes')
            });
        } catch (xhr) {
            registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
        }
    }

    async function cargarComboProveedores() {
        try {
            const datos = await $.getJSON(AppConfig.urls.ObtenerProveedores);
            const $sel = $('#claProv').empty().append('<option value="">Seleccione</option>');
            datos.forEach(i => $sel.append(`<option value="${i.PRV_Id}">${i.PRV_Nombre}</option>`));
        } catch (e) {
            registrarErrorjQuery(e.status, e.message);
        }
    }

    async function cargarComboLicenciasTarifas(idProv) {
        try {
            const datos = await $.getJSON(AppConfig.urls.ObtenerLicenciasProveedor, { idProveedor: idProv });
            const $sel = $('#selectLicencia').empty().append('<option value="">Seleccione Licencia</option>');
            datos.forEach(i => $sel.append(`<option value="${i.LAN_Id}">${i.LAN_Nombre}</option>`));
        } catch (e) {
            registrarErrorjQuery(e.status, e.message);
        }
    }

    async function cargarComboLicenciasEntes(idProv) {
        try {
            const datos = await $.getJSON(AppConfig.urls.ObtenerLicenciasProveedor, { idProveedor: idProv });
            const $sel = $('#elaLicencias').empty().append('<option value="">Seleccione Licencia</option>');
            datos.forEach(i => $sel.append(`<option value="${i.LAN_Id}">${i.LAN_Nombre}</option>`));
        } catch (e) {
            registrarErrorjQuery(e.status, e.message);
        }
    }

    $('#btnPrevisulizarConceptosLicenciasAnuales').click(async () => {
        mostrarDivCargando();

        try {
            const response = await $.ajax({
                url: window.AppConfig.urls.PrevisualizarConceptosLicenciasAnuales,
                type: 'POST',
                dataType: 'json',
                contentType: 'application/json; charset=utf-8'
            });
            ocultarDivCargando();

            if (!response.success) {
                await Swal.fire({
                    icon: 'error',
                    title: 'Error al previsualizar',
                    html: `<div class="text-start">${response.mensaje}</div>`,
                    confirmButtonText: 'Entendido'
                });
                return;
            }

            // 2) Extraemos preview y posibles errores
            const { partesPreview, partesErrors } = response;

            // 3) Si no hay preview ni errores, avisamos y salimos
            if ((!partesPreview || partesPreview.length === 0) && (!partesErrors || partesErrors.length)) {
                await Swal.fire({
                    title: "Nada que Previsualizar",
                    html: `<p>No hay conceptos de licencias anuales pendientes.</p>`,
                    icon: "info",
                    confirmButtonText: "Entendido",
                    width: 500
                });
                return;
            }

            // 4) Construimos el HTML agrupado por usuario
            let htmlPartes = "";
            if (partesErrors && partesErrors.length) {
                htmlPartes = `
                  <div class="alert alert-warning">
                    <ul>
                      ${partesErrors.map(e => `<li>${e}</li>`).join("")}
                    </ul>
                  </div>
                `;
            }

            if (!partesPreview || partesPreview.length === 0) {
                htmlPartes = `<p>No hay licencias anuales validas para previsualizar.</p>`;
            } else {
                htmlPartes += generarResumenPorLicenciaAnual(partesPreview);
            }

            // 5) Mostramos un Swal con confirmación “Generar”
            const { isConfirmed } = await Swal.fire({
                title: "Previsualizar Conceptos de Licencias Anuales",
                html: `<div class="small text-start">
                         ${htmlPartes}
                       </div>`,
                width: '80%',
                showCancelButton: true,
                cancelButtonText: "Cerrar",
                confirmButtonText: "Generar Conceptos",
                icon: "info"
            });

            if (isConfirmed) {
                await generarConceptosLicenciasAnuales();
            }

        } catch (err) {
            ocultarDivCargando();
            registrarErrorjQuery(err.status, err.message);
        }
    });
});

function generarResumenPorLicenciaAnual(preview) {
    // Si no hay nada que mostrar...
    if (!Array.isArray(preview) || preview.length === 0) {
        return '<p>No se ha generado ningún concepto.</p>';
    }

    // Formateador de moneda en euros (España)
    const formatoEuro = new Intl.NumberFormat('es-ES', {
        style: 'currency',
        currency: 'EUR'
    });

    // Inicio de la lista principal
    let html = '<ul class="text-start">';

    preview.forEach(row => {
        const mes = String(row.Mes).padStart(2, '0');
        const anyo = row.Anyo;
        const importeTotal = formatMoney(row.TotalImporte);

        html += `
            <li>
                <strong>${row.NombreEmpresa}</strong>
                — Periodo: ${mes}/${anyo}
                — Total: <em>${importeTotal}</em>
        `;

        // **Aquí pintamos los errores de este proveedor, si los hay**
        if (row.ListaErrores && row.ListaErrores.length) {
            html += `
        <div class="alert alert-warning">
          <ul class="mb-2" style="margin-bottom: 0px !important;">
            ${row.ListaErrores.map(err => `<li>${err}</li>`).join('')}
          </ul>
        </div>`;
        }

        if (Array.isArray(row.ListaDetalle) && row.ListaDetalle.length) {
            // Agrupamos por IdLicencia
            const agrupadas = {};

            row.ListaDetalle.forEach(det => {
                const id = det.IdLicencia;
                if (!agrupadas[id]) {
                    agrupadas[id] = {
                        NombreLicencia: det.NombreLicencia,
                        Cantidad: 0,
                        Importe: 0
                    };
                }
                // En lugar de usar det.Cantidad, sumamos 1 por cada elemento agrupado
                agrupadas[id].Cantidad += 1;
                agrupadas[id].Importe += det.Importe;
            });

            html += '<ul>';
            Object.values(agrupadas).forEach(det => {
                const imp = formatMoney(det.Importe);
                html += `<li>${det.NombreLicencia} (x${det.Cantidad}): <em>${imp}</em></li>`;
            });
            html += '</ul>';
        }

        html += '</li>';
    });

    html += '</ul>';
    return html;
}

async function generarConceptosLicenciasAnuales() {
    mostrarDivCargando();
    try {
        const response = await $.ajax({
            url: AppConfig.urls.GenerarConceptosLicenciasAnuales,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json'
        });
        ocultarDivCargando();

        if (response.success) {
            let htmlResultado = '';

            // Si el servidor retorna un array de resultados, lo pintamos
            if (Array.isArray(response.resultados) && response.resultados.length) {
                // Calculamos el total global
                const totalConceptos = response.resultados
                    .reduce((sum, r) => sum + (r.ConceptosCreados || 0), 0);

                htmlResultado += `
                    <p><strong>Total conceptos generados:</strong> ${totalConceptos}</p>
                    <ul class="text-start">
                        ${response.resultados.map(r =>
                    `<li><strong>${r.EmpresaNombre}</strong>: ${r.ConceptosCreados} concepto(s)</li>`
                ).join('')}
                    </ul>
                `;
            } else {
                // Si no vienen detalles, mensaje genérico
                htmlResultado = '<p>Conceptos generados correctamente.</p>';
            }

            await Swal.fire({
                title: 'Conceptos generados',
                html: htmlResultado,
                icon: 'success',
                confirmButtonText: 'Aceptar',
                width: '50%'
            });

            // Recarga la tabla de asuntos
            $('#tableAsuntos').DataTable().ajax.reload(null, false);
        } else {
            // Soporta tanto `.mensaje` (español) como `.message` (inglés)
            const msg = response.mensaje || response.message || 'Error al generar conceptos.';
            mostrarToast(msg, TipoToast.Error);
        }
    } catch (err) {
        ocultarDivCargando();
        const msg = obtenerMensajeErrorAjax(err);
        registrarErrorjQuery(err.status || '', msg);
    }
}
