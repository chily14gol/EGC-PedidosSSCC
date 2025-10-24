$(document).ready(async function () {
    await VerificarSesionActiva(OpcionMenu.Personas);

    let tabla = $('#tablePersonas').DataTable({
        ajax: {
            url: AppConfig.urls.obtenerPersonas,
            type: 'GET',
            dataSrc: ''
        },
        columns: [
            { data: 'NombreCompleto', title: 'Nombre completo' },
            { data: 'DepartamentoNombre', title: 'Departamento' },
            { data: 'PER_Email', title: 'Email' },
            {
                className: 'dt-center ico-status',
                data: null,
                title: 'Activo',
                orderable: false,
                render: function (data, type, row, meta) {
                    return `<input type="checkbox" class="form-check-input"
                            data-id="${row.PER_Id}" disabled
                            ${row.PER_Activo ? 'checked' : ''} />`;
                }
            },
            {
                className: 'td-btn',
                data: null,
                title: '<span class="sReader">Acción</span>',
                orderable: false,
                responsivePriority: 2,
                render: (data, type, row) => `
                    <button class="btn btn-icon btn-editar btn-outline-secondary" onclick="editarPersona(${row.PER_Id}, '${row.PER_Nombre}', '${row.PER_Apellido1}', '${row.PER_Apellido2}', ${row.DepartamentoId}, '${row.PER_Email}', ${row.PER_Activo})">
                        <i class="bi bi-pencil-square" title="Editar"></i>
                    </button>
                    <button class="btn btn-icon btn-eliminar btn-outline-secondary" onclick="eliminarPersona(${row.PER_Id}, '${row.NombreCompleto}')">
                        <i class="bi bi-trash" title="Eliminar"></i>
                    </button>`
            }
        ],
        responsive: true
    });

    ocultarDivCargando();

    function cargarDepartamentos() {
        return $.getJSON(AppConfig.urls.obtenerDepartamentos)
            .then(data => {
                let $sel = $('#selectDepartamento').empty().append('<option value="">Seleccione</option>');
                data.forEach(d => $sel.append(`<option value="${d.DEP_Id}">${d.DEP_Nombre}</option>`));
            });
    }

    $('#btnNuevo').click(() => {
        $('#modalEditarPersonaLabel').text('Agregar Persona');
        $('#personaIdOriginal').val('');
        $('.form-wrapper input, .form-wrapper select').val('');
        $('#checkActivo').prop('checked', true);
        cargarDepartamentos().then(() => $('#modalEditarPersona').modal('show'));
    });

    window.editarPersona = (id, nom, ap1, ap2, depId, email, activo) => {
        $('#modalEditarPersonaLabel').text('Editar Persona');
        $('#personaIdOriginal').val(id);
        $('#inputNombre').val(nom);
        $('#inputApellido1').val(ap1);
        $('#inputApellido2').val(ap2);
        $('#inputEmail').val(email);
        $('#checkActivo').prop('checked', activo);
        cargarDepartamentos().then(() => {
            $('#selectDepartamento').val(depId);
            $('#modalEditarPersona').modal('show');
        });
    };

    window.eliminarPersona = (id, nombre) => {
        mostrarAlertaConfirmacion({
            titulo: `¿Eliminar a '${nombre}'?`,
            onConfirmar: () => {
                $.post(AppConfig.urls.eliminarPersona, { id: id })
                    .done(res => {
                        if (res.success) {
                            mostrarToast("Persona eliminada.", TipoToast.Success);
                            tabla.ajax.reload(null, false);
                        } else {
                            mostrarToast("No se pudo eliminar.", TipoToast.Warning);
                        }
                    });
            }
        });
    };

    $('#btnGuardarPersona').click(() => {
        let dto = {
            PER_Id: parseInt($('#personaIdOriginal').val()) || 0,
            PER_Nombre: $('#inputNombre').val().trim(),
            PER_Apellido1: $('#inputApellido1').val().trim(),
            PER_Apellido2: $('#inputApellido2').val().trim(),
            PER_DEP_Id: parseInt($('#selectDepartamento').val()) || null,
            PER_Email: $('#inputEmail').val().trim(),
            PER_Activo: $('#checkActivo').is(':checked')
        };

        // validación simple
        if (!dto.PER_Nombre || !dto.PER_Apellido1 || !dto.PER_DEP_Id || !dto.PER_Email) {
            mostrarToast("Todos los campos (*) son obligatorios.", TipoToast.Warning);
            return;
        }

        $.ajax({
            url: AppConfig.urls.guardarPersona,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(dto),
            success: res => {
                if (res.success) {
                    mostrarToast("Guardado con éxito.", TipoToast.Success);
                    $('#modalEditarPersona').modal('hide');
                    tabla.ajax.reload(null, false);
                } else {
                    mostrarToast("Error al guardar.", TipoToast.Error);
                }
            }
        });
    });

    $('#formBuscar').on('keyup', () => {
        tabla.search($('#formBuscar').val(), false, false).draw();
    });

    $('#btnBuscar').click(() => {
        tabla.columns(0).search($('#filterNombre').val())
            .columns(1).search($('#filterApellido').val())
            .columns(2).search($('#filterDepto').val())
            .draw();
    });

    $('#btnLimpiar').click(() => {
        $('.table-filter input').val('');
        tabla.search('').columns().search('').draw();
        $('#btnLimpiar').hide();
    });

    $('#btnAvanzado').click(() => {
        $('.table-filter').toggleClass('advance');
        $('#btnLimpiar').toggle();
        $('#btnAvanzado').text($('.table-filter').hasClass('advance') ? 'Ocultar Filtros' : 'Búsqueda Avanzada');
    });
});
