async function cargarCombosTelefonia() {
    try {
        // Empresas
        const empresas = await $.getJSON(AppConfig.urls.ObtenerComboEmpresas);
        const $emp = $('#TFN_EMP_Id');
        $emp.empty().append('<option value="">-- Seleccione --</option>');
        empresas.forEach(e => {
            $emp.append(`<option value="${e.EMP_Id}">${e.EMP_Nombre}</option>`);
        });

        // Plantas EMP
        const plantas = await $.getJSON(AppConfig.urls.ObtenerComboEmpresas);
        const $planta = $('#TFN_Planta_EMP_Id');
        $planta.empty().append('<option value="">-- Seleccione --</option>');
        plantas.forEach(p => {
            $planta.append(`<option value="${p.EMP_Id}">${p.EMP_Nombre}</option>`);
        });
    } catch (err) {
        console.error('Error cargando combos:', err);
    }
}

$(document).ready(async function () {
    await VerificarSesionActiva(OpcionMenu.TelefoniaDatos);
    await cargarCombosTelefonia();
    await ObtenerTelefonia();

    ocultarDivCargando();

    $('#btnNuevo').click(() => {
        $('#formTelefonia')[0].reset();
        $('#TFN_Id').val('');
        $('#modalTelefoniaLabel').text('Alta Telefónica');
        $('#modalTelefonia').modal('show');
    });

    $('#btnImportarExcel').on('click', function () {
        $('#archivoExcel').val('');
        $("#modalImportar").modal("show");
    });

    $('#btnSubirProcesar').click(async function (e) {
        e.preventDefault();

        let archivo = $('#archivoExcel')[0].files[0];
        let tipoImportacion = $('input[name="tipoImportacion"]:checked').val();

        //if (!archivo) {
        //    mostrarToast("Por favor, seleccione un archivo.", TipoToast.Error);
        //    return;
        //}

        let formData = new FormData();
        formData.append("archivoExcel", archivo);

        $("#modalImportar").modal('hide');
        mostrarDivCargando();

        try {
            let urlDestino = tipoImportacion === 'roaming' ? AppConfig.urls.ImportarExcelTelefoniaRoaming : AppConfig.urls.ImportarExcelTelefonia;

            const response = await $.ajax({
                url: urlDestino,
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false
            });

            ocultarDivCargando();

            if (response.success) {
                const total = response.rowsInserted ?? response.count ?? null;
                mostrarToast(
                    total != null ? `Archivo importado correctamente (${total} registros).`
                        : "Archivo importado correctamente.",
                    TipoToast.Success
                );
                return;
            }

            const { excelErroneo, message, fileUrl } = response;

            // Texto base según tipo de error
            const textoBase = excelErroneo
                ? "La importación NO se ha realizado por un error interno. Consulte el log."
                : "La importación NO se ha realizado. Puede descargar el Excel de errores.";

            // Monta HTML con detalle (si viene)
            const html = [
                `<p>${textoBase}</p>`,
                message ? `<p class="text-start"><small>${message}</small></p>` : ""
            ].join("");

            Swal.fire({
                title: "Error al importar",
                html,
                icon: "error",
                confirmButtonText: "Cerrar",
                confirmButtonColor: "#d33",
                showDenyButton: !!fileUrl && !excelErroneo,     // solo si hay Excel de errores
                denyButtonText: "Descargar errores"
            }).then(res => {
                if (res.isDenied && fileUrl) {
                    // Descarga/abre el Excel de errores
                    try {
                        window.open(fileUrl, "_blank"); // más fiable que fabricar un <a> temporal
                    } catch {
                        // último recurso
                        location.href = fileUrl;
                    }
                }
            });

            $('#tablaTelefonia').DataTable().ajax.reload(null, false);
        } catch (xhr) {
            registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
        }
    });

    $('#btnGuardarTelefonia').click(async () => {
        const requeridos = ['#TFN_Anyo', '#TFN_Mes', '#TFN_Tipo', '#TFN_FechaInicio', '#TFN_FechaFin'];
        let faltan = [];

        // Validar y marcar campos vacíos
        requeridos.forEach(id => {
            const $campo = $(id);
            if (!$campo.val()) {
                $campo.addClass('campo-invalido');
                faltan.push(id);
            } else {
                $campo.removeClass('campo-invalido');
            }
        });

        if (faltan.length > 0) {
            mostrarToast('Por favor, completa los campos obligatorios.', TipoToast.Warning);
            return;
        }

        const obj = {
            TFN_Id: $('#TFN_Id').val() || 0,
            TFN_Anyo: +$('#TFN_Anyo').val(),
            TFN_Mes: +$('#TFN_Mes').val(),
            TFN_Tipo: +$('#TFN_Tipo').val(),
            TFN_EMP_Id: +$('#TFN_EMP_Id').val(),
            TFN_Planta_EMP_Id: +$('#TFN_Planta_EMP_Id').val(),
            TFN_Planta_Departamento: $('#TFN_Planta_Departamento').val(),
            TFN_Planta_Sede: $('#TFN_Planta_Sede').val(),
            TFN_Planta_Uso: $('#TFN_Planta_Uso').val(),
            TFN_Ciclo: $('#TFN_Ciclo').val(),
            TFN_NumFactura: $('#TFN_NumFactura').val(),
            TFN_NumCuenta: $('#TFN_NumCuenta').val(),
            TFN_Categoria: $('#TFN_Categoria').val(),
            TFN_Telefono: $('#TFN_Telefono').val(),
            TFN_Extension: $('#TFN_Extension').val(),
            TFN_TipoCuota: $('#TFN_TipoCuota').val(),
            TFN_Bytes: +$('#TFN_Bytes').val() || 0,
            TFN_Importe: +$('#TFN_Importe').val() || 0,
            TFN_FechaInicio: $('#TFN_FechaInicio').val(),
            TFN_FechaFin: $('#TFN_FechaFin').val()
        };

        try {
            const resp = await $.ajax({
                url: AppConfig.urls.GuardarTelefonia,
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(obj)
            });
            if (resp.success) {
                $('#modalTelefonia').modal('hide');
                tablaDatos.ajax.reload(null, false);
                mostrarToast('Guardado con éxito', TipoToast.Success);
            } else {
                mostrarToast(resp.message || 'Error al guardar', TipoToast.Error);
            }
        } catch (xhr) {
            registrarErrorjQuery(xhr.status, obtenerMensajeErrorAjax(xhr));
        }
    });
});

$(document).on('input change', '#TFN_Anyo, #TFN_Mes, #TFN_Tipo, #TFN_FechaInicio, #TFN_FechaFin', function () {
    if ($(this).val()) {
        $(this).removeClass('campo-invalido');
    }
});

let tablaDatos;

async function ObtenerTelefonia() {
    let columnasConFiltro = [];
    tablaDatos = inicializarDataTable("#tablaTelefonia", {
        serverSide: true,
        processing: true, 
        ajax: {
            url: AppConfig.urls.ObtenerTelefonias,
            type: 'POST',
            dataType: "json",
            cache: false,
            dataSrc: function (json) {
                console.log("Datos recibidos del servidor:", json);
                return json.data; // DataTables sigue funcionando
            }
        },
        columns: [
            { data: 'TFN_Anyo' },
            { data: 'TFN_Mes' },
            { data: 'TipoNombre' },
            { data: 'EmpresaNombre' },
            { data: 'TFN_Planta_EMP_Id' },
            { data: 'TFN_Planta_Departamento' },
            { data: 'TFN_Planta_Sede' },
            { data: 'TFN_Planta_Uso' },
            { data: 'TFN_Ciclo' },
            { data: 'TFN_NumFactura' },
            { data: 'TFN_NumCuenta' },
            { data: 'TFN_Categoria' },
            { data: 'TFN_Telefono' },
            { data: 'TFN_Extension' },
            { data: 'TFN_TipoCuota' },
            { data: 'TFN_Bytes' },
            {
                data: 'TFN_Importe',
                render: (v) => v != null ? parseFloat(v).toFixed(2) : ''
            },
            {
                data: 'TFN_FechaInicio',
                title: 'Fecha Inicio',
                type: "date",
                render: function (data, type, row) {
                    if (!data) return "";

                    let fecha = moment(data); // autodetecta el formato ISO
                    if (!fecha.isValid()) {
                        // Por si acaso viene como texto en español
                        fecha = moment(data, "DD/MM/YYYY HH:mm:ss");
                    }

                    if (!fecha.isValid()) return data;

                    return `<span data-order="${fecha.unix()}">${fecha.format("DD/MM/YYYY")}</span>`;
                }
            },
            {
                data: 'TFN_FechaFin',
                title: 'Fecha Inicio',
                type: "date",
                render: function (data, type, row) {
                    if (!data) return "";

                    let fecha = moment(data); // autodetecta el formato ISO
                    if (!fecha.isValid()) {
                        // Por si acaso viene como texto en español
                        fecha = moment(data, "DD/MM/YYYY HH:mm:ss");
                    }

                    if (!fecha.isValid()) return data;

                    return `<span data-order="${fecha.unix()}">${fecha.format("DD/MM/YYYY")}</span>`;
                }
            },
            {
                className: 'td-btn',
                data: null,
                title: '<span class="sReader">Acción</span>',
                responsivePriority: 2,
                orderable: false,
                render: function (data, type, row) {
                    const json = JSON.stringify(row).replace(/"/g, '&quot;');
                    return `
                        <button class="btn btn-icon btn-outline-secondary"
                                onclick="abrirModalTelefonia(this)"
                                data-record="${json}">
                          <i class="bi bi-pencil"></i>
                        </button>
                        <button class="btn btn-icon btn-outline-danger"
                                onclick="eliminarTelefonia(this)"
                                data-record="${json}">
                          <i class="bi bi-trash"></i>
                        </button>`;
                }
            }
        ]
    }, columnasConFiltro, 'export_vodafone');

    $('#tablaTelefonia').on('processing.dt', function (e, settings, processing) {
        if (processing) {
            mostrarDivCargando();
        } else {
            ocultarDivCargando();
        }
    });

    $(window).resize(() => tablaDatos.columns.adjust().draw());

    $("#formBuscar").on("keyup input", function () {
        tablaDatos.search(this.value, false, false).draw();
        //guardarFiltros();
    });

    return tablaDatos;
}

function abrirModalTelefonia(btn) {
    const data = JSON.parse(btn.getAttribute('data-record'));

    const fechaInicio = moment(data.TFN_FechaInicio).format("YYYY-MM-DD");
    const fechaFin = moment(data.TFN_FechaFin).format("YYYY-MM-DD");

    $('#TFN_FechaInicio').val(fechaInicio);
    $('#TFN_FechaFin').val(fechaFin);

    Object.keys(data).forEach(k => {
        const fld = document.getElementById(k);
        if (!fld) return;

        if ((k === "TFN_FechaInicio" || k === "TFN_FechaFin") && data[k]) {
            // Intenta parsear la fecha
            const d = new Date(data[k])
        } else {
            fld.value = data[k] ?? '';
        }
    });

    $('#modalTelefoniaLabel').text('Edición Telefónica');
    $('#modalTelefonia').modal('show');
}

function eliminarTelefonia(btn) {
    const data = JSON.parse(btn.getAttribute('data-record'));
    mostrarAlertaConfirmacion({
        titulo: `Eliminar registro TFN_Id ${data.TFN_Id}?`,
        onConfirmar: async () => {
            try {
                const resp = await $.ajax({
                    url: AppConfig.urls.EliminarTelefonia,
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ idTelefonia: data.TFN_Id })
                });
                if (resp.success) {
                    $('#tablaTelefonia').DataTable().ajax.reload(null, false);
                    mostrarToast('Eliminado con éxito', TipoToast.Success);
                } else {
                    mostrarToast(resp.message || 'No se pudo eliminar', TipoToast.Warning);
                }
            } catch {
                mostrarToast('Error de conexión', TipoToast.Error);
            }
        }
    });
}
