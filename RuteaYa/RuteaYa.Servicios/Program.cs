using Microsoft.EntityFrameworkCore;
using RuteaYa.Repositorios;
using RuteaYa.Servicios;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RuteaYa.Repositorios;
using RuteaYa.Servicios.Controllers;
using RuteaYa.Servicios.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Registrar tu DbContext
builder.Services.AddDbContext<RuteaYaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEnvioService, EnvioService>();
builder.Services.AddScoped<IViajeService, ViajeService>();

var app = builder.Build();

// Inicializar la base de datos y cargar los datos de prueba
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<RuteaYaDbContext>();

    DbInitializer.Initialize(context);
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

app.UseAuthorization();

app.MapControllers();

app.Run();
