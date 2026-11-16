
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using RuteaYa.Handlers;
using RuteaYa.Repositorios;

var builder = WebApplication.CreateBuilder(args);

// MVC: controladores y vistas
builder.Services.AddControllersWithViews();

// Cliente HTTP para autenticación
builder.Services.AddHttpClient("RuteaYaApi", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ApiSettings:BaseUrl"]!
    );
});

//Access Token
builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<JwtAuthorizationHandler>();

// Autenticación mediante cookies
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";

        options.Cookie.Name = "RuteaYa.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
    });

// Política de autorización
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Clientes HTTP de las APIs
builder.Services.AddHttpClient("ApiEnvio", client =>
{
    client.BaseAddress = new Uri("https://localhost:7224/");
}).AddHttpMessageHandler<JwtAuthorizationHandler>();

builder.Services.AddHttpClient("ApiViaje", client =>
{
    client.BaseAddress = new Uri("https://localhost:7224/");
}).AddHttpMessageHandler<JwtAuthorizationHandler>();

builder.Services.AddHttpClient("ApiOptimizador", client =>
{
    client.BaseAddress = new Uri("https://localhost:7224/");
}).AddHttpMessageHandler<JwtAuthorizationHandler>();

builder.Services.AddHttpClient("ApiHome", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ApiSettings:BaseUrl"]!);
}).AddHttpMessageHandler<JwtAuthorizationHandler>();

var app = builder.Build();

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// IMPORTANTE: primero autenticación y después autorización
app.UseAuthentication();
app.UseAuthorization();

// Ruta MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}"
);

app.Run();
