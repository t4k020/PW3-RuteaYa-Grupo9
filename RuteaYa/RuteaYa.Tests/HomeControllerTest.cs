using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RuteaYa.Controllers;
using RuteaYa.Servicios.Dtos;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RuteaYa.Tests;

public class HomeControllerTest
{
    [Fact]
    public async Task DeberiaMostrarElDashboardDevueltoPorLaApi()
    {
        var dashboard = new HomeDashboardDto
        {
            NombreUsuario = "Juan",
            CantidadViajes = 2,
            CantidadEnvios = 1,
            CantidadOportunidades = 3,
            PotencialGanancias = 4500m
        };
        var factory = CrearFactory(HttpStatusCode.OK, dashboard);
        var controller = new HomeController(factory.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var resultado = await controller.Index(CancellationToken.None);

        var view = resultado.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeEquivalentTo(dashboard);
    }

    [Fact]
    public async Task DeberiaMostrarUnDashboardVacioCuandoLaApiFalla()
    {
        var factory = CrearFactory(HttpStatusCode.ServiceUnavailable, null);
        var controller = new HomeController(factory.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var resultado = await controller.Index(CancellationToken.None);

        var view = resultado.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeOfType<HomeDashboardDto>();
        controller.TempData["Error"].Should().NotBeNull();
    }

    private static Mock<IHttpClientFactory> CrearFactory(
        HttpStatusCode statusCode,
        HomeDashboardDto? dashboard)
    {
        var handler = new StubHttpMessageHandler(statusCode, dashboard);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/")
        };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("ApiHome")).Returns(client);
        return factory;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly HomeDashboardDto? _dashboard;

        public StubHttpMessageHandler(HttpStatusCode statusCode, HomeDashboardDto? dashboard)
        {
            _statusCode = statusCode;
            _dashboard = dashboard;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                RequestMessage = request
            };

            if (_dashboard is not null)
            {
                response.Content = JsonContent.Create(_dashboard);
            }

            return Task.FromResult(response);
        }
    }
}
