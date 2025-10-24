$(async function () {
    try {
        await VerificarSesionActiva(OpcionMenu.Tareas);

        // Definimos los combos que queremos cargar.
        const combos = [
            { selector: '#seccion', url: AppConfig.urls.ObtenerComboSecciones, placeholder: 'Seleccione Sección', value: 'SEC_Id', text: 'SEC_Nombre' },
            { selector: '#tipo', url: AppConfig.urls.CargarComboTiposTarea, placeholder: 'Seleccione Tipo', value: 'TTA_Id', text: 'TTA_Nombre' },
            { selector: '#unidad', url: AppConfig.urls.ObtenerComboUnidades, placeholder: 'Seleccione Unidad', value: 'UTI_UTA_Id', text: 'UTI_Nombre' },
            { selector: '#producto', url: AppConfig.urls.ObtenerComboProductosD365, placeholder: 'Seleccione Producto', value: 'PR3_Id', text: 'PR3_Nombre' },
            { selector: '#itemNumber', url: AppConfig.urls.ObtenerComboItemNumber, placeholder: 'Seleccione Item Number', value: 'IN3_Id', text: 'IN3_Nombre' }
        ];

        // Cargamos todos los combos en paralelo
        await Promise.all(combos.map(c => cargarCombo(c)));
        ocultarDivCargando();
    }
    catch (err) {
        console.error('Error al cargar los datos:', err);
    }

    // Función genérica para cargar un combo
    async function cargarCombo({ selector, url, placeholder, value, text }) {
        const $sel = $(selector);
        try {
            const data = await $.ajax({ url, type: 'GET', dataType: 'json' });
            $sel.empty().append(`<option value="">${placeholder}</option>`);
            for (const item of data) {
                $sel.append(`<option value="${item[value]}">${item[text]}</option>`);
            }
        }
        catch (xhr) {
            const msg = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, msg);
            throw msg;  // para que Promise.all detecte el fallo
        }
    }

    // Validación de formulario
    function validarFormulario() {
        const campos = [
            { sel: '#nombre', label: 'Nombre' },
            { sel: '#seccion', label: 'Sección' },
            { sel: '#tipo', label: 'Tipo' },
            { sel: '#iva', label: 'IVA' },
            { sel: '#producto', label: 'Producto' },
            { sel: '#itemNumber', label: 'Item Number' }
        ];
        const invalid = [];

        for (const { sel, label } of campos) {
            if (!$(sel).val()) {
                $(sel).addClass('is-invalid');
                invalid.push(label);
            } else {
                $(sel).removeClass('is-invalid');
            }
        }

        // campos condicionales
        if ($('.campo-importeUnitario').is(':visible')) {
            if (!$('#importeUnitario').val()) {
                $('#importeUnitario').addClass('is-invalid');
                invalid.push('Importe Unitario');
            } else {
                $('#importeUnitario').removeClass('is-invalid');
            }
        }

        if ($('.campo-unidad').is(':visible')) {
            if (!$('#unidad').val()) {
                $('#unidad').addClass('is-invalid');
                invalid.push('Unidad');
            } else {
                $('#unidad').removeClass('is-invalid');
            }
        }

        if (invalid.length) {
            mostrarToast(AppConfig.mensajes.camposObligatorios + invalid.join(', '), TipoToast.Warning);
            return false;
        }
        return true;
    }

    // Mostrar/ocultar campos según tipo
    $('#tipo').on('change', () => {
        const v = $('#tipo').val();
        const porHoras = v === Tipo.POR_HORAS;
        const porUnidades = v === Tipo.POR_UNIDADES;

        $('.campo-importeUnitario').toggle(porHoras || porUnidades);
        $('.campo-unidad').toggle(porUnidades);
    });

    // Guardar
    $('#btnGuardar').on('click', async e => {
        e.preventDefault();
        if (!validarFormulario()) return;

        const objTarea = {
            TAR_Nombre: $('#nombre').val(),
            TAR_SEC_Id: $('#seccion').val(),
            TAR_TTA_Id: $('#tipo').val(),
            TAR_TipoIva: $('#iva').val(),
            TAR_ImporteUnitario: $('#importeUnitario').val(),
            TAR_UTA_Id: $('#unidad').val(),
            TAR_PR3_Id: $('#producto').val(),
            TAR_IN3_Id: $('#itemNumber').val(),
            TAR_Activo: $('#visible').is(':checked')
        };

        try {
            const response = await $.ajax({
                url: AppConfig.urls.GuardarTarea,
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify(objTarea)
            });
            if (response.success) {
                mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                window.location.href = `TareaDetalle/${response.idNuevo}`;
            }
            else {
                mostrarToast('Ocurrió un error al guardar los datos.', TipoToast.Error);
            }
        }
        catch (xhr) {
            const msg = obtenerMensajeErrorAjax(xhr);
            registrarErrorjQuery(xhr.status, msg);
        }
    });

    // Cancelar
    $('#btnCancelar').on('click', e => {
        e.preventDefault();
        window.location.href = AppConfig.urls.BusquedaTareas;
    });
});