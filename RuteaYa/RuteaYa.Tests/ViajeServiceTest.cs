using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios;
using RuteaYa.Servicios.Dtos;
using RuteaYa.Servicios.Servicios.Rutas;
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
            var service = new ViajeService(context, RutasDePrueba.CrearMock());
            var nuevoViaje = CrearViajeDtoValido();

            // Act
            var resultado = await service.CrearViajeAsync(
                nuevoViaje, 1, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(resultado);
            Assert.True(resultado.Id > 0, "El ID del viaje debería generarse correctamente.");

            var viajeEnDb = await context.Viajes
                .FirstAsync(v => v.Id == resultado.Id, TestContext.Current.CancellationToken);
            Assert.NotNull(viajeEnDb);
            Assert.Equal("Av. Corrientes 1234, CABA", viajeEnDb.OrigenDir);
            Assert.Equal(5, viajeEnDb.MaxPaquetes);
            Assert.Equal(1, viajeEnDb.UsuarioId);

            // Las coordenadas y la ruta las calcula el servidor, no el cliente.
            Assert.NotEqual(0m, viajeEnDb.OrigenLat);
            Assert.NotEqual(0m, viajeEnDb.DestinoLng);
            Assert.True(viajeEnDb.DistanciaKm > 0, "La distancia debería calcularse.");
            Assert.True(viajeEnDb.DuracionMin > 0, "La duración debería calcularse.");
        }

        [Fact]
        public async Task CuandoCreoUnViajeConParadasYDiasSePersistenCorrectamente()
        {
            // Arrange
            using var context = new RuteaYaDbContext(CrearOpcionesMemoria());
            var service = new ViajeService(context, RutasDePrueba.CrearMock());
            var nuevoViaje = CrearViajeDtoValido();
            nuevoViaje.Paradas.Add(new ParadaViajeDto { Direccion = "Av. Callao 600, CABA" });
            nuevoViaje.Paradas.Add(new ParadaViajeDto { Direccion = "Av. Belgrano 900, CABA" });
            nuevoViaje.DiasSemana = new List<byte> { 1, 3 };

            // Act
            var resultado = await service.CrearViajeAsync(
                nuevoViaje, 1, TestContext.Current.CancellationToken);

            // Assert
            var viajeEnDb = await context.Viajes
                .Include(v => v.Paradas)
                .Include(v => v.ViajeDias)
                .FirstAsync(v => v.Id == resultado.Id, TestContext.Current.CancellationToken);

            Assert.Equal(2, viajeEnDb.Paradas.Count);
            Assert.Equal(2, viajeEnDb.ViajeDias.Count);
            Assert.Equal(
                new[] { "Av. Callao 600, CABA", "Av. Belgrano 900, CABA" },
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
                var service = new ViajeService(context, RutasDePrueba.CrearMock());
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
                var service = new ViajeService(context, RutasDePrueba.CrearMock());

                // Act
                var resultado = await service.ObtenerPorIdAsync(999);

                // Assert
                Assert.Null(resultado);
            }
        }

        [Fact]
        public async Task ActualizarViajeAsync_DeberiaRecalcularRecorridoYReemplazarParadasYDias()
        {
            // Arrange
            var options = CrearOpcionesMemoria();

            using (var context = new RuteaYaDbContext(options))
            {
                var viajeOriginal = CrearViajeValido(1);
                viajeOriginal.Activo = false;
                viajeOriginal.Paradas.Add(new Parada { Orden = 1, Direccion = "Av. Callao 600, CABA" });
                viajeOriginal.ViajeDias.Add(new ViajeDia { DiaSemana = 1 });

                context.Viajes.Add(viajeOriginal);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var viajeModificado = CrearViajeDtoValido();
            viajeModificado.OrigenDir = "Mitre 450, San Justo";
            viajeModificado.MaxPaquetes = 10;
            viajeModificado.DiasSemana = new List<byte> { 3, 5 };
            viajeModificado.Paradas = new List<ParadaViajeDto>
            {
                new() { Direccion = "Av. Belgrano 900, CABA" },
                new() { Direccion = "Av. Córdoba 1200, CABA" }
            };

            // Act 
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new ViajeService(context, RutasDePrueba.CrearMock());
                var resultado = await service.ActualizarViajeAsync(
                    1, viajeModificado, 1, TestContext.Current.CancellationToken);

                // Assert
                Assert.True(resultado);

                var enDb = await context.Viajes
                    .Include(v => v.Paradas)
                    .Include(v => v.ViajeDias)
                    .FirstAsync(v => v.Id == 1, TestContext.Current.CancellationToken);

                Assert.Equal("Mitre 450, San Justo", enDb.OrigenDir);
                Assert.Equal(10, enDb.MaxPaquetes);

                // Activar o desactivar es una acción aparte: la edición no la pisa.
                Assert.False(enDb.Activo);

                // El recorrido se recalcula con las direcciones nuevas.
                Assert.NotEqual(0m, enDb.OrigenLat);
                Assert.True(enDb.DistanciaKm > 0);
                Assert.True(enDb.DuracionMin > 0);

                Assert.Equal(
                    new[] { "Av. Belgrano 900, CABA", "Av. Córdoba 1200, CABA" },
                    enDb.Paradas.OrderBy(p => p.Orden).Select(p => p.Direccion));

                Assert.Equal(
                    new byte[] { 3, 5 },
                    enDb.ViajeDias.OrderBy(d => d.DiaSemana).Select(d => d.DiaSemana));
            }
        }

        [Fact]
        public async Task ActualizarViajeAsync_DeberiaRetornarFalse_CuandoElViajeNoExiste()
        {
            // Arrange
            var viajeModificado = CrearViajeDtoValido();

            // Act 
            using (var context = new RuteaYaDbContext(CrearOpcionesMemoria()))
            {
                var service = new ViajeService(context, RutasDePrueba.CrearMock());
                var resultado = await service.ActualizarViajeAsync(
                    999, viajeModificado, 1, TestContext.Current.CancellationToken);
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
                var service = new ViajeService(context, RutasDePrueba.CrearMock());
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

                var service = new ViajeService(context, RutasDePrueba.CrearMock());
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
                var service = new ViajeService(context, RutasDePrueba.CrearMock());
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
                var service = new ViajeService(context, RutasDePrueba.CrearMock());
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
            var service = new ViajeService(context, RutasDePrueba.CrearMock());

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
                var service = new ViajeService(context, RutasDePrueba.CrearMock());
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
                var service = new ViajeService(context, RutasDePrueba.CrearMock());

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
                var service = new ViajeService(context, RutasDePrueba.CrearMock());
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
                var service = new ViajeService(context, RutasDePrueba.CrearMock());
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
                var service = new ViajeService(context, RutasDePrueba.CrearMock());
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
                var service = new ViajeService(context, RutasDePrueba.CrearMock());
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
                var service = new ViajeService(context, RutasDePrueba.CrearMock());
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

        [Fact]
        public async Task ObtenerRecorridoAsync_DeberiaOrdenarOrigenParadasYDestino()
        {
            // Arrange
            var options = CrearOpcionesMemoria();

            using (var context = new RuteaYaDbContext(options))
            {
                context.Usuarios.Add(new Usuario { Id = 1 });

                var viaje = CrearViajeValido(1);
                viaje.Paradas.Add(new Parada
                {
                    Orden = 2,
                    Direccion = "Segunda parada",
                    Lat = -34.600000m,
                    Lng = -58.400000m
                });
                viaje.Paradas.Add(new Parada
                {
                    Orden = 1,
                    Direccion = "Primera parada",
                    Lat = -34.610000m,
                    Lng = -58.410000m
                });

                context.Viajes.Add(viaje);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new ViajeService(context, RutasDePrueba.CrearMock());
                var recorrido = await service.ObtenerRecorridoAsync(
                    1, 1, TestContext.Current.CancellationToken);

                // Assert
                Assert.NotNull(recorrido);
                Assert.Equal(
                    new[]
                    {
                        "Av. Corrientes 1234, CABA",
                        "Primera parada",
                        "Segunda parada",
                        "Av. Santa Fe 4321, CABA"
                    },
                    recorrido!.Puntos.Select(punto => punto.Direccion));

                Assert.True(recorrido.Geometria.Count > recorrido.Puntos.Count);
                Assert.True(recorrido.DistanciaKm > 0);
                Assert.True(recorrido.DuracionMin > 0);
                Assert.Equal("Mock", recorrido.Fuente);
            }
        }

        [Fact]
        public async Task ObtenerRecorridoAsync_DeberiaDevolverNull_CuandoElViajeEsDeOtroUsuario()
        {
            // Arrange
            var options = CrearOpcionesMemoria();

            using (var context = new RuteaYaDbContext(options))
            {
                context.Usuarios.Add(new Usuario { Id = 1 });
                context.Viajes.Add(CrearViajeValido(1));
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new ViajeService(context, RutasDePrueba.CrearMock());
                var recorrido = await service.ObtenerRecorridoAsync(
                    1, 2, TestContext.Current.CancellationToken);

                // Assert
                Assert.Null(recorrido);
            }
        }

        [Fact]
        public async Task ObtenerRecorridoAsync_DeberiaUsarLaCache_EnLaSegundaLlamada()
        {
            // Arrange
            var options = CrearOpcionesMemoria();

            using (var context = new RuteaYaDbContext(options))
            {
                context.Usuarios.Add(new Usuario { Id = 1 });
                context.Viajes.Add(CrearViajeValido(1));
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var rutaService = new Mock<IRutaService>();
            rutaService.SetupGet(servicio => servicio.NombreProveedor).Returns("Mock");
            rutaService
                .Setup(servicio => servicio.CalcularRutaAsync(
                    It.IsAny<IReadOnlyList<CoordenadaDto>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RutaDto(10m, 25, new[]
                {
                    new CoordenadaDto(-34.603722m, -58.381592m),
                    new CoordenadaDto(-34.595555m, -58.393333m)
                }));

            using var cache = new MemoryCache(new MemoryCacheOptions());

            // Act
            using (var context = new RuteaYaDbContext(options))
            {
                var service = new ViajeService(context, rutaService.Object, cache);

                await service.ObtenerRecorridoAsync(1, 1, TestContext.Current.CancellationToken);
                await service.ObtenerRecorridoAsync(1, 1, TestContext.Current.CancellationToken);
            }

            // Assert
            rutaService.Verify(
                servicio => servicio.CalcularRutaAsync(
                    It.IsAny<IReadOnlyList<CoordenadaDto>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task PrevisualizarRecorridoAsync_DeberiaCalcularSinPersistirNada()
        {
            // Arrange
            using var context = new RuteaYaDbContext(CrearOpcionesMemoria());
            var service = new ViajeService(context, RutasDePrueba.CrearMock());

            var solicitud = CrearViajeDtoValido();
            solicitud.Paradas.Add(new ParadaViajeDto { Direccion = "Av. Callao 600, CABA" });

            // Act
            var recorrido = await service.PrevisualizarRecorridoAsync(
                solicitud, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(0, recorrido.ViajeId);
            Assert.Equal(3, recorrido.Puntos.Count);
            Assert.Equal("Av. Corrientes 1234, CABA", recorrido.Puntos[0].Direccion);
            Assert.Equal("Av. Callao 600, CABA", recorrido.Puntos[1].Direccion);
            Assert.Equal("Av. Santa Fe 1000, CABA", recorrido.Puntos[2].Direccion);
            Assert.True(recorrido.Geometria.Count > recorrido.Puntos.Count);
            Assert.True(recorrido.DistanciaKm > 0);
            Assert.Equal("Mock", recorrido.Fuente);
            Assert.Empty(context.Viajes);
        }

        // DTO que usa la API para crear y editar viajes.
        private CrearViajeDto CrearViajeDtoValido()        {
            return new CrearViajeDto
            {
                OrigenDir = "Av. Corrientes 1234, CABA",
                DestinoDir = "Av. Santa Fe 1000, CABA",
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(10, 0),
                DiasSemana = new List<byte> { 1, 2 },
                MaxPaquetes = 5,
                PesoMaxKg = 50.0m,
                DesvioMaxKm = 2.5m,
                Paradas = new List<ParadaViajeDto>()
            };
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
