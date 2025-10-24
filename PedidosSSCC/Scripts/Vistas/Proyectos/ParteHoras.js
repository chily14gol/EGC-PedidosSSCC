$(document).ready(async function () {
    await VerificarSesionActiva(OpcionMenu.ParteHoras);
    initFilters();
    bindEvents();
    await loadInitialData();
    initUserDropdown();
    ocultarDivCargando();
});

// Variables globales para mantener datos
let globalPeriodos = [];    // { PEP_Anyo, PEP_Mes, PEP_FechaInicio, PEP_FechaFin }
let globalUsers = [];       // { PER_Id, Nombre }
let globalProjects = [];    // { PRY_Id, PRY_Nombre }
let globalPartes = [];      // { PPA_Id, PPA_PEP_Anyo, PPA_PEP_Mes, PPA_PRY_Id, PPA_PER_Id, PPA_Descripcion, PPA_Validado }

function initFilters() {
    const yearSel = $('#ddlYear');
    const now = new Date();

    for (let y = now.getFullYear() - 1; y <= now.getFullYear() + 1; y++) {
        yearSel.append($('<option>').val(y).text(y));
    }

    yearSel.val(now.getFullYear());

    yearSel.change(async () => {
        await loadPeriodos();
        await loadUsersDept();
        await loadPartes();
    });

    $('#ddlPeriodo').change(async function () {
        await loadUsersDept();
        await loadPartes();
    });

    $('#ddlUser').change(async function () {
        //const sel = $(this);
        //const selId = +sel.val() || null;
        //const selDept = sel.find('option:selected').data('dep');

        //// Si el usuario seleccionado es el mismo que el conectado
        //// y su depto NO está en responsableDeps, ocultamos Validar
        //if (selId === window.currentUserId && !window.responsableDeps.includes(selDept)) {
        //    $('#btnValidate').hide();
        //} else {
        //    $('#btnValidate').show();
        //}

        mostrarDivCargando();
        // primero refrescamos proyectos para que globalProjects esté actualizado
        await loadProjectsDept();
        // ahora ya podemos recargar la tabla y pintar bien el nombre de proyecto
        await loadPartes();
        ocultarDivCargando();
    });
}

function bindEvents() {
    $('#btnAddLinea').click(() => {
        $('#PPA_Id').val(0);
        $('#PPA_PRY_Id').val('');
        $('#PPA_Descripcion').val('');
        $('#modalLineaParteLabel').text('Nueva Línea de Parte');
        $('#modalLineaParte').modal('show');
    });

    $('#formLineaParte').submit(async function (e) {
        e.preventDefault();
        await saveParteLinea();
    });

    $(document).on('click', '.btnEditParte', async function () {
        const tr = $(this).closest('tr');
        const parteId = tr.data('ppa_id');
        const linea = globalPartes.find(x => x.PPA_Id === parteId);
        if (!linea) return;
        if (linea.PPA_Validado) return;
        $('#PPA_Id').val(linea.PPA_Id);
        $('#PPA_PRY_Id').val(linea.PPA_PRY_Id);
        $('#PPA_Descripcion').val(linea.PPA_Descripcion);
        $('#modalLineaParteLabel').text('Editar Línea de Parte');
        $('#modalLineaParte').modal('show');
    });

    $(document).on('click', '.btnDeleteParte', function () {
        const id = $(this).data('id');
        if (!confirm('¿Eliminar esta línea de parte?')) return;
        $.post(window.AppConfig.urls.deleteParte, { PPA_Id: id }, async resp => {
            if (resp.success) {
                await loadPartes();
            } else {
                alert(resp.message || 'No se pudo eliminar la línea de parte.');
            }
        });
    });

    // – Focus: si está vacío, ponemos “00:00” + marcamos data-autoMidnight
    // – Blur: si data-autoMidnight y sigue “00:00”, lo ponemos vacío
    // – Change: quitamos data-autoMidnight, convertimos a decimal y guardamos
    $(document).on('focus', '.hour-input:not(.weekend):not(.validated)', function () {
        const $inp = $(this);
        if (!$inp.val()) {
            $inp.data('autoMidnight', true);
            $inp.val('00:00');
        }
    });

    $(document).on('blur', '.hour-input:not(.weekend):not(.validated)', function () {
        const $inp = $(this);
        if ($inp.data('autoMidnight') && $inp.val() === '00:00') {
            $inp.val('');
            $inp.removeData('autoMidnight');
        }
    });

    $(document).on('change', '.hour-input:not(.weekend):not(.validated)', function () {
        const $inp = $(this);
        if ($inp.data('autoMidnight')) {
            $inp.removeData('autoMidnight');
        }
        const timeVal = $inp.val(); // formato “HH:MM”
        const day = $inp.data('date'); // “YYYY-MM-DD”
        const parteId = $inp.closest('tr').data('ppa_id');

        let horasDec = 0;
        if (timeVal) {
            const [hh, mm] = timeVal.split(':').map(x => parseInt(x, 10));
            horasDec = hh + mm / 60.0;
        }

        $.post(window.AppConfig.urls.saveHoraParte, {
            PPA_Id: parteId,
            Fecha: day,
            Horas: horasDec.toFixed(2).toString().replace('.', ',')
        }, resp => {
            if (!resp.success) {
                alert('Error guardando horas para ' + day);
                $inp.val('');
            } else {
                $inp.addClass('bg-success text-white');
                setTimeout(() => {
                    $inp.removeClass('bg-success text-white');
                    calcularTotales();
                }, 600);
            }
        });
    });

    $('#btnValidate').click(function () {
        $('#modalConfirmValidate').modal('show');
    });

    // Validar todas
    $('#btnConfirmValidate').click(async function () {
        $('#modalConfirmValidate').modal('hide');
        const $btn = $('#btnValidate').prop('disabled', true).text('Validando…');
        const ids = globalPartes.map(x => x.PPA_Id);
        await Promise.all(ids.map(id =>
            $.post(window.AppConfig.urls.validateParte, { PPA_Id: id })
        ));
        await loadPartes();
        $btn.prop('disabled', false);
        $('#btnValidate').text('Anular Validaciones');
    });

    // Anular todas
    $('#btnConfirmUnvalidate').click(async function () {
        $('#modalConfirmValidate').modal('hide');
        const $btn = $('#btnValidate').prop('disabled', true).text('Anulando…');
        const ids = globalPartes.map(x => x.PPA_Id);
        await Promise.all(ids.map(id =>
            $.post(window.AppConfig.urls.unvalidateParte, { PPA_Id: id })
        ));
        await loadPartes();
        $btn.prop('disabled', false);
        $('#btnValidate').text('Validar');
    });

    $(document).on('click', '.btnValidateParteIndividual', async function () {
        if (!confirm('¿Confirmas que deseas validar esta línea de parte?')) return;
        const id = $(this).data('id');
        await $.post(window.AppConfig.urls.validateParte, { PPA_Id: id });
        await loadPartes();
    });

    $(document).on('click', '.btnUnvalidateParteIndividual', async function () {
        if (!confirm('¿Confirmas que deseas anular la validación de esta línea?')) return;
        const id = $(this).data('id');
        await $.post(window.AppConfig.urls.unvalidateParte, { PPA_Id: id });
        await loadPartes();
    });

    $('#btnPrevisualizarConceptosHoras').click(async () => {
        // 1) Leemos año y mes seleccionados
        const anio = +$('#ddlYear').val();
        const mes = +$('#ddlPeriodo').val();

        if (!anio || !mes) {
            await Swal.fire({
                title: "Seleccione Año y Periodo",
                html: `<p>Debe elegir un año y un periodo válidos antes de previsualizar.</p>`,
                icon: "warning",
                confirmButtonText: "Entendido"
            });
            return;
        }

        // 2) Llamamos a PrevisualizarConceptosHoras por AJAX
        mostrarDivCargando();
        try {
            // Si soy responsable, procesamos todos los usuarios (userId=null)
            const selUser = +($('#ddlUser').val() || 0);
            const userId = (window.isResponsable ? null : (selUser || null));

            const response = await $.ajax({
                url: window.AppConfig.urls.previsualizarConceptosHoras,
                type: 'POST',
                dataType: 'json',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify({ anio: anio, mes: mes, userId: userId })
            });
            ocultarDivCargando();

            if (!response.success) {
                // mostramos el error que vino del servidor y salimos
                await Swal.fire({
                    icon: 'error',
                    title: 'Error al previsualizar',
                    html: `<div class="text-start">${response.mensaje}</div>`,
                    confirmButtonText: 'Entendido'
                });
                return;
            }

            // 3) Extraemos preview y posibles errores
            const { partesPreview, partesErrors } = response;

            // 4) Si no hay preview ni errores, avisamos y salimos
            if ((!partesPreview || partesPreview.length === 0) && (!partesErrors || partesErrors.length)) {
                await Swal.fire({
                    title: "Nada que Previsualizar",
                    html: `<p>No hay conceptos de horas pendientes.</p>`,
                    icon: "info",
                    confirmButtonText: "Entendido",
                    width: 500
                });
                return;
            }

            // 5) Construimos el HTML agrupado por usuario
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
                htmlPartes = `<p>No hay horas validadas para previsualizar.</p>`;
            } else {
                htmlPartes += generarResumenPorConcepto(partesPreview);
            }

            // 6) Mostramos un Swal con confirmación “Generar”
            const { isConfirmed } = await Swal.fire({
                title: "Previsualizar Conceptos Horas",
                html: `<div class="small text-start">
                         <h5>Partes de Horas</h5>
                         ${htmlPartes}
                       </div>`,
                width: '80%',
                showCancelButton: true,
                cancelButtonText: "Cerrar",
                confirmButtonText: "Generar Conceptos",
                icon: "info"
            });

            // 7) Si el usuario pulsa “Generar Conceptos”, llamamos al endpoint de generar
            if (isConfirmed) {
                await generarConceptosHoras(anio, mes, userId);
            }

        } catch (err) {
            ocultarDivCargando();
            registrarErrorjQuery(err.status, err.message);
        }
    });
}

async function loadInitialData() {
    await loadPeriodos();
    await loadUsersDept();
    await loadProjectsDept();
    await loadPartes();
}

async function loadPeriodos() {
    const year = +$('#ddlYear').val();
    return new Promise(resolve => {
        $.get(window.AppConfig.urls.getPeriodos, { anyo: year }, data => {
            globalPeriodos = data;
            const sel = $('#ddlPeriodo').empty();
            sel.append($('<option>').val('').text('-- Seleccionar --'));
            globalPeriodos.forEach(p => {
                const mesNum = p.PEP_Mes;
                const nombreMes = new Date(p.PEP_Anyo, mesNum - 1)
                    .toLocaleString('default', { month: 'long' });
                const nombreMesCap = nombreMes.charAt(0).toUpperCase() + nombreMes.slice(1);

                const fiDate = new Date(p.PEP_FechaInicio);
                const diaIni = fiDate.getDate().toString().padStart(2, '0');
                const mesIni = (fiDate.getMonth() + 1).toString().padStart(2, '0');
                const anyoIni = fiDate.getFullYear();
                const fechaIni = `${diaIni}/${mesIni}/${anyoIni}`;

                const ffDate = new Date(p.PEP_FechaFin);
                const diaFin = ffDate.getDate().toString().padStart(2, '0');
                const mesFin = (ffDate.getMonth() + 1).toString().padStart(2, '0');
                const anyoFin = ffDate.getFullYear();
                const fechaFin = `${diaFin}/${mesFin}/${anyoFin}`;

                const texto = `${nombreMesCap} (${fechaIni} → ${fechaFin})`;
                sel.append($('<option>').val(p.PEP_Mes).text(texto));
            });

            if (globalPeriodos.length > 0) {
                const ultimo = globalPeriodos[globalPeriodos.length - 1].PEP_Mes;
                sel.val(ultimo);
            }
            resolve();
        });
    });
}

async function loadProjectsDept() {
    const userId = +($('#ddlUser').val() || 0);

    return new Promise(resolve => {
        $.get(window.AppConfig.urls.getProjectsDept, { userId: userId || null }, data => {
            globalProjects = data;
            const sel = $('#PPA_PRY_Id').empty();
            sel.append($('<option>').val('').text('-- Seleccionar --'));
            globalProjects.forEach(p => {
                sel.append($('<option>').val(p.PRY_Id).text(p.PRY_Nombre));
            });
            resolve();
        });
    });
}

async function loadPartes() {
    const year = +$('#ddlYear').val();
    const mes = +$('#ddlPeriodo').val();
    const userId = +($('#ddlUser').val() || 0);

    if (!mes) {
        $('#tblParte thead tr').empty();
        $('#tblParte tbody').empty();
        return;
    }

    return new Promise(resolve => {
        $.get(window.AppConfig.urls.getPartes, { anyo: year, mes: mes, userId: userId || null }, async data => {
            globalPartes = data;
            await renderTable(globalPeriodos.find(x => x.PEP_Mes === mes));
            toggleValidateAndAddButtons();
            resolve();
        });
    });
}

function toggleValidateAndAddButtons() {
    const todasValidadas = globalPartes.length > 0 && globalPartes.every(p => p.PPA_Validado);
    const algunaConConcepto = globalPartes.some(p => p.ConceptoGenerado);

    // oculto “añadir” si todo ya está validado
    $('#btnAddLinea').toggle(!todasValidadas);

    const $btnVal = $('#btnValidate');

    const sel = $('#ddlUser');
    const selId = +sel.val() || null;
    const selDept = sel.find('option:selected').data('dep');

    if (selId === window.currentUserId && !window.responsableDeps.includes(selDept)) {
        $btnVal.hide();
    } else {
        if (todasValidadas) {
            // si **alguna** parte ya tiene el concepto, deshabilito el botón “Anular Validaciones”
            $btnVal.prop('disabled', algunaConConcepto);
            $btnVal.text('Anular Validaciones')
                .off('click')
                .on('click', () => {
                    // preparamos el modal en modo “anular”
                    $('#modalConfirmTexto').text('¿Anular validación de todas las líneas?');
                    $('#btnConfirmValidate').addClass('d-none');
                    $('#btnConfirmUnvalidate').removeClass('d-none');
                    $('#modalConfirmValidate').modal('show');
                })
                .show();
        }
        else {
            $btnVal.prop('disabled', false);
            $btnVal.text('Validar')
                .off('click')
                .on('click', () => {
                    $('#modalConfirmTexto').text('¿Validar todas las líneas?');
                    $('#btnConfirmUnvalidate').addClass('d-none');
                    $('#btnConfirmValidate').removeClass('d-none');
                    $('#modalConfirmValidate').modal('show');
                })
                .show();
        }
    }
}


async function renderTable(periodo) {
    if (!periodo) return;

    const fi = new Date(periodo.PEP_FechaInicio);
    const ff = new Date(periodo.PEP_FechaFin);
    const dates = [];

    for (let d = new Date(fi); d <= ff; d.setDate(d.getDate() + 1)) {
        dates.push(new Date(d));
    }

    const $tbl = $('#tblParte');
    const $thead = $tbl.find('thead tr').empty();
    const $tbody = $tbl.find('tbody').empty();

    // 1) Cabeceras
    $thead.append('<th style="min-width:50px; width:50px;"></th>'); // Acciones (opcional)
    $thead.append('<th style="min-width:200px; max-width:200px;">Proyecto</th>');
    $thead.append('<th style="min-width:150px; max-width:150px; padding-left:35px!important;">Actividad</th>');


    dates.forEach(dt => {
        const dow = dt.getDay();
        const letra = ['D', 'L', 'M', 'X', 'J', 'V', 'S'][dow];
        const dia = dt.getDate();
        const cls = (dow === 0 || dow === 6) ? 'weekend' : '';
        $thead.append(`<th class="${cls} text-center" style="min-width:60px;">${letra}–${dia}</th>`);
    });
    $thead.append('<th class="text-center" style="min-width:75px;">Total</th>');

    // 2) Filas
    const promesasHoras = [];
    globalPartes.forEach(parte => {
        const $tr = $('<tr>').attr('data-ppa_id', parte.PPA_Id);

        // 2.1) ACCIONES
        let btns;
        const userDept = parte.UserDeptId;
        const esResponsable = window.responsableDeps.includes(userDept);

        const hasConcept = parte.ConceptoGenerado;  // <–– este campo nuevo
        const isValidated = parte.PPA_Validado;

        // En lugar del icono estático:
        if (isValidated) {
            if (!hasConcept && esResponsable) {
                btns = `
                  <button class="btn-action btn btn-outline-secondary btnUnvalidateParteIndividual"
                          data-id="${parte.PPA_Id}" title="Anular Validación">
                    <i class="bi bi-x-circle"></i>
                  </button>`;
            } else {
                btns = `<i class="bi bi-check-circle-fill text-success"></i>`;
            }
        }
        else {
            if (esResponsable) {
                btns = `
                  <button class="btn-action btn btn-outline-success btnValidateParteIndividual"
                          data-id="${parte.PPA_Id}" title="Validar">
                    <i class="bi bi-check-circle"></i>
                  </button>
                  <button class="btn-action btn btn-outline-primary btnEditParte"
                          data-id="${parte.PPA_Id}" title="Editar">
                    <i class="bi bi-pencil"></i>
                  </button>
                  <button class="btn-action btn btn-outline-danger btnDeleteParte"
                          data-id="${parte.PPA_Id}" title="Eliminar">
                    <i class="bi bi-trash"></i>
                  </button>`;
            } else {
                btns = `
                  <button class="btn-action btn btn-outline-primary btnEditParte"
                          data-id="${parte.PPA_Id}" title="Editar">
                    <i class="bi bi-pencil"></i>
                  </button>
                  <button class="btn-action btn btn-outline-danger btnDeleteParte"
                          data-id="${parte.PPA_Id}" title="Eliminar">
                    <i class="bi bi-trash"></i>
                  </button>`;
            }
        }

        $tr.append(`<td class="p-1 text-center">${btns}</td>`);

        // 2.2) PROYECTO
        const projObj = globalProjects.find(x => x.PRY_Id === parte.PPA_PRY_Id) || {};
        const projNombre = projObj.PRY_Nombre || '';
        $tr.append(`<td class="proj-name align-middle p-1">${projNombre}</td>`);

        // 2.3) ACTIVIDAD
        $tr.append(`<td class="align-middle p-1" 
                 style="padding-left:35px!important;" 
                 data-bs-toggle="tooltip" 
                 data-bs-placement="top" 
                 title="${parte.PPA_Descripcion}">
                 ${parte.PPA_Descripcion}
            </td>`);

        // 2.4) Día a día
        dates.forEach(dt => {
            const iso = dt.toISOString().substring(0, 10);
            const dow = dt.getDay();
            const isWeekend = (dow === 0 || dow === 6);
            let inpCls = isWeekend ? 'hour-input weekend' : 'hour-input';
            let disabled = isWeekend ? 'disabled' : '';

            if (parte.PPA_Validado) {
                inpCls = 'hour-input validated';
                disabled = 'disabled';
            }

            $tr.append(`
                <td class="${isWeekend ? 'weekend' : ''} p-1 text-center">
                    <input type="time"
                        class="form-control form-control-sm ${inpCls}"
                        data-date="${iso}"
                        value=""
                        ${disabled} />
                </td>`
            );
        });

        $tr.append(`<td class="row-total align-middle text-center">00:00</td>`);
        $tbody.append($tr);

        // 2.5) Cargar horas existentes
        promesasHoras.push(loadHorasPorParte(parte.PPA_Id, dates, parte.PPA_Validado));
    });

    await Promise.all(promesasHoras);
    calcularTotales();
}

function loadHorasPorParte(PPA_Id, dates, isValidated) {
    return new Promise(resolve => {
        $.get(window.AppConfig.urls.getHorasParte, { PPA_Id: PPA_Id }, data => {
            data.forEach(h => {
                const hNum = parseFloat(h.PPH_Horas);
                const hh = Math.floor(hNum);
                const mm = Math.round((hNum - hh) * 60);
                const hhStr = hh.toString().padStart(2, '0');
                const mmStr = mm.toString().padStart(2, '0');
                const timeVal = `${hhStr}:${mmStr}`;
                const selector = `tr[data-ppa_id="${PPA_Id}"] input[data-date="${h.Fecha}"]`;
                const $inp = $(selector);
                $inp.val(timeVal);
                if (isValidated) {
                    $inp.addClass('validated');
                }
            });
            resolve();
        });
    });
}

async function saveParteLinea() {
    const modelo = {
        PPA_Id: parseInt($('#PPA_Id').val(), 10),
        PPA_PEP_Anyo: +$('#ddlYear').val(),
        PPA_PEP_Mes: +$('#ddlPeriodo').val(),
        PPA_PRY_Id: +$('#PPA_PRY_Id').val(),
        PPA_PER_Id: +$('#ddlUser').val(),
        PPA_Descripcion: $('#PPA_Descripcion').val().trim(),
        PPA_Validado: false
    };

    if (!modelo.PPA_PRY_Id || !modelo.PPA_Descripcion) {
        alert('Debe indicar proyecto y descripción.');
        return;
    }

    $.post(window.AppConfig.urls.saveParte, modelo, async resp => {
        if (resp.success) {
            $('#modalLineaParte').modal('hide');
            await loadPartes();
        } else {
            alert(resp.message || 'Error al guardar la línea de parte.');
        }
    });
}

function calcularTotales() {
    const $tbl = $('#tblParte');

    // 1) Total por fila
    $tbl.find('tbody tr').each(function () {
        let totalMinFila = 0;
        $(this).find('input.hour-input').each(function () {
            const val = $(this).val();
            if (val) {
                const [hh, mm] = val.split(':').map(x => parseInt(x, 10));
                totalMinFila += (hh * 60) + mm;
            }
        });
        const horas = Math.floor(totalMinFila / 60);
        const mins = totalMinFila % 60;
        const hhStr = horas.toString().padStart(2, '0');
        const mmStr = mins.toString().padStart(2, '0');
        $(this).find('td.row-total').html(`<strong>${hhStr}:${mmStr}</strong>`);
    });

    // 2) Totales por columna (en un <tfoot>)
    $tbl.find('tfoot').remove();
    const $tfoot = $('<tfoot></tfoot>');
    const $trFoot = $('<tr></tr>');

    // 2.1) Primera celda (colspan=3) con “Totales:”
    $trFoot.append(`
        <td colspan="3" class="text-end pe-2">
            <strong>Totales:</strong>
        </td>`);

    const totalTh = $tbl.find('thead th').length;
    const dayCount = totalTh - 4; // 3 fijas + 1 “Total” = 4
    let grandTotalMin = 0;

    for (let i = 0; i < dayCount; i++) {
        let totalMinCol = 0;
        $tbl.find('tbody tr').each(function () {
            const $inp = $(this).find('td').eq(3 + i).find('input.hour-input');
            if ($inp.length && $inp.val()) {
                const [hh, mm] = $inp.val().split(':').map(x => parseInt(x, 10));
                totalMinCol += (hh * 60) + mm;
            }
        });
        grandTotalMin += totalMinCol;
        const horas = Math.floor(totalMinCol / 60);
        const mins = totalMinCol % 60;
        const hhStr = horas.toString().padStart(2, '0');
        const mmStr = mins.toString().padStart(2, '0');
        $trFoot.append(`<td class="text-center"><strong>${hhStr}:${mmStr}</strong></td>`);
    }

    const horasG = Math.floor(grandTotalMin / 60);
    const minsG = grandTotalMin % 60;
    const hhG = horasG.toString().padStart(2, '0');
    const mmG = minsG.toString().padStart(2, '0');
    $trFoot.append(`<td class="text-center"><strong>${hhG}:${mmG}</strong></td>`);

    $tfoot.append($trFoot);
    $tbl.append($tfoot);
}

async function generarConceptosHoras(anio, mes, userId) {
    mostrarDivCargando();
    try {
        const response = await $.ajax({
            url: window.AppConfig.urls.generarConceptosHoras,
            type: 'POST',
            dataType: 'json',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ anio: anio, mes: mes, userId: userId })
        });
        ocultarDivCargando();

        if (response.errors && response.errors.length) {
            await Swal.fire({
                icon: 'error',
                title: 'Errores generando conceptos horas',
                html: `<ul style="text-align:left">
                         ${response.errors.map(e => `<li>${e}</li>`).join("")}
                       </ul>`,
                confirmButtonText: 'Entendido'
            });
            return;
        }

        if (!response.success) {
            mostrarErrorPedidos();
            return;
        }

        const htmlFin = `
          <div style="text-align:left">
            <h5>Conceptos de Partes de Horas</h5>
            ${generarResumenConceptosPartesFin(response.resultado)}
          </div>`;
        await Swal.fire({
            title: "Proceso Completado",
            html: htmlFin,
            icon: "success"
        });
    } catch (err) {
        ocultarDivCargando();
        registrarErrorjQuery(err.status, err.message);
    }
}

function generarResumenPorConcepto(previewWithDetail) {
    // Agrupar por tarea-empresa-departamento
    const grupos = {};
    previewWithDetail.forEach(conc => {
        const key = `${conc.TAR_Nombre}|${conc.EmpresaNombre}|${conc.DepartamentoNombre}`;
        if (!grupos[key]) {
            grupos[key] = {
                TAR_Nombre: conc.TAR_Nombre,
                EmpresaNombre: conc.EmpresaNombre,
                DepartamentoNombre: conc.DepartamentoNombre,
                TarifaHora: conc.TarifaHora,
                PorcentajeEmpresa: conc.PorcentajeEmpresa,
                Detalles: []
            };
        }
        conc.Detalles.forEach(d => grupos[key].Detalles.push(d));
    });

    let html = '';
    Object.values(grupos).forEach(grp => {
        html += `
        <div class="card mb-4 shadow-sm">
          <div class="card-header bg-primary text-white d-flex justify-content-between align-items-center" style="background-color: #0092ff !important">
            <span><strong>${grp.TAR_Nombre} - ${grp.EmpresaNombre} — ${grp.DepartamentoNombre}</strong></span>
          </div>
          <div class="card-body p-2">
            <table class="table table-striped table-hover mb-0">
              <thead class="table-light">
                <tr>
                  <th>Persona</th>
                  <th>Proyecto</th>
                  <th>Actividad</th>
                  <th class="text-end">Horas</th>
                  <th class="text-end">Tarifa Tarea</th>
                  <th class="text-end">% Empresa</th>
                  <th class="text-end">Importe</th>
                </tr>
              </thead>
              <tbody>`;

        grp.Detalles.forEach(det => {
            html += `
                <tr>
                  <td>${det.NombreEmpleado}</td>
                  <td>${det.ProyectoNombre}</td>
                  <td>${det.Actividad}</td>
                  <td class="text-end fw-bold">${det.Horas.toFixed(2)}</td>
                  <td class="text-end text-muted">${formatMoney(grp.TarifaHora)}</td>
                  <td class="text-end"><span class="badge bg-secondary">${grp.PorcentajeEmpresa}%</span></td>
                  <td class="text-end text-success fw-semibold">${formatMoney(det.ImporteParte)}</td>
                </tr>`;
        });

        html += `
              </tbody>
            </table>
          </div>
        </div>`;
    });
    return html;
}

function formatMoney(valor) {
    // Ajusta según tu cultura. Aquí, ejemplo para es-ES:
    return new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(valor);
}

function generarResumenConceptosPartesFin(resultado) {
    if (!resultado || !resultado.length) {
        return "<ul><li><strong>No se ha generado ningún concepto de horas</strong></li></ul>";
    }
    let mensaje = "<ul style='text-align: left;'>";
    resultado.forEach(e => {
        mensaje += `<li><strong>${e.EmpresaNombre}</strong>: ${e.ConceptosCreados} concepto(s) generados</li>`;
    });
    mensaje += "</ul>";
    return mensaje;
}

// Formatea cada opción: si el id coincide con el responsable, pone un icono distinto
function formatUserOption(option) {
    if (!option.id) return option.text;  // placeholder
    const id = parseInt(option.id, 10);
    const isResp = (id === window.currentUserId);
    const iconHtml = isResp
        ? '<i class="bi bi-person-badge-fill text-primary me-1"></i>'
        : '<i class="bi bi-person-fill me-1"></i>';
    return `${iconHtml}${option.text}`;
}

function initUserDropdown() {
    $('#ddlUser').select2({
        placeholder: "-- Todos --",
        width: '300px',            // ajústalo a tu gusto
        templateResult: formatUserOption,
        templateSelection: formatUserOption,
        escapeMarkup: m => m      // necesario para que acepte el HTML del icono
    });
}

// Llamar a initUserDropdown tras crear/recargar las opciones
async function loadUsersDept() {
    if ($('#ddlUser').length === 0)
        return;
    const year = +$('#ddlYear').val();
    const mes = +$('#ddlPeriodo').val();

    if (!mes) {
        $('#ddlUser').empty().append($('<option>').val('').text('-- Todos --'));
        $('#ddlUser').trigger('change');  // refresca Select2
        return;
    }

    const data = await $.get(window.AppConfig.urls.getUsuariosDept, { anyo: year, mes: mes });
    globalUsers = data;
    const $sel = $('#ddlUser').empty().append($('<option>').val('').text('-- Todos --'));

    globalUsers.forEach(u => {
        $sel.append($('<option>')
            .val(u.PER_Id)
            .text(u.Nombre)
            .data('dep', u.DepartamentoId)  // guardamos el depto en data-attr
        );
    });

    // selecciona el usuario actual por defecto
    if (window.currentUserId) {
        $sel.val(window.currentUserId.toString());
    }
    $sel.trigger('change');            // refresca Select2
}
