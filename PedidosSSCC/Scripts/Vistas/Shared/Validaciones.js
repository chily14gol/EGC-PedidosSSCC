function validarNumero(input) {
    input.value = input.value.replace(/[^0-9]/g, '');
}

function validarImporte(input) {
    let v = input.value;

    // 1) Eliminar todo lo que no sea dígito, coma o signo menos
    v = v.replace(/[^0-9\-,]/g, '');

    // 2) Permitir un único signo menos y solo en primera posición
    //    Primero quitamos todos los '-'…
    v = v.replace(/-/g, '');
    //    …y si el usuario escribió uno al principio, lo restauramos
    if (input.value.startsWith('-')) {
        v = '-' + v;
    }

    // 3) Garantizar que solo haya una coma decimal
    let partes = v.split(',');
    if (partes.length > 2) {
        // juntamos todo después de la primera coma sin comas extra
        v = partes[0] + ',' + partes.slice(1).join('');
    }

    input.value = v;
}

function validarEmail(input) {
    var regexEmailStr = "^[a-zA-Z0-9._%+-]+\u0040[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$";
    var regexEmail = new RegExp(regexEmailStr);

    if (regexEmail.test(input.value.trim())) {
        input.style.borderColor = "green"; // Email válido (borde verde)
    } else {
        input.style.borderColor = "red"; // Email inválido (borde rojo)
    }
}

function validarCIF(input) {
    var cif = input.value.trim().toUpperCase();

    const inputCif = $('#cif');
    const errorSpan = $('#error-cif');

    // Patrón de validación
    let pattern = /^[A-HJNPQRSUVW]{1}[0-9]{7}[A-J0-9]{1}$/;
    if (!pattern.test(cif)) {
        inputCif.addClass('is-invalid');
        errorSpan.removeClass('d-none');
        return false; // Si no coincide con el formato, es inválido
    }

    let suma = 0;

    // 1️⃣ Sumar los dígitos de las posiciones pares
    suma += parseInt(cif[2]) + parseInt(cif[4]) + parseInt(cif[6]);

    // 2️⃣ Para los dígitos impares, multiplicar por 2 y sumar los dígitos del resultado
    for (let i = 1; i <= 7; i += 2) {
        let doble = (parseInt(cif[i]) * 2).toString();
        suma += doble.length > 1 ? parseInt(doble[0]) + parseInt(doble[1]) : parseInt(doble);
    }

    // 3️⃣ Obtener el dígito de control
    let digitoControlCalculado = (10 - (suma % 10)) % 10;
    let digitoControlReal = cif[8];

    // 4️⃣ Si el CIF empieza por N, P, Q, R, S o W, el control es una LETRA
    if (/[NPQRSW]/.test(cif[0])) {
        let letrasControl = "JABCDEFGHI"; // 1=A, 2=B, ..., 0=J
        return letrasControl[digitoControlCalculado] === digitoControlReal;
    }

    var res = (digitoControlCalculado.toString() === digitoControlReal);

    // Asegúrate de que inputCif es un objeto jQuery
    inputCif.removeClass('is-valid is-invalid');

    if (res) {
        inputCif.addClass('is-valid');
        errorSpan.addClass('d-none');
    } else {
        inputCif.addClass('is-invalid');
        errorSpan.removeClass('d-none');
    }
}

function formatNumber(value) {
    return parseFloat(value).toFixed(2).replace('.', ',').replace(/\B(?=(\d{3})+(?!\d))/g, ".");
}

function formatMoney(data) {
    // Validar que sea un número
    if (!data || isNaN(data)) return "0,00 €";

    // Convertir la entrada a float (quitando separadores erróneos si los hubiera)
    let number = parseFloat(data.toString().replace(/[^\d,.-]/g, '').replace(',', '.'));
    if (isNaN(number)) return "0,00 €";

    // Forzar dos decimales y separar la parte entera de la parte decimal
    let partes = number.toFixed(2).split('.');
    let parteEntera = partes[0];  // antes del punto
    let parteDecimal = partes[1]; // después del punto

    // Insertar puntos cada 3 dígitos en la parte entera
    // (desde la derecha hacia la izquierda, sin afectar decimales)
    parteEntera = parteEntera.replace(/\B(?=(\d{3})+(?!\d))/g, ".");

    // Juntar parte entera + coma + decimales + €
    return parteEntera + ',' + parteDecimal + ' €';
}

function formatPorcentaje(data) {
    // Validar que sea un número
    if (!data || isNaN(data)) return "0,00 %";

    // Convertir la entrada a float (quitando separadores erróneos si los hubiera)
    let number = parseFloat(data.toString().replace(/[^\d,.-]/g, '').replace(',', '.'));
    if (isNaN(number)) return "0,00 %";

    // Forzar dos decimales y separar la parte entera de la parte decimal
    let partes = number.toFixed(2).split('.');
    let parteEntera = partes[0];  // antes del punto
    let parteDecimal = partes[1]; // después del punto

    // Insertar puntos cada 3 dígitos en la parte entera
    // (desde la derecha hacia la izquierda, sin afectar decimales)
    parteEntera = parteEntera.replace(/\B(?=(\d{3})+(?!\d))/g, ".");

    // Juntar parte entera + coma + decimales + %
    return parteEntera + ',' + parteDecimal + ' %';
}

function formatoEuro(valor) {
    const num = parseFloat(valor);
    return !isNaN(num) ? `${num.toFixed(2)} €` : "—";
}

function formatDateToDDMMYYYY(dateStr) {
    if (!dateStr) return ""; // Si está vacío, devuelve una cadena vacía
    var date = new Date(dateStr);
    var day = ("0" + date.getDate()).slice(-2); // Asegura dos dígitos
    var month = ("0" + (date.getMonth() + 1)).slice(-2); // Meses van de 0 a 11
    var year = date.getFullYear();
    return `${day}/${month}/${year}`;
}

function formatDateToDDMMYYYY_Proveedores(dateStr) {
    if (!dateStr) return "";

    // Detectar y parsear el formato /Date(…)/
    const match = /\/Date\((\d+)\)\//.exec(dateStr);
    if (match) {
        const timestamp = parseInt(match[1]);
        const date = new Date(timestamp);
        const day = ("0" + date.getDate()).slice(-2);
        const month = ("0" + (date.getMonth() + 1)).slice(-2);
        const year = date.getFullYear();
        return `${day}/${month}/${year}`;
    }

    // Si no es formato .NET, intentamos parsear como fecha normal
    const date = new Date(dateStr);
    if (isNaN(date)) return "";

    const day = ("0" + date.getDate()).slice(-2);
    const month = ("0" + (date.getMonth() + 1)).slice(-2);
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
}

function parseDotNetDateToDDMMYYYY(dotNetDateStr) {
    if (!dotNetDateStr) return "";
    const match = /\/Date\((\d+)\)\//.exec(dotNetDateStr);
    if (!match) return "";

    const timestamp = parseInt(match[1]);
    const date = new Date(timestamp);
    const day = ("0" + date.getDate()).slice(-2);
    const month = ("0" + (date.getMonth() + 1)).slice(-2);
    const year = date.getFullYear();

    return `${year}-${month}-${day}`;
}

function formatDateInputForDateField(fecha) {
    if (!fecha) return '';
    const d = new Date(fecha);
    const dia = String(d.getDate()).padStart(2, '0');
    const mes = String(d.getMonth() + 1).padStart(2, '0');
    const anio = d.getFullYear();

    return `${anio}-${mes}-${dia}`; // yyyy-MM-dd
}

function toISODate(fecha) {
    if (!fecha) return null;
    const d = new Date(fecha);
    return d.toISOString(); // "2024-04-01T00:00:00.000Z"
}

function obtenerMensajeErrorAjax(xhr) {
    try {
        if (xhr.responseJSON?.message) {
            return xhr.responseJSON.message;
        } else if (xhr.responseJSON?.Message) {
            return xhr.responseJSON.Message;
        } else if (
            xhr.responseText &&
            xhr.getResponseHeader("Content-Type")?.includes("application/json")
        ) {
            const parsed = JSON.parse(xhr.responseText);
            return parsed.message || parsed.Message || "Error sin mensaje";
        } else if (xhr.responseText && !xhr.responseText.includes("<!DOCTYPE html>")) {
            return xhr.responseText;
        }
    } catch (e) {
        console.warn("Error al interpretar el error del servidor:", e);
    }

    return "Error desconocido del servidor.";
}

function obtenerMensajeErrorAjax(xhr, textStatus, errorThrown) {
    let mensaje = '';

    // 1) Intentar extraer campos JSON estándar
    try {
        const json = xhr.responseJSON;
        if (json) {
            mensaje =
                json.message ||
                json.Message ||
                json.error ||
                json.Error ||
                json.exceptionMessage ||
                json.ExceptionMessage ||
                '';
        }
        else if (xhr.responseText && xhr.getResponseHeader("Content-Type")?.includes("application/json")) {
            const parsed = JSON.parse(xhr.responseText);
            mensaje =
                parsed.message ||
                parsed.Message ||
                parsed.error ||
                parsed.Error ||
                parsed.exceptionMessage ||
                parsed.ExceptionMessage ||
                '';
        }
    } catch (e) {
        console.warn("Error parseando el JSON de error:", e);
    }

    // 2) Si no encontramos mensaje JSON, usar el texto plano (si no es HTML)
    if (!mensaje && xhr.responseText && !xhr.responseText.includes("<!DOCTYPE html>")) {
        mensaje = xhr.responseText;
    }

    // 3) Añadir el texto de errorThrown (ej. “Not Found”) si existe
    if (errorThrown) {
        mensaje = mensaje
            ? `${mensaje} — ${errorThrown}`
            : errorThrown;
    }

    // 4) Añadir siempre el código HTTP y texto (“404 Not Found”, “500 Internal Server Error”…)
    if (xhr.status) {
        const statusInfo = `${xhr.status} ${xhr.statusText}`;
        mensaje = mensaje
            ? `${mensaje} (${statusInfo})`
            : statusInfo;
    }

    // 5) Si sigue vacío, mensaje genérico
    return mensaje || "Error desconocido del servidor.";
}

function validarCampo(idCampo, nombreCampo, camposInvalidos) {
    const valor = $(idCampo).val();
    if (!valor) {
        $(idCampo).addClass('is-invalid');
        camposInvalidos.push(nombreCampo);
    } else {
        $(idCampo).removeClass('is-invalid');
    }
}

function limpiarCampos(selectores) {
    selectores.forEach(selector => {
        $(selector).val('').removeClass('is-invalid');
    });
}

function resaltarFilaPorId(idFila) {
    const $fila = $(`#${idFila}`);
    if ($fila.length) {
        $fila.find('td').css('background-color', '#ffff99');
        setTimeout(() => {
            $fila.find('td').css('background-color', '');
        }, 1500);
        sessionStorage.removeItem("ultimaFilaEditada");
    }
}

function cargarComboGenerico(url, selector, valueField, textField, includeEmptyOption = false, emptyText = "Seleccione") {
    return new Promise((resolve, reject) => {
        $.ajax({
            url,
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                let $select = $(selector);
                $select.empty();
                if (includeEmptyOption) {
                    $select.append(`<option value="">${emptyText}</option>`);
                }
                data.forEach(item => {
                    $select.append(`<option value="${item[valueField]}">${item[textField]}</option>`);
                });
                resolve();
            },
            error: function (xhr) {
                const mensaje = obtenerMensajeErrorAjax(xhr);
                registrarErrorjQuery(xhr.status, mensaje);
                reject(new Error(mensaje));
            }
        });
    });
}

function rellenarCombo(selector, data, valueField, textField) {
    const $select = $(selector);
    $select.empty();
    $select.append(`<option value="">Seleccione</option>`);
    data.forEach(item => {
        $select.append(`<option value="${item[valueField]}">${item[textField]}</option>`);
    });
}

function tienePermiso(permisoId) {
    let permisos = sessionStorage.getItem("permisos");
    let lista = JSON.parse(permisos);
    return !lista.find(p => p.SPO_SOP_Id === permisoId && p.SPO_SPE_Id === 0);
}

function mostrarToast(mensaje, tipo) {
    Swal.fire({
        toast: true,
        position: 'top',
        icon: tipo,  // 'warning', 'error' o 'success'
        title: mensaje,
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true
    });
}

function htmlEscape(str) {
    return str
        .replace(/&/g, "&amp;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;");
}