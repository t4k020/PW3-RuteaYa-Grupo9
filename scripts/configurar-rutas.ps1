<#
.SYNOPSIS
Configura la API key compartida del proveedor de rutas (TomTom) usando dotnet user-secrets.

.DESCRIPTION
La key nunca queda en el repositorio: se guarda en el almacén de user-secrets del
usuario actual (%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json).
Si no pasás -ApiKey, el script la pide de forma segura, así no queda en pantalla
ni en el historial de la consola.
Se ejecuta desde la raíz del repo (la carpeta que contiene scripts\): la ruta del
proyecto se resuelve a partir de la ubicación del script, no del directorio actual.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File scripts\configurar-rutas.ps1

.EXAMPLE
powershell -ExecutionPolicy Bypass -File scripts\configurar-rutas.ps1 -ApiKey "tu-key"

.EXAMPLE
powershell -ExecutionPolicy Bypass -File scripts\configurar-rutas.ps1 -Quitar
#>
[CmdletBinding()]
param(
    [string]$ApiKey,
    [switch]$Quitar
)

$ErrorActionPreference = 'Stop'

$proyectoApi = Join-Path $PSScriptRoot '..\RuteaYa\RuteaYa.Servicios'

if (-not (Test-Path $proyectoApi)) {
    throw "No se encontró el proyecto de la API en '$proyectoApi'."
}

# Ejecuta dotnet user-secrets y corta si el comando falla. Los argumentos se
# reciben como arreglo para no interpolar la key en una cadena de comando.
function Invoke-UserSecrets {
    param([string[]]$Argumentos)

    & dotnet user-secrets @Argumentos --project $proyectoApi

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet user-secrets terminó con el código $LASTEXITCODE."
    }
}

Push-Location $PSScriptRoot

try {
    if ($Quitar) {
        # Volver al mock es tolerante a que la key ya no esté guardada.
        & dotnet user-secrets remove "Rutas:ApiKey" --project $proyectoApi | Out-Null
        Invoke-UserSecrets @('set', 'Rutas:Proveedor', 'Mock')

        Write-Host 'Listo: se borró la API key y el proveedor quedó en Mock.'
        return
    }

    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        $segura = Read-Host 'API key de TomTom' -AsSecureString
        $ApiKey = [System.Net.NetworkCredential]::new('', $segura).Password
    }

    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        throw 'La API key no puede estar vacía.'
    }

    Invoke-UserSecrets @('set', 'Rutas:Proveedor', 'TomTom')
    Invoke-UserSecrets @('set', 'Rutas:ApiKey', $ApiKey)

    # Sólo se informa la longitud: el valor no se imprime nunca.
    Write-Host "Listo: proveedor TomTom con una key de $($ApiKey.Length) caracteres."
    Write-Host 'Reiniciá la API para que tome la configuración.'
}
finally {
    Pop-Location
}
