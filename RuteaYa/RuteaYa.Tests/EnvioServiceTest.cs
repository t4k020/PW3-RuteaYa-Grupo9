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
    public class EnvioServiceTest
    {

        [Fact]
        public async Task CuandoCreoUnEnvioSeHaceCorrectamente()
        {
            // Arrange (Preparación)
            // Configuramos una base de datos en memoria única para este test
            var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new RuteaYaDbContext(options);
            var service = new EnvioService(context);

            // Creamos un objeto de prueba válido cumpliendo con las validaciones de la entidad
            var nuevoEnvio = new Envio
            {
                UsuarioId = 1,
                OrigenDir = "Av. Corrientes 1234, CABA",
                OrigenLat = -34.603722m,
                OrigenLng = -58.381592m,
                DestinoDir = "Av. Santa Fe 4321, CABA",
                DestinoLat = -34.595555m,
                DestinoLng = -58.393333m,
                FechaLimite = DateTime.Now.AddDays(3),
                PesoKg = 2.50m,
                Tamano = 1,
                Descripcion = "Documentación importante",
                Monto = 2500.00m,
                Estado = 0 
            };

            // Act 
            var resultado = await service.CrearEnvioAsync(nuevoEnvio);

            // Assert 
            // 1. Verificamos que devuelva el objeto y se le asigne un ID
            Assert.NotNull(resultado);
            Assert.True(resultado.Id > 0, "El ID del envío debería generarse correctamente.");

            // 2. Verificamos que realmente se haya guardado en la base de datos en memoria
            var envioEnDb = await context.Envios.FindAsync(new object[] { resultado.Id }, TestContext.Current.CancellationToken);
            Assert.NotNull(envioEnDb);
            Assert.Equal("Av. Corrientes 1234, CABA", envioEnDb.OrigenDir);
            Assert.Equal(2500.00m, envioEnDb.Monto);
            Assert.Equal(1, envioEnDb.UsuarioId);
        }
        [Fact]
        public async Task ObtenerPorIdAsync_DeberiaRetornarEnvio_CuandoElIdExiste()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_ObtenerPorId_Success")
                .Options;

            // Insertamos datos de prueba en la base de datos en memoria
            using (var context = new RuteaYaDbContext(options))
            {
                context.Envios.Add(new Envio { Id = 1, OrigenDir = "Mitre 1000", DestinoDir = "Belgrano 500" });
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new EnvioService(context);
                var resultado = await service.ObtenerPorIdAsync(1);

                // Assert
                Assert.NotNull(resultado);
                Assert.Equal(1, resultado.Id);
                Assert.Equal("Mitre 1000", resultado.OrigenDir);
            }
        }

        [Fact]
        public async Task ObtenerPorIdAsync_DeberiaRetornarNull_CuandoElIdNoExiste()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_ObtenerPorId_NotFound")
                .Options;

            using (var context = new RuteaYaDbContext(options))
            {
                var service = new EnvioService(context);

                // Act
                var resultado = await service.ObtenerPorIdAsync(999);

                // Assert
                Assert.Null(resultado);
            }
        }

        [Fact]
        public async Task EliminarEnvioAsync_DeberiaRetornarTrue_CuandoElEnvioExiste()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_Eliminar_Success")
                .Options;

            // Insertamos un registro de prueba
            using (var context = new RuteaYaDbContext(options))
            {
                context.Envios.Add(new Envio { Id = 1, OrigenDir = "Mitre 1000", DestinoDir = "Belgrano 500" });
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new EnvioService(context);
                var resultado = await service.EliminarEnvioAsync(1);

                // Assert
                Assert.True(resultado);

                // Verificamos que realmente se haya borrado de la base de datos
                var envioEnDb = await context.Envios.FindAsync(new object[] { 1 }, TestContext.Current.CancellationToken);
                Assert.Null(envioEnDb);
            }
        }

        [Fact]
        public async Task EliminarEnvioAsync_DeberiaRetornarFalse_CuandoElEnvioNoExiste()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_Eliminar_NotFound")
                .Options;

            using (var context = new RuteaYaDbContext(options))
            {
                var service = new EnvioService(context);

                // Act
                var resultado = await service.EliminarEnvioAsync(999);

                // Assert
                Assert.False(resultado);
            }
        }

        [Fact]
        public async Task EliminarEnvioAsync_DeberiaEliminarAsignacionesEHistorial_CuandoElEnvioTieneDependencias()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new RuteaYaDbContext(options))
            {
                context.Usuarios.Add(new Usuario
                {
                    Id = 1,
                    Nombre = "Carlos",
                    Apellido = "González",
                    Email = "carlos@test.com",
                    PasswordHash = "hash"
                });
                context.Viajes.Add(new Viaje
                {
                    Id = 10,
                    UsuarioId = 1,
                    OrigenDir = "Origen viaje",
                    DestinoDir = "Destino viaje",
                    HoraSalida = new TimeOnly(8, 0),
                    HoraLlegada = new TimeOnly(9, 0)
                });
                context.Envios.Add(new Envio
                {
                    Id = 20,
                    UsuarioId = 1,
                    OrigenDir = "Origen",
                    DestinoDir = "Destino",
                    Estado = (byte)EstadoEnvio.Cancelado,
                    CreadoEn = DateTime.Now
                });
                context.Asignaciones.Add(new Asignacion
                {
                    Id = 1,
                    EnvioId = 20,
                    ViajeId = 10,
                    FechaViaje = DateTime.Today,
                    DesvioKm = 1,
                    TiempoExtraMin = 5,
                    Ganancia = 100
                });
                context.EnvioEstadoHistoriales.Add(new EnvioEstadoHistorial
                {
                    Id = 1,
                    EnvioId = 20,
                    EstadoAnterior = (byte)EstadoEnvio.ConductorAsignado,
                    EstadoNuevo = (byte)EstadoEnvio.Cancelado,
                    Fecha = DateTime.Now
                });

                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            using (var context = new RuteaYaDbContext(options))
            {
                var service = new EnvioService(context);

                // Act
                var resultado = await service.EliminarEnvioAsync(20, 1);

                // Assert
                Assert.True(resultado);
            }

            using (var context = new RuteaYaDbContext(options))
            {
                Assert.Null(await context.Envios.FindAsync(
                    new object[] { 20 },
                    TestContext.Current.CancellationToken));
                Assert.False(await context.Asignaciones.AnyAsync(
                    a => a.EnvioId == 20,
                    TestContext.Current.CancellationToken));
                Assert.False(await context.EnvioEstadoHistoriales.AnyAsync(
                    h => h.EnvioId == 20,
                    TestContext.Current.CancellationToken));
            }
        }

        [Fact]
        public async Task ObtenerListadoPorUsuarioAsync_DeberiaFiltrarPorEstadoYCalcularAcciones()
        {
            var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new RuteaYaDbContext(options);
            context.Envios.AddRange(
                new Envio
                {
                    Id = 1,
                    UsuarioId = 10,
                    OrigenDir = "Origen 1",
                    DestinoDir = "Destino 1",
                    Estado = (byte)EstadoEnvio.BuscandoConductor,
                    CreadoEn = DateTime.Now
                },
                new Envio
                {
                    Id = 2,
                    UsuarioId = 10,
                    OrigenDir = "Origen 2",
                    DestinoDir = "Destino 2",
                    Estado = (byte)EstadoEnvio.ConductorAsignado,
                    CreadoEn = DateTime.Now.AddMinutes(-1)
                },
                new Envio
                {
                    Id = 3,
                    UsuarioId = 10,
                    OrigenDir = "Origen 3",
                    DestinoDir = "Destino 3",
                    Estado = (byte)EstadoEnvio.Entregado,
                    CreadoEn = DateTime.Now.AddMinutes(-2)
                });
            context.Asignaciones.Add(new Asignacion
            {
                EnvioId = 2,
                ViajeId = 20,
                FechaViaje = DateTime.Today,
                DesvioKm = 1,
                TiempoExtraMin = 5,
                Ganancia = 100
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var service = new EnvioService(context);

            var resultado = await service.ObtenerListadoPorUsuarioAsync(
                10,
                EstadoEnvio.ConductorAsignado,
                TestContext.Current.CancellationToken);

            var envio = Assert.Single(resultado);
            Assert.Equal(2, envio.Id);
            Assert.Equal("Conductor asignado", envio.EstadoNombre);
            Assert.True(envio.PuedeCancelar);
            Assert.False(envio.PuedeEliminar);
            Assert.Null(envio.Viaje);
        }

        [Fact]
        public async Task CancelarEnvioAsync_DeberiaCambiarEstadoYRegistrarHistorial()
        {
            var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new RuteaYaDbContext(options);
            context.Envios.Add(new Envio
            {
                Id = 1,
                UsuarioId = 10,
                OrigenDir = "Origen",
                DestinoDir = "Destino",
                Estado = (byte)EstadoEnvio.ConductorAsignado
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var service = new EnvioService(context);
            var resultado = await service.CancelarEnvioAsync(1, 10);

            Assert.True(resultado);
            var envio = await context.Envios.FindAsync(
                new object[] { 1 },
                TestContext.Current.CancellationToken);
            Assert.Equal((byte)EstadoEnvio.Cancelado, envio!.Estado);
            Assert.True(await context.EnvioEstadoHistoriales.AnyAsync(h =>
                h.EnvioId == 1 &&
                h.EstadoNuevo == (byte)EstadoEnvio.Cancelado,
                TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CancelarEnvioAsync_DeberiaRechazarEstadosNoCancelables()
        {
            var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new RuteaYaDbContext(options);
            context.Envios.Add(new Envio
            {
                Id = 1,
                UsuarioId = 10,
                OrigenDir = "Origen",
                DestinoDir = "Destino",
                Estado = (byte)EstadoEnvio.Entregado
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var service = new EnvioService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CancelarEnvioAsync(1, 10));
        }

        [Theory]
        [InlineData(EstadoEnvio.ConductorAsignado)]
        [InlineData(EstadoEnvio.Retirado)]
        [InlineData(EstadoEnvio.EnCamino)]
        [InlineData(EstadoEnvio.Entregado)]
        public async Task ObtenerDetalleDtoAsync_DeberiaMostrarConductor_CuandoElEstadoLoRequiere(
            EstadoEnvio estado)
        {
            using var context = await CrearContextoConEnvioAsignadoAsync(estado);
            var service = new EnvioService(context);

            var resultado = await service.ObtenerDetalleDtoAsync(
                20,
                1,
                TestContext.Current.CancellationToken);

            Assert.NotNull(resultado);
            Assert.NotNull(resultado!.Viaje);
            Assert.Equal("Lucía Martínez", resultado.Viaje!.NombreConductor);
            Assert.Equal(new[] { "Lun" }, resultado.Viaje.DiasSemana);
        }

        [Theory]
        [InlineData(EstadoEnvio.Cancelado)]
        public async Task ObtenerDetalleDtoAsync_NoDeberiaMostrarConductor_CuandoElEstadoNoLoRequiere(
            EstadoEnvio estado)
        {
            using var context = await CrearContextoConEnvioAsignadoAsync(estado);
            var service = new EnvioService(context);

            var resultado = await service.ObtenerDetalleDtoAsync(
                20,
                1,
                TestContext.Current.CancellationToken);

            Assert.NotNull(resultado);
            Assert.NotNull(resultado!.Viaje);
            Assert.Null(resultado.Viaje!.NombreConductor);
        }

        [Fact]
        public async Task ObtenerDetalleDtoAsync_NoDeberiaMostrarViaje_CuandoElEnvioEstaBuscandoConductor()
        {
            using var context = await CrearContextoConEnvioAsignadoAsync(EstadoEnvio.BuscandoConductor);
            var service = new EnvioService(context);

            var resultado = await service.ObtenerDetalleDtoAsync(
                20,
                1,
                TestContext.Current.CancellationToken);

            Assert.NotNull(resultado);
            Assert.Null(resultado!.Viaje);
        }

        [Fact]
        public async Task ObtenerDetalleDtoAsync_DeberiaPermitirEliminar_CuandoElEnvioEstaCancelado()
        {
            using var context = await CrearContextoConEnvioAsignadoAsync(EstadoEnvio.Cancelado);
            var service = new EnvioService(context);

            var resultado = await service.ObtenerDetalleDtoAsync(
                20,
                1,
                TestContext.Current.CancellationToken);

            Assert.NotNull(resultado);
            Assert.True(resultado!.PuedeEliminar);
            Assert.False(resultado.PuedeCancelar);
        }

        private static async Task<RuteaYaDbContext> CrearContextoConEnvioAsignadoAsync(
            EstadoEnvio estado)
        {
            var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new RuteaYaDbContext(options);

            var duenioEnvio = new Usuario
            {
                Id = 1,
                Nombre = "Carlos",
                Apellido = "González",
                Email = "carlos@test.com",
                PasswordHash = "hash"
            };
            var conductor = new Usuario
            {
                Id = 2,
                Nombre = "Lucía",
                Apellido = "Martínez",
                Email = "lucia@test.com",
                PasswordHash = "hash"
            };
            var viaje = new Viaje
            {
                Id = 10,
                UsuarioId = conductor.Id,
                OrigenDir = "Origen viaje",
                DestinoDir = "Destino viaje",
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(9, 0)
            };
            viaje.ViajeDias.Add(new ViajeDia { ViajeId = 10, DiaSemana = 1 });

            context.Usuarios.AddRange(duenioEnvio, conductor);
            context.Viajes.Add(viaje);
            context.Envios.Add(new Envio
            {
                Id = 20,
                UsuarioId = duenioEnvio.Id,
                OrigenDir = "Origen",
                DestinoDir = "Destino",
                Estado = (byte)estado,
                CreadoEn = DateTime.Now
            });
            context.Asignaciones.Add(new Asignacion
            {
                Id = 1,
                EnvioId = 20,
                ViajeId = viaje.Id,
                FechaViaje = DateTime.Today,
                DesvioKm = 1,
                TiempoExtraMin = 5,
                Ganancia = 100
            });

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return context;
        }
    }
}
