function editarUsuario(idUsuario, idPersona, email, idPerfil, verTodo) {
    $('#usuarioOriginal').val(decodeURIComponent(idUsuario)); // Guardamos el ID original (para edición)
    $('#usuario').val(decodeURIComponent(idUsuario)).prop('disabled', true); // Deshabilitado en edición
    $('#nombrePersona').val(idPersona).prop('disabled', true);
    $('#email').val(email).prop('disabled', true);
    $('#perfiles').val(idPerfil);
    $('#verTodo').prop('checked', verTodo);

    $('#usuario').removeClass('is-invalid');
    $('#nombrePersona').removeClass('is-invalid');
    $('#email').removeClass('is-invalid');
    $('#perfiles').removeClass('is-invalid');

    $('#modalEditarLabel').text('Editar Usuario'); // Cambiar título
    $('#modalEditar').modal('show');
}

function eliminarUsuario(idUsuario) {
    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar el usuario '${idUsuario}'?`,
        onConfirmar: function () {
            $.ajax({
                url: AppConfig.urls.eliminarUsuario,
                type: 'POST',
                contentType: 'application/json; charset=utf-8', // Indica que estás enviando JSON
                data: JSON.stringify({ idUsuario: decodeURIComponent(idUsuario) }), // Serializa los datos como JSON
                dataType: 'json', // Esperas un JSON de respuesta
                success: function (response) {
                    if (response.success) {
                        mostrarToast("Usuario eliminado correctamente.", TipoToast.Success);

                        let tabla = $('#table').DataTable();
                        tabla.ajax.reload(null, false); // 'false' mantiene la página actual
                    } else {
                        mostrarToast.error("No se puede eliminar el usuario. Tiene relaciones", TipoToast.Warning);
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

async function guardarUsuario() {
    // 1) Leer y cachear valores
    const id = decodeURIComponent($('#usuario').val().trim()),
        idPersona = $('#nombrePersona').val(),
        emailVal = $('#email').val().trim(),
        idPerfil = $('#perfiles').val(),
        verTodo = $('#verTodo').is(':checked'),
        orig = $('#usuarioOriginal').val().trim();

    // 2) Validación genérica de campos
    const campos = [
        { el: $('#usuario'), val: id },
        { el: $('#nombrePersona'), val: idPersona },
        { el: $('#email'), val: emailVal },
        { el: $('#perfiles'), val: idPerfil }
    ];

    // marcar inválidos
    let hayInvalidos = false;
    campos.forEach(({ el, val }) => {
        if (!val) {
            el.addClass('is-invalid');
            hayInvalidos = true;
        } else {
            el.removeClass('is-invalid');
        }
    });
    if (hayInvalidos) {
        mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
        return;
    }

    // 3) Si es alta, comprobar duplicado
    if (!orig) {
        const dt = $('#table').DataTable();
        const existe = dt.rows().data()
            .toArray()
            .some(r => r.USU_Id.toLowerCase() === id.toLowerCase());
        if (existe) {
            $('#usuario').addClass('is-invalid');
            mostrarToast("El usuario ya existe en el sistema.", TipoToast.Error);
            return;
        }
    }

    // 4) Preparo payload
    const payload = {
        usuario: {
            USU_Id: id,
            USU_PER_Id: idPersona,
            USU_SPE_Id: idPerfil,
            USU_VerTodo: verTodo
        },
        email: emailVal
    };

    // 5) Envío AJAX con async/await
    try {
        const resp = await $.ajax({
            url: AppConfig.urls.guardarUsuario,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(payload),
            dataType: 'json'
        });

        if (resp.success) {
            mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
            $('#modalEditar').modal('hide');
            $('#table').DataTable().ajax.reload(null, false); // refresca sin cambiar página
        } else {
            mostrarToast(resp.message || "Error al guardar usuario.", TipoToast.Error);
        }
    } catch (xhr) {
        const msg = obtenerMensajeErrorAjax(xhr);
        registrarErrorjQuery(xhr.status, msg);
    }
}

$(document).ready(function () {
    let storageKey = Filtros.Usuarios;

    VerificarSesionActiva(OpcionMenu.Usuarios).then(() => {
        Promise.all([CargarComboPersonas(), CargarComboPerfiles()])
            .then(() => {
                establecerFiltros();
            })
            .catch((error) => {
                console.error('Error al cargar los datos:', error);
            });
    });

    function CargarComboPersonas() {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: AppConfig.urls.obtenerPersonas,
                type: 'GET',
                dataType: 'json',
                success: function (data) {
                    let $select = $('#nombrePersona');
                    $select.empty(); // Limpiar opciones previas
                    $select.append('<option value="">Seleccione una persona</option>');

                    $.each(data, function (index, item) {
                        $select.append(`<option value="${item.PER_Id}">${item.ApellidosNombre}</option>`);
                    });

                    resolve(); // Indica que la operación fue exitosa
                },
                error: function (xhr, status, error) {
                    const mensaje = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, mensaje);
                    reject();
                }
            });
        });
    }

    function CargarComboPerfiles() {
        $.ajax({
            async: false,
            type: 'GET',
            contentType: 'application/json; charset=utf-8',
            url: AppConfig.urls.obtenerPerfiles,
            dataType: "json",
            success: function (data) {
                let $select = $('#perfiles');
                $select.empty();
                $select.append('<option value="">Seleccione un perfil</option>');

                $.each(data, function (index, item) {
                    $select.append('<option value="' + item.SPE_Id + '">' + item.SPE_Nombre + '</option>');
                });
            },
            error: function (xhr, status, error) {
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
            }
        });
    }

    function establecerFiltros() {
        // Recuperar filtros previos si existen
        let savedFilters = localStorage.getItem(storageKey);
        let hasFilters = false;

        if (savedFilters) {
            savedFilters = JSON.parse(savedFilters);
            $("#formBuscar").val(savedFilters.general);
            $("#filterUsuario").val(savedFilters.usuario || "");
            $("#filterPersona").val(savedFilters.persona || "");
            $("#filterPerfil").val(savedFilters.perfil || "");

            // Comprobar si al menos un filtro tiene valor
            let { general, ...otrosFiltros } = savedFilters;
            hasFilters = Object.values(otrosFiltros).some(value => value !== "" && value !== null && value !== undefined);
        }

        ObtenerUsuarios();

        setTimeout(function () {
            let table = $('#table').DataTable();
            if (savedFilters?.general) {
                table.search(savedFilters.general, false, false).draw();
            }
        }, 200);

        // Si hay filtros aplicados, mostrar el div de filtros
        if (hasFilters) {
            $("#btnLimpiar").show();
            $(".table-filter").addClass('advance');
            $('#btnAvanzado').html(`Ocultar Filtros`); // Cambiar icono y texto
        }
    }

    function guardarFiltros() {
        let general = $("#formBuscar").val();
        let usuario = $("#filterUsuario").val();
        let persona = $('#filterPersona').val();
        let perfil = $('#filterPerfil').val();

        let filtroActual = {
            general: general,
            usuario: usuario,
            persona: persona,
            perfil: perfil
        };

        // Guardar en localStorage
        localStorage.setItem(storageKey, JSON.stringify(filtroActual));
    }

    function ObtenerUsuarios() {
        let columnasConFiltro = [];
        let tablaDatos = inicializarDataTable('#table', {
            ajax: {
                url: AppConfig.urls.obtenerUsuarios,
                type: 'GET',
                dataSrc: ''
            },
            columns: [
                { data: 'USU_Id', title: 'Usuario' },
                { data: 'NombrePersona', title: 'Persona' },
                { data: 'Perfil', title: 'Perfil' },
                {
                    className: 'td-btn',
                    data: null,
                    title: '<span class="sReader">Acción</span>',
                    responsivePriority: 2,
                    orderable: false,
                    render: function (data, type, row) {
                        return `
                                                        <button type="button" class="btn btn-icon btn-editar btn-outline-secondary" onclick="editarUsuario('${row.USU_Id.replace(/\\/g, '\\\\')}', '${row.USU_PER_Id}', '${row.Email}', '${row.IdPerfil}', ${row.VerTodo})">
                                                            <i class="bi bi-pencil-square" title="Editar"></i>
                                                        </button>
                                                        <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary" onclick="eliminarUsuario('${row.USU_Id.replace(/\\/g, '\\\\')}')">
                                                            <i class="bi bi-trash" title="Eliminar"></i>
                                                        </button>`;
                    }
                }
            ]
        }, columnasConFiltro, 'export_usuarios');

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

    $('#btnNuevo').on('click', function () {
        $('#usuarioOriginal').val(''); // Dejar vacío para indicar que es un nuevo usuario
        $('#usuario').val('').prop('disabled', false); // Habilitar input para ID
        $('#nombrePersona').val('').prop('disabled', false);
        $('#email').val('').prop('disabled', true);
        $('#perfiles').val('').prop('disabled', false);
        $('#verTodo').prop('checked', false);

        $('#usuario').removeClass('is-invalid');
        $('#nombrePersona').removeClass('is-invalid');
        $('#email').removeClass('is-invalid');
        $('#perfiles').removeClass('is-invalid');

        $('#modalEditarLabel').text('Agregar Usuario'); // Cambiar título del modal
        $('#modalEditar').modal('show'); // Mostrar modal
    });

    $('#nombrePersona').on('change', function () {
        let idPersona = $(this).val();
        if (idPersona) {
            $.ajax({
                url: AppConfig.urls.obtenerEmailPersona,
                type: 'GET',
                data: { idPersona: idPersona },
                success: function (response) {
                    $('#email').val(response);
                },
                error: function (xhr, status, error) {
                    const mensaje = obtenerMensajeErrorAjax(xhr);
                    registrarErrorjQuery(xhr.status, mensaje);
                }
            });
        }
    });

    $('#btnBuscar').on('click', function () {
        let usuario = $('#filterUsuario').val();
        let persona = $('#filterPersona').val();
        let perfil = $('#filterPerfil').val();

        let table = $('#table').DataTable();
        table.columns(0).search(usuario).draw();
        table.columns(1).search(persona).draw();
        table.columns(2).search(perfil).draw();

        guardarFiltros();
    });
});