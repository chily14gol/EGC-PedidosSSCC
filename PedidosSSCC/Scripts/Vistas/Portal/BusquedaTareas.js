let storageKey = Filtros.Tareas;

function verTarea(button) {
    let tarea = JSON.parse(button.getAttribute('data-tarea'));
    window.location.href = `TareaDetalle/${tarea.TAR_Id}?mode=view`;
}

function editarTarea(button) {
    let tarea = JSON.parse(button.getAttribute('data-tarea'));
    window.location.href = `TareaDetalle/${tarea.TAR_Id}?mode=edit`;
}

async function eliminarTarea(button) {
    const tarea = JSON.parse(button.getAttribute('data-tarea'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar la tarea '${tarea.TAR_Nombre}'?`,
        onConfirmar: async function () {
            try {
                const response = await $.ajax({
                    url: AppConfig.urls.EliminarTarea,
                    type: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    data: JSON.stringify({ idTarea: tarea.TAR_Id }),
                    dataType: 'json'
                });

                if (response.success) {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                    const tabla = $('#tablaTareas').DataTable();
                    tabla.ajax.reload(null, false);
                } else {
                    mostrarToast("No se puede eliminar la tarea ya que tiene empresas asociadas.", TipoToast.Warning);
                }
            } catch (error) {
                registrarErrorjQuery(error.status || 'error', error.message || error);
            }
        }
    });
}

function generarBotonesPedido(row, permisoEscritura) {
    const data = `data-tarea="${JSON.stringify(row).replace(/"/g, "&quot;")}"`;

    if (!permisoEscritura) {
        return `
    <button type="button" class="btn btn-icon btn-detalle btn-outline-secondary"
        ${data}
        onclick="verTarea(this)">
        <i class="bi bi-eye-fill" title="Ver"></i>
    </button>`;
    }

    return `
    <button type="button" class="btn btn-icon btn-editar btn-outline-secondary"
        ${data}
        onclick="editarTarea(this)">
        <i class="bi bi-pencil-square" title="Editar"></i>
    </button>
    <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
        ${data}
        onclick="eliminarTarea(this)">
        <i class="bi bi-trash" title="Eliminar"></i>
    </button>`;
}

function guardarFiltros() {
    let general = $("#formBuscar").val();
    let anio = $("#ddlAño").val();
    let tarea = $('#filterTarea').val();
    let seccion = $('#filterSeccion').val();
    let tipo = $('#filterTipo').val();
    let importe = $('#filterImporte').val();
    let producto = $('#filterProducto').val();
    let itemNumber = $('#filterItemNumber').val();

    let filtroActual = {
        general: general,
        anio: anio,
        tarea: tarea,
        seccion: seccion,
        tipo: tipo,
        importe: importe,
        producto: producto,
        itemNumber: itemNumber
    };

    // Guardar en localStorage
    localStorage.setItem(storageKey, JSON.stringify(filtroActual));
}

async function ObtenerTareas() {
    let permisoEscritura = false;
    let permisosMenu = JSON.parse(sessionStorage.getItem("permisos")) || [];
    let permiso = permisosMenu.find(p => p.SPO_SOP_Id === OpcionMenu.Tareas);

    if (permiso && permiso.SPO_Escritura === true) {
        permisoEscritura = true;
    }

    let columnasConFiltro = [];
    let tablaDatos = inicializarDataTable("#tablaTareas", {
        ajax: {
            url: AppConfig.urls.ObtenerTareas,
            type: 'GET',
            dataSrc: '',
            dataType: "json"
        },
        columns: [
            { data: 'Anios', title: 'Anios', visible: false },
            { data: 'TAR_Nombre', title: 'Tarea' },
            { data: 'TAR_Seccion', title: 'Sección' },
            { data: 'TAR_Tipo', title: 'Tipo' },
            {
                data: 'TAR_ImporteUnitario',
                title: 'Importe Unitario',
                className: 'dt-type-numeric-with-decimal',
                render: function (data) {
                    return formatMoney(data);
                }
            },
            { data: 'PR3_Nombre', title: 'Producto (D365)' },
            { data: 'IN3_Nombre', title: 'Item number (D365)' },
            {
                className: 'dt-center ico-status',
                data: null,
                title: 'Visible',
                orderable: false,
                render: function (data, type, row, meta) {
                    return `
    <input type="checkbox" class="form-check-input"
        id="checkbox-${meta.row}"
        data-index="${meta.row}"
        data-id="${row.TAR_Id}" disabled
        ${row.TAR_Activo ? 'checked' : ''} />
    `;
                }
            },
            {
                className: 'td-btn',
                data: null,
                title: '<span class="sReader">Acción</span>',
                responsivePriority: 2,
                orderable: false,
                render: function (data, type, row) {
                    return generarBotonesPedido(row, permisoEscritura);
                }
            }
        ]
    }, columnasConFiltro, 'export_tareas');

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

$(document).ready(async function () {
    await VerificarSesionActiva(OpcionMenu.Tareas);
    await establecerFiltros();

    async function establecerFiltros() {
        // Recuperar filtros previos si existen
        let savedFilters = localStorage.getItem(storageKey);
        let hasFilters = false;

        if (savedFilters) {
            savedFilters = JSON.parse(savedFilters);
            $("#formBuscar").val(savedFilters.general);
            $("#ddlAño").val(savedFilters.anio);
            $("#filterTarea").val(savedFilters.tarea || "");
            $("#filterSeccion").val(savedFilters.seccion || "");
            $("#filterTipo").val(savedFilters.tipo || "");
            $("#filterImporte").val(savedFilters.importe || "");
            $("#filterProducto").val(savedFilters.producto || "");
            $("#filterItemNumber").val(savedFilters.itemNumber || "");

            // Comprobar si al menos un filtro tiene valor
            let { general, ...otrosFiltros } = savedFilters;
            hasFilters = Object.values(otrosFiltros).some(value => value !== "" && value !== null && value !== undefined);
        }

        await ObtenerTareas();

        setTimeout(function () {
            let table = $('#tablaTareas').DataTable();
            if (savedFilters?.general) {
                table.search(savedFilters.general, false, false).draw();
            }
        }, 200); // Ajusta el tiempo de espera si es necesario

        // Si hay filtros aplicados, mostrar el div de filtros
        if (hasFilters) {
            $("#btnLimpiar").show();
            $(".table-filter").addClass('advance');
            $('#btnAvanzado').html(`Ocultar Filtros`); // Cambiar icono y texto
        }
    }

    async function mostrarDivProcesando() {
        return new Promise(resolve => {
            $('#mensajeCargando').text('PROCESANDO...');
            $('#divCargando').show();
            setTimeout(resolve, 200);
        });
    }

    function ocultarDivProcesando() {
        $('#mensajeCargando').text('CARGANDO...');
        $('#divCargando').hide();
    }

    $('#ddlAño').on('change', function () {
        let anioValor = $(this).val();
        let table = $('#tablaTareas').DataTable();

        if (anioValor) {
            table.column(0).search(anioValor).draw();
        } else {
            table.column(0).search("").draw();
        }

        guardarFiltros();
    });

    $('#btnNuevo').on('click', function (e) {
        window.location.href = AppConfig.urls.TareaNueva;
    });

    $('#btnDescargarPlantilla').click(function () {
        window.location.href = AppConfig.urls.DescargarPlantillaTareasEmpresas;
    });

    $("#btnImportarExcel").on("click", function () {
        $('#archivoExcel').val('');
        $('#archivoExcel').replaceWith($('#archivoExcel').clone());
        $("#modalImportar").modal("show");
    });

    $('#btnSubirProcesar').click(async function (event) {
        event.preventDefault(); // Evita el envío normal del formulario

        let formData = new FormData();
        let archivo = $('#archivoExcel')[0].files[0];

        if (!archivo) {
            mostrarToast("Por favor, seleccione un archivo.", TipoToast.Error);
            return;
        }

        formData.append("archivoExcel", archivo);

        // Esto cierra el modal y también oculta el fondo gris
        $("#modalImportar").modal('hide');
        await mostrarDivProcesando();

        $.ajax({
            url: AppConfig.urls.ImportarExcelTareas,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success) {
                    ocultarDivProcesando();
                    mostrarToast("Archivo importado correctamente.", TipoToast.Success);

                    setTimeout(function () {
                        mostrarDivCargando();
                        $('#tablaTareas').DataTable().destroy();
                        ObtenerTareas();
                    }, 200);
                } else if (response.excelErroneo) {
                    ocultarDivProcesando();
                    mostrarToast(response.mensajeExcelErroneo, TipoToast.Error);
                } else {
                    ocultarDivProcesando();

                    if (response.erroresExcel === null) {
                        registrarErrorjQuery("", response.message);
                    }

                    let errores = response.erroresExcel.map(error => error.Errores).join("\n");
                    Swal.fire({
                        title: "Error al importar",
                        text: "La importación se ha realizado con algunos errores. Consulte el fichero generado.",
                        icon: "error",
                        confirmButtonText: "Cerrar",
                        confirmButtonColor: "#d33"
                    }).then((result) => {
                        if (result.isConfirmed) {
                            setTimeout(function () {
                                mostrarDivCargando();
                                $('#tablaTareas').DataTable().destroy();
                                ObtenerTareas();
                            }, 200);
                        }
                    });

                    if (response.fileUrl) {
                        // Si hay un archivo de errores, descargarlo automáticamente
                        let downloadLink = document.createElement("a");
                        downloadLink.href = response.fileUrl;
                        downloadLink.download = "Errores_Importacion.xlsx";
                        document.body.appendChild(downloadLink);
                        downloadLink.click();
                        document.body.removeChild(downloadLink);
                    }
                }
            },
            error: function (xhr, status, error) {
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
            }
        });
    });

    $('#btnBuscar').on('click', function () {
        let tarea = $('#filterTarea').val();
        let seccion = $('#filterSeccion').val();
        let tipo = $('#filterTipo').val();
        let importe = $('#filterImporte').val();
        let producto = $('#filterProducto').val();
        let itemNumber = $('#filterItemNumber').val();

        let table = $('#tablaTareas').DataTable();
        table.columns(1).search(tarea).draw();
        table.columns(2).search(seccion).draw();
        table.columns(3).search(tipo).draw();
        table.columns(4).search(importe).draw();
        table.columns(5).search(producto).draw();
        table.columns(6).search(itemNumber).draw();

        guardarFiltros();
    });
});