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
                var resultado = await service.ActualizarViajeAsync(1, viajeModificado);
                
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
                var resultado = await service.ActualizarViajeAsync(999, viajeModificado);
                //assert
                Assert.False(resultado);
            }
        }

        [Fact]
        public async Task EliminarViajeAsync_DeberiaRetornarTrue_CuandoElViajeExiste()
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
                var resultado = await service.EliminarViajeAsync(1);

                // Assert
                Assert.True(resultado);

                var viajeEnDb = await context.Viajes.FindAsync(new object[] { 1 }, TestContext.Current.CancellationToken);
                Assert.Null(viajeEnDb);
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
                var resultado = await service.EliminarViajeAsync(999);

                // Assert
                Assert.False(resultado);
            }
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
