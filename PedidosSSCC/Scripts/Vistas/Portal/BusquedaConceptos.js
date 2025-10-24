function verConcepto(button) {
    let concepto = JSON.parse(button.getAttribute('data-concepto'));

    let urlBase = AppConfig.urls.ConceptoDetalle;
    let finalUrl = `${urlBase}/${concepto.TLE_Id}?mode=view`;
    window.location.href = finalUrl;
}

function editarConcepto(button) {
    let concepto = JSON.parse(button.getAttribute('data-concepto'));

    let urlBase = AppConfig.urls.ConceptoDetalle;
    let finalUrl = `${urlBase}/${concepto.TLE_Id}?mode=edit`;
    window.location.href = finalUrl;
}

function eliminarConcepto(button) {
    let concepto = JSON.parse(button.getAttribute('data-concepto'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar el concepto con ID: ${concepto.TareaNombre}-${concepto.EmpresaNombre}?`,
        onConfirmar: function () {
            $.ajax({
                url: AppConfig.urls.EliminarConcepto,
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify({ idConcepto: concepto.TLE_Id }),
                dataType: 'json',
                success: function (response) {
                    if (response.success) {
                        mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);

                        let tabla = $('#tablaConceptos').DataTable();
                        tabla.row($(button).closest('tr')).remove().draw(false);
                    } else {
                        mostrarToast("Error al eliminar el concepto.", TipoToast.Error);
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

$(document).ready(async function () {
    let storageKey = Filtros.Conceptos;

    await VerificarSesionActiva(OpcionMenu.Conceptos);
    await establecerFiltros();

    async function establecerFiltros() {
        const anioSeleccionado = document.querySelector('#datosConceptos')?.dataset.anio;

        // Recuperar filtros previos si existen
        let savedFilters = localStorage.getItem(storageKey);
        let hasFilters = false;

        if (savedFilters) {
            savedFilters = JSON.parse(savedFilters);
            $("#formBuscar").val(savedFilters.general);
            $("#ddlAño").val(savedFilters.anio || anioSeleccionado);
            $("#filterTarea").val(savedFilters.tarea || "");
            $("#filterEmpresa").val(savedFilters.empresa || "");
            $("#filterMes").val(savedFilters.mes || "");
            $("#filterCantidad").val(savedFilters.cantidad || "");
            $("#filterDescripcion").val(savedFilters.descripcion || "");
            $("#filterEstado").val(savedFilters.estado || "");
            $("#filterImporte").val(savedFilters.importe || "");

            // Comprobar si al menos un filtro tiene valor, sin contar el año
            let { general, anio, ...otrosFiltros } = savedFilters;
            hasFilters = Object.values(otrosFiltros).some(value =>
                value !== "" && value !== null && value !== undefined
            );
        }

        await ObtenerConceptosFacturacion();

        setTimeout(function () {
            let table = $('#tablaConceptos').DataTable();
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
        let anio = $("#ddlAño").val();
        let tarea = $('#filterTarea').val();
        let empresa = $('#filterEmpresa').val();
        //let anioAvanzado = $('#filterAnio').val();
        let mes = $('#filterMes').val();
        let cantidad = $('#filterCantidad').val();
        let descripcion = $('#filterDescripcion').val();
        let estado = $('#filterEstado').val();
        let importe = $('#filterImporte').val();

        let filtroActual = {
            general: general,
            anio: anio,
            tarea: tarea,
            empresa: empresa,
            //anioAvanzado: anioAvanzado,
            mes: mes,
            cantidad: cantidad,
            descripcion: descripcion,
            estado: estado,
            importe: importe
        };

        // Guardar en localStorage
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }

    function generarBotonesPedido(row, permisoEscritura) {
        const data = `data-concepto="${JSON.stringify(row).replace(/"/g, "&quot;")}"`;

        if (!permisoEscritura ||
            [EstadosSolicitud.PendienteAprobacion, EstadosSolicitud.Aprobado, EstadosSolicitud.Rechazado].includes(row.TLE_ESO_Id)) {
            return `
            <button type="button" class="btn btn-icon btn-detalle btn-outline-secondary"
                ${data}
                onclick="verConcepto(this)">
                <i class="bi bi-eye-fill" title="Ver"></i>
            </button>`;
        }

        return `
        <button type="button" class="btn btn-icon btn-editar btn-outline-secondary"
            ${data}
            onclick="editarConcepto(this)">
            <i class="bi bi-pencil-square" title="Editar"></i>
        </button>
        <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
            ${data}
            onclick="eliminarConcepto(this)">
            <i class="bi bi-trash" title="Eliminar"></i>
        </button>`;
    }

    async function ObtenerConceptosFacturacion() {
        let permisoEscritura = false;
        let permisosMenu = JSON.parse(sessionStorage.getItem("permisos")) || [];
        let permiso = permisosMenu.find(p => p.SPO_SOP_Id === OpcionMenu.Conceptos);

        if (permiso && permiso.SPO_Escritura === true) {
            permisoEscritura = true;
        }

        const columnasConFiltro = [];
        const fechaActual = new Date().toISOString().slice(0, 10).replace(/-/g, '');

        try {
            const response = await fetch(AppConfig.urls.ObtenerConceptosFacturacion);
            const data = await response.json();

            if (data.length > 0 && anio === null) {
                const { AnioExportar, MesExportar } = data[0];
                if (AnioExportar)
                    $('#anio').val(AnioExportar);
                if (MesExportar)
                    $('#mes').val(MesExportar);
            }

            const tablaDatos = $('#tablaConceptos').DataTable({
                data,
                columns: [
                    { data: 'TLE_TAR_Id', title: 'ID_Tarea', visible: false },
                    { data: 'TareaNombre', title: 'Tarea', responsivePriority: 1 },
                    { data: 'EmpresaNombre', title: 'Empresa' },
                    { data: 'TLE_Anyo', title: 'Año' },
                    { data: 'TLE_Mes', title: 'Mes' },
                    {
                        data: 'CantidadNombre',
                        title: 'Cantidad',
                        className: 'dt-type-numeric dt-type-numeric-with-decimal',
                        render: data => data.toLocaleString("es-ES", { minimumFractionDigits: 2, maximumFractionDigits: 2 })
                    },
                    { data: 'TLE_Descripcion', title: 'Descripción' },
                    { data: 'EstadoNombre', title: 'Estado' },
                    {
                        data: 'ImporteTotal',
                        title: 'Importe Total',
                        className: 'dt-type-numeric-with-decimal',
                        render: formatMoney
                    },
                    {
                        data: null,
                        title: 'Inversión',
                        orderable: false,
                        className: 'dt-center ico-status',
                        render: (data, type, row, meta) => `
                                    <input type="checkbox" class="form-check-input"
                                        id="checkbox-${meta.row}"
                                        data-index="${meta.row}"
                                        data-id="${row.TLE_Id}" disabled
                                        ${row.TLE_Inversion ? 'checked' : ''} />`
                    },
                    {
                        className: 'td-btn',
                        data: null,
                        title: '<span class="sReader">Acción</span>',
                        responsivePriority: 2,
                        orderable: false,
                        render: (data, type, row) => {
                            return generarBotonesPedido(row, permisoEscritura);
                        }
                    }
                ],
                autoWidth: false,
                destroy: true,
                paging: true,
                searching: true,
                stateSave: true,
                stateLoadParams: function (settings, data) {
                    // Limpiamos el search global al cargar el estado
                    data.search.search = '';
                },
                lengthMenu: [5, 10, 25, 50, 100],
                pageLength: 50,
                layout: {
                    topStart: 'info',
                    topEnd: {
                        buttons: [
                            {
                                extend: 'excelHtml5',
                                text: 'Excel',
                                className: 'btn btn-outline-secondary',
                                title: 'CONCEPTOS',
                                filename: `conceptos_${fechaActual}`,
                                exportOptions: {
                                    columns: ':not(:first, :last-child)',
                                    modifier: { page: 'all' },
                                    format: {
                                        body: (data, row, column, node) => {
                                            const table = $('#tablaConceptos').DataTable();
                                            const rowData = table.row($(node).closest('tr')).data();
                                            if (column === 9) return rowData?.TLE_Inversion ? 'SI' : 'NO';
                                            return data;
                                        }
                                    }
                                }
                            }
                        ]
                    },
                    bottomStart: 'pageLength',
                    bottomEnd: { paging: { buttons: 10 } }
                },
                pagingType: "simple_numbers",
                responsive: true,
                language: {
                    lengthMenu: "_MENU_ entradas por página",
                    info: "Mostrando registros del _START_ al _END_ de un total de _TOTAL_",
                    infoEmpty: "No hay registros disponibles",
                    infoFiltered: "",
                    loadingRecords: "Cargando...",
                    zeroRecords: "No se encontraron registros",
                    emptyTable: "No hay datos disponibles en la tabla",
                    buttons: { copy: "Copiar", colvis: "Visibilidad" }
                },
                initComplete: function () {
                    const api = this.api();
                    const thead = $(api.table().header());

                    api.columns().every((index) => {
                        const th = $('<th>');
                        if (columnasConFiltro.includes(index)) {
                            const input = $('<input type="text" class="form-control form-control-sm" placeholder="Filtrar..." />')
                                .appendTo(th)
                                .on('keyup change clear', function () {
                                    if (api.column(index).search() !== this.value) {
                                        api.column(index).search(this.value).draw();
                                    }
                                });

                            const savedFilter = api.column(index).search();
                            if (savedFilter) input.val(savedFilter);
                        }
                    });

                    $('.dt-search').hide();
                    ocultarDivCargando();
                }
            });

            let savedFilters = localStorage.getItem(storageKey);
            if (!savedFilters) {
                const anioDesdeVista = document.querySelector('#datosConceptos')?.dataset.anio;
                $("#ddlAño").val(anioDesdeVista);
                tablaDatos.column(3).search("^" + anioDesdeVista + "$", true, false).draw();
            }

            $(window).resize(() => tablaDatos.columns.adjust().draw());

            $("#formBuscar").on("keyup input", function () {
                tablaDatos.search(this.value, false, false).draw();
                tablaDatos.responsive.rebuild();
                tablaDatos.responsive.recalc();
                tablaDatos.columns.adjust();
                tablaDatos.draw(false);
                guardarFiltros();
            });

        } catch (error) {
            console.error("Error al obtener los conceptos de facturación:", error);
        }
    };

    function ExportarConceptosModal() {
        return new Promise((resolve, reject) => {
            let anio = $('#anio').val();
            let mes = $('#mes').val();
            let idTarea = $('#idTarea').val();

            if (!anio || !mes) {
                mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
                reject();
                return false;
            }

            $.ajax({
                url: AppConfig.urls.ExportarConceptos,
                type: 'POST',
                data: { anio: anio, mes: mes, idTarea: idTarea },
                xhrFields: {
                    responseType: 'blob' // Indica que esperas un archivo binario en la respuesta
                },
                success: function (data, status, xhr) {
                    let fileName = "";
                    let disposition = xhr.getResponseHeader("Content-Disposition");

                    if (disposition && disposition.indexOf("attachment") !== -1) {
                        let matches = /filename="?([^;]+)"?/.exec(disposition);
                        if (matches.length > 1) fileName = matches[1];
                    }

                    if (!fileName) {
                        fileName = "Exportacion_Conceptos.xlsx"; // Nombre por defecto si no se obtiene del servidor
                    }

                    let blob = new Blob([data], { type: xhr.getResponseHeader("Content-Type") });
                    let link = document.createElement("a");
                    link.href = window.URL.createObjectURL(blob);
                    link.download = fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);

                    resolve();
                },
                error: function (xhr, status, error) {
                    const mensaje = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, mensaje);
                    reject();
                }
            });
        });
    }

    $("#limpiarTarea").click(function () {
        $("#tarea").val("");
        $("#idTarea").val(""); // Opcional si necesitas limpiar también el campo oculto
    });

    $('#ddlAño').on('change', function () {
        let anioValor = $(this).val();
        let table = $('#tablaConceptos').DataTable();

        if (anioValor) {
            table.column(3).search("^" + anioValor + "$", true, false).draw();
        } else {
            table.column(3).search("").draw();
        }

        guardarFiltros();
    });

    $('#btnNuevo').on('click', function () {
        window.location.href = AppConfig.urls.ConceptoNuevo;
    });

    $('#btnDescargarPlantilla').click(function () {
        window.location.href = AppConfig.urls.DescargarPlantillaConceptos;
    });

    $("#btnImportarExcel").on("click", function () {
        $('#archivoExcel').val('');
        $('#archivoExcel').replaceWith($('#archivoExcel').clone());
        $("#modalImportar").modal("show");
    });

    $("#btnExportarConceptos").on("click", function () {
        $('#anio').val('');
        $('#mes').val('');
        $('#tarea').val('');
        $("#idTarea").val("");
        $("#modalEditar").modal("show");
    });

    $("#btnExportarConceptosAceptar").on("click", function () {
        Promise.all([ExportarConceptosModal()])
            .then(() => {
                let table = $('#tablaConceptos').DataTable();
                $("#modalEditar").modal("hide");
            })
            .catch((error) => {
                console.error('Error al cargar los datos:', error);
            });
    });

    $('#buscarTarea').on('click', function () {
        let tablaContainer = $('#tablaTareasContainer');

        if (tablaContainer.is(':hidden')) {
            tablaContainer.show();

            if (!$.fn.DataTable.isDataTable('#tablaTareas')) {
                table = $('#tablaTareas').DataTable({
                    ajax: {
                        url: AppConfig.urls.ObtenerTareas,
                        type: 'GET',
                        dataSrc: '',
                        data: { anio: null },
                        dataType: "json"
                    },
                    paging: false,
                    searching: true,
                    info: false,
                    ordering: false,
                    select: 'single',
                    scrollY: "200px",
                    scrollCollapse: true,
                    language: {
                        emptyTable: "No hay tareas disponibles",
                        search: "Buscar:",
                    },
                    columns: [
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
                            data: null,
                            title: 'Visible',
                            className: 'dt-center ico-status',
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
                        }
                    ],
                    responsive: true,
                });
            } else {
                table.columns.adjust().draw();
            }
        }
    });

    $('#tablaTareas tbody').on('click', 'tr:not(.selected)', function (e) {
        let table = $('#tablaTareas').DataTable();
        let data = table.row(this).data(); // Obtener los datos de la fila seleccionada

        // Si el clic ocurrió en un elemento dentro de la columna de control, salir
        if ($(e.target).closest('td').hasClass('dt-control')) {
            return;
        }

        if (data) {
            // Remover la selección de otras filas
            $('#tablaTareas tbody tr').removeClass('selected');

            // Agregar la clase 'selected' a la fila actual
            //$(this).addClass('selected');

            // Asignar valores a los campos ocultos
            $('#idTarea').val(data.TAR_Id);
            $('#tarea').val(data.TAR_Nombre);

            // Ocultar el contenedor después de seleccionar
            //$('#tablaTareasContainer').hide();
        }
    });

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

    $('#btnSubirProcesar').click(async function (event) {
        event.preventDefault();

        let formData = new FormData();
        let archivo = $('#archivoExcel')[0].files[0];

        if (!archivo) {
            ocultarDivProcesando();
            mostrarToast("Por favor, seleccione un archivo.", TipoToast.Warning);
            return;
        }

        formData.append("archivoExcel", archivo);

        // Esto cierra el modal y también oculta el fondo gris
        $("#modalImportar").modal('hide');
        await mostrarDivProcesando();

        $.ajax({
            url: AppConfig.urls.ImportarExcelConceptos,
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
                        $('#tablaConceptos').DataTable().destroy();
                        ObtenerConceptosFacturacion();
                    }, 200);
                } else if (response.errorFormato) {
                    ocultarDivProcesando();
                    mostrarToast(response.message, TipoToast.Warning);
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
                                $('#tablaConceptos').DataTable().destroy();
                                ObtenerConceptosFacturacion();
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
                ocultarDivProcesando();
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
            }
        });
    });

    $('#btnBuscar').on('click', function () {
        let tarea = $('#filterTarea').val();
        let empresa = $('#filterEmpresa').val();
        let mes = $('#filterMes').val();
        let cantidad = $('#filterCantidad').val();
        let descripcion = $('#filterDescripcion').val();
        let estado = $('#filterEstado').val();
        let importe = $('#filterImporte').val();

        let table = $('#tablaConceptos').DataTable();
        table.columns(0).search('').draw();
        table.columns(1).search(tarea).draw();
        table.columns(2).search(empresa).draw();

        // Si `mes` tiene un valor, aplica búsqueda exacta; si está vacío, limpia el filtro
        if (mes) {
            table.column(4).search("^" + mes + "$", true, false).draw();
        } else {
            table.column(4).search("").draw();
        }

        table.columns(5).search(cantidad).draw();
        table.columns(6).search(descripcion).draw();
        table.columns(7).search(estado).draw();
        table.columns(8).search(importe).draw();

        guardarFiltros();
    });

    $('#btnGenerarConceptosLicencias').on('click', async () => {
        mostrarDivCargando();
        try {
            const response = await $.ajax({
                url: AppConfig.urls.PrevisualizarConceptos,
                type: 'POST',
                dataType: 'json',
                contentType: 'application/json; charset=utf-8'
            });
            ocultarDivCargando();

            // Si hay un fallo global del endpoint
            if (!response.success) {
                mostrarErrorPedidos();
                return;
            }

            // Extraemos previews y sus errores
            const {
                licencias, licenciasErrors,
                soporte, soporteErrors,
                asuntos, asuntosErrors
            } = response;

            // Si no hay nada en ninguna sección
            if (
                !licencias.length && !soporte.length && !asuntos.length &&
                !(licenciasErrors?.length) && !(soporteErrors?.length) && !(asuntosErrors?.length)
            ) {
                await Swal.fire({
                    title: "Nada que Generar",
                    html: `<p>No hay conceptos pendientes ni mensajes de error.</p>`,
                    icon: "info",
                    confirmButtonText: "Entendido",
                    confirmButtonColor: "#3085d6",
                    width: 500
                });
                return;
            }

            // Generamos el HTML combinado con errores inline
            const htmlAll = `
                <div class="small text-start">
                  <h5>Licencias</h5>
                  ${renderLicenciasSection(licencias, licenciasErrors)}
                  <hr/>
                  <h5>Soporte CAU</h5>
                  ${renderSoporteSection(soporte, soporteErrors)}
                  <hr/>
                  <h5>Asuntos Proveedores</h5>
                  ${renderAsuntosSection(asuntos, asuntosErrors)}
                </div>`;

            const hasConcepts = licencias.length > 0 || soporte.length > 0 || asuntos.length > 0;

            // Mostramos modal de confirmación
            const { isConfirmed } = await Swal.fire({
                title: "¿Deseas generar los conceptos?",
                html: htmlAll,
                width: '70%',
                showCancelButton: true,
                cancelButtonText: hasConcepts ? "No, cancelar" : "Cerrar",
                showConfirmButton: hasConcepts,
                confirmButtonText: "Sí, generar"
            });

            if (isConfirmed) {
                await ejecutarGeneracionConceptos();
            }

        } catch (err) {
            ocultarDivCargando();
            registrarErrorjQuery(err.status, err.message);
        }
    });
    function renderLicenciasSection(preview, errors) {
        let html = "";
        if (errors?.length) {
            html += `<div class="alert alert-warning">
      <ul>${errors.map(e => `<li>${e}</li>`).join("")}</ul>
    </div>`;
        }
        if (!preview.length) {
            html += `<p>No hay licencias pendientes este mes.</p>`;
        } else {
            html += generarResumenConceptosLicenciasPrev(preview);
        }
        return html;
    }

    function renderSoporteSection(preview, errors) {
        let html = "";
        if (errors?.length) {
            html += `<div class="alert alert-warning">
      <ul>${errors.map(e => `<li>${e}</li>`).join("")}</ul>
    </div>`;
        }
        // aprovechamos la función existente (que ya maneja preview vacío)
        html += generarResumenConceptosSoportePrev(preview);
        return html;
    }

    function renderAsuntosSection(preview, errors) {
        let html = "";
        if (errors?.length) {
            html += `<div class="alert alert-warning">
      <ul>${errors.map(e => `<li>${e}</li>`).join("")}</ul>
    </div>`;
        }
        html += generarResumenConceptosAsuntosPrev(preview);
        return html;
    }

    function generarResumenConceptosLicenciasPrev(preview) {
        let html = `
      <table style="width:100%;border-collapse:collapse;font-size:0.9em">
        <thead>
          <tr style="background:#f0f0f0">
            <th style="padding:6px;border:1px solid #ddd">Tarea</th>
            <th style="padding:6px;border:1px solid #ddd">Empresa</th>
            <th style="padding:6px;border:1px solid #ddd;text-align:center">Periodo</th>
            <th style="padding:6px;border:1px solid #ddd">Licencias incluidas</th>
            <th style="padding:6px;border:1px solid #ddd;text-align:right">Importe</th>
          </tr>
        </thead>
        <tbody>
    `;
        preview.forEach(r => {
            html += `
          <tr>
            <td>${r.TAR_Nombre}</td>
            <td>${r.EmpresaNombre}</td>
            <td style="text-align:center">${r.Mes}/${r.Anyo}</td>
            <td>${r.LicenciasIncluidas}</td>
            <td style="text-align:right">${formatMoney(r.ImporteTotal)}</td>
          </tr>
        `;
        });
        html += `
        </tbody>
      </table>
    `;
        return html;
    }

    function generarResumenConceptosSoportePrev(preview) {
        if (!preview || !preview.length) {
            return "<p>No hay conceptos de soporte pendientes.</p>";
        }

        let html = "<div class='small text-start'>";

        preview.forEach(dto => {
            html += `
      <div style="
        border:1px solid #e0e0e0;
        border-radius:8px;
        padding:10px;
        margin-bottom:12px;">
        <div class="fw-semibold mb-2" style="color:#001f3f;">
          Categoría: ${dto.CategoriaNombre}
        </div>
        <table style="width:100%;border-collapse:collapse;font-size:0.9em">
          <thead>
            <tr style="background:#f0f0f0">
              <th style="padding:6px;border:1px solid #ddd;text-align:left">Empresa</th>
              <th style="padding:6px;border:1px solid #ddd;text-align:center">Periodo</th>
              <th style="padding:6px;border:1px solid #ddd;text-align:right">Importe</th>
            </tr>
          </thead>
          <tbody>
    `;

            dto.Filas.forEach(r => {
                html += `
        <tr>
          <td style="padding:6px;border:1px solid #eee">${r.EmpresaNombre}</td>
          <td style="padding:6px;border:1px solid #eee;text-align:center">${r.Mes}/${r.Anyo}</td>
          <td style="padding:6px;border:1px solid #eee;text-align:right">${formatMoney(r.ImporteTotal)}</td>
        </tr>
      `;
            });

            html += `
          </tbody>
        </table>
      </div>
    `;
        });

        html += "</div>";
        return html;
    }

    function generarResumenConceptosAsuntosPrev(preview) {
        if (!preview || !preview.length) {
            return "<p>No hay asuntos pendientes este mes.</p>";
        }
        let html = `
    <table style="width:100%;border-collapse:collapse;font-size:0.9em">
      <thead>
        <tr style="background:#f0f0f0">
          <th style="padding:6px;border:1px solid #ddd">Tipo</th>
          <th style="padding:6px;border:1px solid #ddd">Empresa</th>
          <th style="padding:6px;border:1px solid #ddd;text-align:center">Periodo</th>
          <th style="padding:6px;border:1px solid #ddd;text-align:right">Horas</th>
        </tr>
      </thead>
      <tbody>
  `;
        preview.forEach(r => {
            html += `
      <tr>
        <td>${r.TAR_Nombre}</td>
        <td>${r.EmpresaNombre}</td>
        <td style="text-align:center">${r.Mes}/${r.Anyo}</td>
        <td style="text-align:right">${r.ImporteTotal.toFixed(2)} h</td>
      </tr>
    `;
        });
        html += `
      </tbody>
    </table>
  `;
        return html;
    }


    async function ejecutarGeneracionConceptos() {
        mostrarDivCargando();
        try {
            const response = await $.ajax({
                url: AppConfig.urls.GenerarTodosConceptos,
                type: 'POST',
                dataType: 'json'
            });
            ocultarDivCargando();

            // — Si devolvió errores de validación al persistir —
            if (response.errors?.length) {
                await Swal.fire({
                    icon: 'error',
                    title: 'Errores generando conceptos',
                    html: `<ul style="text-align:left">
                 ${response.errors.map(e => `<li>${e}</li>`).join('')}
               </ul>`,
                    confirmButtonText: 'Entendido'
                });
                return;
            }

            // — Si success=false por otro motivo —
            if (!response.success) {
                mostrarErrorPedidos();
                return;
            }

            // — Éxito: mostrar resumen de conceptos creados —
            const htmlFin = `
      <div style="text-align:left">
        <h5>Conceptos de Licencias</h5>
        ${generarResumenConceptosLicenciasFin(response.licencias)}
        <hr/>
        <h5>Conceptos de Soporte</h5>
        ${generarResumenConceptosSoporteFin(response.soporte)}
        <hr/>
        <h5>Conceptos de Asuntos</h5>
        ${generarResumenConceptosAsuntosFin(response.asuntos)}
      </div>
    `;
            await Swal.fire({
                title: "Proceso Completado",
                html: htmlFin,
                icon: "success"
            });

            // — REFRESCAR TABLA —
            // 1) Destruir el DataTable actual
            const tabla = $('#tablaConceptos').DataTable();
            tabla.destroy();

            mostrarDivCargando();
            // 2) Vaciar el cuerpo de la tabla
            $('#tablaConceptos tbody').empty();

            // 3) Llamar de nuevo a la carga de datos
            await ObtenerConceptosFacturacion();
            ocultarDivCargando();

        } catch (err) {
            ocultarDivCargando();
            registrarErrorjQuery(err.status, err.message);
        }
    }

    function generarResumenConceptosLicenciasFin(resultado) {
        if (!resultado || resultado.length === 0) {
            return "<ul><li><strong>No se ha generado ningún concepto de licencia</strong></li></ul>";
        }

        let mensaje = "<ul style='text-align: left;'>";
        resultado.forEach(empresa => {
            mensaje += `<li><strong>${empresa.EmpresaNombre}</strong>: ${empresa.ConceptosCreados} concepto(s) generados</li>`;
        });
        mensaje += "</ul>";
        return mensaje;
    }

    function generarResumenConceptosSoporteFin(resultado) {
        if (!resultado || resultado.length === 0) {
            return "<ul><li><strong>No se ha generado ningún concepto de soporte</strong></li></ul>";
        }
        let mensaje = "<ul style='text-align: left;'>";
        resultado.forEach(empresa => {
            mensaje += `<li><strong>${empresa.EmpresaNombre}</strong>: ${empresa.ConceptosCreados} concepto(s) generados</li>`;
        });
        mensaje += "</ul>";
        return mensaje;
    }

    function generarResumenConceptosAsuntosFin(resultado) {
        if (!resultado || !resultado.length) {
            return "<ul><li><strong>No se ha generado ningún concepto de asuntos</strong></li></ul>";
        }
        let msg = "<ul style='text-align:left;'>";
        resultado.forEach(e => {
            msg += `<li><strong>${e.EmpresaNombre}</strong>: ${e.ConceptosCreados} concepto(s)</li>`;
        });
        msg += "</ul>";
        return msg;
    }
});