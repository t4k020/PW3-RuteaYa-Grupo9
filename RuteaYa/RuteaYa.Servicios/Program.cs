using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using RuteaYa.Repositorios;
using RuteaYa.Servicios;
using RuteaYa.Servicios.Controllers;
using RuteaYa.Servicios.Servicios;
using RuteaYa.Servicios.Servicios.Rutas;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Nombre del esquema de seguridad que se publica en Swagger.
const string EsquemaJwt = "Bearer";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    // Sin esto Swagger UI no muestra el botón Authorize y no hay forma de
    // probar los endpoints protegidos con el token de Api/Auth/Login.
    opciones.AddSecurityDefinition(EsquemaJwt, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = HeaderNames.Authorization,
        Description = "Pegá el token que devuelve POST Api/Auth/Login, sin la palabra Bearer."
    });

    opciones.AddSecurityRequirement(documento =>
    {
        var requisito = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(EsquemaJwt, documento)] = new List<string>()
        };

        return requisito;
    });
});

// Base de datos
builder.Services.AddDbContext<RuteaYaDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Servicios
builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEnvioService, EnvioService>();
builder.Services.AddScoped<IViajeService, ViajeService>();
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<IOptimizadorService, OptimizadorService>();

// Caché en memoria para no pedir la geometría al proveedor en cada vista.
builder.Services.AddMemoryCache();

// Rutas y mapas: proveedor real o mock según configuración.
builder.Services.AddRutas(builder.Configuration);

// Configuración JWT
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Key");

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Inicialización de base de datos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<RuteaYaDbContext>();

        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogError(ex, "Ocurrió un error al poblar la base de datos.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // 2. Genera el JSON de Swagger
    app.UseSwaggerUI(c => // 3. Habilita la interfaz visual gráfica
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RuteaYa API v1");
        c.RoutePrefix = string.Empty; // Esto hace que Swagger abra directo en la raíz (ej: https://localhost:7224/)
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
