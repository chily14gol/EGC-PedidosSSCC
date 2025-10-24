$(document).ready(function () {
    const storageKey = Filtros.Licencias;

    async function inicializarVistaLicencias() {
        try {
            await VerificarSesionActiva(OpcionMenu.Licencias);
            await Promise.all([
                cargarComboTareas(),
                cargarComboLicenciasPadre(),
                obtenerComboEmpresas()
            ]);

            establecerFiltros();
        } catch (xhr) {
            const mensaje = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, mensaje);
        }
    }

    inicializarVistaLicencias();

    InicializarSelectTiposEnte();
    cargarComboTiposEnte(0);

    InicializarSelectLicenciasIncompatibles();
    cargarComboLicenciasIncompatibles(0);

    async function cargarComboTareas() {
        const tipos = [Tipo.CANTIDAD_FIJA];

        const tareas = await $.ajax({
            url: AppConfig.urls.obtenerTareasCombo,
            method: 'GET',
            dataType: 'json',
            traditional: true,
            data: { listaTiposTarea: tipos }
        });

        // helper para llenar un <select>
        function fill($sel, items, placeholder) {
            $sel.empty().append(`<option value="">${placeholder}</option>`);
            items.forEach(t => $sel.append(`<option value="${t.TAR_Id}">${t.TAR_Nombre}</option>`));
        }

        fill($('#tareaSW'), tareas, 'Seleccione SW');
        fill($('#tareaAntivirus'), tareas, 'Seleccione AV');
        fill($('#tareaBackup'), tareas, 'Seleccione BK');
    }

    async function cargarComboLicenciasPadre() {
        try {
            const response = await $.ajax({
                url: AppConfig.urls.obtenerLicenciasCombo,
                type: 'GET',
                dataType: 'json'
            });

            const $select = $('#padre');
            $select.empty().append('<option value="">Seleccione licencia</option>');

            $.each(response, function (index, item) {
                $select.append(`<option value="${item.LIC_Id}">${item.LIC_Nombre}</option>`);
            });
        } catch (xhr) {
            const mensaje = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, mensaje);
        }
    }

    async function obtenerComboEmpresas() {
        try {
            const response = await $.ajax({
                url: AppConfig.urls.ObtenerComboEmpresas,
                type: 'GET',
                dataType: 'json'
            });

            const $select = $('#empresa');
            $select.empty().append('<option value="">Seleccione empresa</option>');

            $.each(response, function (index, item) {
                $select.append(`<option value="${item.EMP_Id}">${item.EMP_Nombre}</option>`);
            });
        } catch (xhr) {
            const mensaje = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, mensaje);
        }
    }
    function guardarFiltros() {
        let filtroActual = {
            general: $("#formBuscar").val(),
            nombre: $('#filterNombre').val(),
            grupo: $('#filterGrupo').val(),
            padre: $('#filterPadre').val()
        };
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }

    function establecerFiltros() {
        let savedFilters = localStorage.getItem(storageKey);
        let hasFilters = false;

        if (savedFilters) {
            savedFilters = JSON.parse(savedFilters);
            $("#formBuscar").val(savedFilters.general);
            $("#filterNombre").val(savedFilters.nombre || "");
            $("#filterGrupo").val(savedFilters.grupo || "");
            $("#filterPadre").val(savedFilters.padre || "");

            let { general, ...otrosFiltros } = savedFilters;
            hasFilters = Object.values(otrosFiltros).some(value => value);
        }

        ObtenerLicencias();

        setTimeout(function () {
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

    $('#btnNuevo').on('click', function () {
        $('#idLicencia').val('-1');
        $('#nombre').val('');
        $('#grupo').val('');
        $('#padre').val('');
        $('#nombre').removeClass('is-invalid');
        $('#nombreMS').val('');
        $('#gestionado').prop('checked', true);

        $('#modalEditarLabel').text('Agregar Licencia');
        $('#modalEditar').modal('show');
    });

    $('#btnBuscar').on('click', function () {
        let nombre = $('#filterNombre').val();
        let grupo = $('#filterGrupo').val();
        let padre = $('#filterPadre').val();

        let table = $('#table').DataTable();
        table.columns(0).search(nombre).draw();
        table.columns(1).search(grupo).draw();
        table.columns(2).search(padre).draw();

        guardarFiltros();
    });

    $('#modalTarifas, #modalExcepciones, #modalPedidosEsfuerzo').modal({
        backdrop: 'static',
        keyboard: false
    });

    $('#btnProcesarExcelLicencias').on('click', async function () {
        const archivo = $('#archivoLicencias')[0].files[0];
        if (!archivo) {
            mostrarToast("Selecciona un archivo primero", TipoToast.Warning);
            return;
        }

        $('#modalImportar').modal('hide');
        mostrarDivCargando();

        try {
            const formData = new FormData();
            formData.append("archivoExcel", archivo);
            const resp = await fetch(AppConfig.urls.ProcesarLicenciasDesdeExcel, {
                method: 'POST',
                body: formData
            });
            const contentType = resp.headers.get("Content-Type") || "";

            if (contentType.startsWith("application/json")) {
                const json = await resp.json();
                if (json.success) {
                    await Swal.fire("Proceso Completado",
                        "Las licencias se han procesado correctamente.",
                        "success");
                }
                else if (json.excelErroneo) {
                    await Swal.fire("Error al importar Excel",
                        json.mensajeExcelErroneo,
                        "error");
                }
                else {
                    await Swal.fire("Error",
                        json.message || "Ha ocurrido un error",
                        "error");
                }
            }
            else {
                // Viene un Excel (bloqueantes o no bloqueantes)
                const blob = await resp.blob();
                const disposition = resp.headers.get("Content-Disposition") || "";
                const match = disposition.match(/filename="?(.+)"?/);
                const filename = match ? match[1] : "resultado.xlsx";

                // descargamos
                const url = URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = url;
                a.download = filename;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(url);

                // miramos el tipo para el semáforo
                const tipo = resp.headers.get("X-Type");
                if (tipo === "bloqueante") {
                    await Swal.fire("Se encontraron errores bloqueantes",
                        "Descarga el Excel para ver el detalle.",
                        "error");
                }
                else if (tipo === "warning") {
                    await Swal.fire("Advertencias al importar",
                        "Descarga el Excel para revisar las advertencias.",
                        "warning");
                }
            }
        }
        catch (err) {
            console.error(err);
            const mensaje = obtenerMensajeErrorAjax(err);
            registrarErrorjQuery(err.status || 0, mensaje);
            mostrarToast("Error al conectar con el servidor", TipoToast.Error);
        }
        finally {
            ocultarDivCargando();
        }
    });

    $('#btnAbrirModalProcesar').on('click', function () {
        $('#modalImportar').modal('show');
    });

    $('#empresa').on('change', function () {
        const idEmpresa = $(this).val();
        const idLicencia = $('#idLicenciaExcepciones').val();

        if (idEmpresa && idLicencia) {
            $('#licenciasReemplazo').prop('disabled', false); // Activar el select
            cargarComboLicenciasReemplazo(idLicencia, idEmpresa); // Cargar datos
        } else {
            $('#licenciasReemplazo').val(null).trigger('change');
            $('#licenciasReemplazo').prop('disabled', true); // Desactivar si no hay datos válidos
        }
    });

    // Cada vez que se muestre el modal de Excepciones
    $('#modalExcepciones').on('shown.bs.modal', function () {
        // Recuperamos el DataTable ya inicializado
        const tabla = $('#tableExcepciones').DataTable();
        // Fuerza a DataTables a recalcular anchos y responsive
        tabla.columns.adjust().responsive.recalc();
    });

    $('#btnNuevaTarifa').on('click', function () {
        limpiarFormularioTarifa();
        $('#btnNuevaTarifa').hide();
        $('#btnCancelarTarifa').show();
        $('#btnGuardarTarifa').show();
        $('#formTarifas').slideDown();
    });

    $('#btnGuardarTarifa').click(async function (e) {
        e.preventDefault();

        const inicio = $('#fechaInicio').val(),
            fin = $('#fechaFin').val();

        // 1) Limpiar estilos previos
        ['#fechaInicio', '#precioUnitarioSW', '#precioUnitarioAntivirus', '#precioUnitarioBackup']
            .forEach(sel => $(sel).removeClass('is-invalid'));

        // 2) Validar obligatorios
        const campos = [
            { sel: '#fechaInicio', name: 'Fecha Inicio' },
            { sel: '#precioUnitarioSW', name: 'Precio SW' },
            { sel: '#precioUnitarioAntivirus', name: 'Precio Antivirus' },
            { sel: '#precioUnitarioBackup', name: 'Precio Backup' }
        ];

        let invalidos = [];

        campos.forEach(c => {
            if (!$(c.sel).val().toString().trim()) {
                invalidos.push(c.name);
                $(c.sel).addClass('is-invalid');
            }
        });
        if (invalidos.length) {
            mostrarToast(`Los siguientes campos son obligatorios: ${invalidos.join(', ')}`, TipoToast.Warning);
            return;
        }

        if (fin && new Date(fin) < new Date(inicio)) {
            $('#tarifaFechaFin').addClass('is-invalid');
            mostrarToast("Fecha Fin no puede ser anterior a Fecha Inicio.", TipoToast.Warning);
            return;
        }

        const obj = {
            LIT_LIC_Id: $('#idLicenciaTarifas').val(),
            LIT_FechaInicio: inicio,
            LIT_FechaFin: fin || null,
            LIT_PrecioUnitarioSW: parseFloat($('#precioUnitarioSW').val()),
            LIT_PrecioUnitarioAntivirus: parseFloat($('#precioUnitarioAntivirus').val()),
            LIT_PrecioUnitarioBackup: parseFloat($('#precioUnitarioBackup').val())
        };

        try {
            const res = await $.ajax({
                url: AppConfig.urls.GuardarTarifa,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(obj),
                dataType: 'json'
            });
            if (res.success) {
                mostrarToast('Tarifa guardada correctamente', TipoToast.Success);
                $('#btnNuevaTarifa').show();
                $('#btnCancelarTarifa').hide();
                $('#btnGuardarTarifa').hide();
                $('#formTarifas').hide();
                $('#tableTarifas').DataTable().ajax.reload(null, false);
            } else {
                mostrarToast(res.message || 'Hay solapamiento de fechas', TipoToast.Warning);
            }
        } catch (err) {
            registrarErrorjQuery(err.status, err.responseText);
        }
    });

    $('#btnGuardarMinimo').on('click', async function (e) {
        e.preventDefault();
        await guardarMinimo();
    });

    $('#btnCancelarMinimo').on('click', function () {
        cancelarMinimo();
    });

    $('#btnNuevoMinimo').on('click', function () {
        limpiarFormularioMinimo();
        $('#btnNuevoMinimo').hide();
        $('#btnCancelarMinimo').show();
        $('#btnGuardarMinimo').show();
        $('#formMinimos').slideDown();
    });

    $('#btnNuevoEnte').on('click', () => {
        $('#formEntesLicencias').slideDown();
        $('#btnGuardarEnteLicencia').show();
        $('#btnCancelarEnte').show();
        $('#btnNuevoEnte').hide();
    });

    $('#btnCancelarEnte').on('click', () => {
        limpiarFormularioEntes();
        $('#formEntesLicencias').slideUp();
        $('#btnGuardarEnteLicencia').hide();
        $('#btnCancelarEnte').hide();
        $('#btnNuevoEnte').show();
    });

    $('#btnGuardarEnteLicencia').on('click', async () => {
        const payload = {
            ENL_ENT_Id: parseInt($('#selectEnte').val(), 10),
            ENL_LIC_Id: parseInt($('#idLicenciaEntes').val(), 10),
            ENL_FechaInicio: $('#fechaInicioEnte').val(),
            ENL_FechaFin: $('#fechaFinEnte').val() || null
        };
        try {
            const res = await $.ajax({
                url: AppConfig.urls.GuardarEnteLicencia,
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(payload),
                dataType: 'json'
            });
            if (res.success) {
                mostrarToast('Asociación guardada', TipoToast.Success);
                limpiarFormularioEntes();
                $('#tableEntesLicencias').DataTable().ajax.reload(null, false);
                // volver al estado “nuevo” oculto
                $('#formEntesLicencias').slideUp();
                $('#btnGuardarEnteLicencia').hide();
                $('#btnCancelarEnte').hide();
                $('#btnNuevoEnte').show();
            } else {
                mostrarToast(res.message || 'Error al guardar', TipoToast.Error);
            }
        } catch (xhr) {
            console.error(xhr);
        }
    });

    $('#btnPrevisulizarConceptos').click(async () => {
        mostrarDivCargando();
        try {
            const response = await $.ajax({
                url: AppConfig.urls.PrevisualizarConceptos,
                type: 'POST',
                dataType: 'json',
                contentType: 'application/json; charset=utf-8'
            });
            ocultarDivCargando();

            if (!response.success) {
                mostrarErrorPedidos();
                return;
            }

            // Extraemos previews y sus errores
            const { licencias, licenciasErrors } = response;

            // Si no hay nada en ninguna sección
            if (!licencias.length && !(licenciasErrors?.length)
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
                </div>`;

            const hasConcepts = licencias.length > 0;

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
function generarResumenConceptosLicenciasPrev(preview) {
    let html = `
      <table style="width:100%;border-collapse:collapse;font-size:0.9em">
        <thead>
          <tr style="background:#f0f0f0">
            <th style="padding:6px 8px;border:1px solid #ddd;width:10%">Tarea</th>
            <th style="padding:6px 8px;border:1px solid #ddd;text-align:center;width:10%">Periodo</th>
            <th style="padding:6px 8px;border:1px solid #ddd;text-align:center;width:10%" title="Sin ajustes">Cantidad (real) &#9432;</th>
            <th style="padding:6px 8px;border:1px solid #ddd;width:43%">Licencias incluidas</th>
            <th style="padding:6px 8px;border:1px solid #ddd;text-align:right;width:15%">Importe</th>
          </tr>
        </thead>
        <tbody>
    `;

    const empresas = {};
    preview.forEach(r => {
        if (!empresas[r.EmpresaNombre]) {
            empresas[r.EmpresaNombre] = [];
        }
        empresas[r.EmpresaNombre].push(r);
    });

    Object.keys(empresas).forEach(emp => {
        html += `
          <tr style="background:#e9ecef;font-weight:bold">
            <td colspan="5" style="padding:6px;border:1px solid #ddd">${emp}</td>
          </tr>
        `;

        empresas[emp].forEach(r => {
            html += `
              <tr>
                <td style="padding:6px 8px;border:1px solid #ddd">${r.TAR_Nombre}</td>
                <td style="padding:6px 8px;border:1px solid #ddd;text-align:center">${r.Mes}/${r.Anyo}</td>
                <td style="padding:6px 8px;border:1px solid #ddd;text-align:center">${r.Cantidad}</td>
                <td style="padding:6px 8px;border:1px solid #ddd">${r.LicenciasIncluidas}</td>
                <td style="padding:6px 8px;border:1px solid #ddd;text-align:right">${formatMoney(r.ImporteTotal)}</td>
              </tr>
            `;
        });
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
      </div>
    `;
        await Swal.fire({
            title: "Proceso Completado",
            html: htmlFin,
            icon: "success"
        });

        // 1) Destruir el DataTable actual
        const tabla = $('#table').DataTable();
        tabla.destroy();

        mostrarDivCargando()
        $('#table tbody').empty();

        await ObtenerLicencias();
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

////////////////////////////////////////////////////////////////////
function verMinimos(button) {
    const obj = JSON.parse(button.getAttribute('data-licencia'));
    // Guardar el ID de la licencia en el hidden
    $('#idLicenciaMinimos').val(obj.LIC_Id);

    // Limpiar formularios
    limpiarFormularioMinimo();

    // Cargar combo de empresas (para que el select tenga todas las empresas)
    cargarComboEmpresasMinimo();

    // Cargar la tabla de mínimos (por licencia)
    ObtenerMinimos(obj.LIC_Id);

    // Ocultar el formulario por defecto
    $('#formMinimos').hide();
    $('#btnCancelarMinimo').hide();
    $('#btnGuardarMinimo').hide();
    $('#btnNuevoMinimo').show();

    // Mostrar el modal
    $('#modalMinimos').modal('show');
}

async function cargarComboEmpresasMinimo() {
    try {
        // Reutilizamos el endpoint ya existente ObtenerComboEmpresas
        const response = await $.ajax({
            url: AppConfig.urls.ObtenerComboEmpresas,
            type: 'GET',
            dataType: 'json'
        });
        const $sel = $('#empresaMinimo');
        $sel.empty().append('<option value="">Seleccione empresa</option>');
        $.each(response, function (idx, item) {
            // Se asume que el JSON devuelve { EMP_Id, EMP_Nombre }
            $sel.append(`<option value="${item.EMP_Id}">${item.EMP_Nombre}</option>`);
        });
    } catch (xhr) {
        const msg = obtenerMensajeErrorAjax(xhr);
        registrarErrorjQuery(xhr.status, msg);
    }
}

function ObtenerMinimos(idLicencia) {
    let columnasConFiltro = [];
    let tablaDatos = inicializarDataTable('#tableMinimos', {
        paging: false,
        searching: false,
        info: false,
        ordering: false,
        dom: 't',
        ajax: {
            url: AppConfig.urls.ObtenerMinimos,
            type: 'GET',
            dataSrc: function (json) {
                return json;
            },
            data: { licenciaId: idLicencia },
            dataType: 'json',
            error: function (xhr, status, error) {
                const msg = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, msg);
            }
        },
        rowId: function (row) {
            // Identificador único de la fila: “fila-{LIC_Id}-{EMP_Id}”
            return `fila-${row.LEM_LIC_Id}-${row.LEM_EMP_Id}`;
        },
        columns: [
            { data: 'EmpresaNombre', title: 'Empresa' },
            { data: 'LEM_MinimoFacturar', title: 'Mínimo', className: 'text-end' },
            {
                className: 'td-btn',
                data: null,
                title: '<span class="sReader">Acción</span>',
                responsivePriority: 2,
                orderable: false,
                render: function (data, type, row) {
                    // Botones “Editar” y “Eliminar”:
                    const rowJson = JSON.stringify(row).replace(/"/g, "&quot;");
                    return `
                            <button type="button" class="btn btn-icon btn-editar btn-outline-secondary me-2"
                                data-minimo='${rowJson}'
                                onclick="editarMinimo(this)"
                                title="Editar">
                                <i class="bi bi-pencil-square"></i>
                            </button>
                            <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
                                data-minimo='${rowJson}'
                                onclick="eliminarMinimo(this)"
                                title="Eliminar">
                                <i class="bi bi-trash"></i>
                            </button>`;
                }
            }
        ]
    }, columnasConFiltro, 'export_minimos');

    $(window).resize(function () {
        tablaDatos.columns.adjust().draw();
    });
}

function limpiarFormularioMinimo() {
    $('#empresaMinimo').val('');
    $('#minimoFacturar').val('');
    // Dejar el form oculto
    $('#formMinimos').hide();
    $('#btnGuardarMinimo').hide();
    $('#btnCancelarMinimo').hide();
}

function editarMinimo(button) {
    const obj = JSON.parse(button.getAttribute('data-minimo'));
    // Rellenar el formulario con los valores existentes
    $('#empresaMinimo').val(obj.LEM_EMP_Id);
    $('#minimoFacturar').val(obj.LEM_MinimoFacturar);
    // Deshabilitamos selección de empresa (no se permite cambiar la empresa de un mínimo ya creado)
    $('#empresaMinimo').prop('disabled', true);
    // Mostrar formulario
    $('#btnNuevoMinimo').hide();
    $('#btnCancelarMinimo').show();
    $('#btnGuardarMinimo').show();
    $('#formMinimos').slideDown();
}

function cancelarMinimo() {
    limpiarFormularioMinimo();
    // Reactivar el combo empresa
    $('#empresaMinimo').prop('disabled', false);
    // Solo mostrar botón “Nueva entrada”
    $('#btnNuevoMinimo').show();
}

async function guardarMinimo() {
    // 1) Limpiar estilos previos de validación
    ['#empresaMinimo', '#minimoFacturar'].forEach(sel => $(sel).removeClass('is-invalid'));

    // 2) Validar obligatorios
    let invalidos = [];
    if (!$('#empresaMinimo').val()) {
        invalidos.push('Empresa');
        $('#empresaMinimo').addClass('is-invalid');
    }
    if (!$('#minimoFacturar').val().toString().trim()) {
        invalidos.push('Mínimo a facturar');
        $('#minimoFacturar').addClass('is-invalid');
    }
    if (invalidos.length) {
        mostrarToast(AppConfig.mensajes.camposObligatorios + `${invalidos.join(', ')}`, TipoToast.Warning);
        return;
    }

    // 3) Recolectar datos
    const payload = {
        LEM_LIC_Id: parseInt($('#idLicenciaMinimos').val()),
        LEM_EMP_Id: parseInt($('#empresaMinimo').val()),
        LEM_MinimoFacturar: parseInt($('#minimoFacturar').val())
    };

    // 4) Enviar AJAX
    try {
        const res = await $.ajax({
            url: AppConfig.urls.GuardarMinimo,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(payload),
            dataType: 'json'
        });
        if (res.success) {
            mostrarToast('Mínimo guardado correctamente', TipoToast.Success);
            // Limpiar y ocultar formulario
            limpiarFormularioMinimo();
            $('#empresaMinimo').prop('disabled', false);
            $('#btnNuevoMinimo').show();
            // Recargar tabla
            $('#tableMinimos').DataTable().ajax.reload(null, false);
        } else {
            registrarErrorjQuery(res.status, res.message);
        }
    } catch (xhr) {
        const msg = obtenerMensajeErrorAjax(xhr);
        registrarErrorjQuery(xhr.status, msg);
    }
}

function eliminarMinimo(button) {
    setTimeout(() => {
        const obj = JSON.parse(button.getAttribute('data-minimo'));
        // Formatear la empresa para mostrar en el diálogo
        const empresaNombre = obj.EmpresaNombre;
        mostrarAlertaConfirmacion({
            titulo: `¿Estás seguro de que deseas eliminar el mínimo para '${empresaNombre}'?`,
            backdrop: false,
            onConfirmar: function () {
                $.ajax({
                    url: AppConfig.urls.EliminarMinimo,
                    type: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    data: JSON.stringify({
                        idLicencia: obj.LEM_LIC_Id,
                        idEmpresa: obj.LEM_EMP_Id
                    }),
                    dataType: 'json'
                })
                    .done(function (response) {
                        if (response.success) {
                            mostrarToast('Mínimo eliminado correctamente', TipoToast.Success);
                            let tabla = $('#tableMinimos').DataTable();
                            tabla.ajax.reload(null, false);
                        } else {
                            registrarErrorjQuery(response.status, response.message);
                        }
                    })
                    .fail(function (xhr, status, error) {
                        const msg = obtenerMensajeErrorAjax(xhr);
                        registrarErrorjQuery(xhr.status, msg);
                    });
            }
        });
    }, 300);
}



function ObtenerLicencias() {
    let columnasConFiltro = [];
    let tablaDatos = inicializarDataTable('#table', {
        ajax: {
            url: AppConfig.urls.obtenerLicencias,
            type: 'GET',
            dataSrc: '',
            error: function (xhr, status, error) {
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
            }
        },
        columns: [
            { data: 'LIC_Nombre', title: 'Nombre' },
            { data: 'LIC_MaximoGrupo', title: 'Máximo Grupo' },
            { data: 'NombrePadre', title: 'Licencia Padre' },
            {
                className: 'td-btn',
                data: null,
                title: '<span class="sReader">Acción</span>',
                responsivePriority: 2,
                orderable: false,
                render: function (data, type, row) {
                    // 1) Lógica para el resto de botones:
                    const tieneHijas = row.TieneHijas === true;
                    const deshabilitadoGeneral = tieneHijas
                        ? 'disabled title="Esta licencia tiene hijas" class="btn btn-icon btn-outline-secondary me-2 btn-disabled"'
                        : 'class="btn btn-icon btn-outline-secondary me-2"';

                    // 2) Lógica específica para el botón de "Mínimos": solo habilitar si es padre
                    const esPadre = row.LIC_LIC_Id_Padre == null;
                    const deshabilitadoMinimos = esPadre
                        ? 'class="btn btn-icon btn-outline-secondary me-2"'
                        : 'disabled title="Solo licencias padre pueden gestionar mínimos" class="btn btn-icon btn-outline-secondary me-2 btn-disabled"';

                    const deshabilitadoEntes = (esPadre && tieneHijas)
                        ? 'disabled title="Solo licencias padre pueden gestionar los entes" class="btn btn-icon btn-outline-secondary me-2 btn-disabled"'
                        : 'class="btn btn-icon btn-outline-secondary me-2"'

                    const dataLicencia = JSON.stringify(row).replace(/"/g, "&quot;");

                    return `<div class="btn-group" role="group">
                            <button type="button" class="btn btn-icon btn-editar btn-outline-secondary me-2"
                                data-licencia='${dataLicencia}' onclick="editarLicencia(this)">
                                <i class="bi bi-pencil-square" title="Editar"></i>
                            </button>

                            <button type="button" ${deshabilitadoMinimos} data-licencia='${dataLicencia}' onclick="verTarifas(this)">
                                <i class="bi bi-currency-dollar" title="Tarifas"></i>
                            </button>

                            <button type="button" ${deshabilitadoGeneral}
                                data-licencia='${dataLicencia}' onclick="verExcepciones(this)">
                                <i class="bi bi-exclamation-triangle" title="Excepciones"></i>
                            </button>

                            <button type="button" ${deshabilitadoMinimos} 
                                data-licencia='${dataLicencia}' onclick="verMinimos(this)" title="Mínimos">
                                <i class="bi bi-cash-stack" title="Mínimos"></i>
                            </button>

                            <button type="button" ${deshabilitadoEntes}
                                    data-licencia='${dataLicencia}'
                                    onclick="verEntesLicencias(this)">
                                <i class="bi bi-people-fill" title="Entidades"></i>
                            </button>

                            <button type="button" ${deshabilitadoGeneral.replace('btn-outline-secondary', 'btn-outline-secondary btn-eliminar')}
                                data-licencia='${dataLicencia}' onclick="eliminarLicencia(this)">
                                <i class="bi bi-trash" title="Eliminar"></i>
                            </button>
                        </div>`;
                }

            }
        ]
    }, columnasConFiltro, 'export_licencias');

    $(window).resize(function () {
        tablaDatos.columns.adjust().draw();
    });

    $("#formBuscar").on("keyup input", function () {
        tablaDatos.search(this.value, false, false).draw();
        tablaDatos.responsive.rebuild();
        tablaDatos.responsive.recalc();
        tablaDatos.columns.adjust();
        tablaDatos.draw(false);
    });
}