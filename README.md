# RuteaYa - PW3 Grupo 9

Plataforma de transporte colaborativo construida con ASP.NET Core MVC, Web API, Entity Framework Core y SQL Server.

## Requisitos

- .NET SDK 10 (la version se fija en `global.json`).
- SQL Server LocalDB o SQL Server Express.

## Ejecucion local

1. Restaurar dependencias y compilar:

   ```powershell
   dotnet restore RuteaYa/RuteaYa.slnx
   dotnet build RuteaYa/RuteaYa.slnx
   ```

2. Crear/aplicar la base de datos desde el proyecto de servicios:

   ```powershell
   dotnet ef database update --project RuteaYa/RuteaYa.Repositorios --startup-project RuteaYa/RuteaYa.Servicios
   ```

3. Iniciar la API y la aplicacion MVC en dos terminales:

   ```powershell
   dotnet run --project RuteaYa/RuteaYa.Servicios
   dotnet run --project RuteaYa/RuteaYa
   ```

La URL de la API se configura en `RuteaYa/RuteaYa/appsettings.json`. Swagger queda disponible al iniciar la API en modo Development. El inicializador carga datos de demostracion cuando ya existen usuarios; se pueden crear cuentas desde la pantalla de registro.

## Pruebas

```powershell
dotnet test RuteaYa/RuteaYa.Tests/RuteaYa.Tests.csproj
```

## Funcionalidades principales

- Registro, inicio y cierre de sesion con JWT entre MVC y API.
- ABM de viajes y envios, con validaciones y transiciones de estado.
- Optimizador de combinaciones de envios y confirmacion atomica de reservas.
- Potencial mensual: proyecta viajes recurrentes futuros, evita reutilizar envios y permite priorizar ganancia, menor desvio o equilibrio.

La estimacion de rutas y desvio actual es una alternativa temporal mientras se integra un proveedor de mapas/geolocalizacion.
