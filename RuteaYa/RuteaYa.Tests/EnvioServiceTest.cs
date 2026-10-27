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
    }
}
