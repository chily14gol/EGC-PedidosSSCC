const urlParams = new URLSearchParams(window.location.search);
const mode = urlParams.get('mode');

// Valor de tipo "Cantidad Fija" obtenido en servidor
const TIPO_CANTIDAD_FIJA = 3;

document.addEventListener("DOMContentLoaded", function () {
    if (mode === 'view') {
        // Deshabilitar todos los inputs si el modo es solo lectura
        document.querySelectorAll("input, textarea, select").forEach(element => {
            element.setAttribute("disabled", "disabled");
        });
        $("#datosSolicitud").show();
        $("#btnGuardar").hide();
    }
});

$(document).ready(function () {
    VerificarSesionActiva(OpcionMenu.Conceptos)
        .then(() => {
            return CargarComboTareaEmpresa();
        })
        .then(() => {
            let idConcepto = window.IdConcepto;
            return ObtenerConceptoDetalle(idConcepto)
                .then((data) => {
                    // Una vez cargado el detalle, pinto la tabla de Licencias MS
                    ObtenerLicenciasMS(idConcepto);
                    ObtenerTicketsConcepto(idConcepto);
                    ObtenerPartesConcepto(idConcepto);
                    ObtenerAplicacionesConcepto(idConcepto);
                    ObtenerModulosConcepto(idConcepto);
                    ObtenerModulosRepartoConcepto(idConcepto);
                    ObtenerProveedoresAsuntosConcepto(idConcepto);
                    ObtenerLicenciasAnualesConcepto(idConcepto);

                    // Después de poblar los campos, ajustar visibilidad según tipo de tarea
                    ajustarCamposPorTipo(data.TAR_TTA_Id, data.TLE_Cantidad, data.TAR_ImporteUnitario);
                });
        })
        .then(() => {
            ocultarDivCargando();
        })
        .catch((error) => {
            console.error('Error al cargar los datos:', error);
            ocultarDivCargando();
        });

    function CargarComboTareaEmpresa() {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: AppConfig.urls.ObtenerComboTareaEmpresa,
                type: 'GET',
                data: { verTodas: true },
                dataType: 'json',
                success: function (data) {
                    let $select = $('#tareaEmpresa');
                    $select.empty();
                    $select.append(`<option value=""></option>`);

                    $.each(data, function (index, item) {
                        $select.append(`<option value="${item.TEM_TAR_Id}|${item.TEM_EMP_Id}|${item.TEM_Anyo}">${item.EmpresaNombre}</option>`);
                    });

                    resolve();
                },
                error: function (xhr, status, error) {
                    const mensaje = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, mensaje);
                    reject(error);
                }
            });
        });
    }

    function ObtenerConceptoDetalle(idConcepto) {
        return new Promise((resolve, reject) => {
            $.ajax({
                type: 'GET',
                contentType: 'application/json; charset=utf-8',
                url: AppConfig.urls.ObtenerConceptoDetalle,
                data: { idConcepto: idConcepto },
                dataType: "json",
                success: function (data) {
                    // Rellenar campos básicos
                    $('#anioConcepto').val(data.TLE_Anyo);
                    $('#mesConcepto').val(data.TLE_Mes);

                    $('#presupuesto').val(formatNumber(data.TEM_Presupuesto));
                    $('#presupuestoConsumido').val(formatNumber(data.TEM_PresupuestoConsumido));
                    $('#cantidad').val(formatNumber(data.TLE_Cantidad));
                    $('#importeUnitario').val(formatNumber(data.TAR_ImporteUnitario));
                    $('#descripcion').val(data.TLE_Descripcion);
                    $('#inversion').prop('checked', data.TLE_Inversion);

                    $('#estado').val(data.EstadoNombre);
                    $('#fechaAprobacion').val(data.TLE_FechaAprobacion);
                    $('#comentarios').val(data.TLE_ComentarioAprobacion);
                    $('#aprobador').val(data.PersonaAprobador);

                    // Seleccionar opción de tarea - empresa en el combo
                    let idTarea = data.TLE_TAR_Id;
                    let idEmpresa = data.TLE_EMP_Id;
                    let Anyo = data.TLE_Anyo;
                    let idItemSeleccionado = idTarea + '|' + idEmpresa + '|' + Anyo;
                    let idEstado = data.TLE_ESO_Id;

                    if (idEstado == EstadosSolicitud.SinSolicitar) {
                        $('#btnSolicitar').show();
                    }

                    let $selectTareaEmpresa = $('#tareaEmpresa');
                    if ($selectTareaEmpresa.find('option[value="' + idItemSeleccionado + '"]').length > 0) {
                        $selectTareaEmpresa.val(idItemSeleccionado);
                    } else {
                        console.warn('El valor de la tarea empresa no está en el combo. Asegúrate de que esté cargado.');
                    }

                    // Calcular y mostrar Importe Total
                    // Para evitar problemas de coma/punto, parseamos a número primero
                    let cantidadNum = parseFloat(data.TLE_Cantidad) || 0;
                    let importeUnitarioNum = parseFloat(data.TAR_ImporteUnitario) || 0;

                    let totalCalculado;
                    // Si la tarea es de tipo CANTIDAD FIJA (3), el importe total = la propia cantidad
                    if (data.TAR_TTA_Id === TIPO_CANTIDAD_FIJA) {
                        totalCalculado = cantidadNum;
                    } else {
                        // En cualquier otro caso, mantenemos Cantidad × Importe unitario
                        totalCalculado = cantidadNum * importeUnitarioNum;
                    }

                    $('#importeTotal').val(formatNumber(totalCalculado));
                    // -------------------------------------------------------

                    resolve(data); // Devolvemos el objeto data para usarlo fuera
                },
                error: function (xhr, status, error) {
                    const mensaje = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, mensaje);
                    reject(error);
                }
            });
        });
    }

    function ajustarCamposPorTipo(tipoTareaId, cantidad, importeUnitario) {
        // Si el tipo de tarea es Cantidad Fija, ocultamos los campos de Cantidad e Importe Unitario
        //tipoTareaId = 3;
        if (tipoTareaId === TIPO_CANTIDAD_FIJA) {
            $('.filaImporteYCantidad').hide();
        } else {
            $('.filaImporteYCantidad').show();
        }
        // El campo Importe Total siempre se muestra (ya se llenó en ObtenerConceptoDetalle)
    }

    function validarFormulario() {
        let tareaEmpresa = $('#tareaEmpresa').val();
        let anioConcepto = $('#anioConcepto').val();
        let mesConcepto = $('#mesConcepto').val();
        let cantidad = $('#cantidad').val();

        let camposInvalidos = [];

        if (!tareaEmpresa || tareaEmpresa.length === 0) {
            $('.select2-selection__placeholder').css("color", "red");
            camposInvalidos.push("tareaEmpresa");
        } else {
            $('.select2-selection__placeholder').css("color", "black");
        }

        // Solo validamos cantidad si el campo está visible
        if ($('.filaImporteYCantidad').is(':visible')) {
            if (!cantidad) {
                $('#cantidad').addClass('is-invalid');
                camposInvalidos.push("cantidad");
            } else {
                $('#cantidad').removeClass('is-invalid');
            }
        }

        if (camposInvalidos.length > 0) {
            mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
            return false;
        }

        return true;
    }

    function guardarConcepto(estado) {
        if (!validarFormulario()) return;

        mostrarDivCargando();

        // Obtener valores del select tareaEmpresa
        let valores = $('#tareaEmpresa').val().split("|");
        let tareaId = valores[0];
        let empresaId = valores[1];
        let anyo = valores[2];

        // Para Cantidad Fija, fuerza cantidad = 1 e importe unitario = 0
        let cantidadEnviar = $('.filaImporteYCantidad').is(':visible')
            ? $('#cantidad').val()
            : 1;
        let importeUnitarioEnviar = $('.filaImporteYCantidad').is(':visible')
            ? $('#importeUnitario').val()
            : 0;

        let idConcepto = window.IdConcepto;
        let objConcepto = {
            TLE_Id: idConcepto,
            TLE_TAR_Id: tareaId,
            TLE_EMP_Id: empresaId,
            TLE_Anyo: anyo,
            TLE_Mes: $('#mesConcepto').val(),
            TLE_Cantidad: cantidadEnviar,
            TLE_Descripcion: $('#descripcion').val(),
            TLE_Inversion: $('#inversion').prop('checked'),
            TLE_ESO_Id: estado,
            // Incluimos, opcionalmente, el importUnitario en el objeto si la lógica de backend lo requiere
            // TLE_ImporteUnitario: importeUnitarioEnviar
        };

        $.ajax({
            url: AppConfig.urls.GuardarConcepto,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(objConcepto),
            success: function (response) {
                if (response.success) {
                    // Si el envío de correo falla, guardamos el mensaje en sessionStorage para mostrarlo después
                    if (!response.ok_email) {
                        sessionStorage.setItem("toastMensaje", response.mensaje_email);
                        sessionStorage.setItem("toastTipo", "error");
                        sessionStorage.setItem("errorEmail", true);
                    }
                    window.location.href = AppConfig.urls.BusquedaConceptos;
                } else {
                    registrarErrorjQuery("", response.message);
                }
            },
            error: function (xhr, status, error) {
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
                registrarErrorjQuery(status, error);
            }
        });
    }

    $('#btnGuardar').on('click', function (e) {
        e.preventDefault();
        guardarConcepto(EstadosSolicitud.SinSolicitar);
    });

    $('#btnSolicitar').on('click', function (e) {
        e.preventDefault();
        guardarConcepto(EstadosSolicitud.PendienteAprobacion);
    });

    $('#btnCancelar').on('click', function (e) {
        e.preventDefault();
        window.location.href = AppConfig.urls.BusquedaConceptos;
    });

    function ObtenerLicenciasMS(idConcepto) {
        const tabla = inicializarDataTable('#tableLicenciasMS', {
            ajax: {
                url: AppConfig.urls.ObtenerLicenciasMs,
                type: 'GET',
                data: { idConcepto: idConcepto },
                dataSrc: ''
            },
            paging: false,
            searching: false,
            info: false,
            ordering: false,
            dom: 't',
            columns: [
                { data: 'Licencia', title: 'Licencia' },
                { data: 'Entidad', title: 'Entidad' },
                {
                    data: 'Importe',
                    title: 'Importe',
                    className: 'text-end',
                    render: formatNumber
                }
            ]
        }, [], 'export_licencias_ms');

        tabla.on('xhr', function () {
            const datos = tabla.ajax.json() || [];
            if (datos.length > 0) {
                $('#divLicenciasMS').show();
                $('#btnExportarLicenciasMS').off('click').on('click', function () {
                    const url = AppConfig.urls.ExportarLicenciasMS + '?idConcepto=' + encodeURIComponent(idConcepto);
                    window.location.href = url;
                });
            } else {
                $('#divLicenciasMS').hide();
            }
        });
    }

    function ObtenerTicketsConcepto(idConcepto) {
        // inicializarDataTable es tu función utilitaria (ya usada para Licencias MS)
        const tablaTickets = inicializarDataTable('#tableTickets', {
            ajax: {
                url: AppConfig.urls.ObtenerTicketsConcepto,
                type: 'GET',
                data: { idConcepto: idConcepto },
                dataSrc: ''
            },
            paging: false,      // sin paginación
            searching: false,   // sin buscador
            info: false,        // sin texto “mostrando…”
            ordering: false,    // sin orden
            dom: 't',           // sólo la tabla
            columns: [
                { data: 'TicketId', title: 'Ticket GLPI' },
                { data: 'DescripcionTicket', title: 'Título ticket' },
                { data: 'Entidad', title: 'Entidad' },
                {  
                    data: 'TKC_Duracion',
                    title: 'Duración (min)',
                    className: 'text-end',
                    render: d => d ?? ''
                },
                {
                    data: 'Importe',
                    title: 'Importe',
                    className: 'text-end',
                    render: formatNumber
                },
                {
                    data: null,
                    title: 'Acción',
                    orderable: false,
                    className: 'text-center',
                    render: function (row) {
                        // guardamos el payload completo para el modal
                        const json = $('<div/>').text(JSON.stringify(row)).html(); // escape seguro
                        return `
                      <button type="button"
                        class="btn btn-sm btn-outline-secondary"
                        data-ticket='${json}'
                        onclick="verDetalleTicketConcepto(this)">
                        <i class="bi bi-eye-fill"></i>
                      </button>`;
                    }
                }
            ]
        }, [], 'export_tickets_concepto');

        // Tras la llamada AJAX, mostramos/ocultamos el contenedor según haya filas:
        tablaTickets.on('xhr', function () {
            const datos = tablaTickets.ajax.json() || [];
            if (datos.length > 0) {
                $('#divTickets').show();
                $('#btnExportarTickets').off('click').on('click', function () {
                    // descarga directa
                    const url = AppConfig.urls.ExportarTicketsConcepto + '?idConcepto=' + encodeURIComponent(idConcepto);
                    window.location.href = url;
                });
            } else {
                $('#divTickets').hide();
            }
        });
    }
    function ObtenerPartesConcepto(idConcepto) {
        const tablaPartes = inicializarDataTable('#tablePartes', {
            ajax: {
                url: AppConfig.urls.ObtenerPartesConcepto,
                type: 'GET',
                data: { idConcepto: idConcepto },
                dataSrc: ''
            },
            paging: false,
            searching: false,
            info: false,
            ordering: false,
            dom: 't',
            columns: [
                { data: 'PPA_PEP_Anyo', title: 'Año' },
                { data: 'PPA_PEP_Mes', title: 'Mes' },
                { data: 'ProyectoNombre', title: 'Proyecto' },
                { data: 'Actividad', title: 'Actividad' },
                { data: 'Persona', title: 'Persona' },
                {
                    data: 'TCP_Fecha',
                    title: 'Fecha',
                    render: function (data, type, row) {
                        if (!data) return '';

                        var m = moment(data);
                        if (type === 'sort' || type === 'type') {
                            return m.valueOf();
                        }

                        return m.format("DD/MM/YYYY");
                    }
                },
                {
                    data: 'TCP_Horas',
                    title: 'Horas',
                    className: 'text-end',
                    render: formatNumber
                }
            ]
        }, [], 'export_partes_concepto');

        tablaPartes.on('xhr', function () {
            const datos = tablaPartes.ajax.json() || [];
            if (datos.length > 0) {
                $('#divPartes').show();
            } else {
                $('#divPartes').hide();
            }
        });
    }
    function ObtenerAplicacionesConcepto(idConcepto) {
        const tabla = inicializarDataTable('#tableAplicaciones', {
            ajax: {
                url: AppConfig.urls.ObtenerAplicacionesPorConcepto,  // crea este endpoint
                type: 'GET',
                data: { idConcepto },
                dataSrc: ''
            },
            paging: false, searching: false, info: false, ordering: false, dom: 't',
            columns: [
                { data: 'NombreAplicacion', title: 'Aplicación' },
                { data: 'NombreEntidad', title: 'Entidad' },
                {
                    data: 'TCA_Importe',
                    title: 'Importe',
                    className: 'dt-type-numeric-with-decimal',
                    render: function (data) {
                        return formatMoney(data);
                    }
                }
            ]
        }, [], 'export_aplicaciones_concepto');

        tabla.on('xhr', function () {
            const datos = tabla.ajax.json() || [];
            if (datos.length) $('#divAplicaciones').show();
            else $('#divAplicaciones').hide();
        });
    }
    function ObtenerModulosConcepto(idConcepto) {
        const tabla = inicializarDataTable('#tableModulos', {
            ajax: {
                url: AppConfig.urls.ObtenerModulosPorConcepto,  // crea este endpoint
                type: 'GET',
                data: { idConcepto },
                dataSrc: ''
            },
            paging: false, searching: false, info: false, ordering: false, dom: 't',
            columns: [
                { data: 'NombreModulo', title: 'Módulo' },
                {
                    data: 'TCM_Importe',
                    title: 'Importe',
                    className: 'dt-type-numeric-with-decimal',
                    render: function (data) {
                        return formatMoney(data);
                    }
                }
            ]
        }, [], 'export_modulos_concepto');

        tabla.on('xhr', function () {
            const datos = tabla.ajax.json() || [];
            if (datos.length) $('#divModulos').show();
            else $('#divModulos').hide();
        });
    }
    function ObtenerModulosRepartoConcepto(idConcepto) {
        const tabla = inicializarDataTable('#tableModulosReparto', {
            ajax: {
                url: AppConfig.urls.ObtenerModulosRepartoPorConcepto,  // crea este endpoint
                type: 'GET',
                data: { idConcepto },
                dataSrc: ''
            },
            paging: false, searching: false, info: false, ordering: false, dom: 't',
            columns: [
                { data: 'NombreModulo', title: 'Módulo' },
                {
                    data: 'TCR_ImporteTotal',
                    title: 'Importe a repartir',
                    className: 'dt-type-numeric-with-decimal',
                    render: function (data) {
                        return formatMoney(data);
                    }
                },
                {
                    data: 'TCR_Porcentaje',
                    title: 'Porcentaje',
                    className: 'text-end',
                    render: d => formatPorcentaje(d)
                },

                {
                    data: 'TCR_Importe',
                    title: 'Importe',
                    className: 'dt-type-numeric-with-decimal',
                    render: function (data) {
                        return formatMoney(data);
                    }
                }
            ]
        }, [], 'export_modulos_reparto_concepto');

        tabla.on('xhr', function () {
            const datos = tabla.ajax.json() || [];
            if (datos.length) $('#divModulosReparto').show();
            else $('#divModulosReparto').hide();
        });
    }

    function ObtenerProveedoresAsuntosConcepto(idConcepto) {
        const tabla = inicializarDataTable('#tableProveedoresAsuntos', {
            ajax: {
                url: AppConfig.urls.ObtenerProveedoresAsuntosPorConcepto,
                type: 'GET',
                data: { idConcepto },
                dataSrc: ''
            },
            paging: false, searching: false, info: false, ordering: false, dom: 't',
            columns: [
                { data: 'NombreAsunto', title: 'Asunto' },
                {
                    data: 'NombreContrato',
                    title: 'Contrato Proveedor',
                    className: 'dt-body-right dt-head-right'  // ← aquí alineamos contenido y cabecera a la derecha
                },
                {
                    data: 'Entidad',
                    title: 'Entidad',
                    className: 'dt-body-right dt-head-right'  // ← aquí alineamos contenido y cabecera a la derecha
                },
                {
                    data: 'Departamento',
                    title: 'Departamento',
                    className: 'dt-body-right dt-head-right'  // ← aquí alineamos contenido y cabecera a la derecha
                },
                { data: 'TCA_Horas', title: 'Horas', className: 'dt-body-right dt-head-right', type: 'string' },
                {
                    data: 'TCA_Importe',
                    title: 'Importe',
                    className: 'dt-type-numeric-with-decimal',
                    render: function (data) {
                        return formatMoney(data);
                    }
                }
            ]
        }, [], 'export_prov_asuntos_concepto');

        // Tras la llamada AJAX, mostramos/ocultamos el contenedor según haya filas:
        tabla.on('xhr', function () {
            const datos = tabla.ajax.json() || [];
            if (datos.length > 0) {
                $('#divProveedoresAsuntos').show();
                $('#btnExportarAsuntos').off('click').on('click', function () {
                    // descarga directa
                    const url = AppConfig.urls.ExportarAsuntosConcepto + '?idConcepto=' + encodeURIComponent(idConcepto);
                    window.location.href = url;
                });
            } else {
                $('#divProveedoresAsuntos').hide();
            }
        });
    }

    function ObtenerLicenciasAnualesConcepto(idConcepto) {
        const tabla = inicializarDataTable('#tableLicenciasAnuales', {
            ajax: {
                url: AppConfig.urls.ObtenerLicenciasAnualesPorConcepto,
                type: 'GET',
                data: { idConcepto },
                dataSrc: ''
            },
            paging: false, searching: false, info: false, ordering: false, dom: 't',
            columns: [
                { data: 'NombreLicenciaAnual', title: 'Licencia Anual' },
                { data: 'NombreEntidad', title: 'Entidad' },
                {
                    data: 'TCL_Importe',
                    title: 'Importe',
                    className: 'dt-type-numeric-with-decimal',
                    render: function (data) {
                        return formatMoney(data);
                    }
                }
            ]
        }, [], 'export_lic_anuales_concepto');

        tabla.on('xhr', function () {
            const datos = tabla.ajax.json() || [];
            if (datos.length) $('#divLicenciasAnuales').show();
            else $('#divLicenciasAnuales').hide();
        });
    }
});

window.verDetalleTicketConcepto = function (btn) {
    const data = JSON.parse(btn.getAttribute('data-ticket'));

    // formateos básicos
    const fmtFecha = (f) => f ? moment(f).format('DD/MM/YYYY') : '';
    const n = (v) => (v ?? '') + '';

    $('#v_TKC_Id_GLPI').text(n(data.TKC_Id_GLPI));
    $('#v_TKC_Titulo').text(n(data.TKC_Titulo));
    $('#v_TKC_GrupoAsignado').text(n(data.TKC_GrupoAsignado));
    $('#v_TKC_Categoria').text(n(data.TKC_Categoria));
    $('#v_TKC_CTK_Id').text(n(data.TKC_CTK_Id));
    $('#v_TKC_Ubicacion').text(n(data.TKC_Ubicacion));
    $('#v_TKC_Duracion').text(n(data.TKC_Duracion));
    $('#v_TKC_Descripcion').text(n(data.TKC_Descripcion));
    $('#v_TKC_ETK_Id').text(n(data.TKC_ETK_Id));
    $('#v_TKC_TTK_Id').text(n(data.TKC_TTK_Id));
    $('#v_TKC_OTK_Id').text(n(data.TKC_OTK_Id));
    $('#v_TKC_VTK_Id').text(n(data.TKC_VTK_Id));
    $('#v_TKC_ENT_Id_Solicitante').text(n(data.TKC_ENT_Id_Solicitante));
    $('#v_TKC_ProveedorAsignado').text(n(data.TKC_ProveedorAsignado));
    $('#v_TKC_GrupoCargo').text(n(data.TKC_GrupoCargo));
    $('#v_TKC_FechaApertura').text(fmtFecha(data.TKC_FechaApertura));
    $('#v_TKC_FechaResolucion').text(fmtFecha(data.TKC_FechaResolucion));

    $('#modalTicketDetalle').modal('show');
};