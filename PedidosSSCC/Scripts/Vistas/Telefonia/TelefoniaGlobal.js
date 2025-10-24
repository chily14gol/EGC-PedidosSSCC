let tablaDatos;

$(document).ready(async function () {
    await VerificarSesionActiva(OpcionMenu.TelefoniaGlobal);
    await initTabla();

    ocultarDivCargando();

    $('#btnFiltrar').on('click', function () {
        tablaConsumo.ajax.reload();
    });
});

async function initTabla() {
    let columnasConFiltro = [];
    tablaDatos = inicializarDataTable("#tablaConsumo", {
        ajax: {
            url: AppConfig.urls.ObtenerConsumoGbPorUnYUso,
            type: 'GET',
            dataSrc: '',
            data: function (d) {
                d.anyo = $('#filtroAnyo').val() || null;
                d.mes = $('#filtroMes').val() || null;
                d.tipo = $('#filtroTipo').val() || null;
            }
        },
        columns: [
            { data: 'UN' },
            { data: 'Uso' },
            { data: 'TotalGB', render: v => v != null ? Number(v).toFixed(3) : '0.000' },
            { data: 'Registros' }
        ]
    }, columnasConFiltro, 'export_vodafone');

    $(window).resize(() => tablaDatos.columns.adjust().draw());

    $("#formBuscar").on("keyup input", function () {
        tablaDatos.search(this.value, false, false).draw();
        //guardarFiltros();
    });

    return tablaDatos;
}