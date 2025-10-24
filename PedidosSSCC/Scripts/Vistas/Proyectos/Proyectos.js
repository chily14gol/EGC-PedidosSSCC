$(document).ready(async function () {
    await VerificarSesionActiva(OpcionMenu.Proyectos);
   
    // 1) Inicializaciones
    inicializarDataTable();
    cargarComboTareas();

    InicializarSelectDepartamentos();
    cargarSelectDepartamentos();

    cargarComboEmpresas();        // Trae lista de empresas

    // 2) Botón “Nuevo Proyecto”
    $('#btnNuevoProyecto').click(function () {
        limpiarFormulario();
        $('#modalProyectoLabel').text('Nuevo Proyecto');
        $('#modalProyecto').modal('show');
    });

    // 3) Submit del formulario (Guardar o Actualizar)
    $('#formProyecto').submit(function (e) {
        e.preventDefault();
        guardarProyecto();
    });

    // 4) Agregar fila en la tabla de Empresas
    $('#btnAgregarEmpresa').click(function () {
        agregarFilaEmpresa(null, 100); // Por defecto porcentaje = 100
    });

    // 5) Cargar tabla con los proyectos existentes
    await cargarTablaProyectos();

    ocultarDivCargando();
});

function inicializarDataTable() {
    $('#tblProyectos').DataTable({
        columns: [
            { data: 'PRY_Id' },
            { data: 'PRY_Nombre' },
            { data: 'TareaNombre' },
            {
                className: 'dt-center ico-status',
                data: null,
                title: 'Imputable',
                orderable: false,
                render: function (data, type, row, meta) {
                    return `<input type="checkbox" class="form-check-input"
                            data-id="${row.PRY_Id}" disabled
                            ${row.PRY_Imputable ? 'checked' : ''} />`;
                }
            },
            {
                className: 'dt-center ico-status',
                data: null,
                title: 'Activo',
                orderable: false,
                render: function (data, type, row, meta) {
                    return `<input type="checkbox" class="form-check-input"
                            data-id="${row.PRY_Id}" disabled
                            ${row.PRY_Activo ? 'checked' : ''} />`;
                }
            },
            {
                data: null,
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    return `
                        <button class="btn btn-icon btn-editar btn-outline-secondary" data-id="${row.PRY_Id}" title="Editar">
                            <i class="bi bi-pencil-square"></i>
                        </button>
                        <button class="btn btn-icon btn-eliminar btn-outline-secondary" data-id="${row.PRY_Id}" title="Eliminar">
                            <i class="bi bi-trash"></i>
                        </button>`;
                }
            }
        ],
        language: {
            url: AppConfig.urls.DataTablesLang
        }
    });

    // Delegación de eventos para los botones “Editar” y “Eliminar”
    $('#tblProyectos tbody').on('click', '.btn-editar', function () {
        let id = $(this).data('id');
        editarProyecto(id);
    });

    $('#tblProyectos tbody').on('click', '.btn-eliminar', function () {
        let id = $(this).data('id');
        eliminarProyecto(id);
    });
}

function cargarTablaProyectos() {
    return new Promise((resolve, reject) => {
        $.ajax({
            url: AppConfig.urls.GetProyectos,
            type: 'GET',
            success: function (resp) {
                if (resp && resp.length >= 0) {
                    // Obtenemos la instancia de DataTable
                    let dt = $('#tblProyectos').DataTable();
                    dt.clear();
                    dt.rows.add(resp);
                    dt.draw();

                    // Resolver la promesa **después** de que DataTable haya pintado todo
                    // Usamos setTimeout 0 para asegurarnos de que el draw termine de renderizar
                    setTimeout(() => {
                        resolve();
                    }, 0);
                } else {
                    // Si la respuesta no es válida, resolvemos de todas formas
                    resolve();
                }
            },
            error: function (xhr, status, error) {
                console.error('Error al cargar proyectos:', error);
                alert('No se pudieron cargar los proyectos.');
                // Resolvemos para que no quede colgado el await
                resolve();
            }
        });
    });
}

function cargarComboTareas() {
    const tipos = [Tipo.POR_HORAS];

    $.ajax({
        url: AppConfig.urls.ObtenerTareasCombo,
        type: 'GET',
        data: {
            listaTiposTarea: tipos 
        },
        traditional: true, 
        success: function (resp) {
            let html = '<option value="">Seleccione Tarea</option>';
            resp.forEach(t => {
                html += `<option value="${t.TAR_Id}">${t.TAR_Nombre}</option>`;
            });
            $('#PRY_TAR_Id').html(html);
        },
        error: function () {
            alert('Error al cargar las tareas.');
        }
    });
}

async function cargarSelectDepartamentos() {
    try {
        const response = await fetch(`${AppConfig.urls.GetDepartamentos}`, {
            method: 'GET',
            headers: { 'Content-Type': 'application/json; charset=utf-8' }
        });

        const data = await response.json();
        const $select = $('#PRY_Departamentos');
        $select.empty();

        data.forEach(item => {
            const option = new Option(item.DEP_Nombre, item.DEP_Id, item.selected, item.selected);
            $select.append(option);
        });

        $select.trigger('change');
    } catch (error) {
        registrarErrorjQuery('fetch', error.message);
    }
}

function InicializarSelectDepartamentos() {
    if (!$.fn.select2) {
        console.error("Select2 no está disponible");
        return;
    }

    let $select = $('#PRY_Departamentos');
    if ($select.length === 0) {
        console.error("El select #PRY_Departamentos no está en el DOM");
        return;
    }

    $select.select2({
        placeholder: "",
        allowClear: true,
        multiple: true,
        dropdownParent: $('#modalProyecto'), // o el modal donde esté el select
        width: '100%'
    });
}

// ------------------------------------------------------------------------------
// Función: cargarComboEmpresas
// Descripción: obtiene la lista de empresas y las almacena en listaEmpresasGlobal
// ------------------------------------------------------------------------------
let listaEmpresasGlobal = [];
function cargarComboEmpresas() {
    $.ajax({
        url: AppConfig.urls.GetEmpresas,
        type: 'GET',
        success: function (resp) {
            listaEmpresasGlobal = resp; // Guardar globalmente para usar al agregar filas
        },
        error: function () {
            alert('Error al cargar las empresas.');
        }
    });
}

// ------------------------------------------------------------------------------
// Función: agregarFilaEmpresa
// Descripción: agrega una nueva fila en la tabla de empresas dentro del modal.
//              Parámetros opcionales: seleccionado (empresaId), porcentaje.
// ------------------------------------------------------------------------------
function agregarFilaEmpresa(seleccionado = null, porcentaje = '') {
    let nuevaFila = $('<tr></tr>');

    // Celda: Dropdown de empresas
    let htmlEmpresas = '<select class="form-select selectEmpresa" required>';
    htmlEmpresas += '<option value="">-- Seleccionar --</option>';
    listaEmpresasGlobal.forEach(e => {
        if (seleccionado !== null && seleccionado == e.EMP_Id) {
            htmlEmpresas += `<option value="${e.EMP_Id}" selected>${e.EMP_Nombre}</option>`;
        } else {
            htmlEmpresas += `<option value="${e.EMP_Id}">${e.EMP_Nombre}</option>`;
        }
    });
    htmlEmpresas += '</select>';
    nuevaFila.append(`<td>${htmlEmpresas}</td>`);

    // Celda: Input de porcentaje (por defecto 100 si no se pasa otro valor)
    let valorPorcentaje = porcentaje !== '' ? porcentaje : 100;
    nuevaFila.append(`<td><input type="number" class="form-control inputPorcentaje" min="0" max="100" step="0.01" value="${valorPorcentaje}" required></td>`);

    // Celda: Botón eliminar fila
    nuevaFila.append(`<td><button type="button" class="btn btn-icon btnEliminarEmpresa btn-danger">
        <i class="bi bi-trash" title="Eliminar"></i>
        </button></td>`);

    $('#tblEmpresasProyecto tbody').append(nuevaFila);
}

// ------------------------------------------------------------------------------
// Función: limpiarFormulario
// Descripción: reinicia todos los campos del modal, incluido el Select2 de departamentos
// ------------------------------------------------------------------------------
function limpiarFormulario() {
    $('#PRY_Id').val(0);
    $('#PRY_Nombre').val('');
    $('#PRY_TAR_Id').val('');
    $('#PRY_Imputable').prop('checked', true);
    $('#PRY_Activo').prop('checked', true);

    // Limpiar Select2 de Departamentos
    $('#PRY_Departamentos').val(null).trigger('change');

    // Limpiar tabla de empresas
    $('#tblEmpresasProyecto tbody').empty();
}

// ------------------------------------------------------------------------------
// Función: editarProyecto
// Descripción: carga vía AJAX un proyecto por ID, rellena el modal y abre el modal
// ------------------------------------------------------------------------------
function editarProyecto(id) {
    $.ajax({
        url: AppConfig.urls.GetProyecto,
        data: { id: id },
        type: 'GET',
        success: function (resp) {
            if (resp && resp.success) {
                let dto = resp.proyecto;
                // Cambiar título del modal
                $('#modalProyectoLabel').text('Editar Proyecto');

                // Rellenar campos principales
                $('#PRY_Id').val(dto.PRY_Id);
                $('#PRY_Nombre').val(dto.PRY_Nombre);
                $('#PRY_TAR_Id').val(dto.PRY_TAR_Id);
                $('#PRY_Imputable').prop('checked', dto.PRY_Imputable);
                $('#PRY_Activo').prop('checked', dto.PRY_Activo);

                // Marcar valores en Select2 de Departamentos
                $('#PRY_Departamentos').val(dto.Departamentos).trigger('change');

                // Limpiar y rellenar tabla de empresas
                $('#tblEmpresasProyecto tbody').empty();
                dto.Empresas.forEach(function (e) {
                    agregarFilaEmpresa(e.PRE_EMP_Id, e.PRE_Porcentaje);
                });

                // Mostrar modal
                $('#modalProyecto').modal('show');
            } else {
                alert('No se encontró el proyecto solicitado.');
            }
        },
        error: function () {
            alert('Error al cargar los datos del proyecto.');
        }
    });
}

// ------------------------------------------------------------------------------
// Función: eliminarProyecto
// Descripción: confirma y envía petición AJAX para eliminar proyecto (y sus dependencias)
// ------------------------------------------------------------------------------
function eliminarProyecto(id) {
    if (!confirm('¿Estás seguro de eliminar este proyecto? Esta acción no se puede deshacer.')) {
        return;
    }

    $.ajax({
        url: AppConfig.urls.DeleteProyecto,
        data: { id: id },
        type: 'POST',
        success: function (resp) {
            if (resp && resp.success) {
                cargarTablaProyectos();
            } else {
                alert('No se pudo eliminar el proyecto.');
            }
        },
        error: function () {
            alert('Error al eliminar el proyecto.');
        }
    });
}

// ------------------------------------------------------------------------------
// Función: guardarProyecto
// Descripción: valida el formulario, recopila datos y envía por AJAX para crear/actualizar
// ------------------------------------------------------------------------------
function guardarProyecto() {
    // Validar formulario HTML5
    if (!$('#formProyecto')[0].checkValidity()) {
        $('#formProyecto')[0].reportValidity();
        return;
    }

    // Construir objeto DTO
    let dto = {
        PRY_Id: parseInt($('#PRY_Id').val()) || 0,
        PRY_Nombre: $('#PRY_Nombre').val(),
        PRY_TAR_Id: parseInt($('#PRY_TAR_Id').val()),
        PRY_Imputable: $('#PRY_Imputable').is(':checked'),
        PRY_Activo: $('#PRY_Activo').is(':checked'),
        Departamentos: [],
        Empresas: []
    };

    // Recoger Departamentos desde Select2
    let deps = $('#PRY_Departamentos').val();  // Array de strings
    if (deps && deps.length > 0) {
        deps.forEach(function (depIdStr) {
            let depId = parseInt(depIdStr);
            if (!isNaN(depId)) {
                dto.Departamentos.push(depId);
            }
        });
    }

    // Recoger Empresas y porcentajes
    $('#tblEmpresasProyecto tbody tr').each(function () {
        let empresaId = parseInt($(this).find('.selectEmpresa').val());
        let porcentaje = parseFloat($(this).find('.inputPorcentaje').val());
        if (!isNaN(empresaId) && !isNaN(porcentaje)) {
            dto.Empresas.push({
                PRE_PRY_Id: parseInt($('#PRY_Id').val()),
                PRE_EMP_Id: empresaId,
                PRE_Porcentaje: porcentaje
            });
        }
    });

    // 1) Validar que la suma de todos los porcentajes sea 100
    let sumaPorcentajes = 0;
    $('#tblEmpresasProyecto tbody tr').each(function () {
        const porcentaje = parseFloat($(this).find('.inputPorcentaje').val()) || 0;
        sumaPorcentajes += porcentaje;
    });
    // Redondeamos a 2 decimales para evitar desajustes numéricos
    sumaPorcentajes = Math.round(sumaPorcentajes * 100) / 100;

    if (sumaPorcentajes !== 100) {
        alert('La suma de los porcentajes de empresas debe ser exactamente 100.');
        return;
    }

    // Enviar petición AJAX para guardar
    $.ajax({
        url: AppConfig.urls.SaveProyecto,
        contentType: 'application/json; charset=utf-8',
        type: 'POST',
        data: JSON.stringify(dto),
        success: function (resp) {
            if (resp && resp.success) {
                $('#modalProyecto').modal('hide');
                cargarTablaProyectos();
            } else {
                alert('No se pudo guardar el proyecto. Verifica los datos e inténtalo de nuevo.');
            }
        },
        error: function (xhr, status, error) {
            console.error(error);
            alert('Error al guardar el proyecto.');
        }
    });
}

// ------------------------------------------------------------------------------
// Delegación del evento “Eliminar fila de empresa”
// ------------------------------------------------------------------------------
$(document).on('click', '.btnEliminarEmpresa', function () {
    $(this).closest('tr').remove();
});
