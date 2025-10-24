// Funcion alternativa al $find de Telerik ya que, por lo visto, hay casos en que devuelve null aunque si que exista el control con ese ClientID
function findAlternativo(clientID) {
	var control = $find(clientID);
	if (control == null) {
		for (var i = 0; i < $telerik.radControls.length; i++) {
			if ($telerik.radControls[i]._element) {
				var elementI = $telerik.radControls[i]._element;
				if (elementI.id.lastIndexOf(clientID) != -1) {
					control = $telerik.radControls[i];
				}
			}
		}
	}
	return control;
}

function obtenerPosicionCaret(el) {

	var retorno = { start: 0, end: 0 };

	if (typeof el.selectionStart != 'undefined') {
		// IE9+, Firefox, Chrome, Opera
		retorno.start = el.selectionStart;
		retorno.end = el.selectionEnd;

		return retorno;
	}

	// Implementacion de selectionStart y selectionEnd para IE8 y otros navegadores arcaicos. Compatible para inputs y textareas
	// Codigo original: http://stackoverflow.com/a/3373056

	var start = 0, end = 0, normalizedValue, range, textInputRange, len, endRange;

	range = document.selection.createRange();

	if (range && range.parentElement() == el) {
		len = el.value.length;
		normalizedValue = el.value.replace(/\r\n/g, "\n");

		// Create a working TextRange that lives only in the input
		textInputRange = el.createTextRange();
		textInputRange.moveToBookmark(range.getBookmark());

		// Check if the start and end of the selection are at the very end
		// of the input, since moveStart/moveEnd doesn't return what we want
		// in those cases
		endRange = el.createTextRange();
		endRange.collapse(false);

		if (textInputRange.compareEndPoints("StartToEnd", endRange) > -1) {
			start = end = len;
		} else {
			start = -textInputRange.moveStart("character", -len);
			start += normalizedValue.slice(0, start).split("\n").length - 1;

			if (textInputRange.compareEndPoints("EndToEnd", endRange) > -1) {
				end = len;
			} else {
				end = -textInputRange.moveEnd("character", -len);
				end += normalizedValue.slice(0, end).split("\n").length - 1;
			}
		}
	}

	retorno.start = start;
	retorno.end = end;

	return retorno;
}

/*************************** INICIO: GRID ****************************/
var idsGrids = [];

//Para que el boton Exportar del RadGrid provoque postback:
function onGridExport(sender, args) {
    if (args.get_eventTarget().indexOf("ExportToExcelButton") >= 0) {
        args.set_enableAjax(false);
    }
}

// Para controlar la visibilidad de los filtros de los grids al pinchar en la ribbon
function toggleFilterItemVisibility() {
    for (var i = 0; i < idsGrids.length; i++) {
        var gridClientId = idsGrids[i];

        var filtroVisible = $find(gridClientId).get_masterTableView().get_isFilterItemVisible();
        if (filtroVisible) hideFilterItem(gridClientId);
        else showFilterItem(gridClientId);

        //Comprobamos si existe el hidden que indica si mostramos o no los filtros por defecto y actualizamos su valor
        var hShowFilterDefault = $("[id^='" + gridClientId + "'][id$='hShowFilterDefault']");
        if (hShowFilterDefault != null) {
            hShowFilterDefault.val(!filtroVisible);
        }
    }
}
function showFilterItem(gridClientId) {
    $find(gridClientId).get_masterTableView().showFilterItem();
}
function hideFilterItem(gridClientId) {
    $find(gridClientId).get_masterTableView().hideFilterItem();
}

function toggleAdvancedFiltersVisibility() {
    var divFiltrosAvanzados = $("[id$='divFiltrosAvanzados']");
    var hFiltrosAvanzadosVisibles = $("[id$='hFiltrosAvanzadosVisibles']");
    if (divFiltrosAvanzados != null && hFiltrosAvanzadosVisibles != null) {
        divFiltrosAvanzados.toggle();
        hFiltrosAvanzadosVisibles.val(divFiltrosAvanzados.is(":visible"));
    }
}

//Para hacer que el doble click sea equivalente a pulsar el boton editar
function gridRowDblClick(sender, eventArgs) {
    var index = eventArgs.get_itemIndexHierarchical();
    if (sender) {
        var MasterTable = sender.get_masterTableView();
        MasterTable.fireCommand("Edit", index);
    }
}

//Para hacer que el doble click redirija a una URL sin hacer un post


var gridsRedirectEdit = [];
function meterRedireccionEditRadGrid(gridClientId, urlRedireccion) {
    var encontrado = false;
    for (var i = 0; i < gridsRedirectEdit.length; i++) {
        if (gridsRedirectEdit[i].idGrid === gridClientId) {
            encontrado = true;
        }
    }
    if (!encontrado) {
        var redireccion = {
            idGrid: gridClientId,
            url: urlRedireccion
        };
        gridsRedirectEdit.push(redireccion);
    }
}
function gridRowDblClickLink(sender, eventArgs) {
    var index = eventArgs.get_itemIndexHierarchical();
    if (sender) {
        var gridClientId = sender.get_id();
        var urlRedireccion = '';
        for (var i = 0; i < gridsRedirectEdit.length; i++) {
            if (gridsRedirectEdit[i].idGrid === gridClientId) {
                urlRedireccion = gridsRedirectEdit[i].url;
            }
        }
        if (urlRedireccion != '') {
            var MasterTable = sender.get_masterTableView();
            var row = MasterTable.get_dataItems()[index];
            var valorPK = '' + row.getDataKeyValue("ValorPK") + '';
            urlRedireccion = urlRedireccion.replace('@ValorPK@', valorPK);
            mostrarDivCargando();
            $(location).attr('href', urlRedireccion);
        } else {
            gridRowDblClick(sender, eventArgs);
        }
    }
}

// Esta funcion es cuando diferentes registros tienen diferentes URLs
var gridsRedirectEditMultiple = [];
function meterRedireccionEditRadGridMultiple(gridClientId, valoresPK, urlsEdicion) {
    var elementosURL = [];
    var elementosPK = [];
    for (var i = 0; i < valoresPK.length; i++) {
        elementosPK.push(valoresPK[i]);
        elementosURL.push(urlsEdicion[i]);
    }
    var encontrado = false;
    for (var i = 0; i < gridsRedirectEditMultiple.length; i++) {
        if (gridsRedirectEditMultiple[i].idGrid === gridClientId) {
            encontrado = true;
            gridsRedirectEditMultiple[i].pks = elementosPK;
            gridsRedirectEditMultiple[i].urls = elementosURL;
        }
    }
    if (!encontrado) {
        var redireccion = {
            idGrid: gridClientId,
            pks: elementosPK,
            urls: elementosURL
        };
        gridsRedirectEditMultiple.push(redireccion);
    }
}
function gridRowDblClickLinkMultiple(sender, eventArgs) {
    if (sender) {
        var gridClientId = sender.get_id();
        var MasterTable = sender.get_masterTableView();
        var index = eventArgs.get_itemIndexHierarchical();
        var row = MasterTable.get_dataItems()[index];
        var valorPK = '' + row.getDataKeyValue("ValorPK") + '';
        var urlRedireccion = '';
        for (var i = 0; i < gridsRedirectEditMultiple.length; i++) {
            if (gridsRedirectEditMultiple[i].idGrid === gridClientId) {
                for (var j = 0; j < gridsRedirectEditMultiple[i].pks.length; j++) {
                    if (gridsRedirectEditMultiple[i].pks[j] === valorPK) {
                        urlRedireccion = gridsRedirectEditMultiple[i].urls[j];
                    }
                }
            }
        }
        if (urlRedireccion != '') {
            mostrarDivCargando();
            $(location).attr('href', urlRedireccion);
        }
    }
}

// Para controlar los checkboxes de seleccion
// gridClientId es el clientID del RadGrid
// chkTodosId es el clientID del checkbox del header del grid. Usado para desmarcar el check de Todos si el checkbox del row esta desmarcado
// ihClientId es el clientID del input hidden en el cual almacenamos los PK de los checkbox seleccionados en el grid (anteriormente solo tenia 'true')
// chkSeleccionID es el clientID del checkbox de la row
// index es el itemIndex del Row del RadGrid
function chkSeleccionClicked(gridClientId, chkTodosId, chkSeleccionId, ihClientId, index, delimitadorPKs) {
    var grid = $find(gridClientId);
    var idGrid = grid.get_id();
    var MasterTable = grid.get_masterTableView();
    var row = MasterTable.get_dataItems()[index];
    var chkSeleccion = row.findElement(chkSeleccionId);

    // Substituimos la logica que metia en el hCheckModificado(ihModificado) el valor 'true' y metemos en su logar los valorPK de los items seleccionados
    //$("[id^='" + idGrid + "'][id$='hCheckModificado']").val(true);

    // obtenermos el valorPK del item
    var valorPK = '' + row.getDataKeyValue("ValorPK") + ''; // Lo concatenamos con string.empty para hacer una conversion a string explicita y que la busqueda posterior no falle por tipo de dato diferente

    // obtenemos el array previo, si existe
    var ihValue = $('#' + ihClientId).val();
    var selectedPKs = [];
    if (ihValue != '') selectedPKs = ihValue.split(delimitadorPKs);
    // actualizamos el contenido del array
    var arrayIndex = $.inArray(valorPK, selectedPKs);
    if (chkSeleccion.checked) {
        if (arrayIndex == -1) {
            selectedPKs.push(valorPK);
        }
        row.set_selected(true);
    }
    else {
        if (arrayIndex != -1) {
            selectedPKs.splice(arrayIndex, 1);
        }
        row.set_selected(false);
        $("[id^='" + idGrid + "'][id$='" + chkTodosId + "']").prop('checked', false);
        $("[id^='" + idGrid + "'][id$='hCheckTodosModificado']").val(true);
    }
    // metemos de nuevo el array en el input hidden
    ihValue = '';
    for (var i = 0; i < selectedPKs.length; i++) {
        if (i > 0) ihValue += delimitadorPKs;
        ihValue += selectedPKs[i];
    }
    $('#' + ihClientId).val(ihValue);
}

// Funciones para seleccionar unicamente al pinchar en el checkbox del grid
function WrapSelectionClick(context, wrapper) {
    var fn = context._click;
    context._click = function () {
        var args = Array.prototype.slice.call(arguments);
        var origDelegate = function () { return fn.apply(context, args); };
        return wrapper.apply(context, [origDelegate].concat(args));
    };
}
function ourClick(origFn, e) {
    //console.log('click GridConsulta');
    var el = (e.target) ? e.target : e.srcElement;
    if (!el.tagName) return;
    if (el.tagName.toLowerCase() == 'input' && el.type.toLowerCase() == 'checkbox' && (el.id && el.id.indexOf('SelectCheckBox') != -1)) {
        origFn(e);
    }
}

//Funciones para hacer que ciertos comandos del grid provoquen postback
var arrComandosPost = [];
function addComandoPost(comando, gridClientID) {
	var encontrado = false;
	for (var i = 0; i < arrComandosPost.length; i++) {
		if (arrComandosPost[i].commandName === comando) {
			encontrado = true;
			break;
		}
	}
	if (!encontrado) {
		var nuevoItem = {
			commandName: comando,
			clientID: gridClientID
		};
		arrComandosPost.push(nuevoItem);
	}
}
function ComprobarComandoPostGrid(sender, args) {
    if (args._eventTarget) {
        if (args.get_eventTarget().indexOf("ExportToExcelButton") >= 0) {
            args.set_enableAjax(false);
        }
        else {
            for (var iArrayComandos = 0; iArrayComandos < arrComandosPost.length; iArrayComandos++) {
            	if (args.get_eventTarget().indexOf(arrComandosPost[iArrayComandos].commandName) >= 0) {
            		if (arrComandosPost[iArrayComandos].clientID) {
            			refrescarGrid(arrComandosPost[iArrayComandos].clientID);
            		}
            		args.set_enableAjax(false);
                }
            }
        }
    }
}

function secuestrarExportExcels() {
    for (var i = 0; i < idsGrids.length; i++) {
        var gridClientID = idsGrids[i];
        $("[id^='" + gridClientID + "'][id$='ExportToExcelButton']").attr('onclick', "secuestrarExportExcel('" + gridClientID + "'); return false;");
    }
}

function secuestrarExportExcel(gridID) {
    // No empleamos la exportacion de cliente de Telerik ya que falla cuando no esta encapsulada en una peticion de un RadAjaxManager
    /*
    var masterTable = $find(gridID).get_masterTableView();
    masterTable.exportToExcel();
    */
    var ihExportar = $("#ctl00_ihExportarGrid");
    var botonExportar = $("#ctl00_btnExportarGrid");
    ihExportar.val(gridID);
    botonExportar.click();
}

function gridCreated(sender, args) {
    //Añadimos el id del grid a la lista de grids
	var idGrid = sender.get_id();
    if ($.inArray(idGrid, idsGrids) === -1) idsGrids.push(idGrid);

    //console.log('gridCreated: ' + idGrid);

    //Miramos si queremos ocultar los filtros por defecto
    var hShowFilterDefault = $("[id^='" + idGrid + "'][id$='hShowFilterDefault']");
    if (hShowFilterDefault != null && hShowFilterDefault.val() == 'false') {
        hideFilterItem(idGrid);
        //console.log('Ocultamos filtros y actualizamos el hidden');
    }

    //Miramos si el filtro de busqueda avanzada debe estar visible o no (se pierde en cada postback)
    var divFiltrosAvanzados = $("[id$='divFiltrosAvanzados']");
    var hFiltrosAvanzadosVisibles = $("[id$='hFiltrosAvanzadosVisibles']");
    if (divFiltrosAvanzados != null && hFiltrosAvanzadosVisibles != null) {
        if (hFiltrosAvanzadosVisibles.val() == 'false')
            divFiltrosAvanzados.hide();
        else divFiltrosAvanzados.show();
    }

    //Limpiamos el check de control de seleccion.
    var hCheckTodosModificado = $("[id^='" + idGrid + "'][id$='hCheckTodosModificado']");
    if (hCheckTodosModificado != null) hCheckTodosModificado.val('');

    // Interceptamos los eventos a los cuales queremos incorporar logica especifica
    WrapSelectionClick(sender._selection, ourClick);

    setTimeout(function () {
        var grid = $find(sender.get_id());
        var master = grid.get_masterTableView();
        //If you do not want to disable drag-to-select, comment this line out
        $clearHandlers(master.get_element().tBodies[0]);
    }, 0);
}

function gridMasterTableCreated() {
	// reasignamos el valor de los filtros que telerik pone por defecto
	// El gridCreated parece ser demasiado pronto
	booleanFilter_setCombos();
	telerikFilter_setValues();
}

function gridCreatedEditable(sender, args) {
    //Añadimos el id del grid a la lista de grids
	var idGrid = sender.get_id();
    if ($.inArray(idGrid, idsGrids) === -1) idsGrids.push(idGrid);

    //Miramos si queremos ocultar los filtros por defecto
    var hShowFilterDefault = $("[id^='" + idGrid + "'][id$='hShowFilterDefault']");
    if (hShowFilterDefault != null && hShowFilterDefault.val() == 'false') {
        hideFilterItem(idGrid);
        hShowFilterDefault.val(true);
        //console.log('Ocultamos filtros y actualizamos el hidden');
    }
}

function gridEditableOnCommand(sender, args) {
    var commandName = args.get_commandName();
    if (commandName == "InitInsert") {
        var masterTable = sender.get_masterTableView();
        var insertItem = masterTable.get_insertItem();
        if (insertItem) {
            //console.log('cancelando nuevo insert');
            args.set_cancel(true);
            //console.log('insert');
            masterTable.fireCommand("PerformInsert", "");
        }
    }
}

function insertarGridItem(idGrid, indiceFila) {
    var grid = $find(sender.get_id());
    var master = grid.get_masterTableView();
    masterTable.fireCommand("PerformInsert", "");
}

function refrescarGrid(idGrid) {
    var grid = $find(idGrid);
    if (grid) {
        var masterTable = grid.get_masterTableView();
        masterTable.fireCommand("Rebind", "");
    }
}

function editRowOnClick(sender, eventArgs) {
    var newRowIndex = eventArgs.get_itemIndexHierarchical();
    var masterTable = sender.get_masterTableView();

    var editedItemsArray = masterTable.get_editItems();
    var insertItem = masterTable.get_insertItem();

    if (insertItem) {
        //console.log('insert');
        masterTable.fireCommand("PerformInsert", "");
    }
    else {
        if (editedItemsArray.length > 0) {
            //console.log('lanzando update');
            var previousIndex = editedItemsArray[0].get_itemIndex();
            masterTable.fireCommand("Update", previousIndex);
        } else {
            //console.log('lanzando edit');
            masterTable.editItem(newRowIndex);
        }
    }
}

// filtros de GridConsulta de las columnas booleanas (en pantalla son combos)
var booleanFilterComboValues = [];

function booleanFilter_addInit(clientID, valorCombo) {
	var encontrado = false;
	for (var i = 0; i < booleanFilterComboValues.length; i++) {
		if (booleanFilterComboValues[i].clientid === clientID) {
			booleanFilterComboValues[i].valorcombo = valorCombo;
		}
	}
	if (!encontrado) {
		var config = {
			clientid: clientID,
			valorcombo: valorCombo
		};
		booleanFilterComboValues.push(config);
	}
}

// Esta funcion, si se llama csin parametros asigna todos los filtros de todos los grids. Si se pasa un gridClientID asigna todos filtros de telerik de dicho grid
function booleanFilter_setCombos(gridClientID) {
	for (var i = 0; i < booleanFilterComboValues.length; i++) {
		if (gridClientID == null || gridClientID === booleanFilterComboValues[i].clientid) {
			booleanFilter_setFilterValue(booleanFilterComboValues[i].clientid, booleanFilterComboValues[i].valorcombo);
		}
	}
}

function booleanFilter_partialPostBackInit() {
	Sys.Application.remove_load(booleanFilter_partialPostBackInit);
	booleanFilter_setCombos();
}

// filtros propios de Telerik

var telerikFilterValues = [];

function telerikFilter_addInit(clientID, uniqueName, valorFiltro, filterFunction) {
	var encontrado = false;
	for (var i = 0; i < telerikFilterValues.length; i++) {
		if (telerikFilterValues[i].clientid === clientID && telerikFilterValues[i].uniquename === uniqueName) {
			encontrado = true;
			telerikFilterValues[i].valorfiltro = valorFiltro;
			telerikFilterValues[i].filterfunction = filterFunction;
		}
	}
	if (!encontrado) {
		var config = {
			clientid: clientID,
			uniquename: uniqueName,
			valorfiltro: valorFiltro,
			filterfunction: filterFunction
		};
		telerikFilterValues.push(config);
	}
}

function filtrarGridSinPostBack(clientID, uniqueName, valor, filterFunction) {
	// enumeracion y parametros de funcion filter: http://www.telerik.com/help/aspnet-ajax/grid-gridtableview-filter.html
	var grid = $find(clientID);
	if (grid != null) {
		var masterTable = grid.get_masterTableView();
		if (masterTable != null) {
			var filtro = obtenerTelerikFilterFunction(filterFunction);
			cancelarProximoPostAjax = true;
			masterTable.filter(uniqueName, valor, filtro, false); // El ultimo parametro indica si ademas de filtrar se asigna el valor al control del filtro.Si no se especifica por defecto vale false. Telerik(t)	
			setTimeout(function () {
				cancelarProximoPostAjax = false;
			}, 200);
		}
	}
}
function obtenerTelerikFilterFunction(filterFunction) {
	var retorno = null;
	switch (filterFunction) {
		case 'NoFilter':
			retorno = Telerik.Web.UI.GridFilterFunction.NoFilter;
			break;
		case 'Contains':
			retorno = Telerik.Web.UI.GridFilterFunction.Contains;
			break;
		case 'DoesNotContain':
			retorno = Telerik.Web.UI.GridFilterFunction.DoesNotContain;
			break;
		case 'StartsWith':
			retorno = Telerik.Web.UI.GridFilterFunction.StartsWith;
			break;
		case 'EndsWith':
			retorno = Telerik.Web.UI.GridFilterFunction.EndsWith;
			break;
		case 'EqualTo':
			retorno = Telerik.Web.UI.GridFilterFunction.EqualTo;
			break;
		case 'NotEqualTo':
			retorno = Telerik.Web.UI.GridFilterFunction.NotEqualTo;
			break;
		case 'GreaterThan':
			retorno = Telerik.Web.UI.GridFilterFunction.GreaterThan;
			break;
		case 'LessThan':
			retorno = Telerik.Web.UI.GridFilterFunction.LessThan;
			break;
		case 'GreaterThanOrEqualTo':
			retorno = Telerik.Web.UI.GridFilterFunction.GreaterThanOrEqualTo;
			break;
		case 'LessThanOrEqualTo':
			retorno = Telerik.Web.UI.GridFilterFunction.LessThanOrEqualTo;
			break;
		case 'Between':
			retorno = Telerik.Web.UI.GridFilterFunction.Between;
			break;
		case 'NotBetween':
			retorno = Telerik.Web.UI.GridFilterFunction.NotBetween;
			break;
		case 'IsEmpty':
			retorno = Telerik.Web.UI.GridFilterFunction.IsEmpty;
			break;
		case 'NotIsEmpty':
			retorno = Telerik.Web.UI.GridFilterFunction.NotIsEmpty;
			break;
		case 'IsNull':
			retorno = Telerik.Web.UI.GridFilterFunction.IsNull;
			break;
		case 'NotIsNull':
			retorno = Telerik.Web.UI.GridFilterFunction.NotIsNull;
			break;
		case 'Custom':
			retorno = Telerik.Web.UI.GridFilterFunction.Custom;
			break;
		default:
			retorno = Telerik.Web.UI.GridFilterFunction.NoFilter;
			break;
	}
	return retorno;
}

function telerikFilter_setValues() {
	for (var i = 0; i < telerikFilterValues.length; i++) {
		//console.log('telerikFilter_setValues, estableciendo valor filtro: ' + telerikFilterValues[i].valorfiltro);
		filtrarGridSinPostBack(telerikFilterValues[i].clientid, telerikFilterValues[i].uniquename, telerikFilterValues[i].valorfiltro, telerikFilterValues[i].filterfunction);
	}
}

function telerikFilter_partialPostBackInit() {
	Sys.Application.remove_load(telerikFilter_partialPostBackInit);
	telerikFilter_setValues();
}

// Comun filtros, reasignacion despues de postbacks parciales
$(window).ready(
	function () {
		//Sys.Application.add_load(booleanFilter_partialPostBackInit);
		//Sys.Application.add_load(telerikFilter_partialPostBackInit);
	}
);

/*************************** FIN: GRID ****************************/

/*************************** INICIO: RadWindow ****************************/
var infoVentanas = [];

function mostrarRadWindow(idVentana, idGrid, ordenpopup) {
    limpiezaArrayVentanas(idVentana, idGrid, true, ordenpopup);
    Sys.Application.add_load(mostrarRadWindows);
}

function cerrarRadWindow(idVentana, idGrid, ordenpopup) {
    limpiezaArrayVentanas(idVentana, idGrid, false, ordenpopup);
    Sys.Application.add_load(cerrarRadWindows);
}

function cerrarRadWindowAhora(idVentana, idGrid, ordenpopup) {
    limpiezaArrayVentanas(idVentana, idGrid, false, ordenpopup);
    $find(idVentana).close();
    if (idGrid) refrescarGrid(idGrid);
}

function radWindowClose(sender, args) {
    var idVentana = sender.get_id();
    for (var iArrayVentana = 0; iArrayVentana < infoVentanas.length; iArrayVentana++) {
        if (infoVentanas[iArrayVentana].ventanaClientID == idVentana) {
            var idGrid = infoVentanas[iArrayVentana].gridClientID;
            cerrarRadWindowAhora(idVentana, idGrid, infoVentanas[iArrayVentana].orden);
        }
    }
}

function centrarRadWindow(idVentana) {
    var ventana = $find(idVentana);
    if (ventana != null)
        ventana.center();
}

function limpiezaArrayVentanas(idVentana, idGrid, mostrar, ordenPopup) {
    var encontradoEnArray = false;
    var posEncontrado = 0;
    for (var iArrayVentana = 0; iArrayVentana < infoVentanas.length; iArrayVentana++) {
        if (infoVentanas[iArrayVentana].ventanaClientID == idVentana) {
            posEncontrado = iArrayVentana;
            encontradoEnArray = true;
        }
    }
    if (!encontradoEnArray) {
        var nuevaVentana = {
            ventanaClientID: idVentana,
            gridClientID: idGrid,
            mostrar: mostrar,
            orden: ordenPopup
        };
        infoVentanas.push(nuevaVentana);
    }
    else {
        infoVentanas[posEncontrado].mostrar = mostrar;
        infoVentanas[posEncontrado].orden = ordenPopup;
    }
    if (ordenPopup != 0) {
    	infoVentanas.sort(
			function (a, b) {
       			return (a.orden > b.orden) ? 1 : -1;
       		}
		);
    }
}

function mostrarRadWindows() {
	for (var iArrayVentana = 0; iArrayVentana < infoVentanas.length; iArrayVentana++) {
		var ventana = $find(infoVentanas[iArrayVentana].ventanaClientID);
		if (ventana) {
            if (infoVentanas[iArrayVentana].mostrar == true) {
                ventana.show();
                CentrarPopup(infoVentanas[iArrayVentana].ventanaClientID);
            }
			else ventana.hide();
		}
    }
    Sys.Application.remove_load(mostrarRadWindows);
}

function CentrarPopup(clientId) {
    // CONTROL: en caso de que se ejecute la función sin esos elementos se evita cualquier error de JS que pare el resto de acciones
    if (document.querySelector('[id*="' + clientId + '"]') != null && document.querySelector('.TelerikModalOverlay[unselectable]') != null) {
        // asignamos los elementos de la pantalla
        wnmdl = document.querySelector('[id*="' + clientId + '"]');
        if (wnmdl != null) {
            wnmdlH = wnmdl.querySelector('table.rwTable');
            mldBG = document.querySelector('.TelerikModalOverlay[unselectable]');

            if (wnmdlH != null && mldBG != null) {
                // configuramos la ventana modal para ajustarlo en la pantalla:
                //  - Asignamos la altura de la ventana +/- al 80% de la pantalla
                wnmdl.style.height = (mldBG.offsetHeight * .8) + 'px';
                //  - Calculamos la posición de la ventana para centrarla en el contenido
                wnmdl.style.top = (((mldBG.offsetHeight - wnmdlH.offsetHeight) / 2) - 28) + 'px'; //28 es la altura aprox de la barra sup de la ventana, subimos un poco la mitad para ajustar un poco la altura.
            }
        }
    }
}

function cerrarRadWindows() {
    for (var iArrayVentana = 0; iArrayVentana < infoVentanas.length; iArrayVentana++) {
    	if (infoVentanas[iArrayVentana].mostrar == false) {
    		var ventanaCerrar = $find(infoVentanas[iArrayVentana].ventanaClientID);
    		if (ventanaCerrar) ventanaCerrar.close();
    	}
    }
    Sys.Application.remove_load(cerrarRadWindows);
}

//Para evitar que el pop-up de perfiles tenga una altura que se salga de la pantalla:
function rwInicializar(sender, eventArgs) {
    var prov = document.getElementsByClassName('rwWindowContent');
    var td = prov[0];
    td.className += " rwWindowAltuegi";
}
/*************************** FIN: RadWindow ****************************/

/*************************** INICIO: Button ****************************/
function confirmarBoton(button, mensaje, titulo, ancho, alto) {
    function callbackconfirmarBoton(arg) {
        if (arg) {
            __doPostBack(button.name, "");
        }
    }
    radconfirm(mensaje, callbackconfirmarBoton, ancho, alto, null, titulo);
}
/*************************** FIN: Button ****************************/

/**************************** INICIO: GRID BATCH **************************/
function ObtenerValorFloatControl(row, nombreControl) {
    var controlRow = row.findControl(nombreControl);
    if (controlRow != null) {
        return parseFloat(controlRow.get_value()) || 0;
    }
    else {
        controlRow = row.findElement(nombreControl);
        if (controlRow != null) {
            //Si el numero es 1.000,00 lo transformamos en 1000.00
        	if (controlRow.value != null) {
        	    return parseFloat(controlRow.value.replace('.', '').replace(',', '.')) || 0;
        	} else {
                return parseFloat(controlRow.innerHTML.replace('.', '').replace(',', '.')) || 0;
        	}
        }
        else return 0;
    }
}

function CalcularTotalFloat(gridClientID, nombreControl, nombreFooter) {
    CalcularTotalFloatFormateado(gridClientID, nombreControl, nombreFooter, "")
}

function CalcularTotalFloatFormateado(gridClientID, nombreControl, nombreFooter, formato) {
    var grid = $find(gridClientID);
    var totalColumna = 0;

    if (grid) {
        var MasterTable = grid.get_masterTableView();
        var Rows = MasterTable.get_dataItems();
        for (var i = 0; i < Rows.length; i++) {
            var row = Rows[i];
            totalColumna = totalColumna + ObtenerValorFloatControl(row, nombreControl);
        }

        //Buscamos el total de los registros de otras paginas:
        var controlFooterOtrasPaginas = $("[id$='" + nombreFooter + "_pag']");
        var totalOtrasPaginas = 0;
        if (controlFooterOtrasPaginas.val() != undefined) {
            totalOtrasPaginas = parseFloat(controlFooterOtrasPaginas.val().replace('.', '').replace(',', '.'));
        }
        totalColumna = totalColumna + totalOtrasPaginas;

        //Alternativa selector (mirando el start y el end): $('[id ^=gridClientID][id $=nombreFooter]')
        var controlFooter = $("[id$='" + nombreFooter + "']");
        controlFooter.text(formatNumber(totalColumna, formato));
    }
}

// Para controlar los diferentes totales de fila
var totalesFila = [];
function meterTotalFila(pGridClientID, pIdTotal, pNombreControlTotalFila, pNombreControlTotalColumna) {
    var encontradoEnArray = false;
    var posEncontrado = 0;
    for (var iArray = 0; iArray < totalesFila.length; iArray++) {
        if (totalesFila[iArray].gridClientID == pGridClientID &&
            totalesFila[iArray].idTotal == pIdTotal &&
            totalesFila[iArray].nombreControlTotalFila == pNombreControlTotalFila &&
            totalesFila[iArray].nombreControlTotalColumna == pNombreControlTotalColumna) {
            posEncontrado = iArray;
            encontradoEnArray = true;
        }
    }

    if (!encontradoEnArray) {
        var totalFila = {
            gridClientID: pGridClientID,
            idTotal: pIdTotal,
            nombreControlTotalFila: pNombreControlTotalFila,
            nombreControlTotalColumna: pNombreControlTotalColumna
        };
        totalesFila.push(totalFila);
    }
}
// Para controlar que controles de la fila del grid se deben contabilizar en el total de fila
var controlesTotalFila = [];
function meterControlTotalFila(pGridClientID, pIdTotal, pFila, pIdControl) {
    var encontradoEnArray = false;
    var posEncontrado = 0;
    for (var iArray = 0; iArray < controlesTotalFila.length; iArray++) {
        if (controlesTotalFila[iArray].gridClientID == pGridClientID &&
            controlesTotalFila[iArray].fila == pFila &&
            controlesTotalFila[iArray].idTotal == pIdTotal &&
            controlesTotalFila[iArray].idControl == pIdControl) {
            posEncontrado = iArray;
            encontradoEnArray = true;
        }
    }

    if (!encontradoEnArray) {

        var controlTotalFila = {
            gridClientID: pGridClientID,
            fila: pFila,
            idTotal: pIdTotal,
            idControl: pIdControl
        };
        controlesTotalFila.push(controlTotalFila);
    }
}

function CalcularTotalesFila(gridClientID) {
    var grid = $find(gridClientID);

    if (grid) {
        var MasterTable = grid.get_masterTableView();
        var Rows = MasterTable.get_dataItems();
        for (var iNumTotal = 0; iNumTotal < totalesFila.length; iNumTotal++) {
			// Nos aseguramos que el totalFila se corresponde con el del grid que desencadena el evento.
        	if (totalesFila[iNumTotal].gridClientID === gridClientID) {
        		var totalColumna = 0;
        		for (var i = 0; i < Rows.length; i++) {
        			var totalFila = 0;
        			var row = Rows[i];
        			var idControlTotalFila = '';
        			//Recorremos los controles que se desean incluir en el total de fila y calculamos la suma de todos ellos
        			for (var iControl = 0; iControl < controlesTotalFila.length; iControl++) {
        				var temp = totalFila;
        				if (controlesTotalFila[iControl].gridClientID == gridClientID && controlesTotalFila[iControl].idTotal == totalesFila[iNumTotal].idTotal && controlesTotalFila[iControl].fila == i) {
        					totalFila = temp + ObtenerValorFloatControl(row, controlesTotalFila[iControl].idControl);
        					idControlTotalFila = totalesFila[iNumTotal].nombreControlTotalFila;
        				}
        			}

        			//Asignamos el total de la fila al control (usamos findElement en lugar de findControl porque es un label)
        			var controlTotalRow = row.findElement(idControlTotalFila);
        			if (controlTotalRow != null) {
        				controlTotalRow.innerHTML = formatNumber(totalFila, "");
        			}

        			totalColumna = totalColumna + totalFila;
        		}

        		//Actualizamos el total de la columna
        		var controlFooter = $("[id ^=" + gridClientID + "][id$='" + totalesFila[iNumTotal].nombreControlTotalColumna + "']");
        		if (controlFooter != null) {
        			controlFooter.text(formatNumber(totalColumna, ""));
        		}
        	}
        }
    }
}

function GridBatch_KeyDown(control, evt, tipo) {
    var caracteresPermitidos = [];
    var reemplazos = [];

    if (tipo == 1) {
        caracteresPermitidos = [
			8, 9, // retroceso, tab
			16, 17, 18, // Shift, Ctrl, Alt
			33, 34, 35, 36, // Re pag, Av pag, inicio, fin
			45, 46, // Insert, del
			48, 49, 50, 51, 52, 53, 54, 55, 56, 57, // del 0 al 9
			96, 97, 98, 99, 100, 101, 102, 103, 104, 105, // del 0 al 9 (KeyPad)
			110, 188, 190, // Punto del KeyPad, Coma, Punto
		];
        reemplazos[110] = ",";
        reemplazos[190] = ",";
    } else if (tipo == 2) {
        caracteresPermitidos = [
			8, 9, // retroceso, tab
			16, 17, 18, // Shift, Ctrl, Alt
			33, 34, 35, 36, // Re pag, Av pag, inicio, fin
			45, 46, // Insert, del
			48, 49, 50, 51, 52, 53, 54, 55, 56, 57, // del 0 al 9
			96, 97, 98, 99, 100, 101, 102, 103, 104, 105, // del 0 al 9 (KeyPad)
			110, 188, 190, // Punto del KeyPad, Coma, Punto
            109, 173, 189, //- (173 FF)
		];
        reemplazos[110] = ",";
        reemplazos[190] = ",";
    }

    var key = (evt.which) ? evt.which : evt.keyCode;
    var grid = $(control).closest("table");
    var fila = $(control).attr("data-pos").split(",")[0];
    var columna = $(control).attr("data-pos").split(",")[1];
    var desplazar = false;

    switch (key) {
        case 39: //Derecha
            columna++;
            desplazar = true;
            break;
        case 37: //Izquierda
            columna--;
            desplazar = true;
            break;
        case 38: //Arriba
            fila--;
            desplazar = true;
            break;
        case 40: //Abajo
            fila++;
            desplazar = true;
            break;
    }

    var controlTo = $("[data-pos='" + fila + "," + columna + "']", grid);

    if (desplazar && controlTo) {
        controlTo.focus();
        setTimeout(function () { controlTo.select(); }, 50);
        return true;
    }

    if (evt.ctrlKey) { // Para permitir copy-paste y Ctrl+Z
        return true;
    }

    // En caso de que el input sea de solo lectura se cancela todo keypress
    if ($(control).is('[readonly]')) {
        if (typeof evt.preventDefault === 'undefined') evt.returnValue = false; // IE8 no tiene preventDefault
        return false;
    }

    if (reemplazos.length > 0) {
        var realizarRemplazo = false;
        var caracterReemplazo = '';

        if (reemplazos[key]) {
            realizarRemplazo = true;
            caracterReemplazo = reemplazos[key];
        }
        if (realizarRemplazo) {
            replaceInputValue(control, caracterReemplazo);
            if (typeof evt.preventDefault === 'undefined') evt.returnValue = false;
            return false;
        }
    }

    if (caracteresPermitidos.length > 0) {
        var caracterPermitidoColumna = false;
        for (var i = 0; i < caracteresPermitidos.length; i++) {
            if (key == caracteresPermitidos[i]) caracterPermitidoColumna = true;
        }
        if (!caracterPermitidoColumna) {
            if (typeof evt.preventDefault === 'undefined') evt.returnValue = false;
            return false;
        }
    }
    //En caso de que sea un "-" tiene que estar en la primera posición y una única vez (salvo para los de tipo texto (3))
    if (tipo != 3 && (key == 109 || key == 173 || key == 189)) {
        if (control.selectionStart > 0 || (control.selectionStart > 0 && control.value.indexOf("-") >= 0)) return false;
    }

    return true;
}

function replaceInputValue(txt, inputLetter) {
	var posCaret = obtenerPosicionCaret(txt);
	var posicionCaret = posCaret.start;
	var startString = txt.value.slice(0, posCaret.start);
	var endString = txt.value.slice(posCaret.end, txt.value.length);
	txt.value = startString + inputLetter + endString;
	setCaretPosition(txt, posicionCaret + inputLetter.length);
}

function setCaretPosition(elem, caretPos) {
	if (elem != null) {
		if (elem.createTextRange) {
			var range = elem.createTextRange();
			range.move('character', caretPos);
			range.select();
		} else {
			if (elem.selectionStart) {
				elem.focus();
				elem.setSelectionRange(caretPos, caretPos);
			} else
				elem.focus();
		}
	}
}


function formatNumber(n, currency) {
	var extra = '';
	if (currency != null && currency != '') extra = ' ' + currency;
	return n.toFixed(2).replace(/(\d)(?=(\d{3})+\.)/g, "$1a").replace(".", ",").replace(/a/g, ".") + extra;
}

function formatNumberCell(n, forzarRedondeo, cantidadDecimales) {
	if (typeof cantidadDecimales != "undefined") {
		if (forzarRedondeo) {
			return n.toFixed(cantidadDecimales).replace('.', ',');
		} else {
			var contenidoStr = n.toString();
			if (contenidoStr.lastIndexOf('.') == -1) {
				return n.replace('.', ',');
			} else {
				var parteDecimal = contenidoStr.substring(contenidoStr.lastIndexOf('.') + 1);
				if (parteDecimal.length > cantidadDecimales) {
					return n.toFixed(cantidadDecimales).replace('.', ',');
				} else {
					return n.replace('.', ',');
				}
			}
		}
    } else if (typeof n == "number") {
        return n.toString().replace('.', ',');
    } else {
        return n.replace('.', ',');
    }
}

function GridBatchValidarTextBox(control, forzarRedondeo, cantidadDecimales) {
	if (control.readOnly) return;
	var contenido = control.value;
	if (contenido == '') return;
	if (contenido == ',') {
		control.value = '';
		return;
	}
	contenido = parseFloat(contenido.replace(',', '.'));
	var formateado = formatNumberCell(contenido, forzarRedondeo, cantidadDecimales);
	control.value = formateado;
}

function GridBatchMeterCalculoTotal(trigger, target, controles, param1, gridId, actualizarResumen, tipoProduccion) {
	$(trigger).on('change',
		function () {
			var sum = 0;
			$(trigger).each(
				function () {
					var valorDiccionario = this.id.substring(this.id.indexOf('Diccionario_') + 12, this.id.indexOf('Diccionario_') + 19);
					var controlColumna = controles + '[id*="' + valorDiccionario + '"]';
					var controlCoste = controles.replace('lblCoste_', 'lblCosteHora_') + '[id*="' + valorDiccionario + '"]';
					//console.log(controlCoste);
					var valorTb = $(this).val().replace('.', '').replace(',', '.');
					if ($.isNumeric(valorTb)) {
						var coste = $(controlCoste).text().replace(',', '.');
						//console.log('Valor coste ' + coste);
						var costeHora = parseFloat(coste);
						//console.log('Coste hora ' + costeHora);
						sum += parseFloat(valorTb) * costeHora;
						//console.log('Importe celda' + parseFloat(valorTb) * costeHora);
						$(controlColumna).text(parseFloat(valorTb) * costeHora);
					}
				}
			);
			var valor = sum;
			$(target).text(formatNumber(valor, '€'));
			if (gridId) {
				var tTotalesFilas = '[id^="' + gridId + '"][id $="lblTotal1Fila"]';
				var tTotalColumna = '[id^="' + gridId + '"][id $="lblTotal1Columna"]';
				var totalesFilas = $(tTotalesFilas);
				var totalColumna = $(tTotalColumna);
				var suma = 0;
				$(totalesFilas).each(function () {
					var valorTb = $(this).text().replace('€', '').replace('.', '').replace(',', '.');
					if ($.isNumeric(valorTb)) {
						suma += parseFloat(valorTb);
					}
				});
				totalColumna.text(formatNumber(suma, '€'));
			}
			if (actualizarResumen) {
				ActualizarDatosResumen(tipoProduccion);
			}
		}
	);
}

function GridBatchMeterCalculoTotalColumna(trigger, target, controls) {
	$(trigger).on('change',
		function () {
			var sum = 0;
			$(controls).each(
				function () {
					var valorTb = $(this).text().replace(',', '.');
					if ($.isNumeric(valorTb)) {
						sum += parseFloat(valorTb);
					}
				}
			);
			$(target).text(formatNumber(sum, '€'));
		}
	);

}

function GridBatchForzarCalculo(trigger) {
	$(trigger).change();
}

/**************************** FIN: GRID BATCH **************************/

function booleanFilter_setFilterValue(dropdownClientID, valor) {
	//var combo = $find(dropdownClientID);
	//var combo = $('#' + dropdownClientID);
	//var combo = document.getElementById(dropdownClientID);
	/*
	// Como metemos el combo en tiempo de ejecucion, no se encuentra tampoco en el array interno de Telerik de ComboBoxes
	var combo = null;
	for (var i = 0; i < Telerik.Web.UI.RadComboBox.ComboBoxes.length; i++) {
		if (Telerik.Web.UI.RadComboBox.ComboBoxes[i]._element) {
			var elementI = Telerik.Web.UI.RadComboBox.ComboBoxes[i]._element;
			if (elementI.id === dropdownClientID) {
				combo = Telerik.Web.UI.RadComboBox.ComboBoxes.radControls[i];
			}
		}
	}*/
	var combo = findAlternativo(dropdownClientID); // Este si encuentra el combo. Es mas paranoico que el $find de Telerik
	if (combo != null) {
		var item = combo.findItemByValue(valor);
		if (item) {
			cancelarProximoPostAjax = true;
			item.select();
		}
	}
}


function EntryAddedRemoved(sender, args) {
    setTimeout(function () { sender.closeDropDown(); }, 200);
}