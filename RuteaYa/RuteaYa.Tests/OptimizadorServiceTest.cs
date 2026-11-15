using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios;
using Xunit;

namespace RuteaYa.Tests
{
    public class OptimizadorServiceTest
    {
        private const int Conductor = 10;
        private const int OtroUsuario = 20;
        private const string MensajeCambio =
            "Otro conductor tomó un envío o ya no está disponible. Recalculá la combinación.";

        // El viaje de prueba corre todos los días, así que cualquier fecha futura sirve.
        private static DateTime Fecha => DateTime.Today.AddDays(7);

        // =====================================================================
        // Estimación de desvío
        // =====================================================================

        [Theory]
        [InlineData(1, 1.0)]
        [InlineData(7, 4.0)]
        [InlineData(8, 0.5)]
        [InlineData(9, 1.0)]
        [InlineData(16, 0.5)]
        public void EstimarDesvioKm_DeberiaDevolverUnValorEstablePorEnvio(int envioId, double esperado)
        {
            Assert.Equal((decimal)esperado, OptimizadorService.EstimarDesvioKm(envioId));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0.5, 2)]
        [InlineData(1.0, 3)]
        [InlineData(1.5, 5)]
        [InlineData(4.0, 12)]
        public void EstimarTiempoExtraMin_DeberiaUsarTresMinutosPorKmRedondeandoHaciaArriba(
            double desvioKm, int esperado)
        {
            Assert.Equal(esperado, OptimizadorService.EstimarTiempoExtraMin((decimal)desvioKm));
        }

        // =====================================================================
        // OptimizarAsync: resultado
        // =====================================================================

        [Fact]
        public async Task OptimizarAsync_DeberiaRecomendarLaCombinacionDeMayorGanancia()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.NotNull(resultado);
            Assert.Equal(1, resultado!.ViajeId);
            Assert.Equal("Belgrano", resultado.OrigenDir);
            Assert.Equal("Lomas de Zamora", resultado.DestinoDir);
            Assert.Equal(Fecha, resultado.FechaViaje);
            Assert.Equal(5, resultado.CandidatosEvaluados);

            var recomendada = resultado.Recomendada!;
            Assert.True(recomendada.EsValida);
            Assert.Null(recomendada.MotivoRechazo);
            Assert.Equal(new[] { 3, 1, 8 }, recomendada.Paquetes.Select(p => p.Id));
            Assert.Equal(12000m, recomendada.GananciaTotal);
            Assert.Equal(9m, recomendada.PesoTotalKg);
            Assert.Equal(3.5m, recomendada.DesvioTotalKm);
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaInformarLaCapacidadDelViaje()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Equal(3, resultado!.CupoDisponible);
            Assert.Equal(10m, resultado.PesoDisponibleKg);
            Assert.Equal(5m, resultado.DesvioDisponibleKm);
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaDevolverDosAlternativasOrdenadasPorGanancia()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Equal(2, resultado!.Alternativas.Count);
            Assert.All(resultado.Alternativas, a => Assert.True(a.EsValida));
            Assert.Equal(new[] { 5, 1 }, resultado.Alternativas[0].Paquetes.Select(p => p.Id));
            Assert.Equal(11000m, resultado.Alternativas[0].GananciaTotal);
            Assert.Equal(10m, resultado.Alternativas[0].PesoTotalKg);
            Assert.Equal(new[] { 1, 9, 8 }, resultado.Alternativas[1].Paquetes.Select(p => p.Id));
            Assert.Equal(10000m, resultado.Alternativas[1].GananciaTotal);
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaMostrarLasCombinacionesDescartadasConSuMotivo()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Equal(2, resultado!.Descartadas.Count);
            Assert.All(resultado.Descartadas, d =>
            {
                Assert.False(d.EsValida);
                Assert.Equal("Supera 10 kg", d.MotivoRechazo);
            });
            Assert.Equal(new[] { 5, 3, 1 }, resultado.Descartadas[0].Paquetes.Select(p => p.Id));
            Assert.Equal(16500m, resultado.Descartadas[0].GananciaTotal);
            Assert.Equal(16m, resultado.Descartadas[0].PesoTotalKg);
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaIndicarElDesvioEstimadoYElTiempoExtraDeCadaPaquete()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            var paquete = resultado!.Recomendada!.Paquetes.Single(p => p.Id == 3);
            Assert.Equal(2.0m, paquete.DesvioKm);
            Assert.Equal(6, paquete.TiempoExtraMin);
            Assert.Equal(5500m, paquete.Monto);
            Assert.Equal(6m, paquete.PesoKg);
            Assert.Equal("Origen 3", paquete.OrigenDir);
            Assert.Equal("Destino 3", paquete.DestinoDir);
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaRespetarElCupoDelViaje()
        {
            using var context = CrearContexto();
            AgregarViaje(context, maxPaquetes: 1);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Equal(new[] { 5 }, resultado!.Recomendada!.Paquetes.Select(p => p.Id));
            Assert.Equal(7000m, resultado.Recomendada.GananciaTotal);
            Assert.Equal(2, resultado.Alternativas.Count);
            Assert.Empty(resultado.Descartadas);
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaDesempatarPorMenorDesvio()
        {
            using var context = CrearContexto();
            AgregarViaje(context, maxPaquetes: 1);
            // Misma ganancia y peso: el envío 1 desvía 1,0 km y el 3 desvía 2,0 km.
            AgregarEnvio(context, 3, monto: 5000m, peso: 1m);
            AgregarEnvio(context, 1, monto: 5000m, peso: 1m);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Equal(new[] { 1 }, resultado!.Recomendada!.Paquetes.Select(p => p.Id));
            Assert.Equal(new[] { 3 }, resultado.Alternativas.Single().Paquetes.Select(p => p.Id));
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaDescontarLoYaAsignadoEseDia()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarOportunidadesBase(context);
            // Ya lleva un paquete de 6 kg ese día: quedan 2 lugares y 4 kg.
            AgregarEnvio(context, 100, monto: 3000m, peso: 6m, estado: EstadoEnvio.ConductorAsignado);
            AgregarAsignacion(context, envioId: 100, viajeId: 1, desvioKm: 0m);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Equal(2, resultado!.CupoDisponible);
            Assert.Equal(4m, resultado.PesoDisponibleKg);
            Assert.Equal(5m, resultado.DesvioDisponibleKm);
            // Los envíos de 6 y 8 kg ya no entran solos.
            Assert.Equal(3, resultado.CandidatosEvaluados);
            Assert.Equal(new[] { 1, 8 }, resultado.Recomendada!.Paquetes.Select(p => p.Id));
            Assert.Equal(6500m, resultado.Recomendada.GananciaTotal);
            Assert.All(resultado.Descartadas, d => Assert.Equal("Supera 4 kg", d.MotivoRechazo));
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaDescontarElDesvioYaAsignadoEseDia()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarEnvio(context, 100, monto: 3000m, peso: 1m, estado: EstadoEnvio.ConductorAsignado);
            AgregarAsignacion(context, envioId: 100, viajeId: 1, desvioKm: 2m);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Equal(3m, resultado!.DesvioDisponibleKm);
            Assert.Equal(2, resultado.CupoDisponible);
            Assert.Equal(9m, resultado.PesoDisponibleKg);
        }

        [Fact]
        public async Task OptimizarAsync_NoDeberiaDescontarLoAsignadoOtroDia()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarEnvio(context, 100, monto: 3000m, peso: 6m, estado: EstadoEnvio.ConductorAsignado);
            AgregarAsignacion(context, envioId: 100, viajeId: 1, desvioKm: 2m, fecha: Fecha.AddDays(1));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Equal(3, resultado!.CupoDisponible);
            Assert.Equal(10m, resultado.PesoDisponibleKg);
            Assert.Equal(5m, resultado.DesvioDisponibleKm);
        }

        [Fact]
        public async Task OptimizarAsync_NoDeberiaOfrecerEnviosQueNoSonOportunidadesReales()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarEnvio(context, 1, monto: 4000m, peso: 2m);                                   // válida
            AgregarEnvio(context, 2, monto: 9000m, peso: 1m, usuarioId: Conductor);            // es del propio conductor
            AgregarEnvio(context, 3, monto: 9000m, peso: 1m, estado: EstadoEnvio.ConductorAsignado);
            AgregarEnvio(context, 4, monto: 9000m, peso: 1m, estado: EstadoEnvio.Cancelado);
            AgregarEnvio(context, 5, monto: 9000m, peso: 1m);                                   // ya tiene asignación
            AgregarAsignacion(context, envioId: 5, viajeId: 99, desvioKm: 0m);
            AgregarEnvio(context, 6, monto: 9000m, peso: 1m, fechaLimite: Fecha.AddDays(-1));   // vence antes del viaje
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Equal(1, resultado!.CandidatosEvaluados);
            Assert.Equal(new[] { 1 }, resultado.Recomendada!.Paquetes.Select(p => p.Id));
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaIncluirEnviosQueVencenElDiaDelViaje()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarEnvio(context, 1, monto: 4000m, peso: 2m, fechaLimite: Fecha);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Equal(1, resultado!.CandidatosEvaluados);
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaIgnorarEnviosQueNoEntranSolosPorPesoODesvio()
        {
            using var context = CrearContexto();
            AgregarViaje(context, pesoMaxKg: 10m, desvioMaxKm: 3m);
            AgregarEnvio(context, 1, monto: 1000m, peso: 20m);   // pesa más que el máximo
            AgregarEnvio(context, 7, monto: 1000m, peso: 1m);    // desvía 4,0 km, más que el máximo
            AgregarEnvio(context, 2, monto: 1000m, peso: 1m);    // desvía 1,5 km: entra
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Equal(1, resultado!.CandidatosEvaluados);
            Assert.Equal(new[] { 2 }, resultado.Recomendada!.Paquetes.Select(p => p.Id));
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaDevolverSinRecomendacion_CuandoNoHayOportunidades()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.NotNull(resultado);
            Assert.Equal(0, resultado!.CandidatosEvaluados);
            Assert.Null(resultado.Recomendada);
            Assert.Empty(resultado.Alternativas);
            Assert.Empty(resultado.Descartadas);
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaDevolverSinRecomendacion_CuandoElViajeNoTieneCupo()
        {
            using var context = CrearContexto();
            AgregarViaje(context, maxPaquetes: 1);
            AgregarOportunidadesBase(context);
            AgregarEnvio(context, 100, monto: 3000m, peso: 1m, estado: EstadoEnvio.ConductorAsignado);
            AgregarAsignacion(context, envioId: 100, viajeId: 1, desvioKm: 0m);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Equal(0, resultado!.CupoDisponible);
            Assert.Null(resultado.Recomendada);
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaLimitarLosCandidatosParaQuelaBusquedaSeaRapida()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            for (var id = 1; id <= 25; id++)
            {
                AgregarEnvio(context, id, monto: 1000m + id * 10, peso: 1m);
            }
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Equal(OptimizadorService.MaximoCandidatos, resultado!.CandidatosEvaluados);
            // El 23 tendría más ganancia pero desviaría 4,0 km y se pasaría de los 5 km.
            Assert.Equal(new[] { 25, 24, 22 }, resultado.Recomendada!.Paquetes.Select(p => p.Id));
            Assert.Equal(3710m, resultado.Recomendada.GananciaTotal);
        }

        // =====================================================================
        // OptimizarAsync: validaciones del viaje
        // =====================================================================

        [Fact]
        public async Task OptimizarAsync_DeberiaRetornarNull_CuandoElViajeNoExiste()
        {
            using var context = CrearContexto();
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                999, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaRetornarNull_CuandoElViajeEsDeOtroUsuario()
        {
            using var context = CrearContexto();
            AgregarViaje(context, usuarioId: OtroUsuario);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaRechazar_CuandoElViajeEstaInactivo()
        {
            using var context = CrearContexto();
            AgregarViaje(context, activo: false);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.OptimizarAsync(1, Fecha, Conductor, TestContext.Current.CancellationToken));

            Assert.Equal("El viaje no se encuentra activo.", ex.Message);
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaRechazar_CuandoLaFechaEsPasada()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.OptimizarAsync(
                    1, DateTime.Today.AddDays(-1), Conductor, TestContext.Current.CancellationToken));

            Assert.Equal("No se puede optimizar para una fecha pasada.", ex.Message);
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaAceptarLaFechaDeHoy()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.OptimizarAsync(
                1, DateTime.Today, Conductor, TestContext.Current.CancellationToken);

            Assert.NotNull(resultado);
        }

        [Fact]
        public async Task OptimizarAsync_DeberiaRechazar_CuandoLaFechaNoCoincideConLosDiasDelViaje()
        {
            using var context = CrearContexto();
            AgregarViaje(context, dias: new byte[] { DiaDistintoA(Fecha) });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.OptimizarAsync(1, Fecha, Conductor, TestContext.Current.CancellationToken));

            Assert.Equal(
                "La fecha seleccionada no coincide con los días programados para este viaje.",
                ex.Message);
        }

        // =====================================================================
        // ReservarCombinacionAsync
        // =====================================================================

        [Fact]
        public async Task ReservarCombinacionAsync_DeberiaCrearUnaAsignacionPorEnvio()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.ReservarCombinacionAsync(
                1, Fecha, new[] { 1, 8 }, Conductor, TestContext.Current.CancellationToken);

            Assert.True(resultado);
            var asignaciones = await context.Asignaciones
                .OrderBy(a => a.EnvioId)
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(2, asignaciones.Count);
            Assert.All(asignaciones, a =>
            {
                Assert.Equal(1, a.ViajeId);
                Assert.Equal(Fecha, a.FechaViaje);
            });

            var primera = asignaciones[0];   // envío 1: desvío 1,0 km, 4.000 de ganancia
            Assert.Equal(1, primera.EnvioId);
            Assert.Equal(1.0m, primera.DesvioKm);
            Assert.Equal(3, primera.TiempoExtraMin);
            Assert.Equal(4000m, primera.Ganancia);

            var segunda = asignaciones[1];   // envío 8: desvío 0,5 km, 2.500 de ganancia
            Assert.Equal(8, segunda.EnvioId);
            Assert.Equal(0.5m, segunda.DesvioKm);
            Assert.Equal(2, segunda.TiempoExtraMin);
            Assert.Equal(2500m, segunda.Ganancia);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_DeberiaPasarLosEnviosAConductorAsignadoYRegistrarElHistorial()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            await service.ReservarCombinacionAsync(
                1, Fecha, new[] { 1, 8 }, Conductor, TestContext.Current.CancellationToken);

            var reservados = await context.Envios
                .Where(e => e.Id == 1 || e.Id == 8)
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.All(reservados, e => Assert.Equal((byte)EstadoEnvio.ConductorAsignado, e.Estado));

            var historial = await context.EnvioEstadoHistoriales
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal(2, historial.Count);
            Assert.All(historial, h =>
            {
                Assert.Equal((byte)EstadoEnvio.BuscandoConductor, h.EstadoAnterior);
                Assert.Equal((byte)EstadoEnvio.ConductorAsignado, h.EstadoNuevo);
            });

            // Los envíos que no se reservaron quedan como estaban.
            var otro = await context.Envios.FindAsync(
                new object[] { 3 }, TestContext.Current.CancellationToken);
            Assert.Equal((byte)EstadoEnvio.BuscandoConductor, otro!.Estado);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_DespuesDeReservarNoDeberiaVolverAOfrecerLosEnvios()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            await service.ReservarCombinacionAsync(
                1, Fecha, new[] { 1, 8 }, Conductor, TestContext.Current.CancellationToken);
            var resultado = await service.OptimizarAsync(
                1, Fecha, Conductor, TestContext.Current.CancellationToken);

            Assert.Equal(1, resultado!.CupoDisponible);
            Assert.Equal(7m, resultado.PesoDisponibleKg);
            Assert.Equal(3.5m, resultado.DesvioDisponibleKm);
            Assert.DoesNotContain(
                resultado.Recomendada?.Paquetes ?? new(),
                p => p.Id == 1 || p.Id == 8);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_DeberiaRetornarFalse_CuandoElViajeNoExiste()
        {
            using var context = CrearContexto();
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.ReservarCombinacionAsync(
                999, Fecha, new[] { 1 }, Conductor, TestContext.Current.CancellationToken);

            Assert.False(resultado);
            Assert.Empty(context.Asignaciones);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_DeberiaRetornarFalse_CuandoElViajeEsDeOtroUsuario()
        {
            using var context = CrearContexto();
            AgregarViaje(context, usuarioId: OtroUsuario);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var resultado = await service.ReservarCombinacionAsync(
                1, Fecha, new[] { 1 }, Conductor, TestContext.Current.CancellationToken);

            Assert.False(resultado);
            Assert.Empty(context.Asignaciones);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_DeberiaRechazarUnaListaVacia()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReservarCombinacionAsync(
                    1, Fecha, Array.Empty<int>(), Conductor, TestContext.Current.CancellationToken));

            Assert.Equal("Elegí al menos un envío para reservar.", ex.Message);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_DeberiaRechazarEnviosRepetidos()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReservarCombinacionAsync(
                    1, Fecha, new[] { 1, 1 }, Conductor, TestContext.Current.CancellationToken));

            Assert.Equal("La combinación tiene envíos repetidos.", ex.Message);
            Assert.Empty(context.Asignaciones);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_DeberiaPedirRecalcular_CuandoUnEnvioNoExiste()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReservarCombinacionAsync(
                    1, Fecha, new[] { 1, 999 }, Conductor, TestContext.Current.CancellationToken));

            Assert.Equal(MensajeCambio, ex.Message);
            Assert.Empty(context.Asignaciones);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_DeberiaPedirRecalcular_YNoReservarNada_CuandoOtroConductorTomoUnEnvio()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarEnvio(context, 1, monto: 4000m, peso: 2m);
            AgregarEnvio(context, 8, monto: 2500m, peso: 1m, estado: EstadoEnvio.ConductorAsignado);
            AgregarAsignacion(context, envioId: 8, viajeId: 99, desvioKm: 0m);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReservarCombinacionAsync(
                    1, Fecha, new[] { 1, 8 }, Conductor, TestContext.Current.CancellationToken));

            Assert.Equal(MensajeCambio, ex.Message);
            // Es todo o nada: el envío 1 sigue disponible y no se creó ninguna asignación nueva.
            var envio1 = await context.Envios.FindAsync(
                new object[] { 1 }, TestContext.Current.CancellationToken);
            Assert.Equal((byte)EstadoEnvio.BuscandoConductor, envio1!.Estado);
            Assert.Equal(1, await context.Asignaciones.CountAsync(TestContext.Current.CancellationToken));
            Assert.Empty(context.EnvioEstadoHistoriales);
        }

        [Theory]
        [InlineData(EstadoEnvio.ConductorAsignado)]
        [InlineData(EstadoEnvio.Retirado)]
        [InlineData(EstadoEnvio.EnCamino)]
        [InlineData(EstadoEnvio.Entregado)]
        [InlineData(EstadoEnvio.Cancelado)]
        public async Task ReservarCombinacionAsync_DeberiaPedirRecalcular_CuandoElEnvioYaNoBuscaConductor(
            EstadoEnvio estado)
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarEnvio(context, 1, monto: 4000m, peso: 2m, estado: estado);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReservarCombinacionAsync(
                    1, Fecha, new[] { 1 }, Conductor, TestContext.Current.CancellationToken));

            Assert.Equal(MensajeCambio, ex.Message);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_DeberiaPedirRecalcular_CuandoElEnvioVenceAntesDeLaFecha()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarEnvio(context, 1, monto: 4000m, peso: 2m, fechaLimite: Fecha.AddDays(-1));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReservarCombinacionAsync(
                    1, Fecha, new[] { 1 }, Conductor, TestContext.Current.CancellationToken));

            Assert.Equal(MensajeCambio, ex.Message);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_NoDeberiaPermitirReservarLosPropiosEnvios()
        {
            using var context = CrearContexto();
            AgregarViaje(context);
            AgregarEnvio(context, 1, monto: 4000m, peso: 2m, usuarioId: Conductor);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReservarCombinacionAsync(
                    1, Fecha, new[] { 1 }, Conductor, TestContext.Current.CancellationToken));

            Assert.Equal("No podés reservar tus propios envíos.", ex.Message);
            Assert.Empty(context.Asignaciones);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_DeberiaRechazar_CuandoSuperaElCupo()
        {
            using var context = CrearContexto();
            AgregarViaje(context, maxPaquetes: 1);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReservarCombinacionAsync(
                    1, Fecha, new[] { 1, 8 }, Conductor, TestContext.Current.CancellationToken));

            Assert.Equal(
                "La combinación supera la cantidad de paquetes disponibles del viaje.",
                ex.Message);
            Assert.Empty(context.Asignaciones);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_DeberiaRechazar_CuandoSuperaElPeso()
        {
            using var context = CrearContexto();
            AgregarViaje(context, pesoMaxKg: 5m);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            // 2 kg + 6 kg = 8 kg, pero el viaje solo lleva 5 kg.
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReservarCombinacionAsync(
                    1, Fecha, new[] { 1, 3 }, Conductor, TestContext.Current.CancellationToken));

            Assert.Equal("La combinación supera el peso disponible del viaje (5 kg).", ex.Message);
            Assert.Empty(context.Asignaciones);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_DeberiaRechazar_CuandoSuperaElDesvio()
        {
            using var context = CrearContexto();
            AgregarViaje(context, desvioMaxKm: 2m);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            // Desvíos de 1,0 km (envío 1) + 2,0 km (envío 3) = 3,0 km, y el viaje acepta 2 km.
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReservarCombinacionAsync(
                    1, Fecha, new[] { 1, 3 }, Conductor, TestContext.Current.CancellationToken));

            Assert.Equal("La combinación supera el desvío disponible del viaje (2 km).", ex.Message);
            Assert.Empty(context.Asignaciones);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_DeberiaTenerEnCuentaLoYaAsignadoAlValidarLosLimites()
        {
            using var context = CrearContexto();
            AgregarViaje(context, maxPaquetes: 2);
            AgregarOportunidadesBase(context);
            AgregarEnvio(context, 100, monto: 3000m, peso: 1m, estado: EstadoEnvio.ConductorAsignado);
            AgregarAsignacion(context, envioId: 100, viajeId: 1, desvioKm: 0m);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            // Con 1 paquete ya asignado, el cupo de 2 deja lugar para uno solo.
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReservarCombinacionAsync(
                    1, Fecha, new[] { 1, 8 }, Conductor, TestContext.Current.CancellationToken));

            Assert.Equal(
                "La combinación supera la cantidad de paquetes disponibles del viaje.",
                ex.Message);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_DeberiaRechazar_CuandoElViajeEstaInactivo()
        {
            using var context = CrearContexto();
            AgregarViaje(context, activo: false);
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReservarCombinacionAsync(
                    1, Fecha, new[] { 1 }, Conductor, TestContext.Current.CancellationToken));

            Assert.Equal("El viaje no se encuentra activo.", ex.Message);
        }

        [Fact]
        public async Task ReservarCombinacionAsync_DeberiaRechazar_CuandoLaFechaNoCoincideConLosDiasDelViaje()
        {
            using var context = CrearContexto();
            AgregarViaje(context, dias: new byte[] { DiaDistintoA(Fecha) });
            AgregarOportunidadesBase(context);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var service = new OptimizadorService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReservarCombinacionAsync(
                    1, Fecha, new[] { 1 }, Conductor, TestContext.Current.CancellationToken));
            Assert.Empty(context.Asignaciones);
        }

        // =====================================================================
        // Métodos auxiliares
        // =====================================================================

        private static RuteaYaDbContext CrearContexto()
        {
            var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new RuteaYaDbContext(options);
        }

        // Día de la semana con la numeración del repositorio (lunes = 1 ... domingo = 7).
        private static byte DiaDe(DateTime fecha)
        {
            return fecha.DayOfWeek == DayOfWeek.Sunday ? (byte)7 : (byte)fecha.DayOfWeek;
        }

        private static byte DiaDistintoA(DateTime fecha)
        {
            return (byte)(DiaDe(fecha) % 7 + 1);
        }

        private static void AgregarViaje(
            RuteaYaDbContext context,
            int id = 1,
            int usuarioId = Conductor,
            int maxPaquetes = 3,
            decimal pesoMaxKg = 10m,
            decimal desvioMaxKm = 5m,
            bool activo = true,
            byte[]? dias = null)
        {
            context.Viajes.Add(new Viaje
            {
                Id = id,
                UsuarioId = usuarioId,
                OrigenDir = "Belgrano",
                DestinoDir = "Lomas de Zamora",
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(8, 55),
                MaxPaquetes = maxPaquetes,
                PesoMaxKg = pesoMaxKg,
                DesvioMaxKm = desvioMaxKm,
                DistanciaKm = 18.4m,
                DuracionMin = 55,
                Activo = activo,
                ViajeDias = (dias ?? new byte[] { 1, 2, 3, 4, 5, 6, 7 })
                    .Select(d => new ViajeDia { DiaSemana = d })
                    .ToList()
            });
        }

        private static void AgregarEnvio(
            RuteaYaDbContext context,
            int id,
            decimal monto,
            decimal peso,
            int usuarioId = OtroUsuario,
            EstadoEnvio estado = EstadoEnvio.BuscandoConductor,
            DateTime? fechaLimite = null)
        {
            context.Envios.Add(new Envio
            {
                Id = id,
                UsuarioId = usuarioId,
                OrigenDir = $"Origen {id}",
                DestinoDir = $"Destino {id}",
                FechaLimite = fechaLimite ?? Fecha.AddDays(10),
                PesoKg = peso,
                Tamano = 1,
                Descripcion = $"Paquete {id}",
                Monto = monto,
                Estado = (byte)estado,
                CreadoEn = DateTime.Now
            });
        }

        private static void AgregarAsignacion(
            RuteaYaDbContext context,
            int envioId,
            int viajeId,
            decimal desvioKm,
            DateTime? fecha = null)
        {
            context.Asignaciones.Add(new Asignacion
            {
                EnvioId = envioId,
                ViajeId = viajeId,
                FechaViaje = (fecha ?? Fecha).Date,
                DesvioKm = desvioKm,
                Ganancia = 1000m,
                AsignadoEn = DateTime.Now
            });
        }

        // Id: monto, peso (kg) y desvío estimado -> 1: 4.000/2/1,0 · 3: 5.500/6/2,0 · 9: 3.500/4/1,0 · 5: 7.000/8/3,0 · 8: 2.500/1/0,5
        private static void AgregarOportunidadesBase(RuteaYaDbContext context)
        {
            AgregarEnvio(context, 1, monto: 4000m, peso: 2m);
            AgregarEnvio(context, 3, monto: 5500m, peso: 6m);
            AgregarEnvio(context, 9, monto: 3500m, peso: 4m);
            AgregarEnvio(context, 5, monto: 7000m, peso: 8m);
            AgregarEnvio(context, 8, monto: 2500m, peso: 1m);
        }
    }
}
