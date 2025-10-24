//Para que el RadTabStrip no provoque postback al cambiar de pestaña:
function onClientTabSelecting(sender, args) {
	var tab = args.get_tab();
	if (tab.get_pageView()) {
		tab.set_postBack(false);
	}
}

// Para cancelar un evento de javascript de los controles Telerik
function CancelEvent(sender, args) {
	args.set_cancel(true);
}

// Para mostrar el mensaje "Cargando" en los eventos
function MostrarCargandoEvent(sender, eventArgs) {
    mostrarDivCargando();
}

// Para controlar la visibilidad de los controles en funcion del valor de un input / combo
var visibilidadControles = [];

function ddlPlazoSelected(sender, eventArgs) {
	var item = eventArgs.get_item();
	var valorIntroducido = item.get_value();
	PlazoChanged(sender.get_id(), valorIntroducido);
}

function PlazoChanged(sender_id, valorIntroducido) {
	if (controlarVisibilidad(sender_id, valorIntroducido) == false)
		controlarVisibilidad(sender_id, '');
}

function controlarVisibilidad(sender_id, valorIntroducido) {
	for (var i = 0; i < visibilidadControles.length; i++) {
		if (visibilidadControles[i].idControl === sender_id && visibilidadControles[i].valor === valorIntroducido) {
			// ocultamos controles en base a la info del array de Ids
			$(visibilidadControles[i].idsOcultar).hide();
			$(visibilidadControles[i].idsMostrar).show();

			return true;
		}
	}
	return false;
}

function meterVisibilidad(pIdControl, pValor, pIdsOcultar, pIdsMostrar) {
	var visibilidadControl = {
		idControl: pIdControl,
		valor: pValor,
		idsOcultar: pIdsOcultar,
		idsMostrar: pIdsMostrar
	};
	visibilidadControles.push(visibilidadControl);
}

function realizarClickControl(clientId) {
	$('#' + clientId).click();
	//__doPostBack(clientId, "");
}

/*************************** INICIO: RadEditor ****************************/
function OnClientLoad(editor, args) {
    setTimeout(function () {
//        var tool = editor.getToolByName("FontName");
//        tool.set_value("Arial");
//        var size = editor.getToolByName("FontSize");
//        size.set_value("12px");
        var style = editor.get_contentArea().style;
        style.fontFamily = 'Arial';
        style.fontSize = '12px';
    }, 100);
}
/*************************** FIN: RadEditor ****************************/


/*************************** INICIO: Grupos ****************************/
var estadoTabs = [];
function actualizarEstadoTab(sectionClientId, contraido) {
	var tabEncontrado;
	var iEstado = 0;
	for (iEstado = 0; iEstado < estadoTabs.length; iEstado++) {
		if (estadoTabs[iEstado].nombre == sectionClientId) {
			tabEncontrado = estadoTabs[iEstado];
		}
	}
	if (tabEncontrado) {
		tabEncontrado.contraido = contraido;
	}
	else {
		var nuevoTab = {
			nombre: sectionClientId,
			contraido: contraido
		}
		estadoTabs.push(nuevoTab);
	}
	var ih = '';
	for (iEstado = 0; iEstado < estadoTabs.length; iEstado++) {
		if (ih != '') ih = ih + ';';
		ih = ih + estadoTabs[iEstado].nombre + '#' + estadoTabs[iEstado].contraido
	}
	$("[id$='ihEstadoTabs']").val(ih);
}

function contraerInstantaneo(sectionClientId, headerClass, contenidoClass) {
	$contenido = $('#' + sectionClientId + " ." + contenidoClass);
	$header = $('#' + sectionClientId + " ." + headerClass);
	if ($contenido.is(":visible")) {
		$contenido.hide();
		$header.addClass("cerrado");
	}
	actualizarEstadoTab(sectionClientId, true);
}

function expandirInstantaneo(sectionClientId, headerClass, contenidoClass) {
	$contenido = $('#' + sectionClientId + " ." + contenidoClass);
	$header = $('#' + sectionClientId + " ." + headerClass);
	if (!$contenido.is(":visible")) {
		$contenido.show();
		$header.removeClass("cerrado");
	}
	actualizarEstadoTab(sectionClientId, false);
}

function contraerExpandir(sectionClientId, headerClass, contenidoClass) {
    //console.log('contraerExpandir: ' + sectionClientId);
	$contenido = $('#' + sectionClientId + " ." + contenidoClass);
	$header = $('#' + sectionClientId + " ." + headerClass);
	$contenido.slideToggle(150,
						function () {
							if ($contenido.is(":visible")) {
								$header.removeClass("cerrado");
								actualizarEstadoTab(sectionClientId, false);
							}
							else {
								$header.addClass("cerrado");
								actualizarEstadoTab(sectionClientId, true);
							}
						}
					);
}

function expandirAnimado(sectionClientId, headerClass, contenidoClass) {
	$contenido = $('#' + sectionClientId + " ." + contenidoClass);
	if (!$contenido.is(":visible")) {
		contraerExpandir(sectionClientId, headerClass, contenidoClass);
	}
	actualizarEstadoTab(sectionClientId, false);
}

var offsetRibbon;

function navegarRecolocarRibbon(tabClientId, claseScroll) {

	//console.log('navegarRecolocarRibbon. tabClientId: ' + tabClientId + " .claseScroll: " + claseScroll);
	expandirAnimado(tabClientId, 'headerTab', 'contenidoTab');

	var scrollProv = $('#' + tabClientId).position().top - $('#' + tabClientId).parent().offset().top;
	$("." + claseScroll).animate({ scrollTop: scrollProv }, '500');

	return false;

}

function recolocarRibbon(sender, args) {
	$("[id$='ribbonOpciones']").offset({ top: offsetRibbon.top, left: offsetRibbon.left });
}

/*************************** Fin: Grupos ****************************/

/*************************** Inicio: Resumen Planifcacion ***********************/
// Para controlar los diferentes totales de columnas
var totalesColumnas = [];

function meterTotalColumna(pGridClientID, pIndiceColumna, pIdTotal, pNombreControlTotalColumna) {
    var encontradoEnArray = false;
    var posEncontrado = 0;
    for (var iArray = 0; iArray < totalesColumnas.length; iArray++) {
        if (totalesColumnas[iArray].gridClientID == pGridClientID &&
            totalesColumnas[iArray].indiceColumna == pIndiceColumna &&
            totalesColumnas[iArray].idTotal == pIdTotal &&
            totalesColumnas[iArray].nombreControlTotalColumna == pNombreControlTotalColumna) {
            posEncontrado = iArray;
            encontradoEnArray = true;
        }
    }

    if (!encontradoEnArray) {
        var totalColumna = {
            gridClientID: pGridClientID,
            indiceColumna: pIndiceColumna,
            idTotal: pIdTotal,
            nombreControlTotalColumna: pNombreControlTotalColumna
        };
        totalesColumnas.push(totalColumna);
    }
}
/*************************** Fin: Resumen Planificacion ***********************/

var infoTooltips = [];

function MeterInfoTooltip(idTooltip, value, tooltipText) {
    var infoEncontrado;
    var iArray = 0;
    for (iArray = 0; iArray < infoTooltips.length; iArray++) {
        if (infoTooltips[iArray].idTooltip === idTooltip && infoTooltips[iArray].value === value) {
            infoEncontrado = infoTooltips[iArray];
        }
    }
    if (infoEncontrado) {
        infoEncontrado.tooltipText = tooltipText;
    }
    else {
        var nuevoInfo = {
            idTooltip: idTooltip,
            value: value,
            tooltipText: tooltipText
        }
        infoTooltips.push(nuevoInfo);
    }
}

function cargarTooltip(idTooltip, selectedValue, forceShow) {
    var infoEncontrado;
    var tooltipText = '';
    var iArray = 0;
    for (iArray = 0; iArray < infoTooltips.length; iArray++) {
        if (infoTooltips[iArray].idTooltip === idTooltip && infoTooltips[iArray].value === selectedValue) {
            infoEncontrado = infoTooltips[iArray];
        }
    }
    if (infoEncontrado) {
        tooltipText = infoEncontrado.tooltipText;
    }
    var divTooltip = $('#' + idTooltip);
    divTooltip.html(tooltipText);
    if (tooltipText !== '' && forceShow) {
        divTooltip.show();
    }
}