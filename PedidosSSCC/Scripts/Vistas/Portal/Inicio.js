async function verConcepto(idConcepto) {
    try {
        // Llamada a la acción que devuelve JSON con todos los datos
        const response = await fetch(AppConfig.urls.ObtenerConceptoDetalle + '?idConcepto=' + idConcepto);
        if (!response.ok) {
            throw new Error(`Error en la respuesta del servidor: ${response.status}`);
        }
        const data = await response.json();

        // Rellenar los campos comunes del modal
        $('#idConcepto').val(data.TLE_Id);
        $('#solicitante').val(data.Solicitante);
        $('#fechaSolicitud').val(data.FechaModificacion);
        $('#tareaEmpresa').val(data.TareaNombre + " - " + data.EmpresaNombre);
        $('#descripcionModal').val(data.TLE_Descripcion);

        // Formatear la cantidad e importe unitario (siempre numéricos)
        let cantidadNum = parseFloat(data.TLE_Cantidad) || 0;
        let importeUnitarioNum = parseFloat(data.TAR_ImporteUnitario) || 0;

        // -------------------------------------------------------
        // Calculamos el Importe total
        // -------------------------------------------------------
        const TIPO_CANTIDAD_FIJA = 3; // Debe coincidir con tu Constantes.TipoTarea.CantidadFija

        let importeTotalCalculado;
        if (data.TAR_TTA_Id === TIPO_CANTIDAD_FIJA) {
            // Si es “Cantidad Fija”, el importe total es igual a la propia cantidad
            importeTotalCalculado = cantidadNum;
        } else {
            // En cualquier otro caso, Importe total = Cantidad × Importe unitario
            importeTotalCalculado = cantidadNum * importeUnitarioNum;
        }
        // -------------------------------------------------------

        // Ajustamos los valores en los inputs
        $('#cantidadModal').val(formatNumber(cantidadNum));
        $('#importeUnitarioModal').val(formatNumber(importeUnitarioNum));
        $('#importeTotalModal').val(formatNumber(importeTotalCalculado));

        // Ahora: en función de data.TAR_TTA_Id (tipo de tarea),
        // ocultamos o mostramos los campos de Cantidad / Importe unitario.
        if (data.TAR_TTA_Id === TIPO_CANTIDAD_FIJA) {
            // Si es “Cantidad Fija”, ocultamos esos dos
            $('.filaImporteYCantidad').hide();
        } else {
            // Si NO es Cantidad Fija, los mostramos
            $('.filaImporteYCantidad').show();
        }

        $('#modalEditar').modal('show');
    } catch (xhr) {
        const mensaje = obtenerMensajeErrorAjax(xhr);
        registrarErrorjQuery(xhr.status, mensaje);
    }
}

$(document).ready(function () {
    let storageKey = Filtros.Tareas;

    VerificarSesionActiva(OpcionMenu.Inicio).then(async () => {
        // Esperar primero a que se carguen los conceptos pendientes
        await establecerFiltros();

        // Ejecutar las otras tareas en paralelo sin bloquear
        ObtenerTareas();
        ObtenerConceptos();
        ObtenerObtenerPedidos();
    });

    async function establecerFiltros() {
        mostrarDivCargando();

        // Recuperar filtros previos si existen
        let savedFilters = localStorage.getItem(storageKey);
        let hasFilters = false;

        if (savedFilters) {
            savedFilters = JSON.parse(savedFilters);
            $("#formBuscar").val(savedFilters.general);
            $("#filterTarea").val(savedFilters.tarea || "");
            $("#filterEmpresa").val(savedFilters.empresa || "");
            $("#filterCantidad").val(savedFilters.cantidad || "");
            $("#filterImporte").val(savedFilters.importe || "");
            $("#filterDescripcion").val(savedFilters.descripcion || "");

            // Comprobar si al menos un filtro tiene valor
            let { general, ...otrosFiltros } = savedFilters;
            hasFilters = Object.values(otrosFiltros).some(value => value !== "" && value !== null && value !== undefined);
        }

        await ObtenerConceptosPendienteAprobacion();

        setTimeout(function () {
            let table = $('#tablaConceptosPendientes').DataTable();
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

    function guardarFiltros() {
        let general = $("#formBuscar").val();
        let tarea = $('#filterTarea').val();
        let empresa = $('#filterEmpresa').val();
        let cantidad = $('#filterCantidad').val();
        let importe = $('#filterImporte').val();
        let descripcion = $('#filterDescripcion').val();

        let filtroActual = {
            general: general,
            tarea: tarea,
            empresa: empresa,
            cantidad: cantidad,
            importe: importe,
            descripcion: descripcion
        };

        // Guardar en localStorage
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }

    async function ObtenerConceptosPendienteAprobacion() {
        try {
            // Muestra un loader o spinner (si tuvieras una función para ello)
            mostrarDivCargando();

            let columnasConFiltro = [];
            let tablaDatos = inicializarDataTable("#tablaConceptosPendientes", {
                ajax: {
                    url: AppConfig.urls.ObtenerConceptosPendienteAprobacion,
                    type: 'GET',
                    dataSrc: '',
                    dataType: "json"
                },
                columns: [
                    {
                        className: 'col-checkbox',
                        data: null,
                        title: '<input type="checkbox" id="selectAll" class="form-check-input">',
                        orderable: false,
                        searchable: false,
                        render: function (data, type, row) {
                            return `<input type="checkbox" class="row-checkbox form-check-input" value="${row.TLE_Id}">`;
                        }
                    },
                    { data: 'TareaNombre', title: 'Tarea' },
                    { data: 'EmpresaNombre', title: 'Empresa' },
                    { data: 'CantidadNombre', title: 'Cantidad', className: 'dt-type-numeric-with-decimal' },
                    {
                        data: 'ImporteTotal',
                        title: 'Importe',
                        className: 'dt-type-numeric-with-decimal',
                        render: function (data) {
                            return formatMoney(data);
                        }
                    },
                    { data: 'TLE_Descripcion', title: 'Descripción' },
                    {
                        className: 'td-btn',
                        data: null,
                        title: '<span class="sReader">Acción</span>',
                        responsivePriority: 2,
                        orderable: false,
                        render: function (data, type, row) {
                            return `<button type="button" class="btn btn-icon btn-detalle btn-outline-secondary" onclick="verConcepto(${row.TLE_Id})">
                                                <i class="bi bi-eye" title="Ver"></i>
                                            </button>`;
                        }
                    }
                ],
                initComplete: function () {
                    const api = this.api();
                    const dataCount = api.rows().count();

                    if (dataCount === 0) {
                        $('#mensaje-bienvenida').show();
                        $('#div-tablaConceptos').hide();
                    } else {
                        $('#mensaje-bienvenida').hide();
                        $('#div-tablaConceptos').show();
                    }

                    ocultarDivCargando();
                }
            }, columnasConFiltro, 'export_conceptos');

            $(window).on('resize', function () {
                tablaDatos.columns.adjust();
                tablaDatos.responsive.recalc();
                tablaDatos.draw();
            });

            $("#formBuscar").on("keyup input", function () {
                tablaDatos.search(this.value, false, false).draw();
                tablaDatos.responsive.rebuild();
                tablaDatos.responsive.recalc();
                tablaDatos.columns.adjust();
                tablaDatos.draw(false);
                guardarFiltros();
            });
        } catch (error) {
            ocultarDivCargando();
        }
    }

    async function ObtenerTareas() {
        try {
            const response = await fetch(AppConfig.urls.ObtenerTareas);
            const data = await response.json();
        } catch (error) {
        }
    };

    async function ObtenerConceptos() {
        try {
            const response = await fetch(AppConfig.urls.ObtenerConceptosFacturacion);
            const data = await response.json();
        } catch (error) {
        }
    };

    async function ObtenerObtenerPedidos() {
        try {
            const response = await fetch(AppConfig.urls.ObtenerPedidos);
            const data = await response.json();
        } catch (error) {
        }
    };

    function procesarConceptos(idEstado, titulo, mensaje, ids = null) {
        let idsSeleccionados = ids || [$('#idConcepto').val()];
        let comentarios = $('#comentarios').val() || "";

        $.ajax({
            url: AppConfig.urls.AprobarRechazarConceptos,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                ids: idsSeleccionados,
                comentarios, idEstado
            }),
            success: function (response) {
                if (response.success) {
                    if (!response.ok_email) {
                        sessionStorage.setItem("toastMensaje", response.mensaje_email);
                        sessionStorage.setItem("toastTipo", "error");
                        sessionStorage.setItem("errorEmail", true);
                    }

                    Swal.fire({
                        title: titulo,
                        text: mensaje,
                        icon: "success",
                        confirmButtonColor: "#28a745"
                    }).then(() => {
                        location.reload();
                    });
                } else {
                    registrarErrorjQuery("", response.message);
                }
            },
            error: function (xhr) {
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
            }
        });
    }

    $("#tablaConceptosPendientes thead").on("change", "#selectAll", function () {
        let checked = this.checked;
        $(".row-checkbox").prop("checked", checked);
    });

    $("#btnAprobar").on("click", function () {
        let idsSeleccionados = $(".row-checkbox:checked").map(function () { return $(this).val(); }).get();
        if (idsSeleccionados.length === 0) {
            mostrarToast(AppConfig.mensajes.conceptosAprobar, TipoToast.Warning);
            return;
        }
        procesarConceptos(EstadosSolicitud.Aprobado, AppConfig.mensajes.conceptosAprobados, "", idsSeleccionados);
    });

    $("#btnRechazar").on("click", function () {
        let idsSeleccionados = $(".row-checkbox:checked").map(function () { return $(this).val(); }).get();
        if (idsSeleccionados.length === 0) {
            mostrarToast("Selecciona al menos un concepto para rechazar", TipoToast.Warning);
            return;
        }
        procesarConceptos(EstadosSolicitud.Rechazado, AppConfig.mensajes.conceptosRechazados, "", idsSeleccionados);
    });

    $("#btnAprobar2").on("click", function () {
        $("#btnAprobar").click();
    });

    $("#btnRechazar2").on("click", function () {
        $("#btnRechazar").click();
    });

    $("#btnAprobarIndividual").on("click", function () {
        procesarConceptos(EstadosSolicitud.Aprobado, "¡Concepto Aprobado!", "");
    });

    $("#btnRechazarIndividual").on("click", function () {
        procesarConceptos(EstadosSolicitud.Rechazado, "¡Concepto Rechazado!", "");
    });

    $('#btnBuscar').on('click', function () {
        let tarea = $('#filterTarea').val();
        let empresa = $('#filterEmpresa').val();
        let cantidad = $('#filterCantidad').val();
        let importe = $('#filterImporte').val();
        let descripcion = $('#filterDescripcion').val();

        let table = $('#tablaConceptosPendientes').DataTable();
        table.columns(1).search(tarea).draw();
        table.columns(2).search(empresa).draw();
        table.columns(3).search(cantidad).draw();
        table.columns(4).search(importe).draw();
        table.columns(5).search(descripcion).draw();

        guardarFiltros();
    });
});