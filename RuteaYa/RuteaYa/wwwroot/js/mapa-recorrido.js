(function () {
    'use strict';

    const contenedor = document.getElementById('mapa-viaje');
    if (!contenedor || !window.L) {
        return;
    }

    const CENTRO_CABA = [-34.6037, -58.3816];
    const ZOOM_INICIAL = 11;

    const mapa = L.map(contenedor, {
        scrollWheelZoom: false,
        zoomControl: true
    }).setView(CENTRO_CABA, ZOOM_INICIAL);

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a>'
    }).addTo(mapa);

    const capa = L.layerGroup().addTo(mapa);

    const escapar = (texto) => String(texto == null ? '' : texto)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');

    const crearIcono = (tipo, texto) => L.divIcon({
        className: '',
        html: `<span class="mapa-marcador mapa-marcador--${tipo}">${escapar(texto)}</span>`,
        iconSize: [30, 30],
        iconAnchor: [15, 15],
        popupAnchor: [0, -16]
    });

    const etiquetaDePunto = (indice, total) => {
        if (indice === 0) {
            return 'Origen';
        }

        if (indice === total - 1) {
            return 'Destino';
        }

        return `Parada ${indice}`;
    };

    const tipoDePunto = (indice, total) => {
        if (indice === 0) {
            return 'origen';
        }

        if (indice === total - 1) {
            return 'destino';
        }

        return 'parada';
    };

    const dibujar = (recorrido) => {
        if (!recorrido || !Array.isArray(recorrido.puntos) || recorrido.puntos.length === 0) {
            return;
        }

        capa.clearLayers();

        const puntos = recorrido.puntos;
        const coordenadas = puntos.map((punto) => [Number(punto.lat), Number(punto.lng)]);

        const trazado = Array.isArray(recorrido.geometria) && recorrido.geometria.length >= 2
            ? recorrido.geometria.map((punto) => [Number(punto.lat), Number(punto.lng)])
            : coordenadas;

        L.polyline(trazado, {
            color: '#0b132b',
            weight: 5,
            opacity: 0.85,
            lineJoin: 'round'
        }).addTo(capa);

        puntos.forEach((punto, indice) => {
            const total = puntos.length;
            const tipo = tipoDePunto(indice, total);
            const etiqueta = etiquetaDePunto(indice, total);

            const marcador = L.marker(coordenadas[indice], {
                icon: crearIcono(tipo, tipo === 'parada' ? String(indice) : '')
            }).addTo(capa);

            const direccion = escapar(punto.direccion || 'Sin dirección');
            marcador.bindPopup(`<strong>${etiqueta}</strong><br>${direccion}`);
        });

        mapa.fitBounds(L.latLngBounds(coordenadas), { padding: [28, 28] });
    };

    // El detalle del viaje trae el recorrido ya calculado por la API.
    const datos = document.getElementById('datos-recorrido');
    if (datos) {
        try {
            dibujar(JSON.parse(datos.textContent));
        } catch (error) {
            // Si el JSON no se puede leer, el mapa queda centrado en CABA.
        }
    }

    // La pantalla de creación pide el recorrido al presionar "Mostrar ruta".
    window.RuteaYaMapa = { dibujar };
})();
