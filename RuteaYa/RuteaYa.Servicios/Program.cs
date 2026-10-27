var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();


// Learn more about configuring Swagger at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // 1. Registramos el generador de Swagger

var app = builder.Build();

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
