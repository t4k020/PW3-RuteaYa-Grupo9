using System.ComponentModel.DataAnnotations;
using RuteaYa.Servicios.Dtos;
using Xunit;

namespace RuteaYa.Tests;

public class CrearEnvioDtoTests
{
    [Fact]
    public void PesoCero_MuestraMensajeEnEspanol()
    {
        var modelo = CrearModeloValido();
        modelo.PesoKg = 0;

        var errores = Validar(modelo);

        Assert.Contains(errores, error => error.ErrorMessage == "El peso debe ser mayor que cero.");
    }

    [Fact]
    public void MontoCero_MuestraMensajeEnEspanol()
    {
        var modelo = CrearModeloValido();
        modelo.Monto = 0;

        var errores = Validar(modelo);

        Assert.Contains(errores, error => error.ErrorMessage == "El monto ofrecido debe ser mayor que cero.");
    }

    [Fact]
    public void TamanoNoSeleccionado_MuestraMensajeEnEspanol()
    {
        var modelo = CrearModeloValido();
        modelo.Tamano = 0;

        var errores = Validar(modelo);

        Assert.Contains(errores, error => error.ErrorMessage == "Seleccioná un tamaño válido.");
    }

    [Fact]
    public void DescripcionMayorASetentaCaracteres_MuestraMensajeEnEspanol()
    {
        var modelo = CrearModeloValido();
        modelo.Descripcion = new string('a', 71);

        var errores = Validar(modelo);

        Assert.Contains(errores, error => error.ErrorMessage == "La descripción no puede superar los 70 caracteres.");
    }

    private static CrearEnvioDto CrearModeloValido()
    {
        return new CrearEnvioDto
        {
            OrigenDir = "Origen",
            DestinoDir = "Destino",
            FechaLimite = DateTime.Today,
            PesoKg = 1,
            Tamano = 1,
            Descripcion = "Paquete",
            Monto = 100
        };
    }

    private static List<ValidationResult> Validar(CrearEnvioDto modelo)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(
            modelo,
            new ValidationContext(modelo),
            resultados,
            validateAllProperties: true);
        return resultados;
    }
}
