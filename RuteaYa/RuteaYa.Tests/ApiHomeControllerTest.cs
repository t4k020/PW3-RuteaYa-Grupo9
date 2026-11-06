using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RuteaYa.Servicios.Controllers;
using RuteaYa.Servicios.Dtos;
using RuteaYa.Servicios.Servicios;
using System.Security.Claims;
using Xunit;

namespace RuteaYa.Tests;

public class ApiHomeControllerTest
{
    [Fact]
    public async Task Obtener_DeberiaRetornarUnauthorized_CuandoNoHayClaimDeUsuario()
    {
        var service = new Mock<IHomeService>();
        var controller = CrearController(service.Object, new ClaimsPrincipal(new ClaimsIdentity()));

        var resultado = await controller.Obtener(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(resultado);
        service.Verify(s => s.ObtenerDashboardAsync(
            It.IsAny<int>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Obtener_DeberiaRetornarUnauthorized_CuandoElClaimNoEsUnEntero()
    {
        var service = new Mock<IHomeService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "usuario-invalido")
        }, "TestAuthentication"));
        var controller = CrearController(service.Object, user);

        var resultado = await controller.Obtener(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(resultado);
        service.Verify(s => s.ObtenerDashboardAsync(
            It.IsAny<int>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Obtener_DeberiaRetornarOkConDashboard_CuandoElUsuarioEstaAutenticado()
    {
        var dashboard = new HomeDashboardDto { NombreUsuario = "Ana" };
        var service = new Mock<IHomeService>();
        service
            .Setup(s => s.ObtenerDashboardAsync(
                7,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboard);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "7")
        }, "TestAuthentication"));
        var controller = CrearController(service.Object, user);

        var resultado = await controller.Obtener(TestContext.Current.CancellationToken);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Same(dashboard, ok.Value);
        service.Verify(s => s.ObtenerDashboardAsync(
            7,
            It.IsAny<DateTime>(),
            TestContext.Current.CancellationToken), Times.Once);
    }

    private static ApiHomeController CrearController(
        IHomeService service,
        ClaimsPrincipal user)
    {
        return new ApiHomeController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
    }
}
