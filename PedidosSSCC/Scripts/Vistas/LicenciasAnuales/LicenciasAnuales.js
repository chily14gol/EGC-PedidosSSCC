$(document).ready(function () {
    initLicenciasAnuales();

    async function initLicenciasAnuales() {
        try {
            await VerificarSesionActiva(OpcionMenu.LicenciasAnuales);
            await loadLicenciasAnuales();
            ocultarDivCargando();

            cargarComboProveedores();
            cargarComboTareas();
        } catch (xhr) {
            registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
        }
    }

    // ——————————————
    // Listado principal Licencias Anuales
    // ——————————————
    async function loadLicenciasAnuales() {
        return new Promise(resolve => {
            const tbl = inicializarDataTable('#tableLicencias', {
                ajax: {
                    url: AppConfig.urls.ObtenerLicenciasAnuales,
                    type: 'GET',
                    dataSrc: '',
                    error: function (xhr, status, errorThrown) {
                        console.error('Error al cargar licencias anuales:', status, errorThrown);
                        console.dir(xhr.responseJSON || xhr.responseText);
                        resolve();
                    },
                    complete: function () {
                        resolve();
                    }
                },
                columns: [
                    { data: 'LAN_Nombre', title: 'Nombre' },
                    { data: 'NombreProveedor', title: 'Proveedor' },
                    { data: 'NombreTarea', title: 'Tarea' },
                    {
                        className: 'td-btn',
                        data: null,
                        title: '<span class="sReader">Acción</span>',
                        orderable: false,
                        render: function (_, __, row) {
                            const js = JSON.stringify(row).replace(/"/g, '&quot;');
                            return `<div class="btn-group" role="group">
                                <button class="btn btn-icon btn-editar me-2" data-row="${js}" onclick="editarLicencia(this)">
                                  <i class="bi bi-pencil-square" title="Editar"></i>
                                </button>
                                <button class="btn btn-icon btn-eliminar" data-row="${js}" onclick="delLicencia(this)">
                                  <i class="bi bi-trash" title="Eliminar"></i>
                                </button>
                            </div>`;
                        }
                    }
                ]
            }, [], 'export_licencias_anuales');

            $(window).resize(() => tbl.columns.adjust().draw());
        });
    }

    $('#btnNuevo').click(function () {
        $('#lanId').val(-1);
        $('#lanNombre, #lanProv').val('');
        $('#modalLabel').text('Nueva Licencia Anual');
        $('#modalEditar').modal('show');

        ObtenerAplicacionesTiposEnte(-1).then(() => {
            if (!$('#tiposEnte').hasClass('select2-hidden-accessible')) {
                InicializarSelectEntesPermitidos();
            } else {
                $('#tiposEnte').trigger('change'); // Refresca por si acaso
            }
        });
    });

    window.editarLicencia = function (obj) {
        const r = JSON.parse(obj.getAttribute('data-row'));

        $('#lanId').val(r.LAN_Id);
        $('#lanProv').val(r.LAN_PRV_Id);
        $('#tarea').val(r.LAN_TAR_Id);
        $('#lanNombre').val(r.LAN_Nombre);
        $('#modalLabel').text('Editar Licencia Anual');
        $('#modalEditar').modal('show');

        ObtenerAplicacionesTiposEnte(r.LAN_Id).then(() => {
            if (!$('#tiposEnte').hasClass('select2-hidden-accessible')) {
                InicializarSelectEntesPermitidos();
            } else {
                $('#tiposEnte').trigger('change'); // Refresca por si acaso
            }
        });
    };

    window.delLicencia = btn => {
        const r = JSON.parse(btn.getAttribute('data-row'));
        mostrarAlertaConfirmacion({
            titulo: `¿Eliminar licencia '${r.LAN_Nombre}'?`,
            onConfirmar: () => {
                $.ajax({
                    url: AppConfig.urls.EliminarLicenciaAnual,
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ idLicencia: r.LAN_Id }),
                    dataType: 'json'
                })
                    .done(resp => {
                        if (resp.success) {
                            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                            $('#tableLicencias').DataTable().ajax.reload(null, false);
                        } else {
                            mostrarToast(resp.message, TipoToast.Warning);
                        }
                    })
                    .fail(xhr => registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr)));
            }
        });
    };

    window.guardarLicencia = async () => {
        const id = parseInt($('#lanId').val(), 10);
        const nombre = $('#lanNombre').val().trim();
        const idProv = $('#lanProv').val().trim();
        const idTarea = $('#tarea').val().trim();

        let invalidos = [];

        if (!idProv) { $('#lanProv').addClass('is-invalid'); invalidos.push('Proveedor'); }
        else $('#lanProv').removeClass('is-invalid');

        if (!nombre) { $('#lanNombre').addClass('is-invalid'); invalidos.push('Nombre'); }
        else $('#lanNombre').removeClass('is-invalid');

        if (invalidos.length) {
            mostrarToast(AppConfig.mensajes.camposObligatorios + invalidos.join(', '), TipoToast.Warning);
            return;
        }

        const dto = {
            LAN_Id: id,
            LAN_PRV_Id: idProv,
            LAN_Nombre: nombre,
            TiposEnte: $('#tiposEnte').val()
        };
        if (idTarea != null) {
            dto.LAN_TAR_Id = idTarea;
        }
        const resp = await $.ajax({
            url: AppConfig.urls.GuardarLicenciaAnual,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(dto),
            dataType: 'json'
        });

        if (resp.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#modalEditar').modal('hide');
            $('#tableLicencias').DataTable().ajax.reload(null, false);
        } else {
            mostrarToast(resp.message || "Error al guardar.", TipoToast.Warning);
        }
    };
    function InicializarSelectEntesPermitidos() {
        const $select = $('#tiposEnte');
        if (!$select.length) return;

        if ($select.hasClass('select2-hidden-accessible')) {
            $select.select2('destroy'); // Evita doble inicialización
        }

        $select.select2({
            placeholder: "",
            allowClear: true,
            multiple: true,
            dropdownParent: $('#modalEditar'),
            width: '100%'
        });
    }

    async function ObtenerAplicacionesTiposEnte(idLicencia) {
        try {
            const response = await fetch(`${AppConfig.urls.ObtenerTiposEntePorLicenciaAnual}?idLicencia=${idLicencia}`, {
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

    async function cargarComboProveedores() {
        try {
            const datos = await $.ajax({
                url: AppConfig.urls.ObtenerComboProveedores,
                type: 'GET',
                dataType: 'json'
            });
            const $sel = $('#lanProv').empty().append('<option value="">Seleccione Proveedor</option>');
            datos.forEach(i => {
                $sel.append(`<option value="${i.PRV_Id}">${i.PRV_Nombre}</option>`);
            });
        } catch (xhr) {
            registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
        }
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
                    const combos = ['#tarea'];
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

        } catch (xhr) {
            registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
        }
    }
});
