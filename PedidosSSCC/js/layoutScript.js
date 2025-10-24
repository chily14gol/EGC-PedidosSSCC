
/*
function showMenu() {
    $('nav > ul').attr('style', 'clip: rect(0 0 0 0)');
    $('nav > ul').attr('aria-hidden', 'true');
    $('[aria-expanded]').attr('aria-expanded', 'false');
    document.getElementById($(this).attr('aria-controls')).setAttribute("aria-hidden", "false");
    $(this).attr("aria-expanded", "true");
    document.getElementById($(this).attr('aria-controls')).setAttribute("style", "left:" + $(this).offset().left + "px; clip: auto;position: absolute;");
};
function hideMenu() {
    $('nav > ul').attr('style', 'clip: rect(0 0 0 0)');
    $('nav > ul').attr('aria-hidden', 'true');
    $('[aria-expanded]').attr('aria-expanded', 'false');
};
/*var hasPopups = document.querySelectorAll('[aria-haspopup=true]');
for (var i = 0; i < hasPopups.length; i++ ) {
hasPopups[i].addEventListener("mouseover", showMenu, false);
hasPopups[i].addEventListener("focus", showMenu, false);
hasPopups[i].addEventListener('mouseover', function(){ showMenu }, false);
}
*-/
$(document).ready(function (e) {
    $('[aria-haspopup]').on('mouseover focus', showMenu);
    $('nav > ul').on('mouseleave', hideMenu);
    $('main a').on('focus', hideMenu);
});
*/

jQuery(function ($) {

    // Menú responsive
    let respBtn = '#menuRespBtn';
    let menuWrapper = '#menuRespWrapper';
    let headerH = document.querySelector('header').clientHeight;
    let footerH = document.querySelector('footer').clientHeight;
    if ($(respBtn).length) {
        function toggleRespMenu(btn, container) {
            if (btn.attr("aria-expanded") == "true") {
                btn.attr("aria-expanded", "false");
                btn.prop("aria-expanded", false);
                container.removeClass('expand');
            } else if (btn.attr("aria-expanded") == "false") {
                btn.attr("aria-expanded", "true");
                btn.prop("aria-expanded", true);
                container.addClass('expand');
            }
        };
        $(respBtn).on('click', function () {
            toggleRespMenu($(respBtn), $(menuWrapper));
        });
    }

    // declaracion de alturas de header y footer
    function cssElementSize() {
        let paramUpdate = 0;
        let newHeaderH = document.querySelector('header').clientHeight;
        let newFooterH = document.querySelector('footer').clientHeight;
        if (newHeaderH != headerH) { paramUpdate++; }
        if (newFooterH != footerH) { paramUpdate++; }
        if (paramUpdate > 0) {
            headerH = newHeaderH;
            footerH = newFooterH;
            setCssElementSize(newHeaderH, newFooterH);
        }
    }
    function setCssElementSize(cssHeader, cssFooter) {
        if (document.getElementById('cssHeight') !== null) { document.getElementById('cssHeight').remove(); }
        document.head.appendChild(document.createRange().createContextualFragment(`<style id="cssHeight">
		:root {
			/*--headerHeight: ${headerH / 16}rem;*/
			/*--footerHeight: ${footerH / 16}rem;*/
			--headerHeight: ${headerH}px;
			--footerHeight: ${footerH}px;
		}
	</style>`));
    }

    // Carga inicial
    setCssElementSize(headerH, footerH);
    window.addEventListener('resize', function (event) {
        cssElementSize();
    }, true);

    //Busqueda avanzada
    if ($('#btnAvanzado').length && $('.table-filter .form-advance').length) {
        let advBtn = $('#btnAvanzado');
        let cleanBtnId = 'btnLimpiar';
        let tableFilterForm = $('.table-filter');

        // Agregar iconos dentro del botón
        if (!advBtn.text().includes("Ocultar Filtros")) {
            //advBtn.html(`<i class="bi bi-chevron-down me-2"></i> Búsqueda Avanzada`);
            advBtn.html(`Búsqueda Avanzada`);
        }

        // Evento para mostrar/ocultar los filtros avanzados
        $(document).on("click", '#btnAvanzado', function () {
            if (tableFilterForm.hasClass('advance')) {
                $("#btnLimpiar").hide();
                tableFilterForm.removeClass('advance');
                //advBtn.html(`<i class="bi bi-chevron-down me-2"></i> Búsqueda Avanzada`); // Restaurar icono y texto
                advBtn.html(`Búsqueda Avanzada`); // Restaurar icono y texto
            } else {
                $("#btnLimpiar").show();
                tableFilterForm.addClass('advance');
                //advBtn.html(`<i class="bi bi-chevron-up me-2"></i> Ocultar Filtros`); // Cambiar icono y texto
                advBtn.html(`Ocultar Filtros`); // Cambiar icono y texto
            }
        });

        // Evento para limpiar los filtros
        $(document).on("click", '#' + cleanBtnId, function () {
            document.querySelector('.table-filter').reset();
            document.getElementById('btnBuscar').click();
            location.reload(); // Recarga la página correctamente
        });
    }
});