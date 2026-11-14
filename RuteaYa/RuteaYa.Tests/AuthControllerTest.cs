using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RuteaYa.Controllers;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace RuteaYa.Tests;

public class AuthControllerTest
{
    private readonly Mock<IAuthenticationService> _authenticationServiceMock = new();

    [Fact]
    public async Task Login_DeberiaIniciarSesionYRedirigirAlHome_CuandoLaApiDevuelveOk()
    {
        var controller = CrearController(_ => RespuestaJson(HttpStatusCode.OK, new
        {
            id = 7,
            nombre = "Ana",
            email = "ana@test.com",
            token = "token-jwt"
        }));

        var resultado = await controller.Login(
            "ana@test.com",
            "contrasena123",
            TestContext.Current.CancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);

        _authenticationServiceMock.Verify(a => a.SignInAsync(
            It.IsAny<HttpContext>(),
            CookieAuthenticationDefaults.AuthenticationScheme,
            It.Is<ClaimsPrincipal>(p =>
                p.FindFirst(ClaimTypes.NameIdentifier)!.Value == "7" &&
                p.FindFirst(ClaimTypes.Name)!.Value == "Ana" &&
                p.FindFirst(ClaimTypes.Email)!.Value == "ana@test.com" &&
                p.FindFirst("AccessToken")!.Value == "token-jwt"),
            It.IsAny<AuthenticationProperties?>()), Times.Once);
    }

    [Fact]
    public async Task Login_DeberiaMostrarError_CuandoLaApiRechazaLasCredenciales()
    {
        var controller = CrearController(_ => RespuestaJson(HttpStatusCode.Unauthorized, new
        {
            mensaje = "Email o contraseña incorrectos."
        }));

        var resultado = await controller.Login(
            "ana@test.com",
            "contrasena-equivocada",
            TestContext.Current.CancellationToken);

        Assert.IsType<ViewResult>(resultado);
        AssertTieneError(controller, "Email o contraseña incorrectos.");
        VerificarSinInicioDeSesion();
    }

    [Theory]
    [InlineData("", "contrasena123")]
    [InlineData("ana@test.com", "")]
    public async Task Login_DeberiaMostrarErrorSinLlamarALaApi_CuandoFaltanCampos(
        string email,
        string password)
    {
        var llamadas = 0;
        var controller = CrearController(_ =>
        {
            llamadas++;
            return RespuestaJson(HttpStatusCode.OK, new { });
        });

        var resultado = await controller.Login(
            email,
            password,
            TestContext.Current.CancellationToken);

        Assert.IsType<ViewResult>(resultado);
        AssertTieneError(controller, "Ingresá tu correo y contraseña.");
        Assert.Equal(0, llamadas);
    }

    [Fact]
    public async Task Login_DeberiaMostrarError_CuandoNoSePuedeConectarConLaApi()
    {
        var controller = CrearController(_ =>
            throw new HttpRequestException("Error de red"));

        var resultado = await controller.Login(
            "ana@test.com",
            "contrasena123",
            TestContext.Current.CancellationToken);

        Assert.IsType<ViewResult>(resultado);
        AssertTieneError(controller, "No se pudo conectar con la API.");
    }

    [Theory]
    [InlineData("{\"id\":7,\"nombre\":\"Ana\"}")]
    [InlineData("{\"id\":7,\"email\":\"ana@test.com\",\"token\":\"token-jwt\"}")]
    [InlineData("{\"id\":7,\"nombre\":\"Ana\",\"email\":\"ana@test.com\"}")]
    public async Task Login_DeberiaMostrarError_CuandoLaRespuestaDeLaApiEstaIncompleta(
        string contenidoJson)
    {
        var controller = CrearController(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(contenidoJson, Encoding.UTF8, "application/json")
        });

        var resultado = await controller.Login(
            "ana@test.com",
            "contrasena123",
            TestContext.Current.CancellationToken);

        Assert.IsType<ViewResult>(resultado);
        AssertTieneError(controller, "La respuesta de la API está incompleta.");
    }

    [Fact]
    public async Task Login_DeberiaMostrarError_CuandoLaApiDevuelveUnJsonInvalido()
    {
        var controller = CrearController(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("esto-no-es-json", Encoding.UTF8, "application/json")
        });

        var resultado = await controller.Login(
            "ana@test.com",
            "contrasena123",
            TestContext.Current.CancellationToken);

        Assert.IsType<ViewResult>(resultado);
        AssertTieneError(controller, "La API devolvió un JSON inválido.");
    }

    [Fact]
    public async Task Register_DeberiaRedirigirAlLogin_CuandoLaApiDevuelveOk()
    {
        var controller = CrearController(_ => RespuestaJson(HttpStatusCode.OK, new
        {
            registrado = true,
            mensaje = "Registro exitoso."
        }));

        var resultado = await controller.Register(
            "Ana",
            "Pérez",
            "ana@test.com",
            "contrasena123",
            TestContext.Current.CancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("Auth", redirect.ControllerName);
        Assert.NotNull(controller.TempData["RegisterSuccess"]);
    }

    [Fact]
    public async Task Register_DeberiaMostrarError_CuandoElEmailYaEstaRegistrado()
    {
        var controller = CrearController(_ => RespuestaJson(HttpStatusCode.Conflict, new
        {
            mensaje = "El email ya está registrado."
        }));

        var resultado = await controller.Register(
            "Ana",
            "Pérez",
            "ana@test.com",
            "contrasena123",
            TestContext.Current.CancellationToken);

        Assert.IsType<ViewResult>(resultado);
        AssertTieneError(
            controller,
            "Ese correo electrónico ya está registrado. Probá con otro.");
    }

    [Fact]
    public async Task Register_DeberiaMostrarError_CuandoLaApiDevuelveBadRequest()
    {
        var controller = CrearController(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest));

        var resultado = await controller.Register(
            "Ana",
            "Pérez",
            "ana@test.com",
            "contrasena123",
            TestContext.Current.CancellationToken);

        Assert.IsType<ViewResult>(resultado);
        AssertTieneError(controller, "Los datos enviados no son válidos.");
    }

    [Fact]
    public async Task Register_DeberiaMostrarError_CuandoLaApiFalla()
    {
        var controller = CrearController(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var resultado = await controller.Register(
            "Ana",
            "Pérez",
            "ana@test.com",
            "contrasena123",
            TestContext.Current.CancellationToken);

        Assert.IsType<ViewResult>(resultado);
        AssertTieneError(
            controller,
            "Ocurrió un error al comunicarse con el servidor.");
    }

    [Fact]
    public async Task Register_DeberiaMostrarError_CuandoNoSePuedeConectarConLaApi()
    {
        var controller = CrearController(_ =>
            throw new HttpRequestException("Error de red"));

        var resultado = await controller.Register(
            "Ana",
            "Pérez",
            "ana@test.com",
            "contrasena123",
            TestContext.Current.CancellationToken);

        Assert.IsType<ViewResult>(resultado);
        AssertTieneError(controller, "No se pudo conectar con el servidor de la API.");
    }

    [Fact]
    public async Task Register_DeberiaMostrarErrorSinLlamarALaApi_CuandoLaContrasenaEsCorta()
    {
        var llamadas = 0;
        var controller = CrearController(_ =>
        {
            llamadas++;
            return RespuestaJson(HttpStatusCode.OK, new { });
        });

        var resultado = await controller.Register(
            "Ana",
            "Pérez",
            "ana@test.com",
            "corta",
            TestContext.Current.CancellationToken);

        Assert.IsType<ViewResult>(resultado);
        AssertTieneError(controller, "La contraseña debe tener al menos 8 caracteres.");
        Assert.Equal(0, llamadas);
    }

    [Fact]
    public async Task Register_DeberiaMostrarErrorSinLlamarALaApi_CuandoFaltanCampos()
    {
        var llamadas = 0;
        var controller = CrearController(_ =>
        {
            llamadas++;
            return RespuestaJson(HttpStatusCode.OK, new { });
        });

        var resultado = await controller.Register(
            "",
            "Pérez",
            "ana@test.com",
            "contrasena123",
            TestContext.Current.CancellationToken);

        Assert.IsType<ViewResult>(resultado);
        AssertTieneError(controller, "Todos los campos son obligatorios.");
        Assert.Equal(0, llamadas);
    }

    [Fact]
    public async Task Logout_DeberiaCerrarSesionYRedirigirAlLogin()
    {
        var controller = CrearController(_ => RespuestaJson(HttpStatusCode.OK, new { }));

        var resultado = await controller.Logout();

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("Auth", redirect.ControllerName);

        _authenticationServiceMock.Verify(a => a.SignOutAsync(
            It.IsAny<HttpContext>(),
            CookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<AuthenticationProperties?>()), Times.Once);
    }

    [Theory]
    [InlineData("Login")]
    [InlineData("Register")]
    public void Get_DeberiaRetornarLaVista_CuandoElUsuarioNoEstaAutenticado(string accion)
    {
        var controller = CrearController(_ => RespuestaJson(HttpStatusCode.OK, new { }));

        var resultado = accion == "Login"
            ? controller.Login()
            : controller.Register();

        Assert.IsType<ViewResult>(resultado);
    }

    [Theory]
    [InlineData("Login")]
    [InlineData("Register")]
    public void Get_DeberiaRedirigirAlHome_CuandoElUsuarioYaEstaAutenticado(string accion)
    {
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "7")
        }, "TestAuthentication"));

        var controller = CrearController(
            _ => RespuestaJson(HttpStatusCode.OK, new { }),
            usuario);

        var resultado = accion == "Login"
            ? controller.Login()
            : controller.Register();

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    private AuthController CrearController(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        ClaimsPrincipal? usuario = null)
    {
        var client = new HttpClient(new StubHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("RuteaYaApi")).Returns(client);

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper
            .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("/");

        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(urlHelper.Object);

        var services = new ServiceCollection();
        services.AddSingleton(_authenticationServiceMock.Object);
        services.AddSingleton(urlHelperFactory.Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = usuario ?? new ClaimsPrincipal(new ClaimsIdentity())
        };

        return new AuthController(factory.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        };
    }

    private static HttpResponseMessage RespuestaJson(
        HttpStatusCode statusCode,
        object contenido)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(contenido)
        };
    }

    private static void AssertTieneError(AuthController controller, string mensaje)
    {
        Assert.Contains(
            controller.ModelState.Values.SelectMany(v => v.Errors),
            error => error.ErrorMessage.Contains(mensaje));
    }

    private void VerificarSinInicioDeSesion()
    {
        _authenticationServiceMock.Verify(a => a.SignInAsync(
            It.IsAny<HttpContext>(),
            It.IsAny<string>(),
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<AuthenticationProperties?>()), Times.Never);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(
            Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                return Task.FromResult(_responder(request));
            }
            catch (Exception ex)
            {
                return Task.FromException<HttpResponseMessage>(ex);
            }
        }
    }
}
