<#
.SYNOPSIS
    Regenera los textos de CodeSweep (inglés / español) desde las tablas TSV.

.DESCRIPTION
    - Strings.tsv   → src/YelcoBot.CodeSweep.Application/Localization/Strings.resx y Strings.es.resx
                      (mensajes, menús, resúmenes; se leen con la clase Strings).
    - VSPackage.tsv → src/YelcoBot.CodeSweep/VSPackage.resx y VSPackage.es.resx
                      (títulos y descripciones de Tools → Options: "@Clave;{guid}" en CodeSweep.registration.json).

    Formato de cada línea: clave <TAB> inglés <TAB> español. Las líneas vacías o que empiezan con # se ignoran.
    Al final verifica que cada "@Clave;" de CodeSweep.registration.json exista en VSPackage.tsv.

.EXAMPLE
    pwsh tools/Localization/Update-Resources.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')

$header = @'
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
'@

function Read-Tsv([string]$path) {
    $keys = @{}
    foreach ($line in Get-Content -Path $path -Encoding UTF8) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { continue }
        $parts = $line -split "`t"
        if ($parts.Count -ne 3) { throw "Línea inválida en $($path): $line" }
        if ($keys.ContainsKey($parts[0])) { throw "Clave repetida en $($path): $($parts[0])" }
        $keys[$parts[0]] = $true
        [pscustomobject]@{ Key = $parts[0]; English = $parts[1]; Spanish = $parts[2] }
    }
}

function Write-Resx([object[]]$rows, [string]$neutralPath, [string]$spanishPath) {
    $en = New-Object System.Text.StringBuilder; [void]$en.Append($header)
    $es = New-Object System.Text.StringBuilder; [void]$es.Append($header)

    foreach ($row in $rows) {
        $key = $row.Key
        [void]$en.AppendLine("  <data name=""$key"" xml:space=""preserve""><value>$([Security.SecurityElement]::Escape($row.English))</value></data>")
        [void]$es.AppendLine("  <data name=""$key"" xml:space=""preserve""><value>$([Security.SecurityElement]::Escape($row.Spanish))</value></data>")
    }

    [void]$en.AppendLine('</root>'); [void]$es.AppendLine('</root>')
    $bom = New-Object System.Text.UTF8Encoding($true)
    [IO.File]::WriteAllText($neutralPath, $en.ToString(), $bom)
    [IO.File]::WriteAllText($spanishPath, $es.ToString(), $bom)
    Write-Host "  $($rows.Count) textos → $(Split-Path $neutralPath -Leaf), $(Split-Path $spanishPath -Leaf)"
}

Write-Host 'Strings (Application):'
$strings = @(Read-Tsv (Join-Path $PSScriptRoot 'Strings.tsv'))
$appLocalization = Join-Path $repo 'src\YelcoBot.CodeSweep.Application\Localization'
Write-Resx $strings (Join-Path $appLocalization 'Strings.resx') (Join-Path $appLocalization 'Strings.es.resx')

Write-Host 'VSPackage (Tools → Options):'
$package = @(Read-Tsv (Join-Path $PSScriptRoot 'VSPackage.tsv'))
$extension = Join-Path $repo 'src\YelcoBot.CodeSweep'
Write-Resx $package (Join-Path $extension 'VSPackage.resx') (Join-Path $extension 'VSPackage.es.resx')

# Cada "@Clave;{guid}" de registration.json debe tener su texto.
$registration = Get-Content (Join-Path $extension 'CodeSweep.registration.json') -Raw
$used = [regex]::Matches($registration, '"@(\w+);') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
$missing = @($used | Where-Object { $_ -notin $package.Key })
if ($missing.Count -gt 0) { throw "Textos usados en CodeSweep.registration.json que faltan en VSPackage.tsv: $($missing -join ', ')" }
Write-Host "OK: los $($used.Count) textos de CodeSweep.registration.json existen."
