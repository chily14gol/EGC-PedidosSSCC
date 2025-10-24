$(document).ready(function () {
    initAplicaciones();

    InicializarSelectEntesPermitidos();

    async function initAplicaciones() {
        try {
            await VerificarSesionActiva(OpcionMenu.Aplicaciones);
            await ObtenerAplicaciones();
            ocultarDivCargando();

            cargarComboTareas();
            cargarComboTiposEnte();
            //cargarComboEntidades();
            disableSubTabs();

            $('#modalModulos').on('show.bs.modal', function () {
                const $m = $(this);

                // 1) Ocultar siempre la barra de pestañas (hasta que edites un módulo)
                $m.find('#modulosTab').hide();

                // 2) Quitar active/show de TODOS los tab-panes
                $m.find('.tab-pane').removeClass('show active');

                // 3) Activar únicamente el pane “General”
                $m.find('#content-general').addClass('show active');
            });

            $('#table').on('click', '.btn-modulos', function () {
                const app = JSON.parse(this.getAttribute('data-app'));
                $('#modalModulosLabel').text(`Módulos de '${app.APP_Nombre}'`);
                window.currentAppId = app.APP_Id;

                limpiarTodasPestañas();
                disableSubTabs();

                loadModulos(app.APP_Id);
                cargarComboEmpresas();

                // el show.bs.modal ya se encarga de volver a General
                $('#modalModulos').modal('show');
            });

        } catch (xhr) {
            registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
        }
    }

    async function ObtenerAplicaciones() {
        return new Promise(resolve => {
            let tabla = inicializarDataTable('#table', {
                ajax: {
                    url: AppConfig.urls.ObtenerAplicaciones,
                    type: 'GET',
                    dataSrc: '',
                    complete: function () {
                        resolve();
                    }
                },
                columns: [
                    { data: 'APP_Nombre', title: 'Nombre' },
                    {
                        data: 'NombreTarea',
                        title: 'Tarea',
                        render: v => v || '-'
                    },
                    {
                        className: 'td-btn',
                        data: null,
                        title: '<span class="sReader">Acción</span>',
                        orderable: false,
                        render: function (_, __, row) {
                            const jsRow = JSON.stringify(row).replace(/"/g, '&quot;');
                            return `<div class="btn-group" role="group">
                                <button class="btn btn-icon btn-editar me-2" data-app="${jsRow}" onclick="editarAplicacion(this)">
                                    <i class="bi bi-pencil-square" title="Editar"></i>
                                </button>
                                <button class="btn btn-icon btn-tarifas me-2" data-app="${jsRow}" onclick="verTarifas(this)">
                                    <i class="bi bi-currency-dollar" title="Tarifas"></i>
                                </button>
                                <button class="btn btn-icon btn-modulos me-2" data-app="${jsRow}">
                                    <i class="bi bi-folder" title="Módulos"></i>
                                </button>
                                <button class="btn btn-icon btn-entes me-2" data-app="${jsRow}" onclick="verEntesAplicacion(this)">
                                    <i class="bi bi-people" title="Entidades"></i>
                                </button>
                                <button class="btn btn-icon btn-eliminar me-2" data-app="${jsRow}" onclick="eliminarAplicacion(this)">
                                    <i class="bi bi-trash" title="Eliminar"></i>
                                </button>
                             </div>`;
                        }
                    }
                ]
            }, [], 'export_aplicaciones');

            $(window).resize(() => tabla.columns.adjust().draw());
        });
    }

    $('#btnNuevo').click(function () {
        $('#idAplicacion').val(-1);
        $('#nombre, #tarea').val('');
        resetTiposEnte();
        $('#modalEditarLabel').text('Nueva Aplicación');
        $('#modalEditar').modal('show');
    });

    // Al abrir el modal: foco en “Entidades”
    $('#modalEntesAplicacion').on('shown.bs.modal', () => {
        $('#entidades').focus();
    });

    // Atajo de teclado: Ctrl/Cmd + Enter para guardar
    $('#modalEntesAplicacion').on('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
            guardarAplicacionEnte();
        }
    });

    window.editarAplicacion = function (b) {
        const objApp = JSON.parse(b.getAttribute('data-app'));

        InicializarSelectEntesPermitidos();
        resetTiposEnte();        
        ObtenerAplicacionesTiposEnte(objApp.APP_Id);

        $('#idAplicacion').val(objApp.APP_Id);
        $('#nombre').val(objApp.APP_Nombre);
        $('#tarea').val(objApp.APP_TAR_Id);
        $('#modalEditarLabel').text('Editar Aplicación');
        $('#modalEditar').modal('show');
    };

    window.eliminarAplicacion = function (b) {
        const a = JSON.parse(b.getAttribute('data-app'));
        mostrarAlertaConfirmacion({
            titulo: `¿Eliminar aplicación '${a.APP_Nombre}'?`,
            onConfirmar: () => {
                $.ajax({
                    url: AppConfig.urls.EliminarAplicacion,
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ idAplicacion: a.APP_Id }),
                    dataType: 'json'
                })
                    .done(resp => {
                        if (resp.success) {
                            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                            $('#table').DataTable().ajax.reload(null, false);
                        } else {
                            mostrarToast(resp.mensaje, TipoToast.Warning);
                        }
                    })
                    .fail(xhr => registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr)));
            }
        });
    };

    window.guardarAplicacion = async function () {
        const id = $('#idAplicacion').val(),
            nombre = $('#nombre').val(),
            tarea = $('#tarea').val();

        let invalidos = [];
        if (!nombre) { $('#nombre').addClass('is-invalid'); invalidos.push('Nombre'); }
        else $('#nombre').removeClass('is-invalid');

        if (!tarea) { $('#tarea').addClass('is-invalid'); invalidos.push('Tarea'); }
        else $('#tarea').removeClass('is-invalid');

        if (invalidos.length) {
            mostrarToast(AppConfig.mensajes.camposObligatorios + invalidos.join(', '), TipoToast.Warning);
            return;
        }

        const dto = {
            APP_Id: id,
            APP_Nombre: nombre,
            APP_TAR_Id: tarea || null,
            TiposEnte: $('#tiposEnte').val()
        };

        try {
            const resp = await $.ajax({
                url: AppConfig.urls.GuardarAplicacion,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(dto),
                dataType: 'json'
            });
            if (resp.success) {
                mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                $('#modalEditar').modal('hide');
                $('#table').DataTable().ajax.reload(null, false);
            } else {
                mostrarToast(resp.message || "Error al guardar.", TipoToast.Warning);
            }
        } catch (xhr) {
            registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
        }
    };

    function InicializarSelectEntesPermitidos() {
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

    function resetTiposEnte(clearOptions = false) {
        const $s = $('#tiposEnte');
        if (clearOptions) {
            // (opcional) vaciar opciones si quieres reconstruirlas después
            $s.find('option').remove().end().append('<option value=""></option>');
        }
        // Quitar selección y notificar a Select2
        $s.val(null).trigger('change');
    }

    async function ObtenerAplicacionesTiposEnte(idApp) {
        try {
            const response = await fetch(`${AppConfig.urls.ObtenerTiposEntePorAplicacion}?idAplicacion=${idApp}`, {
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

    // ——————————————
    // Tarifas por Aplicación
    // ——————————————
    function ObtenerTarifas(idApp) {
        let tabla = inicializarDataTable('#tableTarifas', {
            paging: false,
            searching: false,
            info: false,
            ordering: false,
            dom: 't',
            ajax: {
                url: AppConfig.urls.ObtenerAplicacionesTarifas,
                type: 'GET',
                data: { idAplicacion: idApp },
                dataSrc: ''
            },
            columns: [
                { data: 'APT_FechaInicio', title: 'Fecha Inicio', render: formatDateToDDMMYYYY },
                { data: 'APT_FechaFin', title: 'Fecha Fin', render: formatDateToDDMMYYYY },
                {
                    data: 'APT_PrecioUnitario',
                    title: 'Importe Unitario',
                    className: 'dt-type-numeric-with-decimal',
                    render: function (data) {
                        return formatMoney(data);
                    }
                },
                {
                    className: 'td-btn',
                    data: null,
                    title: '',
                    orderable: false,
                    render: (_, __, row) => {
                        const jsRow = JSON.stringify(row).replace(/"/g, '&quot;');
                        return `
                            <button class="btn btn-icon btn-sm me-1" onclick="editTarifa(${jsRow})">
                                <i class="bi bi-pencil-square"></i>
                            </button>
                            <button class="btn btn-icon btn-sm" data-tarifa="${jsRow}" onclick="eliminarTarifa(this)">
                              <i class="bi bi-trash" title="Eliminar"></i>
                            </button>`;
                    }
                }
            ]
        });
        $(window).resize(() => tabla.columns.adjust().draw());
    }
    function limpiarFormularioTarifa() {
        $('#tarifaFechaInicio, #tarifaFechaFin').val('').prop('disabled', false);
        $('#tarifaPrecio').val('');
        $('#formTarifas').hide();
        $('#btnGuardarTarifa').hide();
    }
    function cancelarTarifa() {
        $('#btnNuevaTarifa').show();
        $('#btnCancelarTarifa').hide();
        $('#btnGuardarTarifa').hide();
        $('#formTarifas').hide();
    }

    $('#btnNuevaTarifa').on('click', function () {
        limpiarFormularioTarifa();
        $('#btnNuevaTarifa').hide();
        $('#btnCancelarTarifa').show();
        $('#btnGuardarTarifa').show();
        $('#formTarifas').slideDown();
    });

    $('#btnCancelarTarifa').on('click', function () {
        cancelarTarifa();
    });

    window.verTarifas = function (btn) {
        const app = JSON.parse(btn.getAttribute('data-app'));
        $('#idAplicacionTarifas').val(app.APP_Id);
        $('#fechaInicioOriginal, #tarifaFechaInicio, #tarifaFechaFin, #tarifaPrecio').val('');

        ObtenerTarifas(app.APP_Id);
        limpiarFormularioTarifa();

        $('#formTarifas').hide();
        $('#btnNuevaTarifa').show();
        $('#modalTarifas').modal('show');
    };

    window.editTarifa = function (obj) {
        $('#fechaInicioOriginal').val(formatDateInputForDateField(obj.APT_FechaInicio));
        $('#tarifaFechaInicio').val(formatDateInputForDateField(obj.APT_FechaInicio));
        $('#tarifaFechaFin').val(obj.AMT_FechaFin ? formatDateInputForDateField(obj.APT_FechaFin) : '');
        $('#tarifaPrecio').val(obj.APT_PrecioUnitario);

        $('#btnNuevaTarifa').hide();
        $('#btnGuardarTarifa').show();
        $('#btnCancelarTarifa').show();
        $('#formTarifas').slideDown();
    };

    window.eliminarTarifa = function (btn) {
        setTimeout(() => {
            const t = JSON.parse(btn.getAttribute('data-tarifa'));
            mostrarAlertaConfirmacion({
                titulo: `¿Eliminar tarifa con fecha de inicio ${formatDateToDDMMYYYY(t.APT_FechaInicio)}?`,
                onConfirmar: () => {
                    $.ajax({
                        url: AppConfig.urls.EliminarAplicacionTarifa,
                        type: 'POST',
                        contentType: 'application/json',
                        data: JSON.stringify({
                            idAplicacion: t.APT_APP_Id,
                            fechaInicio: toISODate(t.APT_FechaInicio)
                        }),
                        dataType: 'json'
                    })
                        .done(resp => {
                            if (resp.success) {
                                mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                                $('#tableTarifas').DataTable().ajax.reload(null, false);
                            } else {
                                mostrarToast(resp.message, TipoToast.Warning);
                            }
                        })
                        .fail(xhr => registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr)));
                }
            });
        }, 300);
    };

    window.guardarTarifa = async function () {
        const idApp = parseInt($('#idAplicacionTarifas').val(), 10),
            orig = $('#fechaInicioOriginal').val(),
            inicio = $('#tarifaFechaInicio').val(),
            fin = $('#tarifaFechaFin').val(),
            precio = $('#tarifaPrecio').val();

        let invalid = [];
        if (!inicio) $('#tarifaFechaInicio').addClass('is-invalid'), invalid.push('Fecha Inicio');
        else $('#tarifaFechaInicio').removeClass('is-invalid');

        if (!precio) $('#tarifaPrecio').addClass('is-invalid'), invalid.push('Precio');
        else $('#tarifaPrecio').removeClass('is-invalid');

        if (invalid.length) {
            mostrarToast(AppConfig.mensajes.camposObligatorios + invalid.join(', '), TipoToast.Warning);
            return;
        }

        if (fin && new Date(fin) < new Date(inicio)) {
            $('#tarifaFechaFin').addClass('is-invalid');
            mostrarToast("Fecha Fin no puede ser anterior a Fecha Inicio.", TipoToast.Warning);
            return;
        }
        $('#tarifaFechaFin').removeClass('is-invalid');

        const dto = {
            APT_APP_Id: idApp,
            APT_FechaInicio: formatDateToDDMMYYYY(inicio),
            APT_FechaFin: fin ? formatDateToDDMMYYYY(fin) : null,
            APT_PrecioUnitario: parseFloat(precio)
        };
        if (orig) dto.APT_FechaInicioOriginal = formatDateToDDMMYYYY(orig);

        try {
            const resp = await $.ajax({
                url: AppConfig.urls.GuardarAplicacionTarifa,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(dto),
                dataType: 'json'
            });
            if (resp.success) {
                mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                limpiarFormularioTarifa();
                cancelarTarifa();
                ObtenerTarifas(idApp);
                $('#fechaInicioOriginal, #tarifaFechaInicio, #tarifaFechaFin, #tarifaPrecio').val('');
            } else {
                mostrarToast(resp.message || "Error al guardar.", TipoToast.Warning);
            }
        } catch (xhr) {
            registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
        }
    };

    // ——————————————
    // Módulos
    // ——————————————
    function limpiarTodasPestañas() {
        // Reset formModulo
        $('#idModulo, #moduloNombre, #tareaModulo').val('');
        // Reset demás formularios
        $('#formModEmpresas')[0].reset();
        $('#formModTarifas')[0].reset();
    }

    $('#modalModulos').on('hidden.bs.modal', () => {
        $('#btnCancelarModulo').hide();
        $('#btnGuardarModulo').hide();
        $('#btnNuevoModulo').show();
        $('#formModulo').hide();
        $('#modulosTab').hide();
        $('#lblModuloActual').text('');
        disableSubTabs();
    });
    function loadModulos(appId) {
        if ($.fn.dataTable.isDataTable('#tableModulos')) {
            $('#tableModulos').DataTable().destroy();
        }

        $('#tableModulos').DataTable({
            ajax: {
                url: AppConfig.urls.ObtenerAplicacionesModulos,
                data: { idAplicacion: appId },
                dataSrc: ''
            },
            columns: [
                { data: 'APM_Nombre', title: 'Módulo' },
                { data: 'TareaNombre', title: 'Tarea' },
                {
                    data: null, orderable: false, className: 'td-btn', render: (_, __, r) =>
                        `<div class="btn-group" role="group">
                            <button type="button" class="btn btn-icon me-2" onclick="editModulo(${r.APM_Id},'${r.APM_Nombre.replace(/'/g, "\\'")}', ${r.APM_TAR_Id})">
                                <i class="bi bi-pencil-square"></i>
                             </button>
                             <button type="button" class="btn btn-icon me-2" onclick="delModulo(${r.APM_Id})">
                                <i class="bi bi-trash"></i>
                             </button>
                         </div>`
                }
            ],
            paging: false, searching: false, info: false, dom: 't'
        });
    }

    window.editModulo = function (id, nombre, idTarea) {
        enableSubTabs();

        $('#lblModuloActual')
            .html(`<i class="bi bi-pencil-fill me-1"></i> ${nombre}`)
            .text(`Editando: ${nombre}`)
            .show();

        limpiarTodasPestañas();
        $('#idModulo').val(id);
        $('#moduloNombre').val(nombre);
        $('#tareaModulo').val(idTarea);

        // Muestra form y botón Cancelar; oculta Nuevo
        $('#formModulo').slideDown();
        $('#modulosTab').slideDown();
        $('#btnCancelarModulo').show();
        $('#btnGuardarModulo').show();
        $('#btnNuevoModulo').hide();

        $('#tab-general').trigger('click');
        $('#idModuloEmpresas').val(id);
        $('#idModuloTarifas').val(id);
        loadEmpresas(id);
        loadTarifas(id);
    }

    window.delModulo = function (id) {
        mostrarAlertaConfirmacion({
            titulo: `¿Eliminar módulo?`,
            onConfirmar: () => {
                $.ajax({
                    url: AppConfig.urls.EliminarAplicacionModulo,
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ id }),
                    dataType: 'json'
                }).done(resp => {
                    if (resp.success) {
                        $('#tableModulos').DataTable().ajax.reload(null, false);
                    }
                });
            }
        });
    }

    window.guardarModulo = function (id) {
        const dto = {
            APM_Id: parseInt($('#idModulo').val() || 0),
            APM_APP_Id: window.currentAppId,
            APM_Nombre: $('#moduloNombre').val(),
            APM_TAR_Id: $('#tareaModulo').val()
        };

        let invalid = [];
        if (!dto.APM_Nombre) $('#moduloNombre').addClass('is-invalid'), invalid.push('Nombre');
        else $('#moduloNombre').removeClass('is-invalid');

        if (!dto.APM_TAR_Id) $('#tareaModulo').addClass('is-invalid'), invalid.push('Tarea');
        else $('#tareaModulo').removeClass('is-invalid');

        if (invalid.length) {
            mostrarToast(AppConfig.mensajes.camposObligatorios + invalid.join(', '), TipoToast.Warning);
            return;
        }

        $.ajax({
            url: AppConfig.urls.GuardarAplicacionModulo,
            method: 'POST', contentType: 'application/json',
            data: JSON.stringify(dto), dataType: 'json'
        }).done(resp => {
            if (resp.success) {
                $('#formModulo').slideUp();
                $('#modulosTab').slideUp();
                $('#idModulo').val('');
                $('#moduloNombre').val('');
                $('#tareaModulo').val('');
                $('#btnCancelarModulo').hide();
                $('#btnGuardarModulo').hide();
                $('#btnNuevoModulo').show();
                $('#lblModuloActual').hide();
                // Si no hay ningún módulo seleccionado, desactiva sub-tabs
                disableSubTabs();
                $('#tableModulos').DataTable().ajax.reload(null, false);
            }
        });
    }

    $('#btnNuevoModulo').on('click', function () {
        // Prepara el formulario vacío
        $('#idModulo, #moduloNombre, #tareaModulo').val('');
        $('#formModulo .is-invalid').removeClass('is-invalid');
        $('#modulosTab .is-invalid').removeClass('is-invalid');

        // Muestra form y botón Cancelar; oculta Nuevo
        $('#formModulo').slideDown();
        $('#modulosTab').slideDown();
        $('#btnCancelarModulo').show();
        $('#btnGuardarModulo').show();
        $('#btnNuevoModulo').hide();

        // Activa sub-tabs sólo mientras se edita/crea
        disableSubTabs();
        loadEmpresas(0);
        loadTarifas(0);
    });

    $('#btnCancelarModulo').on('click', function () {
        $('#formModulo').slideUp();
        $('#modulosTab').slideUp();
        $('#btnCancelarModulo').hide();
        $('#btnGuardarModulo').hide();
        $('#btnNuevoModulo').show();
        $('#lblModuloActual').hide();

        // Si no hay ningún módulo seleccionado, desactiva sub-tabs
        disableSubTabs();
        loadEmpresas(0);
        loadTarifas(0);
    });

    // ——————————————
    // Módulos -> Empresas
    // ——————————————
    $(function () {
        $('#contenedorFormEmpresas').hide();
        $('#btnCancelarEmpresa').hide();

        $('#btnNuevaEmpresa').on('click', function () {
            limpiarFormularioEmpresa();
            $(this).hide();
            $('#btnCancelarEmpresa').show();
            $('#contenedorFormEmpresas').slideDown();
        });

        $('#btnCancelarEmpresa').on('click', function () {
            cancelarEmpresa();
        });
        function limpiarFormularioEmpresa() {
            $('#formModEmpresas')[0].reset();
            $('#idEmpresaOriginal').val('');    // para que sepa que es nueva
            // también limpia validaciones
            $('#formModEmpresas .is-invalid').removeClass('is-invalid');
        }
        function cancelarEmpresa() {
            $('#contenedorFormEmpresas').slideUp(function () {
                $('#btnNuevaEmpresa').show();
                $('#btnCancelarEmpresa').hide();
            });
        }

        window.editEmpresa = function (obj) {
            $('#idEmpresaOriginal').val(obj.AME_Id);
            $('#empresas').val(obj.AME_EMP_Id);
            $('#empresaFechaInicio').val(formatDateInputForDateField(obj.AME_FechaInicio));
            $('#empresaFechaFin').val(obj.AME_FechaFin ? formatDateInputForDateField(obj.AME_FechaFin) : '');
            $('#empresaImporte').val(obj.AME_ImporteMensual);
            $('#porcentajeReparto').val(obj.AME_PorcentajeReparto ?? '');
            $('#empresaDescripcion').val(obj.AME_DescripcionConcepto || '');

            $('#btnNuevaEmpresa').hide();
            $('#btnCancelarEmpresa').show();
            $('#contenedorFormEmpresas').slideDown();
        };
    });
    function loadEmpresas(idModulo) {
        if ($.fn.dataTable.isDataTable('#tableEmpresas')) $('#tableEmpresas').DataTable().destroy();
        $('#tableEmpresas').DataTable({
            ajax: { url: AppConfig.urls.ObtenerModulosEmpresas, data: { idModulo }, dataSrc: '' },
            columns: [
                { data: 'EmpresaNombre', title: 'Empresa' },
                { data: 'AME_FechaInicio', title: 'Fecha Inicio', render: formatDateToDDMMYYYY },
                { data: 'AME_FechaFin', title: 'Fecha Fin', render: formatDateToDDMMYYYY },
                { data: 'AME_ImporteMensual', title: 'Imp. Mensual', render: d => formatMoney(d) },
                { data: 'AME_PorcentajeReparto', title: '% Reparto', render: d => formatPorcentaje(d) },
                { data: 'AME_DescripcionConcepto', title: 'Concepto' },
                {
                    data: null,
                    orderable: false,
                    className: 'td-btn',
                    responsivePriority: 2,
                    render: function (_, __, row) {
                        const jsRow = JSON.stringify(row).replace(/"/g, '&quot;');
                        return `<button class="btn btn-icon btn-sm me-1" onclick="editEmpresa(${jsRow})"><i class="bi bi-pencil-square"></i></button>
                            <button class="btn btn-icon btn-sm" onclick="delEmpresa(${row.AME_Id})"><i class="bi bi-trash"></i></button>`;
                    }      
                }
            ], paging: false, searching: false, info: false, dom: 't'
        });
    }

    window.delEmpresa = function (id) {
        mostrarAlertaConfirmacion({
            titulo: '¿Eliminar empresa?',
            onConfirmar: () => {
                $.ajax({
                    url: AppConfig.urls.EliminarModuloEmpresa,
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ id }),
                    dataType: 'json'
                }).done(resp => {
                    if (resp.success) {
                        $('#tableEmpresas').DataTable().ajax.reload(null, false);
                    }
                });
            }
        });
    }

    window.guardarEmpresaModulo = async function () {
        const
            idEmpresa = parseInt($('#empresas').val(), 10),
            inicio = $('#empresaFechaInicio').val(),
            fin = $('#empresaFechaFin').val(),
            precioStr = $('#empresaImporte').val(),
            precio = precioStr === '' ? null : parseFloat(precioStr),
            porcentajeRepartoStr = $('#porcentajeReparto').val(),
            porcentajeReparto = porcentajeRepartoStr === '' ? null : parseFloat(porcentajeRepartoStr),
            descripcion = $('#empresaDescripcion').val();

        let invalid = [];
        if (!idEmpresa) {
            $('#empresas').addClass('is-invalid');
            invalid.push('Empresa');
        } else $('#empresas').removeClass('is-invalid');

        if (!inicio) {
            $('#empresaFechaInicio').addClass('is-invalid');
            invalid.push('Fecha Inicio');
        } else $('#empresaFechaInicio').removeClass('is-invalid');

        // aquí solo marcamos error si está vacío, no si es 0
        if (precio === null || isNaN(precio)) {
            $('#empresaImporte').addClass('is-invalid');
            invalid.push('Importe Mensual');
        } else $('#empresaImporte').removeClass('is-invalid');

        if (porcentajeReparto === null || isNaN(porcentajeReparto)) {
            $('#porcentajeReparto').addClass('is-invalid');
            invalid.push('Porcentaje Reparto');
        } else $('#porcentajeReparto').removeClass('is-invalid');

        if (invalid.length) {
            mostrarToast(AppConfig.mensajes.camposObligatorios + invalid.join(', '), TipoToast.Warning);
            return;
        }

        if (fin && new Date(fin) < new Date(inicio)) {
            $('#empresaFechaFin').addClass('is-invalid');
            mostrarToast('Fecha Fin no puede ser anterior a Fecha Inicio.', TipoToast.Warning);
            return;
        }
        $('#empresaFechaFin').removeClass('is-invalid');

        const dto = {
            AME_Id: parseInt($('#idEmpresaOriginal').val(), 10),
            AME_APM_Id: parseInt($('#idModuloEmpresas').val(), 10),
            AME_EMP_Id: idEmpresa,
            AME_FechaInicio: formatDateToDDMMYYYY(inicio),
            AME_FechaFin: fin ? formatDateToDDMMYYYY(fin) : null,
            AME_ImporteMensual: precio,
            AME_DescripcionConcepto: descripcion,
            AME_PorcentajeReparto: porcentajeReparto
        };

        const resp = await $.ajax({
            url: AppConfig.urls.GuardarModuloEmpresa,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(dto),
            dataType: 'json'
        });

        if (resp.success) {
            $('#contenedorFormEmpresas').hide();
            $('#btnCancelarEmpresa').hide();
            $('#btnNuevaEmpresa').show();
            $('#tableEmpresas').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast(resp.message, TipoToast.Warning);
        }
    };
    
    // ——————————————
    // Módulos -> Tarifas
    // ——————————————
    $(function () {
        $('#contenedorFormTarifas').hide();
        $('#btnCancelarTarifaModal').hide();

        $('#btnNuevaTarifaModal').on('click', function () {
            limpiarFormularioTarifa();
            $(this).hide();
            $('#btnCancelarTarifaModal').show();
            $('#contenedorFormTarifas').slideDown();
        });

        $('#btnCancelarTarifaModal').on('click', function () {
            cancelarTarifa();
        });
        function limpiarFormularioTarifa() {
            $('#formModTarifas')[0].reset();
            $('#idTarifaOriginal').val('');    // para que sepa que es nueva
            $('#formModTarifas .is-invalid').removeClass('is-invalid');
        }
        function cancelarTarifa() {
            $('#contenedorFormTarifas').slideUp(function () {
                $('#btnNuevaTarifaModal').show();
                $('#btnCancelarTarifaModal').hide();
            });
        }

        window.editTarifaModulo = function (obj) {
            $('#idTarifaOriginal').val(obj.AMT_Id);
            $('#tarifaFechaInicioModulo').val(formatDateInputForDateField(obj.AMT_FechaInicio));
            $('#tarifaFechaFinModulo').val(obj.AMT_FechaFin ? formatDateInputForDateField(obj.AMT_FechaFin) : '');
            $('#tarifaImporteReparto').val(obj.AMT_ImporteMensualReparto);
            $('#tarifaImportePorc').val(obj.AMT_ImporteMensualRepartoPorcentajes);

            $('#btnNuevaTarifaModal').hide();
            $('#btnCancelarTarifaModal').show();
            $('#contenedorFormTarifas').slideDown();
        };
    });
    function loadTarifas(idModulo) {
        if ($.fn.dataTable.isDataTable('#tableTarifasModulos')) $('#tableTarifasModulos').DataTable().destroy();
        $('#tableTarifasModulos').DataTable({
            ajax: { url: AppConfig.urls.ObtenerModulosTarifas, data: { idModulo }, dataSrc: '' },
            columns: [
                { data: 'AMT_FechaInicio', title: 'Fecha Inicio', render: formatDateToDDMMYYYY },
                { data: 'AMT_FechaFin', title: 'Fecha Fin', render: formatDateToDDMMYYYY },
                { data: 'AMT_ImporteMensualReparto', title: 'Importe reparto equitativo', render: d => formatMoney(d) },
                { data: 'AMT_ImporteMensualRepartoPorcentajes', title: 'Importe reparto porcentajes', render: d => formatMoney(d) },
                {
                    data: null, orderable: false, className: 'td-btn',
                    render: function (_, __, row) {
                        const jsRow = JSON.stringify(row).replace(/"/g, '&quot;');
                        return `<button class="btn btn-icon btn-sm me-1" onclick="editTarifaModulo(${jsRow})"><i class="bi bi-pencil-square"></i></button>
                        <button class="btn btn-icon btn-sm" onclick="delTarifaModulo(${row.AMT_Id})"><i class="bi bi-trash"></i></button>`
                    }
                }
            ], paging: false, searching: false, info: false, dom: 't'
        });
    }

    window.delTarifaModulo = function (id) {
        mostrarAlertaConfirmacion({
            titulo: '¿Eliminar tarifa?',
            onConfirmar: () => {
                $.ajax({
                    url: AppConfig.urls.EliminarModuloTarifa,
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ id }),
                    dataType: 'json'
                }).done(resp => {
                    if (resp.success) {
                        $('#tableTarifasModulos').DataTable().ajax.reload(null, false);
                    }
                });
            }
        });
    }

    window.guardarTarifaModulo = function () {
        const
            inicio = $('#tarifaFechaInicioModulo').val(),
            fin = $('#tarifaFechaFinModulo').val(),
            importe = $('#tarifaImporteReparto').val(),
            reparto = $('#tarifaImportePorc').val();
            
        let invalid = [];
        if (!inicio) $('#tarifaFechaInicioModulo').addClass('is-invalid'), invalid.push('Fecha Inicio');
        else $('#tarifaFechaInicioModulo').removeClass('is-invalid');

        if (!importe) $('#tarifaImporteReparto').addClass('is-invalid'), invalid.push('Importe');
        else $('#tarifaImporteReparto').removeClass('is-invalid');

        if (!reparto) $('#tarifaImportePorc').addClass('is-invalid'), invalid.push('Reparto');
        else $('#tarifaImportePorc').removeClass('is-invalid');

        if (invalid.length) {
            mostrarToast(AppConfig.mensajes.camposObligatorios + invalid.join(', '), TipoToast.Warning);
            return;
        }

        if (fin && new Date(fin) < new Date(inicio)) {
            $('#tarifaFechaFinModulo').addClass('is-invalid');
            mostrarToast("Fecha Fin no puede ser anterior a Fecha Inicio.", TipoToast.Warning);
            return;
        }
        $('#tarifaFechaFinModulo').removeClass('is-invalid');

        const dto = {
            AMT_Id: parseInt($('#idTarifaOriginal').val() || 0),
            AMT_APM_Id: parseInt($('#idModuloTarifas').val(), 10),
            AMT_FechaInicio: formatDateToDDMMYYYY(inicio),
            AMT_FechaFin: fin ? formatDateToDDMMYYYY(fin) : null,
            AMT_ImporteMensualReparto: parseFloat(importe),
            AMT_ImporteMensualRepartoPorcentajes: parseFloat(reparto)
        };

        $.ajax({
            url: AppConfig.urls.GuardarModuloTarifa,
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(dto),
            dataType: 'json'
        }).done(resp => {
            if (resp.success) {
                $('#contenedorFormTarifas').hide();
                $('#btnCancelarTarifaModal').hide();
                $('#btnNuevaTarifaModal').show();
                $('#tableTarifasModulos').DataTable().ajax.reload(null, false);
            } else {
                mostrarToast(resp.message || "Error al guardar.", TipoToast.Warning);
            }
        });
    }

    // ——————————————
    // Entidades ↔ Aplicación
    // ——————————————
    function ObtenerEntesAplicaciones(idApp) {
        if ($.fn.dataTable.isDataTable('#tableEntesAplicacion')) {
            $('#tableEntesAplicacion').DataTable().destroy();
        }

        const tabla = $('#tableEntesAplicacion').DataTable({
            ajax: {
                url: AppConfig.urls.ObtenerEntesAplicaciones,
                type: 'GET',
                data: { idAplicacion: idApp },
                dataSrc: ''
            },
            rowId: row => `fila-ente-${row.ENL_ENT_Id}`,
            columns: [
                { data: 'NombreEntidad', title: 'Entidad' },
                { data: 'EMP_Nombre', title: 'Empresa' },
                { data: 'ENL_FechaInicio', render: formatDateToDDMMYYYY, title: 'Fecha Inicio' },
                { data: 'ENL_FechaFin', render: formatDateToDDMMYYYY, title: 'Fecha Fin' },
                {
                    className: 'td-btn', data: null, title: '', orderable: false,
                    render: (data, type, row) => {
                        const js = JSON.stringify(row).replace(/"/g, '&quot;');
                        return `
                      <button class="btn btn-icon btn-eliminar" data-ente="${js}" onclick="eliminarAplicacionEnte(this)">
                        <i class="bi bi-trash" title="Eliminar"></i>
                      </button>`;
                    }
                }
            ],
            paging: false,
            searching: true,
            info: false,
            ordering: false,
            // Quitamos 'B' del dom; los botones los pondremos nosotros en la toolbar
            dom: 'frtipB',
            buttons: [
                {
                    extend: 'excelHtml5',
                    text: 'Exportar a Excel',
                    titleAttr: 'Exportar a Excel',
                    className: 'btn btn-sm btn-success',         // estilo Bootstrap
                    title: `EntesAplicacion_${new Date().toISOString().slice(0, 10)}`,
                    exportOptions: { columns: [0, 1, 2] }
                }
            ]
        });

        // Colocamos el contenedor de botones en la toolbar, a la derecha del filtro
        tabla.buttons().container().appendTo('#toolbarEntesButtons');

        // Filtro por trabajador (columna 0)
        $('#filtroTrabajador').off('keyup change').on('keyup change', function () {
            tabla.column(0).search(this.value).draw();
        });

        // Ajuste columnas al redimensionar
        $(window).off('resize.dtEntes').on('resize.dtEntes', () => tabla.columns.adjust().draw());
    }

    window.verEntesAplicacion = function (b) {
        const a = JSON.parse(b.getAttribute('data-app'));
        $('#idAplicacionEntes').val(a.APP_Id);
        $('#idEnteOriginal, #entidades, #fechaInicio, #fechaFin').val('');

        // NUEVO: cargar entidades filtradas por los TiposEnte permitidos de esta app
        cargarComboEntidadesPorApp(a.APP_Id);

        ObtenerEntesAplicaciones(a.APP_Id);
        $('#modalEntesAplicacion').modal('show');
    };


    window.eliminarAplicacionEnte = function (b) {
        setTimeout(() => {
            const o = JSON.parse(b.getAttribute('data-ente'));
            mostrarAlertaConfirmacion({
                titulo: `¿Eliminar asignación de '${o.NombreEntidad}'?`,
                onConfirmar: () => {
                    $.ajax({
                        url: AppConfig.urls.EliminarAplicacionEnte,
                        type: 'POST',
                        contentType: 'application/json',
                        data: JSON.stringify({
                            idAplicacion: o.ENL_APP_Id,
                            idEnte: o.ENL_ENT_Id,
                            fechaInicio: toISODate(o.ENL_FechaInicio)
                        }),
                        dataType: 'json'
                    })
                        .done(resp => {
                            if (resp.success) {
                                mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                                $('#tableEntesAplicacion').DataTable().ajax.reload(null, false);
                            } else {
                                mostrarToast(resp.message, TipoToast.Warning);
                            }
                        })
                        .fail(xhr => registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr)));
                }
            });
        }, 300);
    };

    window.guardarAplicacionEnte = async function () {
        const idApp = $('#idAplicacionEntes').val(),
            orig = $('#idEnteOriginal').val(),
            entId = $('#entidades').val(),
            inicio = $('#fechaInicio').val(),
            fin = $('#fechaFin').val();

        let invalid = [];
        if (!entId) { $('#entidades').addClass('is-invalid'); invalid.push('Entidad'); }
        else $('#entidades').removeClass('is-invalid');

        if (!inicio) { $('#fechaInicio').addClass('is-invalid'); invalid.push('Fecha Inicio'); }
        else $('#fechaInicio').removeClass('is-invalid');

        if (invalid.length) {
            mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
            return;
        }

        if (fin && new Date(fin) < new Date(inicio)) {
            $('#fechaFin').addClass('is-invalid');
            mostrarToast("Fecha Fin no puede ser anterior a Fecha Inicio.", TipoToast.Warning);
            return;
        }
        $('#fechaFin').removeClass('is-invalid');

        const dto = {
            ENL_APP_Id: parseInt(idApp),
            ENL_ENT_Id: parseInt(entId),
            ENL_FechaInicio: formatDateToDDMMYYYY(inicio),
            ENL_FechaFin: fin ? formatDateToDDMMYYYY(fin) : null
        };
        if (orig) dto.ENL_ENT_IdOriginal = parseInt(orig);

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
                $('#idEnteOriginal, #entidades, #fechaInicio, #fechaFin').val('');
                $('#tableEntesAplicacion').DataTable().ajax.reload(null, false);
            } else {
                await Swal.fire({
                    icon: 'warning',
                    title: 'No se ha guardado',
                    text: resp.message || 'La entidad ya existe para esas fechas.',
                    confirmButtonText: 'Entendido'
                });
            }
        } catch (xhr) {
            registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
        }
    }; 

    // ——————————————
    // Combos
    // ——————————————
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
                    const combos = ['#tarea', '#tareaModulo'];
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
                    registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
                }
            });

        } catch (xhr) {
            registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
        }
    }

    async function cargarComboTiposEnte() {
        try {
            const datos = await $.ajax({ url: AppConfig.urls.ObtenerComboTiposEnte, type: 'GET', dataType: 'json' });
            const $s = $('#tiposEnte').empty();
            datos.forEach(i => $s.append(`<option value="${i.TEN_Id}">${i.TEN_Nombre}</option>`));
        } catch (xhr) {
            registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
        }
    }

    async function cargarComboEntidadesPorApp(appId) {
        try {
            // 1) Pedimos los TiposEnte de la app (el endpoint devuelve todos + selected)
            const tipos = await $.ajax({
                url: AppConfig.urls.ObtenerTiposEntePorAplicacion,
                type: 'GET',
                dataType: 'json',
                data: { idAplicacion: appId }
            });

            const permitidos = (tipos || [])
                .filter(t => t.selected)
                .map(t => t.id);

            const $e = $('#entidades')
                .empty()
                .append('<option value="">Seleccione entidad</option>');

            // 2) Si no hay tipos permitidos, inicializamos Select2 y salimos
            if (!permitidos.length) {
                $('#entidades').select2({
                    placeholder: 'Sin tipos permitidos',
                    allowClear: true,
                    dropdownParent: $('#modalEntesAplicacion'),
                    width: '100%'
                });
                return;
            }

            // 3) Pedimos las entidades filtradas por esos tipos
            const datos = await $.ajax({
                url: AppConfig.urls.ObtenerEntidadesComboPorTipo,
                type: 'GET',
                dataType: 'json',
                data: { idTiposEntidad: permitidos },
                traditional: true
            });

            datos.forEach(i => $e.append(`<option value="${i.ENT_Id}">${i.Text}</option>`));

            $('#entidades').select2({
                placeholder: 'Seleccione entidad',
                allowClear: true,
                dropdownParent: $('#modalEntesAplicacion'),
                width: '100%'
            });
        } catch (xhr) {
            registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
        }
    }
    function cargarComboEmpresas() {
        $.getJSON(AppConfig.urls.ObtenerComboEmpresas, datos => {
            const $c = $('#empresas').empty().append('<option value="">Seleccione</option>');
            datos.forEach(i => $c.append(`<option value="${i.EMP_Id}">${i.EMP_Nombre}</option>`));
        });
    }

    // ——————————————
    // Generar Conceptos
    // ——————————————
    $('#btnPrevisulizarConceptosAplicaciones').click(async () => {
        mostrarDivCargando();

        try {
            const response = await $.ajax({
                url: window.AppConfig.urls.PrevisulizarConceptosAplicaciones,
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

            // Extraemos preview y posibles errores
            const { partesPreview, partesErrors } = response;

            // Si no hay preview ni errores, avisamos y salimos
            if ((!partesPreview || partesPreview.length === 0) && (!partesErrors || partesErrors.length)) {
                await Swal.fire({
                    title: "Nada que Previsualizar",
                    html: `<p>No hay conceptos de aplicaciones pendientes.</p>`,
                    icon: "info",
                    confirmButtonText: "Entendido",
                    width: 500
                });
                return;
            }

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
                htmlPartes = `<p>No hay aplicaciones válidas para previsualizar.</p>`;
            } else {
                htmlPartes += generarResumenPorTareaEmpresa(partesPreview);
            }

            // Mostramos un Swal con confirmación “Generar”
            const { isConfirmed } = await Swal.fire({
                title: "Previsualizar Conceptos de Aplicaciones",
                html: `<div class="small text-start">
                         <h5>Aplicaciones</h5>
                         ${htmlPartes}
                       </div>`,
                width: '80%',
                showCancelButton: true,
                cancelButtonText: "Cerrar",
                confirmButtonText: "Generar Conceptos",
                icon: "info"
            });

            if (isConfirmed) {
                await generarConceptosHoras();
            }
        } catch (err) {
            ocultarDivCargando();
            registrarErrorjQuery(err.status, err.message);
        }
    });
    function generarResumenPorTareaEmpresa(preview) {
        let html = '';

        preview.forEach(row => {
            html += `
<div class="card mb-4 shadow-sm">
  <div class="card-header text-white d-flex justify-content-between align-items-center" style="background-color: #0092ff">
    <div>
      <strong>${row.NombreEmpresa} – ${row.NombreTarea}</strong>
      <small class="text-light ms-3">${row.Anyo}/${row.Mes.toString().padStart(2, '0')}</small>
    </div>
    <div>
      <span class="badge bg-light text-dark fs-6">Importe: ${formatMoney(row.ImporteTotal)}</span>
    </div>
  </div>
  <div class="card-body p-2">`;

            if (Array.isArray(row.ListaErrores) && row.ListaErrores.length > 0) {
                html += `
                    <div class="alert alert-warning">
                      <h6>Errores en esta tarea:</h6>
                      <ul>`;
                row.ListaErrores.forEach(err => {
                    html += `<li>${err}</li>`;
                });
                html += `
                      </ul>
                    </div>`;
            }
 
                // --- Licencias ---
                if (Array.isArray(row.ListaDetallesLicencia) && row.ListaDetallesLicencia.length) {
                    html += `
        <h6>Licencias</h6>
        <table class="table table-sm mb-3">
          <thead class="table-light">
            <tr>
              <th>Concepto</th>
              <th>Aplicación</th>
              <th class="text-end">Importe</th>
            </tr>
          </thead>
          <tbody>`;
                    row.ListaDetallesLicencia.forEach(det => {
                        html += `
            <tr>
              <td>${det.Origen}</td>
              <td>${det.Aplicacion}</td>
              <td class="text-end text-success fw-semibold">${formatMoney(det.Importe)}</td>
            </tr>`;
                    });
                    html += `
          </tbody>
        </table>`;
                }

                // --- Importe Fijo ---
                if (Array.isArray(row.ListaDetallesImporteFijo) && row.ListaDetallesImporteFijo.length) {
                    html += `
        <h6>Importe Fijo</h6>
        <table class="table table-sm mb-3">
          <thead class="table-light">
            <tr>
              <th>Concepto</th>
              <th class="text-end">Importe</th>
            </tr>
          </thead>
          <tbody>`;
                    row.ListaDetallesImporteFijo.forEach(det => {
                        html += `
            <tr>
              <td>${det.Origen}</td>
              <td class="text-end text-success fw-semibold">${formatMoney(det.Importe)}</td>
            </tr>`;
                    });
                    html += `
          </tbody>
        </table>`;
                }

                // --- Importe Reparto ---
                if (Array.isArray(row.ListaDetallesImporteReparto) && row.ListaDetallesImporteReparto.length) {
                    html += `
        <h6>Importe Reparto</h6>
        <table class="table table-sm mb-0">
          <thead class="table-light">
            <tr>
              <th>Concepto</th>
              <th class="text-end">%</th>
              <th class="text-end">Importe</th>
            </tr>
          </thead>
          <tbody>`;
                    row.ListaDetallesImporteReparto.forEach(det => {
                        html += `
            <tr>
              <td>${det.Origen}</td>
              <td class="text-end">${(det.Porcentaje * 100).toFixed(2)}%</td>
              <td class="text-end text-success fw-semibold">${formatMoney(det.Importe)}</td>
            </tr>`;
                    });
                    html += `
          </tbody>
        </table>`;
                }
     

            html += `
  </div>
</div>`;
        });

        return html;
    }

    async function generarConceptosHoras() {
        mostrarDivCargando();
        try {
            const response = await $.ajax({
                url: window.AppConfig.urls.GenerarConceptosAplicaciones,
                type: 'POST',
                dataType: 'json',
                contentType: 'application/json; charset=utf-8'
            });
            ocultarDivCargando();

            if (!response.success) {
                await Swal.fire({
                    icon: 'error',
                    title: 'Error generando conceptos',
                    text: response.mensaje,
                    confirmButtonText: 'Entendido'
                });
                return;
            }

            // Si todo OK, mostramos resumen:
            const { lineasCreadas, detallesApp, detallesMod, detallesRep } = response;
            await Swal.fire({
                icon: 'success',
                title: 'Conceptos Generados',
                html: `
        <p>Se han generado <strong>${lineasCreadas}</strong> conceptos.</p>
      `,
                confirmButtonText: 'Cerrar'
            });

        } catch (err) {
            ocultarDivCargando();
            await Swal.fire({
                icon: 'error',
                title: 'Error inesperado',
                text: err.statusText || err.message,
                confirmButtonText: 'Entendido'
            });
        }
    }
});

function disableSubTabs() {
    $('#tab-empresas, #tab-tarifas, #tab-reparto')
        .addClass('disabled')
        .removeAttr('data-bs-toggle')
        .attr('aria-disabled', 'true');
}

function enableSubTabs() {
    $('#tab-empresas, #tab-tarifas')
        .removeClass('disabled')
        .attr('data-bs-toggle', 'tab')
        .removeAttr('aria-disabled');
}