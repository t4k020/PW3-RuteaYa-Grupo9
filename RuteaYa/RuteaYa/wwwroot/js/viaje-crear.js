(function () {
    'use strict';

    const form = document.getElementById('form-viaje');
    if (!form) {
        return;
    }

    const contenedor = document.getElementById('paradas-container');
    const plantilla = document.getElementById('plantilla-parada');
    const btnAgregar = document.getElementById('btn-agregar-parada');
    const estadoVacio = document.getElementById('paradas-vacio');
    const contador = document.getElementById('contador-paradas');
    const rutaAviso = document.getElementById('ruta-aviso');

    // Aviso general de validación: se muestra solo al intentar guardar.
    const avisoValidacion = document.getElementById('aviso-validacion');

    const mostrarAvisoValidacion = () => {
        if (!avisoValidacion) {
            return;
        }

        avisoValidacion.classList.remove('d-none');
        avisoValidacion.scrollIntoView({ behavior: 'smooth', block: 'start' });
    };

    const jquery = window.jQuery;
    const validacionDisponible = jquery && jquery.validator;

    if (validacionDisponible) {
        // jQuery Validate dispara este evento únicamente al intentar enviar un formulario inválido.
        jquery(form).on('invalid-form.validate', mostrarAvisoValidacion);
    } else {
        form.addEventListener('submit', () => {
            if (!form.checkValidity()) {
                mostrarAvisoValidacion();
            }
        });
    }

    const obtenerFilas = () => Array.from(contenedor.querySelectorAll('[data-parada-row]'));

    const reparsearValidacion = () => {
        const unobtrusive = window.jQuery && window.jQuery.validator && window.jQuery.validator.unobtrusive;
        if (unobtrusive) {
            unobtrusive.parse(form);
        }
    };

    const actualizarEstado = () => {
        const filas = obtenerFilas();

        estadoVacio.classList.toggle('d-none', filas.length > 0);
        contador.textContent = String(filas.length);

        filas.forEach((fila, indice) => {
            const numero = fila.querySelector('.parada-numero');
            if (numero) {
                numero.textContent = String(indice + 1);
            }

            const input = fila.querySelector('[data-parada-direccion]');
            if (input) {
                input.name = `Paradas[${indice}].Direccion`;
                input.id = `Paradas_${indice}__Direccion`;
            }

            const mensaje = fila.querySelector('[data-valmsg-for]');
            if (mensaje) {
                mensaje.setAttribute('data-valmsg-for', `Paradas[${indice}].Direccion`);
            }

            const btnArriba = fila.querySelector('[data-mover-arriba]');
            const btnAbajo = fila.querySelector('[data-mover-abajo]');

            if (btnArriba) {
                btnArriba.disabled = indice === 0;
            }

            if (btnAbajo) {
                btnAbajo.disabled = indice === filas.length - 1;
            }
        });

        reparsearValidacion();
    };

    if (btnAgregar && plantilla && contenedor) {
        btnAgregar.addEventListener('click', () => {
            contenedor.appendChild(plantilla.content.cloneNode(true));
            actualizarEstado();

            const filas = obtenerFilas();
            const ultima = filas[filas.length - 1];
            const input = ultima && ultima.querySelector('[data-parada-direccion]');
            if (input) {
                input.focus();
            }
        });
    }

    if (contenedor) {
        contenedor.addEventListener('click', (evento) => {
            const boton = evento.target.closest('button');
            const fila = boton && boton.closest('[data-parada-row]');

            if (!boton || !fila) {
                return;
            }

            if (boton.hasAttribute('data-eliminar-parada')) {
                fila.remove();
            } else if (boton.hasAttribute('data-mover-arriba')) {
                const anterior = fila.previousElementSibling;
                if (anterior) {
                    contenedor.insertBefore(fila, anterior);
                }
            } else if (boton.hasAttribute('data-mover-abajo')) {
                const siguiente = fila.nextElementSibling;
                if (siguiente) {
                    contenedor.insertBefore(siguiente, fila);
                }
            } else {
                return;
            }

            actualizarEstado();
        });
    }

    const mostrarAvisoRuta = (mensaje, tipo) => {
        if (!rutaAviso) {
            return;
        }

        rutaAviso.textContent = mensaje;
        rutaAviso.classList.remove(
            'd-none',
            'alert-info',
            'alert-warning',
            'alert-danger');
        rutaAviso.classList.add(tipo || 'alert-info');
    };

    const leerMensajeDeError = async (respuesta) => {
        try {
            const problema = await respuesta.json();
            return problema.detail || problema.title || 'No se pudo calcular el recorrido.';
        } catch (error) {
            return 'No se pudo calcular el recorrido.';
        }
    };

    const contenedorMapa = document.getElementById('mapa-viaje');
    const urlPrevisualizar = contenedorMapa
        ? contenedorMapa.getAttribute('data-preview-url')
        : null;

    const calcularRuta = async (boton) => {
        if (!urlPrevisualizar) {
            return;
        }

        // Se validan los campos del formulario antes de gastar una llamada
        // al proveedor de rutas.
        const validador = window.jQuery && window.jQuery(form).data('validator');
        const formularioValido = validador
            ? window.jQuery(form).valid()
            : form.reportValidity();

        if (!formularioValido) {
            mostrarAvisoValidacion();
            return;
        }

        const textoOriginal = boton.textContent;
        boton.disabled = true;
        boton.textContent = 'Calculando...';
        mostrarAvisoRuta('Calculando el recorrido...');

        try {
            const respuesta = await fetch(urlPrevisualizar, {
                method: 'POST',
                body: new FormData(form),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (!respuesta.ok) {
                mostrarAvisoRuta(await leerMensajeDeError(respuesta), 'alert-danger');
                return;
            }

            const recorrido = await respuesta.json();

            if (window.RuteaYaMapa) {
                window.RuteaYaMapa.dibujar(recorrido);
            }

            const distancia = Number(recorrido.distanciaKm || 0).toFixed(1);
            const fuente = recorrido.fuente ? ` · Ruta: ${recorrido.fuente}` : '';

            mostrarAvisoRuta(
                `Recorrido calculado: ${distancia} km · ${recorrido.duracionMin} min${fuente}.`);
        } catch (error) {
            mostrarAvisoRuta('No se pudo conectar con el servidor.', 'alert-danger');
        } finally {
            boton.disabled = false;
            boton.textContent = textoOriginal;
        }
    };

    ['btn-mostrar-ruta', 'btn-actualizar-ruta'].forEach((id) => {
        const boton = document.getElementById(id);
        if (boton) {
            boton.addEventListener('click', () => calcularRuta(boton));
        }
    });

    actualizarEstado();
})();
