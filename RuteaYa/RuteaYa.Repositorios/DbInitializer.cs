using BCrypt.Net;
using RuteaYa.Entidades;
using System;
using System.Collections.Generic;
using System.Text;

namespace RuteaYa.Repositorios
{
    public class DbInitializer
    {
        public static void Initialize(RuteaYaDbContext context)
        {
            // Asegura que la base de datos esté creada
            context.Database.EnsureCreated();

            // Normaliza envíos creados con el estado Pendiente, que ya no existe.
            NormalizarEnviosPendientes(context);

            //  evita duplicados: crea cada usuario demo solo si falta
            var semillas = new[]
            {
                (Nombre: "Admin", Apellido: "Sistema", Email: "admin@ruteaya.com"),
                (Nombre: "Carlos", Apellido: "González", Email: "carlos.gonzalez@example.com"),
                (Nombre: "Lucía", Apellido: "Martínez", Email: "lucia.martinez@example.com")
            };

            foreach (var semilla in semillas)
            {
                if (context.Usuarios.Any(u => u.Email == semilla.Email))
                    continue;

                context.Usuarios.Add(new Usuario
                {
                    Nombre = semilla.Nombre,
                    Apellido = semilla.Apellido,
                    Email = semilla.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234"),
                    CreadoEn = DateTime.Now
                });
            }

            context.SaveChanges();

            var usuarios = context.Usuarios
                .OrderBy(u => u.Id)
                .ToList();

            // Sin usuarios no se pueden crear viajes ni envíos.
            if (!usuarios.Any())
                return;

            var viajesDemo = new List<Viaje>();

            // Agrega un viaje solamente si todavía no existe.
            Viaje GuardarViaje(
                int usuarioId,
                string origen,
                decimal origenLat,
                decimal origenLng,
                string destino,
                decimal destinoLat,
                decimal destinoLng,
                TimeOnly salida,
                TimeOnly llegada,
                int maxPaquetes,
                decimal pesoMax,
                decimal desvioMax,
                decimal distancia,
                int duracion,
                byte[] dias,
                params (string Direccion, decimal Lat, decimal Lng)[] paradas)
            {
                var viaje = context.Viajes.FirstOrDefault(v =>
                    v.UsuarioId == usuarioId &&
                    v.OrigenDir == origen &&
                    v.DestinoDir == destino &&
                    v.HoraSalida == salida);

                if (viaje == null)
                {
                    viaje = new Viaje
                    {
                        UsuarioId = usuarioId,
                        OrigenDir = origen,
                        OrigenLat = origenLat,
                        OrigenLng = origenLng,
                        DestinoDir = destino,
                        DestinoLat = destinoLat,
                        DestinoLng = destinoLng,
                        HoraSalida = salida,
                        HoraLlegada = llegada,
                        MaxPaquetes = maxPaquetes,
                        PesoMaxKg = pesoMax,
                        DesvioMaxKm = desvioMax,
                        DistanciaKm = distancia,
                        DuracionMin = duracion,
                        Activo = true
                    };

                    context.Viajes.Add(viaje);
                    context.SaveChanges();
                }

                foreach (var dia in dias)
                {
                    if (!context.ViajeDias.Any(d =>
                        d.ViajeId == viaje.Id &&
                        d.DiaSemana == dia))
                    {
                        context.ViajeDias.Add(new ViajeDia
                        {
                            ViajeId = viaje.Id,
                            DiaSemana = dia
                        });
                    }
                }

                for (int i = 0; i < paradas.Length; i++)
                {
                    int orden = i + 1;

                    if (!context.Paradas.Any(p =>
                        p.ViajeId == viaje.Id &&
                        p.Orden == orden))
                    {
                        context.Paradas.Add(new Parada
                        {
                            ViajeId = viaje.Id,
                            Orden = orden,
                            Direccion = paradas[i].Direccion,
                            Lat = paradas[i].Lat,
                            Lng = paradas[i].Lng
                        });
                    }
                }

                context.SaveChanges();

                return viaje;
            }

            int UsuarioId(int indice)
            {
                return usuarios[indice % usuarios.Count].Id;
            }

            // -------------------------------------------------
            // VIAJES PARA PROBAR EL OPTIMIZADOR
            // -------------------------------------------------

            viajesDemo.Add(GuardarViaje(
                UsuarioId(0),
                "Av. Rivadavia 1234, CABA",
                -34.603722m, -58.381592m,
                "Av. Corrientes 5678, CABA",
                -34.599722m, -58.435833m,
                new TimeOnly(8, 0), new TimeOnly(12, 0),
                5, 50m, 5m, 15.5m, 45,
                new byte[] { 1, 3, 5 },
                ("Corrientes 1000, CABA", -34.6025m, -58.385m)));

            viajesDemo.Add(GuardarViaje(
                UsuarioId(1),
                "Calle Falsa 123, Morón",
                -34.6534m, -58.6192m,
                "Plaza de Mayo, CABA",
                -34.6083m, -58.3712m,
                new TimeOnly(9, 30), new TimeOnly(11, 0),
                3, 30m, 3.5m, 22m, 60,
                new byte[] { 2, 4, 6 },
                ("Gaona 2500, Morón", -34.625m, -58.47m)));

            viajesDemo.Add(GuardarViaje(
                UsuarioId(2),
                "Mitre 450, San Justo",
                -34.6851m, -58.5587m,
                "Cabildo 2000, CABA",
                -34.5627m, -58.459m,
                new TimeOnly(14, 0), new TimeOnly(16, 0),
                10, 100m, 8m, 18.2m, 50,
                new byte[] { 1, 3, 5 },
                ("Ruta 3 Km 28", -34.75m, -58.59m)));

            // Viaje desactivado: permite probar los botones Activar y Eliminar.
            var viajeDesactivado = GuardarViaje(
                UsuarioId(0),
                "Av. La Plata 1500, CABA",
                -34.6265m, -58.4263m,
                "Av. Directorio 3000, CABA",
                -34.6412m, -58.4789m,
                new TimeOnly(7, 15), new TimeOnly(8, 30),
                4, 25m, 2m, 9.4m, 35,
                new byte[] { 2, 4, 6 });

            if (viajeDesactivado.Activo)
            {
                viajeDesactivado.Activo = false;
                context.SaveChanges();
            }

            viajesDemo.Add(viajeDesactivado);

            viajesDemo.Add(GuardarViaje(
                UsuarioId(1),
                "Av. de Mayo 800, Ramos Mejía",
                -34.641m, -58.563m,
                "Av. Rivadavia 5000, CABA",
                -34.627m, -58.422m,
                new TimeOnly(8, 30), new TimeOnly(10, 0),
                8, 80m, 7m, 17m, 50,
                new byte[] { 1, 2, 3, 4, 5 },
                ("Av. Gaona 1200, Ramos Mejía", -34.646m, -58.55m),
                ("Av. Rivadavia 3000, CABA", -34.615m, -58.405m)));

            viajesDemo.Add(GuardarViaje(
                UsuarioId(2),
                "Av. Don Bosco 2200, San Justo",
                -34.677m, -58.56m,
                "Av. Santa Fe 2500, CABA",
                -34.595m, -58.405m,
                new TimeOnly(10, 0), new TimeOnly(12, 0),
                6, 60m, 5m, 20m, 65,
                new byte[] { 1, 3, 5, 6 },
                ("Av. Mosconi 1800, San Justo", -34.67m, -58.565m)));

            viajesDemo.Add(GuardarViaje(
                UsuarioId(0),
                "Av. Crovara 2500, La Tablada",
                -34.685m, -58.505m,
                "Av. Juan B. Justo 4500, CABA",
                -34.608m, -58.48m,
                new TimeOnly(13, 0), new TimeOnly(15, 0),
                12, 120m, 10m, 19m, 55,
                new byte[] { 1, 2, 3, 4, 5 },
                ("Av. San Martín 3000, La Tablada", -34.67m, -58.49m),
                ("Av. Gaona 4000, CABA", -34.615m, -58.49m)));

            viajesDemo.Add(GuardarViaje(
                UsuarioId(1),
                "Av. Cristianía 1800, Isidro Casanova",
                -34.704m, -58.59m,
                "Av. Rivadavia 11000, Liniers",
                -34.643m, -58.53m,
                new TimeOnly(16, 0), new TimeOnly(18, 0),
                4, 35m, 4m, 13m, 45,
                new byte[] { 2, 4, 6 },
                ("Av. Juan Manuel de Rosas 6000, Isidro Casanova", -34.69m, -58.58m)));

            // -------------------------------------------------
            // ENVÍOS PARA PROBAR OPORTUNIDADES Y ASIGNACIONES
            // -------------------------------------------------

            void GuardarEnvio(
                int usuarioId,
                string origen,
                decimal origenLat,
                decimal origenLng,
                string destino,
                decimal destinoLat,
                decimal destinoLng,
                DateTime fechaLimite,
                decimal peso,
                byte tamano,
                string descripcion,
                decimal monto,
                EstadoEnvio estado,
                int? indiceViaje = null,
                decimal desvio = 0m,
                int tiempoExtra = 0,
                decimal ganancia = 0m)
            {
                byte estadoActual = (byte)estado;

                var envio = context.Envios.FirstOrDefault(e =>
                    e.UsuarioId == usuarioId &&
                    e.OrigenDir == origen &&
                    e.DestinoDir == destino &&
                    e.Descripcion == descripcion);

                if (envio == null)
                {
                    envio = new Envio
                    {
                        UsuarioId = usuarioId,
                        OrigenDir = origen,
                        OrigenLat = origenLat,
                        OrigenLng = origenLng,
                        DestinoDir = destino,
                        DestinoLat = destinoLat,
                        DestinoLng = destinoLng,
                        FechaLimite = fechaLimite,
                        PesoKg = peso,
                        Tamano = tamano,
                        Descripcion = descripcion,
                        Monto = monto,
                        Estado = estadoActual,
                        CreadoEn = DateTime.Now
                    };

                    context.Envios.Add(envio);
                    context.SaveChanges();
                }

                // Si el envío todavía no tiene historial, crea uno.
                if (!context.EnvioEstadoHistoriales.Any(h =>
                    h.EnvioId == envio.Id))
                {
                    context.EnvioEstadoHistoriales.Add(
                        new EnvioEstadoHistorial
                        {
                            EnvioId = envio.Id,
                            EstadoAnterior = estadoActual,
                            EstadoNuevo = estadoActual,
                            Fecha = DateTime.Now
                        });
                }

                // Crea una asignación solamente para los envíos
                // que tienen un viaje indicado y no están cancelados
                // ni buscando conductor.
                if (indiceViaje.HasValue &&
                    estado != EstadoEnvio.BuscandoConductor &&
                    estado != EstadoEnvio.Cancelado &&
                    !context.Asignaciones.Any(a => a.EnvioId == envio.Id))
                {
                    var viaje = viajesDemo[indiceViaje.Value];

                    context.Asignaciones.Add(new Asignacion
                    {
                        EnvioId = envio.Id,
                        ViajeId = viaje.Id,
                        FechaViaje = fechaLimite.Date,
                        DesvioKm = desvio,
                        TiempoExtraMin = tiempoExtra,
                        Ganancia = ganancia,
                        AsignadoEn = DateTime.Now
                    });
                }

                context.SaveChanges();
            }

            // Envíos sin conductor: sirven para probar oportunidades.
            GuardarEnvio(
                UsuarioId(1),
                "Av. Santa Fe 1000, CABA", -34.598m, -58.388m,
                "Av. Pueyrredón 500, CABA", -34.602m, -58.405m,
                DateTime.Today.AddDays(2), 2.5m, 1,
                "Documentación importante", 1500m,
                EstadoEnvio.BuscandoConductor);

            GuardarEnvio(
                UsuarioId(2),
                "Suipacha 400, CABA", -34.604m, -58.379m,
                "Esmeralda 800, CABA", -34.601m, -58.378m,
                DateTime.Today.AddDays(3), 5m, 2,
                "Caja de repuestos", 3500m,
                EstadoEnvio.BuscandoConductor);

            GuardarEnvio(
                UsuarioId(0),
                "Av. Belgrano 900, CABA", -34.611m, -58.377m,
                "Av. Independencia 1800, CABA", -34.620m, -58.383m,
                DateTime.Today.AddDays(2), 3m, 1,
                "Caja de herramientas", 2200m,
                EstadoEnvio.BuscandoConductor);

            GuardarEnvio(
                UsuarioId(1),
                "Av. Córdoba 1200, CABA", -34.598m, -58.398m,
                "Av. San Juan 2400, CABA", -34.627m, -58.390m,
                DateTime.Today.AddDays(1), 1.5m, 1,
                "Sobre con documentación", 1800m,
                EstadoEnvio.BuscandoConductor);

            GuardarEnvio(
                UsuarioId(2),
                "Av. Las Heras 1500, CABA", -34.588m, -58.402m,
                "Av. Scalabrini Ortiz 2200, CABA", -34.583m, -58.426m,
                DateTime.Today.AddDays(2), 4m, 2,
                "Repuestos de bicicleta", 2800m,
                EstadoEnvio.BuscandoConductor);

            GuardarEnvio(
                UsuarioId(0),
                "Av. Callao 600, CABA", -34.603m, -58.395m,
                "Av. Belgrano 1500, CABA", -34.613m, -58.389m,
                DateTime.Today.AddDays(1), 2m, 1,
                "Caja de libros", 2100m,
                EstadoEnvio.BuscandoConductor);

            GuardarEnvio(
                UsuarioId(1),
                "Av. Rivadavia 1400, Ramos Mejía", -34.645m, -58.56m,
                "Av. Rivadavia 4200, CABA", -34.622m, -58.43m,
                DateTime.Today.AddDays(2), 8m, 2,
                "Paquete de indumentaria", 4200m,
                EstadoEnvio.BuscandoConductor);

            GuardarEnvio(
                UsuarioId(2),
                "Av. San Martín 2800, La Tablada", -34.68m, -58.51m,
                "Av. Juan B. Justo 4300, CABA", -34.61m, -58.48m,
                DateTime.Today.AddDays(3), 12m, 3,
                "Electrodoméstico pequeño", 6500m,
                EstadoEnvio.BuscandoConductor);

            GuardarEnvio(
                UsuarioId(0),
                "Av. Mosconi 1900, San Justo", -34.67m, -58.565m,
                "Av. Santa Fe 2400, CABA", -34.596m, -58.407m,
                DateTime.Today.AddDays(1), 6m, 2,
                "Caja de accesorios", 3200m,
                EstadoEnvio.BuscandoConductor);

            GuardarEnvio(
                UsuarioId(1),
                "Av. Cristianía 2000, Isidro Casanova", -34.703m, -58.59m,
                "Av. Rivadavia 10800, Liniers", -34.644m, -58.532m,
                DateTime.Today.AddDays(2), 3.5m, 1,
                "Repuestos electrónicos", 2900m,
                EstadoEnvio.BuscandoConductor);

            // Envíos con diferentes estados para probar el seguimiento.
            GuardarEnvio(
                UsuarioId(2),
                "Bolívar 200, CABA", -34.611m, -58.374m,
                "Defensa 700, CABA", -34.615m, -58.370m,
                DateTime.Today.AddDays(1), 1m, 1,
                "Sobre cerrado", 1200m,
                EstadoEnvio.ConductorAsignado,
                2, 0.5m, 5, 400m);

            GuardarEnvio(
                UsuarioId(0),
                "Av. Belgrano 1100, CABA", -34.612m, -58.377m,
                "Av. Independencia 1700, CABA", -34.619m, -58.382m,
                DateTime.Today.AddDays(1), 3m, 1,
                "Herramientas de trabajo", 2500m,
                EstadoEnvio.Retirado,
                0, 1m, 8, 700m);

            GuardarEnvio(
                UsuarioId(1),
                "Av. Rivadavia 3000, CABA", -34.615m, -58.405m,
                "Av. Corrientes 4000, CABA", -34.605m, -58.423m,
                DateTime.Today.AddDays(-1), 2m, 1,
                "Documentos entregados", 1800m,
                EstadoEnvio.Entregado,
                4, 0.8m, 6, 600m);

            GuardarEnvio(
                UsuarioId(2),
                "Av. Callao 500, CABA", -34.603m, -58.394m,
                "Av. Belgrano 1500, CABA", -34.613m, -58.389m,
                DateTime.Today.AddDays(1), 2m, 1,
                "Caja en camino", 2100m,
                EstadoEnvio.EnCamino,
                1, 1.5m, 12, 650m);

            GuardarEnvio(
                UsuarioId(0),
                "Av. Las Heras 1200, CABA", -34.59m, -58.402m,
                "Av. Santa Fe 2000, CABA", -34.595m, -58.405m,
                DateTime.Today.AddDays(2), 4m, 2,
                "Envío cancelado de prueba", 2800m,
                EstadoEnvio.Cancelado);
        }

        private static void NormalizarEnviosPendientes(
            RuteaYaDbContext context)
        {
            const byte estadoPendiente = 0;
            byte estadoBuscandoConductor =
                (byte)EstadoEnvio.BuscandoConductor;

            var envios = context.Envios
                .Where(e => e.Estado == estadoPendiente)
                .ToList();

            foreach (var envio in envios)
            {
                envio.Estado = estadoBuscandoConductor;
            }

            var historiales = context.EnvioEstadoHistoriales
                .Where(h =>
                    h.EstadoAnterior == estadoPendiente ||
                    h.EstadoNuevo == estadoPendiente)
                .ToList();

            foreach (var historial in historiales)
            {
                if (historial.EstadoAnterior == estadoPendiente)
                    historial.EstadoAnterior = estadoBuscandoConductor;

                if (historial.EstadoNuevo == estadoPendiente)
                    historial.EstadoNuevo = estadoBuscandoConductor;
            }

            if (envios.Count > 0 || historiales.Count > 0)
                context.SaveChanges();
        }
    }
}
