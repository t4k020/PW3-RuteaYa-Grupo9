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

            //  evita duplicados
            if (context.Usuarios.Any())
            {
                return;
            }

            var usuarios = new List<Usuario>
            {
                new Usuario
                {
                    Nombre = "Admin",
                    Apellido = "Sistema",
                    Email = "admin@ruteaya.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234"),
                    CreadoEn = DateTime.Now
                },
                new Usuario
                {
                    Nombre = "Carlos",
                    Apellido = "González",
                    Email = "carlos.gonzalez@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234"),
                    CreadoEn = DateTime.Now
                },
                new Usuario
                {
                    Nombre = "Lucía",
                    Apellido = "Martínez",
                    Email = "lucia.martinez@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234"),
                    CreadoEn = DateTime.Now
                }
            };

            context.Usuarios.AddRange(usuarios);
            context.SaveChanges();


            var viajes = new List<Viaje>
            {
                new Viaje
                {
                    UsuarioId = usuarios[0].Id, // Admin
                    OrigenDir = "Av. Rivadavia 1234, CABA",
                    OrigenLat = -34.603722m,
                    OrigenLng = -58.381592m,
                    DestinoDir = "Av. Corrientes 5678, CABA",
                    DestinoLat = -34.599722m,
                    DestinoLng = -58.435833m,
                    HoraSalida = new TimeOnly(8, 0),
                    HoraLlegada = new TimeOnly(12, 0),
                    MaxPaquetes = 5,
                    PesoMaxKg = 50.00m,
                    DesvioMaxKm = 5.00m,
                    DistanciaKm = 15.50m,
                    DuracionMin = 45,
                    Activo = true
                },
                new Viaje
                {
                    UsuarioId = usuarios[1].Id, // Carlos
                    OrigenDir = "Calle Falsa 123, Morón",
                    OrigenLat = -34.653400m,
                    OrigenLng = -58.619200m,
                    DestinoDir = "Plaza de Mayo, CABA",
                    DestinoLat = -34.608300m,
                    DestinoLng = -58.371200m,
                    HoraSalida = new TimeOnly(9, 30),
                    HoraLlegada = new TimeOnly(11, 0),
                    MaxPaquetes = 3,
                    PesoMaxKg = 30.00m,
                    DesvioMaxKm = 3.50m,
                    DistanciaKm = 22.00m,
                    DuracionMin = 60,
                    Activo = true
                },
                new Viaje
                {
                    UsuarioId = usuarios[2].Id, // Lucía
                    OrigenDir = "Mitre 450, San Justo",
                    OrigenLat = -34.685100m,
                    OrigenLng = -58.558700m,
                    DestinoDir = "Cabildo 2000, CABA",
                    DestinoLat = -34.562700m,
                    DestinoLng = -58.459000m,
                    HoraSalida = new TimeOnly(14, 0),
                    HoraLlegada = new TimeOnly(16, 0),
                    MaxPaquetes = 10,
                    PesoMaxKg = 100.00m,
                    DesvioMaxKm = 8.00m,
                    DistanciaKm = 18.20m,
                    DuracionMin = 50,
                    Activo = true
                }
            };

            context.Viajes.AddRange(viajes);
            context.SaveChanges();


            var viajeDias = new List<ViajeDia>
            {
                new ViajeDia { ViajeId = viajes[0].Id, DiaSemana = 1 }, // Lunes
                new ViajeDia { ViajeId = viajes[1].Id, DiaSemana = 3 }, // Miércoles
                new ViajeDia { ViajeId = viajes[2].Id, DiaSemana = 5 }  // Viernes
            };

            context.ViajeDias.AddRange(viajeDias);


            var paradas = new List<Parada>
            {
                new Parada { ViajeId = viajes[0].Id, Orden = 1, Direccion = "Corrientes 1000", Lat = -34.602500m, Lng = -58.385000m },
                new Parada { ViajeId = viajes[1].Id, Orden = 1, Direccion = "Gaona 2500", Lat = -34.625000m, Lng = -58.470000m },
                new Parada { ViajeId = viajes[2].Id, Orden = 1, Direccion = "Ruta 3 Km 28", Lat = -34.750000m, Lng = -58.590000m }
            };

            context.Paradas.AddRange(paradas);

            var envios = new List<Envio>
            {
                new Envio
                {
                    UsuarioId = usuarios[1].Id, // Carlos envía
                    OrigenDir = "Av. Santa Fe 1000",
                    OrigenLat = -34.598000m,
                    OrigenLng = -58.388000m,
                    DestinoDir = "Av. Pueyrredón 500",
                    DestinoLat = -34.602000m,
                    DestinoLng = -58.405000m,
                    FechaLimite = DateTime.Now.AddDays(2),
                    PesoKg = 2.50m,
                    Tamano = 1, // Pequeño
                    Descripcion = "Documentación importante",
                    Monto = 1500.00m,
                    Estado = 1, // Pendiente / Creado
                    CreadoEn = DateTime.Now
                },
                new Envio
                {
                    UsuarioId = usuarios[2].Id, // Lucía envía
                    OrigenDir = "Suipacha 400",
                    OrigenLat = -34.604000m,
                    OrigenLng = -58.379000m,
                    DestinoDir = "Esmeralda 800",
                    DestinoLat = -34.601000m,
                    DestinoLng = -58.378000m,
                    FechaLimite = DateTime.Now.AddDays(3),
                    PesoKg = 5.00m,
                    Tamano = 2, // Mediano
                    Descripcion = "Caja de repuestos",
                    Monto = 3500.00m,
                    Estado = 1,
                    CreadoEn = DateTime.Now
                },
                new Envio
                {
                    UsuarioId = usuarios[0].Id, // Admin envía
                    OrigenDir = "Bolívar 200",
                    OrigenLat = -34.611000m,
                    OrigenLng = -58.374000m,
                    DestinoDir = "Defensa 700",
                    DestinoLat = -34.615000m,
                    DestinoLng = -58.370000m,
                    FechaLimite = DateTime.Now.AddDays(1),
                    PesoKg = 1.00m,
                    Tamano = 1,
                    Descripcion = "Sobre cerrado",
                    Monto = 1200.00m,
                    Estado = 2, // En proceso
                    CreadoEn = DateTime.Now
                }
            };

            context.Envios.AddRange(envios);
            context.SaveChanges();


            var historiales = new List<EnvioEstadoHistorial>
            {
                new EnvioEstadoHistorial { EnvioId = envios[0].Id, EstadoAnterior = 0, EstadoNuevo = 1, Fecha = DateTime.Now },
                new EnvioEstadoHistorial { EnvioId = envios[1].Id, EstadoAnterior = 0, EstadoNuevo = 1, Fecha = DateTime.Now },
                new EnvioEstadoHistorial { EnvioId = envios[2].Id, EstadoAnterior = 1, EstadoNuevo = 2, Fecha = DateTime.Now }
            };

            context.EnvioEstadoHistoriales.AddRange(historiales);


            var asignaciones = new List<Asignacion>
            {
                new Asignacion
                {
                    EnvioId = envios[0].Id,
                    ViajeId = viajes[0].Id,
                    FechaViaje = DateTime.Now.AddDays(1),
                    DesvioKm = 1.20m,
                    TiempoExtraMin = 10,
                    Ganancia = 500.00m,
                    AsignadoEn = DateTime.Now
                },
                new Asignacion
                {
                    EnvioId = envios[1].Id,
                    ViajeId = viajes[1].Id,
                    FechaViaje = DateTime.Now.AddDays(2),
                    DesvioKm = 2.50m,
                    TiempoExtraMin = 15,
                    Ganancia = 900.00m,
                    AsignadoEn = DateTime.Now
                },
                new Asignacion
                {
                    EnvioId = envios[2].Id,
                    ViajeId = viajes[2].Id,
                    FechaViaje = DateTime.Now.AddDays(1),
                    DesvioKm = 0.50m,
                    TiempoExtraMin = 5,
                    Ganancia = 400.00m,
                    AsignadoEn = DateTime.Now
                }
            };

            context.Asignaciones.AddRange(asignaciones);


            context.SaveChanges();
        }
    }
}

