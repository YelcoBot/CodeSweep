<#
.SYNOPSIS
    Compila CodeSweep en Release y lo publica en el Visual Studio Marketplace.

.PARAMETER Token
    Personal Access Token de Azure DevOps con el scope "Marketplace (Manage)".
    Si no se pasa, se lee de la variable de entorno VS_MARKETPLACE_TOKEN.

.PARAMETER SkipBuild
    Publica el .vsix ya compilado sin volver a compilar.

.EXAMPLE
    .\Publish-Marketplace.ps1 -Token "xxxxxxxx"
#>
param(
    [string] $Token = $env:VS_MARKETPLACE_TOKEN,
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Token)) {
    throw "Falta el token. Usa -Token o define la variable de entorno VS_MARKETPLACE_TOKEN."
}

$profileDir  = $PSScriptRoot
$projectDir  = Resolve-Path (Join-Path $profileDir '..\..')
$projectFile = Join-Path $projectDir 'YelcoBot.CodeSweep.csproj'
$manifest    = Join-Path $projectDir 'Marketplace\publishManifest.json'

# Visual Studio más reciente con el workload de extensiones.
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$vsPath  = & $vswhere -latest -prerelease -requires Microsoft.VisualStudio.Workload.VisualStudioExtension -property installationPath
if (-not $vsPath) {
    throw "No se encontró Visual Studio con el workload 'Desarrollo de extensiones de Visual Studio'."
}

$msbuild       = Join-Path $vsPath 'MSBuild\Current\Bin\MSBuild.exe'
$vsixPublisher = Join-Path $vsPath 'VSSDK\VisualStudioIntegration\Tools\Bin\VsixPublisher.exe'

if (-not $SkipBuild) {
    Write-Host "Compilando Release..." -ForegroundColor Cyan
    & $msbuild $projectFile -restore -p:Configuration=Release -v:m -nologo
    if ($LASTEXITCODE -ne 0) { throw "La compilación falló." }
}

$vsix = Get-ChildItem (Join-Path $projectDir 'bin\Release') -Filter '*.vsix' -Recurse |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
if (-not $vsix) {
    throw "No se encontró el .vsix en bin\Release."
}

Write-Host "Publicando $($vsix.FullName)..." -ForegroundColor Cyan
& $vsixPublisher publish -payload $vsix.FullName -publishManifest $manifest -personalAccessToken $Token
if ($LASTEXITCODE -ne 0) { throw "VsixPublisher falló." }

Write-Host "Publicado." -ForegroundColor Green
