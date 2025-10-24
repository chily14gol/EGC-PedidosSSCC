async function abrirModalPerfil(id, nombre) {
    $('#modalEditar').data('idPerfil', id);
    $('#nombrePerfil').val(nombre);

    await CargarArbolPermisos(id, false);
    $('#modalEditar').modal('show');
}

function prepararDataTree(data, esAcceso, esNuevo = false) {
    const parentIds = new Set();
    data.forEach(item => {
        const parentId = item.SPO_SOP_Id.includes('.')
            ? item.SPO_SOP_Id.substring(0, item.SPO_SOP_Id.lastIndexOf('.'))
            : null;
        if (parentId) parentIds.add(parentId);
    });

    return data.map(item => {
        const id = item.SPO_SOP_Id.trim();
        const nivel = id.split('.').length - 1;
        const tabulacion = '&nbsp;'.repeat(nivel * 4);

        const tienePermiso = item.SPO_SPE_Id !== 0;
        const isPadre = parentIds.has(id);

        const selected = esNuevo
            ? false // 🔒 NUEVO PERFIL: ninguno seleccionado
            : esAcceso
                ? tienePermiso && !isPadre
                : tienePermiso && item.SPO_Escritura === true && !isPadre;

        return {
            id: id,
            parent: id.includes('.') ? id.substring(0, id.lastIndexOf('.')) : "#",
            text: tabulacion + item.SOI_Nombre,
            data: { SPO_SOP_Id: id },
            state: {
                selected: selected,
                opened: true
            }
        };
    });
}

async function CargarArbolPermisos(id, nuevo) {
    mostrarDivCargando();

    try {
        const data = await $.ajax({
            type: 'GET',
            contentType: 'application/json; charset=utf-8',
            url: AppConfig.urls.ObtenerPermisos,
            data: { idPerfil: id },
            dataType: 'json'
        });

        let treeDataAcceso = prepararDataTree(structuredClone(data), true, nuevo);
        let treeDataEdicion = prepararDataTree(structuredClone(data), false, nuevo);

        $('#tree-container-acceso').jstree("destroy").empty().jstree({
            core: { data: treeDataAcceso },
            plugins: ["checkbox"],
            checkbox: {
                keep_selected_style: false,
                three_state: true,            // ← Habilita el estado indeterminado (semi-marcado)
                cascade: "up"
            },
        });

        $('#tree-container-edicion').jstree("destroy").empty().jstree({
            core: { data: treeDataEdicion },
            plugins: ["checkbox"],
            checkbox: {
                keep_selected_style: false,
                three_state: true,            // ← Habilita el estado indeterminado (semi-marcado)
                cascade: "up"
            },
        });

        ocultarDivCargando();
    } catch (error) {
        ocultarDivCargando();
        registrarErrorjQuery(error.statusText, error.message);
    }
}

function eliminarPerfil(button) {
    const csrfToken = document.querySelector('#antiForgeryContainer input[name="__RequestVerificationToken"]')?.value;

    let perfil = JSON.parse(button.getAttribute('data-perfil'));

    mostrarAlertaConfirmacion({
        titulo: `¿Estás seguro de que deseas eliminar el perfil '${perfil.SPE_Nombre}'?`,
        onConfirmar: function () {
            $.ajax({
                url: AppConfig.urls.EliminarPerfil,
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify({ idPerfil: perfil.SPE_Id }),
                dataType: 'json'//,
                //headers: {
                //    'RequestVerificationToken': csrfToken
                //}
            })
                .done(function (response) {
                    if (response.success) {
                        mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);

                        let tabla = $('#table').DataTable();
                        tabla.ajax.reload(null, false);
                    } else {
                        mostrarToast("No se puede eliminar por que hay usuarios con el perfil.", TipoToast.Warning);
                    }
                })
                .fail(function (xhr, status, error) {
                    registrarErrorjQuery(status, error);
                });
        }
    });
}

$(document).ready(function () {
    VerificarSesionActiva(OpcionMenu.Perfiles).then(() => {
        ObtenerPerfiles();
    });

    function ObtenerPerfiles() {
        let columnasConFiltro = [];
        let tablaDatos = inicializarDataTable('#table', {
            ajax: {
                url: AppConfig.urls.ObtenerPerfiles,
                type: 'GET',
                dataSrc: ''
            },
            columns: [
                { data: 'SPE_Nombre', title: 'Nombre' },
                {
                    className: 'td-btn',
                    data: null,
                    title: '<span class="sReader">Acción</span>',
                    responsivePriority: 2,
                    orderable: false,
                    render: function (data, type, row) {
                        return `
                                    <button type="button" class="btn btn-icon btn-editar btn-outline-secondary" onclick="abrirModalPerfil(${row.SPE_Id}, '${row.SPE_Nombre}')">
                                        <i class="bi bi-pencil-square" title="Editar"></i>
                                    </button>
                                    <button type="button" class="btn btn-icon btn-eliminar btn-outline-secondary"
                                        data-perfil="${JSON.stringify(row).replace(/"/g, "&quot;")}"
                                        onclick="eliminarPerfil(this)">
                                        <i class="bi bi-trash" title="Eliminar"></i>
                                    </button>`;
                    }
                }
            ]
        }, columnasConFiltro, 'export_perfiles');

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

    function obtenerPermisosConPadres(jstreeSelector) {
        const tree = $(jstreeSelector).jstree(true);
        const seleccionados = tree.get_checked(true); // Nodos seleccionados completamente
        const permisosMap = {};

        seleccionados.forEach(node => {
            agregarNodoYPadres(node);
        });

        function agregarNodoYPadres(node) {
            const id = node.data?.SPO_SOP_Id || node.id;
            if (!permisosMap[id]) {
                permisosMap[id] = {
                    Id: id,
                    Permiso: true
                };
            }

            let padreId = node.parent;
            while (padreId && padreId !== '#') {
                const padreNode = tree.get_node(padreId);
                const padreRealId = padreNode.data?.SPO_SOP_Id || padreNode.id;

                if (!permisosMap[padreRealId]) {
                    permisosMap[padreRealId] = {
                        Id: padreRealId,
                        Permiso: true
                    };
                }

                padreId = padreNode.parent;
            }
        }

        return Object.values(permisosMap);
    }

    $('#btnNuevo').on('click', async function (e) {
        e.preventDefault();

        $('#modalEditar').data('idPerfil', -1);
        $('#nombrePerfil').val('');

        await CargarArbolPermisos(1, true);
        $('#modalEditar').modal('show');
    });

    $('#btnGuardarPerfil').on('click', function (e) {
        e.preventDefault();

        let nombrePerfil = $('#nombrePerfil').val().trim();

        let camposInvalidos = [];

        if (!nombrePerfil) {
            $('#nombrePerfil').addClass('is-invalid');
            camposInvalidos.push("Nombre");
        } else {
            $('#nombrePerfil').removeClass('is-invalid');
        }

        if (camposInvalidos.length > 0) {
            mostrarToast(AppConfig.mensajes.camposObligatorios, TipoToast.Warning);
            return;
        }

        let permisosAccesoSeleccionados = obtenerPermisosConPadres('#tree-container-acceso');
        let permisosEdicionSeleccionados = obtenerPermisosConPadres('#tree-container-edicion');

        let perfil = {
            IdPerfil: $('#modalEditar').data('idPerfil'),
            NombrePerfil: nombrePerfil,
            PermisosAcceso: permisosAccesoSeleccionados,
            PermisosEdicion: permisosEdicionSeleccionados
        };

        $.ajax({
            url: AppConfig.urls.GuardarPerfil,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(perfil),
            dataType: 'json'
        })
            .done(function (response) {
                if (response.success) {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Success);
                    $('#modalEditar').modal('hide');

                    let tabla = $('#table').DataTable();
                    tabla.ajax.reload(null, false);
                } else {
                    mostrarToast(AppConfig.mensajes.operacionOk, TipoToast.Error);
                }
            })
            .fail(function (xhr, status, error) {
                registrarErrorjQuery(status, error);
            });
    });

    document.getElementById("nombrePerfil").addEventListener("paste", function (event) {
        event.preventDefault(); // Evita que se pegue contenido
    });

    document.getElementById("nombrePerfil").addEventListener("keydown", function (event) {
        if (event.ctrlKey && event.key === 'v') {
            event.preventDefault(); // Bloquea Ctrl+V
        }
        if (event.metaKey && event.key === 'v') { // Para Mac (Cmd+V)
            event.preventDefault();
        }
    });
});