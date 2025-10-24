const urlParams = new URLSearchParams(window.location.search); //test
const mode = urlParams.get('mode');

document.addEventListener("DOMContentLoaded", function () {
    if (mode === 'view') {
        document.querySelectorAll("input, textarea, select").forEach(element => {
            element.setAttribute("disabled", "disabled");
        });

        $("#btnGuardar").hide();
        $("#btnNuevaEmpresa").hide();
    }
});

function CargarComboAprobadores(idEmpresa, idAprobador) {
    $.ajax({
        url: AppConfig.urls.ObtenerAprobadoresEmpresas,
        type: 'GET',
        dataType: 'json',
        data: { idEmpresa: idEmpresa },
        success: function (data) {
            let $select = $('#aprobador');
            $select.empty();

            $.each(data, function (index, item) {
                $select.append('<option value="' + item.EMA_PER_Id + '">' + item.NombrePersona + '</option>');
            });

            if (idAprobador != null)
                $('#aprobador').val(idAprobador);
        },
        error: function (xhr, status, error) {
            const mensaje = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, mensaje);
        }
    });
}

function editarVerTareaEmpresa(idTarea, idEmpresa, anio, modo) {
    $.ajax({
        url: AppConfig.urls.ObtenerTareaEmpresa,
        type: 'GET',
        data: { idTarea: idTarea, idEmpresa: idEmpresa, anio: anio },
        dataType: "json",
        success: function (data) {
            $('#empresa').val(data.TEM_EMP_Id);

            CargarComboAprobadores(data.TEM_EMP_Id, data.TEM_PER_Id_Aprobador);

            $('#anio').val(data.TEM_Anyo);

            if ($('#tipo').val() === Tipo.CANTIDAD_FIJA) {
                $('#presupuestoUnidades').val((data.TEM_Presupuesto ?? '').toString().replace(".", ","));
            } else {
                $('#presupuestoUnidades').val((data.TEM_Elementos ?? '').toString().replace(".", ","));
            }


            $('#empresa').prop('disabled', true);

            if (modo == "ver") {
                $('#modalEditarLabel').text('Ver empresa');
                $('#aprobador').prop('disabled', true);
                $('#presupuestoUnidades').prop('disabled', true);
                $('#btnGuardarEmpresa').hide();
                $('#modoEdicion').val('ver');
            }
            else {
                //editar
                $('#modalEditarLabel').text('Editar empresa');
                $('#aprobador').prop('disabled', false);
                $('#presupuestoUnidades').prop('disabled', false);
                $('#btnGuardarEmpresa').show();
                $('#modoEdicion').val('editar'); // Modo edición
            }

            $('#modalEditar').modal('show');
        },
        error: function (xhr, status, error) {
            const mensaje = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, mensaje);
        }
    });
}

function eliminarTareaEmpresa(idTarea, idEmpresa, anio, empresaNombre) {
    Swal.fire({
        title: `¿Estás seguro de que deseas eliminar la empresa ${empresaNombre}?`,
        icon: "warning",
        showCancelButton: true,
        customClass: {
            confirmButton: "btn btn-lg btn-primary",
            cancelButton: "btn btn-lg btn-outline-secondary",
        },
        confirmButtonText: "Sí, continuar",
        cancelButtonText: "No, cancelar",
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: AppConfig.urls.EliminarTareaEmpresa,
                type: 'POST',
                data: {
                    idTarea: idTarea,
                    idEmpresa: idEmpresa,
                    anio: anio
                },
                success: function (response) {
                    if (response.success) {
                        mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                        $('#modalEditar').modal('hide');

                        let tabla = $('#table').DataTable();
                        tabla.ajax.reload(null, false); // 'false' mantiene la página actual
                    }
                    else {
                        mostrarToast('No se puede eliminar. La empresa/año esta asociada a un concepto', TipoToast.Warning);
                    }
                },
                error: function (xhr, status, error) {
                    const mensaje = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, mensaje);
                }
            });
        }
    });
}

function guardarEmpresa() {
    let idTarea = AppConfig.IdTarea;
    let idEmpresa = $('#empresa').val();
    let idAprobador = $('#aprobador').val();
    let anio = $('#anio').val();

    let valorInput = $('#presupuestoUnidades').val();
    let tipo = $('#tipo').val(); // Este es clave

    let modoEdicion = $('#modoEdicion').val();

    let camposInvalidos = [];
    validarCampo('#empresa', 'empresa', camposInvalidos);
    validarCampo('#aprobador', 'Aprobador', camposInvalidos);
    validarCampo('#anio', 'Año', camposInvalidos);
    validarCampo('#presupuestoUnidades', 'Presupuesto / Cantidad', camposInvalidos);

    if (camposInvalidos.length > 0) {
        mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
        return;
    }

    // Construir los datos según el tipo
    let datos = {
        idTarea: idTarea,
        idEmpresa: idEmpresa,
        anio: anio,
        idAprobador: idAprobador
    };

    if (tipo === Tipo.CANTIDAD_FIJA) {
        datos.unidades = 1;
        datos.presupuesto = valorInput;
    } else {
        datos.unidades = valorInput;
        datos.presupuesto = 0;
    }

    $.ajax({
        url: AppConfig.urls.CrearTareaEmpresa,
        type: 'POST',
        data: datos,
        dataType: 'json',
        success: function (response) {
            if (response.success) {
                mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                $('#modalEditar').modal('hide');

                let tabla = $('#table').DataTable();
                tabla.ajax.reload(function () {
                    if (modoEdicion === "editar") {
                        resaltarFilaPorId(sessionStorage.getItem("ultimaFilaEditada"));
                    }
                }, false);
            }
            else {
                // Si llega mensaje concreto, lo mostramos; si no, mensaje genérico
                const texto = response.message
                    || 'Error en la operación';
                mostrarToast(texto, TipoToast.Warning);
            }
        },
        error: function (xhr, status, error) {
            const mensaje = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, mensaje);
        }
    });
}

$(document).ready(function () {
    let anioConfig;

    if (tienePermiso(OpcionMenu.EdicionPresupuestos)) {
        $("#btnNuevaEmpresa").show();
    } else {
        $("#btnNuevaEmpresa").hide();
    }

    VerificarSesionActiva(OpcionMenu.Tareas).then(() => {
        Promise.all([
            CargarComboSecciones(),
            CargarComboTiposTarea(),
            CargarComboUnidades(),
            CargarComboProductosD365(),
            CargarComboItemNumber(),
            CargarComboEmpresas(),
            CargarConfiguracionAnio()
        ])
            .then(async () => {
                let idTarea = AppConfig.IdTarea;
                await ObtenerTareaDetalle(idTarea);
                ObtenerTareaEmpresas(idTarea);
                ocultarDivCargando();
            })
            .catch((error) => {
                mostrarToast(error, TipoToast.Error);
            });
    });

    function CargarComboSecciones() {
        return cargarComboGenerico(AppConfig.urls.ObtenerComboSecciones, '#seccion', 'SEC_Id', 'SEC_Nombre', true);
    }

    function CargarComboTiposTarea() {
        return cargarComboGenerico(AppConfig.urls.ObtenerComboTipos, '#tipo', 'TTA_Id', 'TTA_Nombre', true);
    }

    function CargarComboUnidades() {
        return cargarComboGenerico(AppConfig.urls.ObtenerComboUnidades, '#unidad', 'UTI_UTA_Id', 'UTI_Nombre', true);
    }

    function CargarComboProductosD365() {
        //const key = 'combo_productos_d365';
        //const cached = sessionStorage.getItem(key);

        //if (cached) {
        //    const data = JSON.parse(cached);
        //    rellenarCombo('#producto', data, 'PR3_Id', 'PR3_Nombre');
        //    return Promise.resolve(); // Devuelve una promesa ya resuelta
        //}

        return $.ajax({
            url: AppConfig.urls.ObtenerComboProductosD365,
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                //sessionStorage.setItem(key, JSON.stringify(data));
                rellenarCombo('#producto', data, 'PR3_Id', 'PR3_Nombre');
            },
            error: function (xhr) {
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
            }
        });
    }

    function CargarComboItemNumber() {
        const key = 'combo_item_number';
        const cached = sessionStorage.getItem(key);

        if (cached) {
            const data = JSON.parse(cached);
            rellenarCombo('#itemNumber', data, 'IN3_Id', 'IN3_Nombre');
            return Promise.resolve();
        }

        return $.ajax({
            url: AppConfig.urls.ObtenerComboItemNumber,
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                sessionStorage.setItem(key, JSON.stringify(data));
                rellenarCombo('#itemNumber', data, 'IN3_Id', 'IN3_Nombre');
            },
            error: function (xhr) {
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
            }
        });
    }

    function CargarComboEmpresas() {
        return cargarComboGenerico(AppConfig.urls.ObtenerComboEmpresas, '#empresa', 'EMP_Id', 'EMP_Nombre', true);
    }

    function CargarConfiguracionAnio() {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: AppConfig.urls.CargarConfiguracionAnio,
                type: 'GET',
                dataType: 'json',
                success: function (data) {
                    $('#anio').val(data);
                    anioConfig = data;
                    resolve();
                },
                error: function (xhr, status, error) {
                    const mensaje = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, mensaje);
                }
            });
        });
    }

    async function ObtenerTareaDetalle(idTarea) {
        $.ajax({
            type: 'GET',
            contentType: 'application/json; charset=utf-8',
            url: AppConfig.urls.ObtenerTareaDetalle,
            data: { idTarea: idTarea }, // Solo incluye el parámetro si está definido
            dataType: "json",
            success: function (data) {
                $('#nombre').val(data.TAR_Nombre);
                $('#seccion').val(data.TAR_SEC_Id);
                $('#tipo').val(data.TAR_TTA_Id);
                $('#iva').val(data.TAR_TipoIva);
                $('#importeUnitario').val(data.TAR_ImporteUnitario);

                if (String(data.TAR_TTA_Id) === Tipo.POR_HORAS) {
                    $(".campo-importeUnitario").show();
                    $(".campo-unidad").hide();
                    $("#labelPresupuesto").text("Unidades *");
                } else if (String(data.TAR_TTA_Id) === Tipo.POR_UNIDADES) {
                    $(".campo-importeUnitario").show();
                    $(".campo-unidad").show();
                    $("#labelPresupuesto").text("Unidades *");
                } else if (String(data.TAR_TTA_Id) === Tipo.CANTIDAD_FIJA) {
                    $(".campo-importeUnitario").hide();
                    $(".campo-unidad").hide();
                    $("#labelPresupuesto").text("Presupuesto *");
                }

                let unidadId = data.TAR_UTA_Id;
                let $selectUnidad = $('#unidad');

                // Asegúrate de que el combo ya esté cargado antes de asignar el valor
                if ($selectUnidad.find('option[value="' + unidadId + '"]').length > 0) {
                    $selectUnidad.val(unidadId);
                }

                let productoValor = data.TAR_PR3_Id;
                let $select = $('#producto');

                // Asegúrate de que el combo ya esté cargado antes de asignar el valor
                if ($select.find('option[value="' + productoValor + '"]').length > 0) {
                    $select.val(productoValor);
                }

                let itemValor = data.TAR_IN3_Id;
                let $selectItem = $('#itemNumber');

                // Asegúrate de que el combo ya esté cargado antes de asignar el valor
                if ($selectItem.find('option[value="' + itemValor + '"]').length > 0) {
                    $selectItem.val(itemValor);
                }

                let $chkVisible = $('#visible');
                $chkVisible.prop('checked', data.TAR_Activo);
            },
            error: function (xhr, status, error) {
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
            }
        });
    }

    function ObtenerTareaEmpresas(idTarea) {
        let permisoEscritura = false;
        let permisosMenu = JSON.parse(sessionStorage.getItem("permisos")) || [];
        let permiso = permisosMenu.find(p => p.SPO_SOP_Id === OpcionMenu.Tareas);

        if (permiso && permiso.SPO_Escritura === true) {
            permisoEscritura = true;
        }

        if (!permisoEscritura) {
            $('#btnNuevaEmpresa').hide();
        }

        let columnasConFiltro = [];
        let table = inicializarDataTable('#table', {
            ajax: {
                url: AppConfig.urls.ObtenerTareaEmpresas,
                type: 'GET',
                dataSrc: '',
                data: { idTarea: idTarea },
                dataType: "json",
            },
            rowId: function (row) {
                return `fila-${row.TEM_TAR_Id}-${row.TEM_EMP_Id}-${row.TEM_Anyo}`; // Genera un id único para cada fila
            },
            columns: [
                { data: 'EmpresaNombre', title: 'Empresa' },
                { data: 'TEM_Anyo', title: 'Año' },
                { data: 'TEM_Elementos', title: 'Unidades' },
                {
                    data: 'TEM_Presupuesto',
                    title: 'Presupuesto',
                    className: 'dt-type-numeric-with-decimal',
                    render: function (data) {
                        return formatMoney(data);
                    }
                },
                {
                    className: 'td-btn',
                    data: null,
                    title: '<span class="sReader">Acción</span>',
                    orderable: false,
                    render: function (data, type, row) {
                        if (row.TEM_Anyo == anioConfig && permisoEscritura) {
                            return `
                                <button type="button" class="btn btn-icon btn-editar btn-outline-secondary"
                                    onclick="editarVerTareaEmpresa(${row.TEM_TAR_Id}, ${row.TEM_EMP_Id}, ${row.TEM_Anyo}, 'editar')">
                                    <i class="bi bi-pencil-square" title="Editar"></i>
                                </button>
                                <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
                                    onclick="eliminarTareaEmpresa(${row.TEM_TAR_Id}, ${row.TEM_EMP_Id}, ${row.TEM_Anyo}, '${row.EmpresaNombre}')">
                                    <i class="bi bi-trash" title="Eliminar"></i>
                                </button>`;
                        } else {
                            return `
                                <button type="button" class="btn btn-icon btn-detalle btn-outline-secondary"
                                    onclick="editarVerTareaEmpresa(${row.TEM_TAR_Id}, ${row.TEM_EMP_Id}, ${row.TEM_Anyo}, 'ver')">
                                    <i class="bi bi-eye-fill" title="Ver"></i>
                                </button>`;
                        }
                    }
                }
            ]
        }, columnasConFiltro, 'export_tarea_empresas');

        $(window).on('resize', function () {
            table.columns.adjust().responsive.recalc();
        });
    }

    $("#empresa").change(function () {
        let selectedValue = $(this).val();
        CargarComboAprobadores(selectedValue);
    });

    $("#tipo").change(function () {
        let selectedValue = $(this).val();

        if (selectedValue === Tipo.POR_HORAS) {
            $(".campo-importeUnitario").show();
            $(".campo-unidad").hide();
            $("#labelPresupuesto").text("Unidades *");
        } else if (selectedValue === Tipo.POR_UNIDADES) {
            $(".campo-importeUnitario").show();
            $(".campo-unidad").show();
            $("#labelPresupuesto").text("Unidades *");
        } else if (selectedValue === Tipo.CANTIDAD_FIJA) {
            $(".campo-importeUnitario").hide();
            $(".campo-unidad").hide();
            $("#labelPresupuesto").text("Presupuesto *");
        }

    });

    $('#btnGuardar').on('click', function (e) {
        e.preventDefault();

        let objTarea = {
            TAR_Id: AppConfig.IdTarea,
            TAR_Nombre: $('#nombre').val(),
            TAR_SEC_Id: $('#seccion').val(),
            TAR_TTA_Id: $('#tipo').val(),
            TAR_TipoIva: $('#iva').val(),
            TAR_ImporteUnitario: $('#importeUnitario').val(),
            TAR_UTA_Id: $('#unidad').val(),
            TAR_PR3_Id: $('#producto').val(),
            TAR_IN3_Id: $('#itemNumber').val(),
            TAR_Activo: $('#visible').is(':checked') // Obtiene el estado del checkbox
        };

        $.ajax({
            url: AppConfig.urls.GuardarTarea,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(objTarea), 
            success: function (response) {
                if (response.success) {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                    window.location.href = AppConfig.urls.BusquedaTareas;
                } else {
                    mostrarToast('Error en la operacion', TipoToast.Error);
                }
            },
            error: function (xhr, status, error) {
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
            }
        });
    });

    $('#btnCancelar').on('click', function (e) {
        e.preventDefault();
        window.location.href = AppConfig.urls.BusquedaTareas;
    });

    $('#btnNuevaEmpresa').on('click', function () {
        $('#modalEditarLabel').text('Alta de empresa');
        $('#empresa').val('').prop('disabled', false);
        $('#presupuestoUnidades').val('').prop('disabled', false);
        $('#aprobador').val('').prop('disabled', false);

        $('#btnGuardarEmpresa').show();
        $('#modalEditar').modal('show');
    });
});