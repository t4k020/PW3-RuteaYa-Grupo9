using System.ComponentModel.DataAnnotations;
using RuteaYa.Servicios.Dtos;
using Xunit;

namespace RuteaYa.Tests;

public class CrearViajeDtoTests
{
    [Fact]
    public void ViajeValido_NoTieneErrores()
    {
        var dto = CrearViajeValido();

        var errores = Validar(dto);

        Assert.Empty(errores);
    }

    [Fact]
    public void OrigenYDestinoIguales_DevuelveError()
    {
        var dto = CrearViajeValido();
        dto.DestinoDir = dto.OrigenDir;

        var errores = Validar(dto);

        Assert.Contains(errores, error => error.ErrorMessage!.Contains("origen y el destino"));
    }

    [Fact]
    public void SinDias_DevuelveError()
    {
        var dto = CrearViajeValido();
        dto.DiasSemana.Clear();

        var errores = Validar(dto);

        Assert.Contains(errores, error =>
            error.MemberNames.Contains(nameof(CrearViajeDto.DiasSemana)));
    }

    [Fact]
    public void DiaFueraDeRango_DevuelveError()
    {
        var dto = CrearViajeValido();
        dto.DiasSemana = new List<byte> { 1, 8 };

        var errores = Validar(dto);

        Assert.Contains(errores, error => error.ErrorMessage!.Contains("días"));
    }

    [Fact]
    public void DiasRepetidos_DevuelveError()
    {
        var dto = CrearViajeValido();
        dto.DiasSemana = new List<byte> { 1, 1 };

        var errores = Validar(dto);

        Assert.Contains(errores, error => error.ErrorMessage!.Contains("repetir un día"));
    }

    [Fact]
    public void HoraLlegadaAnteriorASalida_DevuelveError()
    {
        var dto = CrearViajeValido();
        dto.HoraSalida = new TimeOnly(10, 0);
        dto.HoraLlegada = new TimeOnly(9, 0);

        var errores = Validar(dto);

        Assert.Contains(errores, error =>
            error.MemberNames.Contains(nameof(CrearViajeDto.HoraLlegada)));
    }

    [Fact]
    public void ParadaRepetida_DevuelveError()
    {
        var dto = CrearViajeValido();
        dto.Paradas = new List<ParadaViajeDto>
        {
            new() { Direccion = "Av. Rivadavia 1000" },
            new() { Direccion = "av. rivadavia 1000" }
        };

        var errores = Validar(dto);

        Assert.Contains(errores, error => error.ErrorMessage!.Contains("repetir una parada"));
    }

    [Fact]
    public void ParadaIgualAlOrigen_DevuelveError()
    {
        var dto = CrearViajeValido();
        dto.Paradas = new List<ParadaViajeDto>
        {
            new() { Direccion = dto.OrigenDir }
        };

        var errores = Validar(dto);

        Assert.Contains(errores, error => error.ErrorMessage!.Contains("igual al origen"));
    }

    [Fact]
    public void ParadaIgualAlDestino_DevuelveError()
    {
        var dto = CrearViajeValido();
        dto.Paradas = new List<ParadaViajeDto>
        {
            new() { Direccion = dto.DestinoDir }
        };

        var errores = Validar(dto);

        Assert.Contains(errores, error => error.ErrorMessage!.Contains("igual al destino"));
    }

    [Fact]
    public void ParadaVacia_DevuelveError()
    {
        var dto = CrearViajeValido();
        dto.Paradas = new List<ParadaViajeDto>
        {
            new() { Direccion = string.Empty }
        };

        var errores = Validar(dto);

        Assert.Contains(errores, error =>
            error.MemberNames.Contains("Paradas[0].Direccion"));
    }

    private static List<ValidationResult> Validar(CrearViajeDto dto)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            resultados,
            validateAllProperties: true);
        return resultados;
    }

    private static CrearViajeDto CrearViajeValido()
    {
        return new CrearViajeDto
        {
            OrigenDir = "Av. Cabildo 1234, CABA",
            DestinoDir = "Av. Hipólito Yrigoyen 5678, Lanús",
            HoraSalida = new TimeOnly(8, 0),
            HoraLlegada = new TimeOnly(10, 0),
            DiasSemana = new List<byte> { 1, 3, 5 },
            MaxPaquetes = 3,
            PesoMaxKg = 10,
            DesvioMaxKm = 5,
            Paradas = new List<ParadaViajeDto>
            {
                new() { Direccion = "Av. Rivadavia 1000, CABA" }
            }
        };
    }
}
