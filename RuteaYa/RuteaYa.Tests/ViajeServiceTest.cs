using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace RuteaYa.Tests
{
    public class ViajeServiceTest
    {


        [Fact]
        public async Task CuandoCreoUnViajeSeHaceCorrectamente()
        {
            // Arrange 
            using var context = new RuteaYaDbContext(CrearOpcionesMemoria());
            var service = new ViajeService(context);
            var nuevoViaje = CrearViajeValido(0);

            // Act
            var resultado = await service.CrearViajeAsync(nuevoViaje);

            // Assert
            Assert.NotNull(resultado);
            Assert.True(resultado.Id > 0, "El ID del viaje debería generarse correctamente.");

            var viajeEnDb = await context.Viajes.FindAsync(new object[] { resultado.Id }, TestContext.Current.CancellationToken);
            Assert.NotNull(viajeEnDb);
            Assert.Equal("Av. Corrientes 1234, CABA", viajeEnDb.OrigenDir);
            Assert.Equal(5, viajeEnDb.MaxPaquetes);
            Assert.Equal(1, viajeEnDb.UsuarioId);
        }

        [Fact]
        public async Task CuandoCreoUnViajeConParadasYDiasSePersistenCorrectamente()
        {
            // Arrange
            using var context = new RuteaYaDbContext(CrearOpcionesMemoria());
            var service = new ViajeService(context);
            var nuevoViaje = CrearViajeValido(0);
            nuevoViaje.Paradas.Add(new Parada { Orden = 1, Direccion = "Parada 1" });
            nuevoViaje.Paradas.Add(new Parada { Orden = 2, Direccion = "Parada 2" });
            nuevoViaje.ViajeDias.Add(new ViajeDia { DiaSemana = 1 });
            nuevoViaje.ViajeDias.Add(new ViajeDia { DiaSemana = 3 });

            // Act
            var resultado = await service.CrearViajeAsync(nuevoViaje);

            // Assert
            var viajeEnDb = await context.Viajes
                .Include(v => v.Paradas)
                .Include(v => v.ViajeDias)
                .FirstAsync(v => v.Id == resultado.Id, TestContext.Current.CancellationToken);

            Assert.Equal(2, viajeEnDb.Paradas.Count);
            Assert.Equal(2, viajeEnDb.ViajeDias.Count);
            Assert.Equal(
                new[] { "Parada 1", "Parada 2" },
                viajeEnDb.Paradas.OrderBy(p => p.Orden).Select(p => p.Direccion));
            Assert.Equal(
                new byte[] { 1, 3 },
                viajeEnDb.ViajeDias.OrderBy(d => d.DiaSemana).Select(d => d.DiaSemana));
        }

        [Fact]
        public async Task ObtenerPorIdAsync_DeberiaRetornarViaje_CuandoElIdExiste()
        {
            // Arrange
            var options = CrearOpcionesMemoria();

            using (var context = new RuteaYaDbContext(options))
            {
                //usuario requerido 
                context.Usuarios.Add(new Usuario { Id = 1 });

                // viaje de prueba
                context.Viajes.Add(CrearViajeValido(1));

                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new ViajeService(context);
                var resultado = await service.ObtenerPorIdAsync(1);

                // Assert
                Assert.NotNull(resultado);
                Assert.Equal(1, resultado.Id);
                Assert.Equal("Av. Corrientes 1234, CABA", resultado.OrigenDir);
            }
        }

        [Fact]
        public async Task ObtenerPorIdAsync_DeberiaRetornarNull_CuandoElIdNoExiste()
        {
            // Arrange
            using (var context = new RuteaYaDbContext(CrearOpcionesMemoria()))
            {
                var service = new ViajeService(context);

                // Act
                var resultado = await service.ObtenerPorIdAsync(999);

                // Assert
                Assert.Null(resultado);
            }
        }

        [Fact]
        public async Task ActualizarViajeAsync_DeberiaRetornarTrue_CuandoElViajeExiste()
        {
            // Arrange
            var options = CrearOpcionesMemoria();

            using (var context = new RuteaYaDbContext(options))
            {
                context.Viajes.Add(CrearViajeValido(1));
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var viajeModificado = CrearViajeValido(1);
            viajeModificado.OrigenDir = "Origen Nuevo";
            viajeModificado.MaxPaquetes = 10;
            viajeModificado.Activo = false;

            // Act 
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new ViajeService(context);
                var resultado = await service.ActualizarViajeAsync(1, viajeModificado, 1);
                
                //assert
                Assert.True(resultado);

                var enDb = await context.Viajes.FindAsync(new object[] { 1 }, TestContext.Current.CancellationToken);
                Assert.NotNull(enDb);
                Assert.Equal("Origen Nuevo", enDb.OrigenDir);
                Assert.Equal(10, enDb.MaxPaquetes);
                Assert.False(enDb.Activo);
            }
        }

        [Fact]
        public async Task ActualizarViajeAsync_DeberiaRetornarFalse_CuandoElViajeNoExiste()
        {
            // Arrange
            var viajeModificado = CrearViajeValido(999);

            // Act 
            using (var context = new RuteaYaDbContext(CrearOpcionesMemoria()))
            {
                var service = new ViajeService(context);
                var resultado = await service.ActualizarViajeAsync(999, viajeModificado, 1);
                //assert
                Assert.False(resultado);
            }
        }

        [Fact]
        public async Task EliminarViajeAsync_DeberiaHacerBorradoLogico_CuandoElViajeExiste()
        {
            // Arrange
            var options = CrearOpcionesMemoria();

            using (var context = new RuteaYaDbContext(options))
            {
                context.Viajes.Add(CrearViajeValido(1));
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new ViajeService(context);
                var resultado = await service.EliminarViajeAsync(1, 1);

                // Assert
                Assert.True(resultado);
            }

            // Assert: la fila se conserva en la base para no perder el historial,
            // pero deja de devolverse en los listados y en el detalle.
            using (var context = new RuteaYaDbContext(options))
            {
                var viajeEnDb = await context.Viajes
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(v => v.Id == 1, TestContext.Current.CancellationToken);
                Assert.NotNull(viajeEnDb);
                Assert.NotNull(viajeEnDb.EliminadoEn);

                var service = new ViajeService(context);
                Assert.Null(await service.ObtenerDetallePorIdAsync(1, 1));
                Assert.Empty(await service.ObtenerParaListadoAsync(null, 1));
            }
        }

        [Fact]
        public async Task EliminarViajeAsync_DeberiaRetornarFalse_CuandoElViajeEsDeOtroUsuario()
        {
            // Arrange
            var options = CrearOpcionesMemoria();

            using (var context = new RuteaYaDbContext(options))
            {
                context.Viajes.Add(CrearViajeValido(1));
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new ViajeService(context);
                var resultado = await service.EliminarViajeAsync(1, 99);

                // Assert
                Assert.False(resultado);
            }
        }

        [Fact]
        public async Task ActivarViajeAsync_DeberiaVolverActivoElViaje()
        {
            // Arrange
            var options = CrearOpcionesMemoria();
            var viajeDesactivado = CrearViajeValido(1);
            viajeDesactivado.Activo = false;

            using (var context = new RuteaYaDbContext(options))
            {
                context.Viajes.Add(viajeDesactivado);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new ViajeService(context);
                var resultado = await service.ActivarViajeAsync(1, 1);

                // Assert
                Assert.True(resultado);

                var viajeEnDb = await context.Viajes.FindAsync(new object[] { 1 }, TestContext.Current.CancellationToken);
                Assert.NotNull(viajeEnDb);
                Assert.True(viajeEnDb.Activo);
            }
        }

        [Fact]
        public async Task ActivarViajeAsync_DeberiaRetornarFalse_CuandoElViajeNoExiste()
        {
            // Arrange
            using var context = new RuteaYaDbContext(CrearOpcionesMemoria());
            var service = new ViajeService(context);

            // Act
            var resultado = await service.ActivarViajeAsync(999, 1);

            // Assert
            Assert.False(resultado);
        }

        [Fact]
        public async Task ObtenerDetallePorIdAsync_DeberiaDevolverParadasOrdenadasYEstadoLegible()
        {
            // Arrange
            var options = CrearOpcionesMemoria();

            using (var context = new RuteaYaDbContext(options))
            {
                context.Usuarios.Add(new Usuario
                {
                    Id = 1,
                    Nombre = "Ana",
                    Apellido = "Paz",
                    Email = "ana@example.com",
                    PasswordHash = "hash-de-prueba"
                });

                var viaje = CrearViajeValido(1);
                viaje.Paradas.Add(new Parada { Orden = 2, Direccion = "Parada 2" });
                viaje.Paradas.Add(new Parada { Orden = 1, Direccion = "Parada 1" });

                context.Viajes.Add(viaje);
                context.Envios.Add(new Envio
                {
                    Id = 10,
                    UsuarioId = 2,
                    OrigenDir = "Origen del paquete",
                    DestinoDir = "Destino del paquete",
                    FechaLimite = DateTime.Today.AddDays(2),
                    PesoKg = 1.5m,
                    Tamano = 1,
                    Descripcion = "Caja de libros",
                    Monto = 2500m,
                    Estado = (byte)EstadoEnvio.Retirado,
                    CreadoEn = DateTime.Now
                });
                context.Asignaciones.Add(new Asignacion
                {
                    EnvioId = 10,
                    ViajeId = 1,
                    FechaViaje = DateTime.Today,
                    DesvioKm = 1m,
                    TiempoExtraMin = 5,
                    Ganancia = 500m,
                    AsignadoEn = DateTime.Now
                });

                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new ViajeService(context);
                var detalle = await service.ObtenerDetallePorIdAsync(1, 1);

                // Assert
                Assert.NotNull(detalle);
                Assert.Equal(
                    new[] { "Parada 1", "Parada 2" },
                    detalle!.Paradas.Select(p => p.Direccion));
                Assert.Equal(15.0m, detalle.DistanciaKm);
                Assert.Equal(60, detalle.DuracionMin);

                var paquete = Assert.Single(detalle.PaquetesAsignados);
                Assert.Equal("Caja de libros", paquete.Descripcion);
                Assert.Equal("Retirado", paquete.EstadoNombre);
            }
        }

        [Fact]
        public async Task EliminarViajeAsync_DeberiaRetornarFalse_CuandoElViajeNoExiste()
        {
            // Arrange
            using (var context = new RuteaYaDbContext(CrearOpcionesMemoria()))
            {
                var service = new ViajeService(context);

                // Act
                var resultado = await service.EliminarViajeAsync(999, 1);

                // Assert
                Assert.False(resultado);
            }
        }

        [Fact]
        public async Task ObtenerPaqueteAsignadoAsync_DeberiaDevolverElDetalle_CuandoElViajeEsDelUsuario()
        {
            // Arrange
            var options = CrearOpcionesMemoria();

            using (var context = new RuteaYaDbContext(options))
            {
                await SembrarPaqueteAsignadoAsync(context);
            }

            // Act
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new ViajeService(context);
                var paquete = await service.ObtenerPaqueteAsignadoAsync(
                    1, 10, 1, TestContext.Current.CancellationToken);

                // Assert
                Assert.NotNull(paquete);
                Assert.Equal(10, paquete!.EnvioId);
                Assert.Equal(1, paquete.ViajeId);
                Assert.Equal("Caja de libros", paquete.Descripcion);
                Assert.Equal("Retirado", paquete.EstadoNombre);
                Assert.Equal("Mediano", paquete.TamanoNombre);
                Assert.Equal("Bruno Remitente", paquete.NombreRemitente);
                Assert.Equal(4.5m, paquete.PesoKg);
                Assert.Equal(1.5m, paquete.DesvioKm);
                Assert.Equal(7, paquete.TiempoExtraMin);
                Assert.Equal(3200m, paquete.Ganancia);
            }
        }

        [Fact]
        public async Task ObtenerPaqueteAsignadoAsync_DeberiaRetornarNull_CuandoElViajeEsDeOtroUsuario()
        {
            // Arrange
            var options = CrearOpcionesMemoria();

            using (var context = new RuteaYaDbContext(options))
            {
                await SembrarPaqueteAsignadoAsync(context);
            }

            // Act
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new ViajeService(context);
                var paquete = await service.ObtenerPaqueteAsignadoAsync(
                    1, 10, 99, TestContext.Current.CancellationToken);

                // Assert
                Assert.Null(paquete);
            }
        }

        [Fact]
        public async Task ObtenerPaqueteAsignadoAsync_DeberiaRetornarNull_CuandoElEnvioNoEstaAsignadoAlViaje()
        {
            // Arrange
            var options = CrearOpcionesMemoria();

            using (var context = new RuteaYaDbContext(options))
            {
                await SembrarPaqueteAsignadoAsync(context);
            }

            // Act
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new ViajeService(context);
                var paquete = await service.ObtenerPaqueteAsignadoAsync(
                    1, 999, 1, TestContext.Current.CancellationToken);

                // Assert
                Assert.Null(paquete);
            }
        }

        [Fact]
        public async Task ObtenerPaqueteAsignadoAsync_DeberiaRetornarNull_CuandoElViajeNoExiste()
        {
            // Arrange
            var options = CrearOpcionesMemoria();

            using (var context = new RuteaYaDbContext(options))
            {
                await SembrarPaqueteAsignadoAsync(context);
            }

            // Act
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new ViajeService(context);
                var paquete = await service.ObtenerPaqueteAsignadoAsync(
                    999, 10, 1, TestContext.Current.CancellationToken);

                // Assert
                Assert.Null(paquete);
            }
        }

        [Fact]
        public async Task ObtenerPaqueteAsignadoAsync_DeberiaDevolverUnEnvioCancelado_CuandoConservaLaAsignacion()
        {
            // Arrange
            var options = CrearOpcionesMemoria();

            using (var context = new RuteaYaDbContext(options))
            {
                await SembrarPaqueteAsignadoAsync(context, (byte)EstadoEnvio.Cancelado);
            }

            // Act
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new ViajeService(context);
                var paquete = await service.ObtenerPaqueteAsignadoAsync(
                    1, 10, 1, TestContext.Current.CancellationToken);

                // Assert
                Assert.NotNull(paquete);
                Assert.Equal("Cancelado", paquete!.EstadoNombre);
            }
        }

        private async Task SembrarPaqueteAsignadoAsync(
            RuteaYaDbContext context,
            byte estado = (byte)EstadoEnvio.Retirado)
        {
            context.Usuarios.AddRange(
                new Usuario
                {
                    Id = 1,
                    Nombre = "Ana",
                    Apellido = "Paz",
                    Email = "ana@example.com",
                    PasswordHash = "hash-de-prueba"
                },
                new Usuario
                {
                    Id = 2,
                    Nombre = "Bruno",
                    Apellido = "Remitente",
                    Email = "bruno@example.com",
                    PasswordHash = "hash-de-prueba"
                });

            context.Viajes.Add(CrearViajeValido(1));

            context.Envios.Add(new Envio
            {
                Id = 10,
                UsuarioId = 2,
                OrigenDir = "Origen del paquete",
                DestinoDir = "Destino del paquete",
                FechaLimite = DateTime.Today.AddDays(2),
                PesoKg = 4.5m,
                Tamano = 2,
                Descripcion = "Caja de libros",
                Monto = 3200m,
                Estado = estado,
                CreadoEn = DateTime.Today.AddDays(-1)
            });

            context.Asignaciones.Add(new Asignacion
            {
                EnvioId = 10,
                ViajeId = 1,
                FechaViaje = DateTime.Today,
                DesvioKm = 1.5m,
                TiempoExtraMin = 7,
                Ganancia = 3200m,
                AsignadoEn = DateTime.Now
            });

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        private Viaje CrearViajeValido(int id = 1)
        {
            return new Viaje
            {
                Id = id,
                UsuarioId = 1,
                OrigenDir = "Av. Corrientes 1234, CABA",
                OrigenLat = -34.603722m,
                OrigenLng = -58.381592m,
                DestinoDir = "Av. Santa Fe 4321, CABA",
                DestinoLat = -34.595555m,
                DestinoLng = -58.393333m,
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(10, 0),
                MaxPaquetes = 5,
                PesoMaxKg = 50.0m,
                DesvioMaxKm = 2.5m,
                DistanciaKm = 15.0m,
                DuracionMin = 60,
                Activo = true
            };
        }

        private DbContextOptions<RuteaYaDbContext> CrearOpcionesMemoria()
        {
            return new DbContextOptionsBuilder<RuteaYaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }
    }


}
