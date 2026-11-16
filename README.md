# RuteaYa — Transporte colaborativo de paquetes

TP Integrador de Programación Web 3 (2026, 2C).

RuteaYa conecta a personas que viajan seguido con personas que necesitan enviar un paquete por la misma ruta.
Un mismo usuario puede ser conductor y remitente: no hay roles fijos.

## Stack

- ASP.NET Core MVC (front)
- ASP.NET Core Web API REST (back)
- Entity Framework Core + SQL Server
- xUnit + Moq (tests)

## Arquitectura

```
MVC (Controllers/Views)
   │  HttpClient tipado + DTOs
   ▼
Web API (Controllers)
   ▼
Services (lógica de negocio)
   ▼
Acceso a datos (DbContext / Repositorios)
   ▼
SQL Server
```

El proyecto MVC **no** conoce `DbContext` ni las entidades de EF: solo consume la API.

| Proyecto | Rol |
| --- | --- |
| `RuteaYa.Servicios` | Web API + Services + DTOs |
| `RuteaYa` | MVC |
| `RuteaYa.Entidades` | Entidades de dominio |
| `RuteaYa.Repositorios` | `DbContext`, migraciones, seed |
| `RuteaYa.Tests` | Tests unitarios |

## Requisitos

- .NET SDK 10
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) o una instancia de SQL Server
- Opcional: `dotnet-ef` (`dotnet tool install --global dotnet-ef`)

## Configuración

La cadena de conexión y la clave JWT están en `RuteaYa/RuteaYa.Servicios/appsettings.json`.
Para usar otra base, cambiar `ConnectionStrings:DefaultConnection`.

Las credenciales reales **no se versionan**: usar `dotnet user-secrets` o variables de entorno.

## Cómo ejecutar

Desde la carpeta de la solución (`RuteaYa/`):

```bash
# 1) API (crea la base y carga los datos iniciales la primera vez)
dotnet run --project RuteaYa.Servicios

# 2) MVC (en otra terminal)
dotnet run --project RuteaYa
```

- API: <https://localhost:7224> — Swagger se abre en la raíz.
- MVC: <https://localhost:7223>
- La API se inicializa con `EnsureCreated()` + seed al arrancar.

### Migraciones (cambios de esquema)

```bash
dotnet ef migrations add NombreDescriptivo --project RuteaYa.Repositorios --startup-project RuteaYa.Servicios
dotnet ef database update --project RuteaYa.Repositorios --startup-project RuteaYa.Servicios
```

No modificar migraciones ya aplicadas: crear una nueva.

> Si ya tenías una base `RuteaYaDb` local, **borrala** para que el seed se vuelva a cargar con las
> direcciones nuevas: `sqllocaldb stop MSSQLLocalDB && sqllocaldb delete MSSQLLocalDB`
> (o `DROP DATABASE RuteaYaDb`). El seed solo corre con la tabla `Usuarios` vacía.

## Rutas y mapas

La integración con el proveedor de mapas está detrás de `IRutaService`
(`RuteaYa.Servicios/Servicios/Rutas`). Hay dos implementaciones:

| Implementación | Cuándo se usa |
| --- | --- |
| `RutaServiceMock` | Por defecto, sin API key ni internet |
| `TomTomRutaService` | `Rutas:Proveedor = "TomTom"` + API key configurada |

El mock geocodifica contra un catálogo de direcciones de AMBA y, para cualquier otra dirección,
devuelve una coordenada estable derivada del texto. Las distancias se calculan con Haversine ×
factor de sinuosidad y la duración con una velocidad promedio, así el resultado es determinístico.

Configuración (`RuteaYa.Servicios/appsettings.json`, sección `Rutas`):

```json
"Rutas": {
  "Proveedor": "Mock",
  "ApiKey": "",
  "BaseUrl": "https://api.tomtom.com",
  "PaisBusqueda": "AR",
  "TimeoutSegundos": 10,
  "Mock": { "FactorSinuosidad": 1.3, "VelocidadKmH": 30 }
}
```

### Usar TomTom en local

El proyecto no necesita `dotnet user-secrets init`: el `UserSecretsId` ya está en el `.csproj`.
Para cargar la key hay un script que evita escribirla en el historial de la consola.

> Corré estos comandos **desde la raíz del repo** (la carpeta que contiene `scripts/`), no desde
> `RuteaYa/`: la ruta `scripts\configurar-rutas.ps1` es relativa a la raíz.

```powershell
# Pide la key de forma segura y deja el proveedor en TomTom
powershell -ExecutionPolicy Bypass -File scripts\configurar-rutas.ps1

# Variante sin prompt (útil para scripts de setup)
powershell -ExecutionPolicy Bypass -File scripts\configurar-rutas.ps1 -ApiKey "<tu-api-key>"

# Volver al mock y borrar la key del equipo
powershell -ExecutionPolicy Bypass -File scripts\configurar-rutas.ps1 -Quitar
```

`-ExecutionPolicy Bypass` se aplica sólo a ese proceso: no cambia la configuración de seguridad del
equipo y evita el error "la ejecución de scripts está deshabilitada en este sistema", que es el que
da Windows por defecto.

Si preferís no escribir la invocación completa, `scripts\configurar-rutas.cmd` es un envoltorio que
hace lo mismo (agrega `-ExecutionPolicy Bypass` y resuelve la ruta solo) y acepta los mismos
parámetros, desde cualquier carpeta del repo:

```bat
rem Pide la key y deja el proveedor en TomTom
scripts\configurar-rutas.cmd

rem Vuelve al mock y borra la key
scripts\configurar-rutas.cmd -Quitar
```

También se puede ejecutar con doble clic; si algo falla, la ventana queda abierta para leer el error.

El script sólo orquesta `dotnet user-secrets`: la key queda en
`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`, fuera del repositorio.
Reiniciá la API después de correrlo.

> **Key compartida del equipo.** Usamos una sola key para todos. Se pasa por el grupo (nunca por el
> repo) y cada integrante corre el script una vez. El que no la configure sigue trabajando con el
> mock sin tocar nada, y correr el script con `-Quitar` deja el entorno como recién clonado.

Equivalente manual:

```bash
cd RuteaYa/RuteaYa.Servicios
dotnet user-secrets set "Rutas:Proveedor" "TomTom"
dotnet user-secrets set "Rutas:ApiKey" "<tu-api-key>"
dotnet user-secrets list   # verificar
```

En servidores y CI (donde los user-secrets no aplican) usar variables de entorno:
`Rutas__Proveedor=TomTom` y `Rutas__ApiKey=<tu-api-key>`.
Si `Proveedor` está en `TomTom` pero falta la key, la API avisa por log y arranca con el mock.

Al crear o editar un viaje y al crear un envío, la API geocodifica las direcciones y calcula
la ruta: las coordenadas, la distancia y la duración las define el servidor, nunca el cliente.

### Mapa del recorrido

El detalle de un viaje dibuja el trazado con **Leaflet + OpenStreetMap**: la API devuelve la
geometría en `GET api/ApiViaje/{id}/recorrido` y el MVC la pasa a la vista. En la pantalla de alta,
el botón **Mostrar ruta** pide una previsualización (`POST api/ApiViaje/recorrido/previsualizar`)
sin guardar el viaje.

- Leaflet 1.9.4 está vendorizado en `wwwroot/lib/leaflet` (no depende de un CDN).
- Los tiles se descargan de `tile.openstreetmap.org`: el navegador necesita internet, aunque la
  API use el mock.
- La geometría **no se persiste**: se calcula on-demand y se cachea en memoria 10 minutos por
  viaje y conjunto de coordenadas.
- Donde se muestran datos de ruta se incluye la atribución `© OpenStreetMap contributors` y el
  proveedor que resolvió el trazado (`TomTom` o `Mock`).

## Usuarios de prueba

| Email | Contraseña |
| --- | --- |
| `admin@ruteaya.com` | `1234` |
| `carlos.gonzalez@example.com` | `1234` |
| `lucia.martinez@example.com` | `1234` |

El seed incluye viajes (uno desactivado y uno con paradas), envíos en todos los estados, historial
y asignaciones, con direcciones que el mock resuelve.

## Tests

```bash
cd RuteaYa
dotnet test --solution RuteaYa.slnx
```

Los tests usan el servicio de rutas mock: no dependen de internet ni de una API key.
`dotnet test` corre en modo Microsoft.Testing.Platform (`global.json`): **no** pasar `--nologo`,
porque termina con 0 tests ejecutados.
