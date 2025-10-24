$(document).ready(async function () {

    await VerificarSesionActiva(OpcionMenu.TiposCuota);

    const storageKey = "Filtros.TiposCuota";
    let tabla;

    await inicializar();

    ocultarDivCargando();

    function inicializar() {
        establecerFiltros();
        cargarTabla();
        wireEvents();
    }

    function wireEvents() {
        $('#btnNuevo').on('click', abrirNuevo);
        $('#btnGuardar').on('click', guardar);

        $('#btnBuscar').on('click', function () {
            let table = $('#tableTiposCuota').DataTable();
            table.columns(0).search($('#filterCuota').val()).draw();
            table.columns(1).search($('#filterDepartamento').val()).draw();
            table.columns(2).search($('#filterSede').val()).draw();
            table.columns(3).search($('#filterUso').val()).draw();
            guardarFiltros();
        });

        $('#formBuscar').on('keyup input', function () {
            let table = $('#tableTiposCuota').DataTable();
            table.search(this.value, false, false).draw();
            guardarFiltros();
        });
    }

    function establecerFiltros() {
        let saved = localStorage.getItem(storageKey);
        if (saved) {
            try {
                const f = JSON.parse(saved);
                $("#formBuscar").val(f.general || "");
                $("#filterCuota").val(f.cuota || "");
                $("#filterDepartamento").val(f.departamento || "");
                $("#filterSede").val(f.sede || "");
                $("#filterUso").val(f.uso || "");
                if (f.cuota || f.departamento || f.sede || f.uso) {
                    $("#btnLimpiar").show();
                    $(".table-filter").addClass('advance');
                    $('#btnAvanzado').html(`Ocultar Filtros`);
                }
            } catch { }
        }
    }

    function guardarFiltros() {
        const filtro = {
            general: $("#formBuscar").val(),
            cuota: $("#filterCuota").val(),
            departamento: $("#filterDepartamento").val(),
            sede: $("#filterSede").val(),
            uso: $("#filterUso").val()
        };
        localStorage.setItem(storageKey, JSON.stringify(filtro));
    }

    function cargarTabla() {
        tabla = inicializarDataTable('#tableTiposCuota', {
            ajax: {
                url: AppConfig.urls.ObtenerTiposCuota,
                type: 'GET',
                dataSrc: ''
            },
            columns: [
                { data: 'TCU_Cuota', title: 'Cuota' },
                { data: 'TCU_Departamento', title: 'Departamento' },
                { data: 'TCU_Sede', title: 'Sede' },
                { data: 'TCU_Uso', title: 'Uso' },
                {
                    data: null,
                    className: 'td-btn',
                    orderable: false,
                    title: '<span class="sReader">Acción</span>',
                    render: function (data, type, row) {
                        const jsRow = JSON.stringify(row).replace(/"/g, '&quot;');
                        return `
                        <div class="btn-group" role="group">
                            <button class="btn btn-icon me-2 btn-editar" data-row="${jsRow}" title="Editar">
                                <i class="bi bi-pencil-square"></i>
                            </button>
                            <button class="btn btn-icon btn-eliminar" data-row="${jsRow}" title="Eliminar">
                                <i class="bi bi-trash"></i>
                            </button>
                        </div>`;
                    }
                }
            ],
            initComplete: function () {
                // aplicar búsqueda general guardada
                const saved = localStorage.getItem(storageKey);
                if (saved) {
                    const f = JSON.parse(saved);
                    if (f?.general) tabla.search(f.general, false, false).draw();
                }
            }
        }, [], 'export_tiposcuota');

        // Delegated events for action buttons
        $('#tableTiposCuota').on('click', '.btn-editar', function () {
            const row = JSON.parse($(this).attr('data-row').replace(/&quot;/g, '"'));
            editar(row);
        });

        $('#tableTiposCuota').on('click', '.btn-eliminar', function () {
            const row = JSON.parse($(this).attr('data-row').replace(/&quot;/g, '"'));
            eliminar(row);
        });

        $(window).resize(function () {
            tabla.columns.adjust().draw();
        });
    }

    function abrirNuevo() {
        limpiarModal();
        $('#modalEditarLabel').text('Agregar Tipo de Cuota');
        $('#modalEditar').modal('show');
    }

    function editar(row) {
        limpiarModal();
        $('#idTipoCuota').val(row.TCU_Id);
        $('#cuota').val(row.TCU_Cuota);
        $('#departamento').val(row.TCU_Departamento);
        $('#sede').val(row.TCU_Sede);
        $('#uso').val(row.TCU_Uso);
        $('#modalEditarLabel').text('Editar Tipo de Cuota');
        $('#modalEditar').modal('show');
    }

    function eliminar(row) {
        mostrarAlertaConfirmacion({
            titulo: `¿Eliminar el tipo de cuota '${htmlEscape(row.TCU_Cuota)}'?`,
            onConfirmar: async () => {
                try {
                    const resp = await $.ajax({
                        url: AppConfig.urls.EliminarTipoCuota,
                        method: 'POST',
                        contentType: 'application/json; charset=utf-8',
                        data: JSON.stringify({ id: row.TCU_Id }),
                        dataType: 'json'
                    });
                    if (resp.success) {
                        mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                        $('#tableTiposCuota').DataTable().ajax.reload(null, false);
                    } else {
                        mostrarToast(resp.message || 'No se pudo eliminar.', TipoToast.Warning);
                    }
                } catch (e) {
                    registrarErrorjQuery('Eliminar', e.message || e);
                }
            }
        });
    }

    function validarObligatorios() {
        let ok = true;
        ['cuota', 'departamento', 'sede', 'uso'].forEach(id => {
            const $el = $('#' + id);
            if (!$el.val()) {
                $el.addClass('is-invalid');
                ok = false;
            } else {
                $el.removeClass('is-invalid');
            }
        });
        if (!ok) mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
        return ok;
    }

    async function guardar() {
        if (!validarObligatorios()) return;

        const payload = {
            TCU_Id: parseInt($('#idTipoCuota').val() || '0'),
            TCU_Cuota: $('#cuota').val().trim(),
            TCU_Departamento: $('#departamento').val().trim(),
            TCU_Sede: $('#sede').val().trim(),
            TCU_Uso: $('#uso').val().trim()
        };

        try {
            const resp = await $.ajax({
                url: AppConfig.urls.GuardarTipoCuota,
                method: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify(payload),
                dataType: 'json'
            });

            if (resp.success) {
                mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                $('#modalEditar').modal('hide');
                $('#tableTiposCuota').DataTable().ajax.reload(null, false);
            } else {
                mostrarToast(resp.message || 'Error al guardar.', TipoToast.Warning);
            }
        } catch (e) {
            registrarErrorjQuery('Guardar', e.message || e);
        }
    }

    function limpiarModal() {
        $('#idTipoCuota').val('');
        $('#cuota, #departamento, #sede, #uso')
            .val('')
            .removeClass('is-invalid');
    }
});