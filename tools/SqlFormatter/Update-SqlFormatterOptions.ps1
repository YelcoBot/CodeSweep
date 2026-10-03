<#
.SYNOPSIS
    Regenera las opciones del formateador T-SQL de CodeSweep desde SqlFormatterOptions.tsv.

.DESCRIPTION
    SqlFormatterOptions.tsv tiene una línea por propiedad de ScriptDOM (SqlScriptGeneratorOptions):
        Nombre <TAB> grupo <TAB> default de CodeSweep <TAB> clave .editorconfig <TAB> título inglés <TAB> título español

    El script:
    1. Verifica contra el ScriptDOM de Directory.Packages.props que estén TODAS sus propiedades y ninguna de más,
       y que cada default sea válido (enum / bool / int).
    2. Regenera (archivos completos, se puede correr varias veces):
       - src/YelcoBot.CodeSweep.Domain/Options/SqlFormatterCatalog.cs
       - src/YelcoBot.CodeSweep.Infrastructure.VisualStudio/Settings/SqlFormatterUIContexts.cs (conserva los GUID existentes)
       - src/YelcoBot.CodeSweep.Infrastructure.VisualStudio/Settings/ClassicOptions.SqlFormatter.cs
    3. Reemplaza el bloque "Formateador SQL (ScriptDOM)" de CodeSweep.registration.json.
    4. Reemplaza los títulos Setting_SqlFmt_* de tools/Localization/VSPackage.tsv y regenera los .resx.
    5. Sube el CacheTag de CodeSweep.Settings.pkgdef (VS vuelve a leer registration.json).

    Los grupos (general, alignment, formatting, indentation, multiline, newLine, spacing) son las categorías
    codeSweep.sqlFormatter.* de registration.json; un grupo nuevo hay que agregarlo allí a mano.

.EXAMPLE
    # Tras actualizar Microsoft.SqlServer.TransactSql.ScriptDom en Directory.Packages.props (y hacer restore):
    pwsh tools/SqlFormatter/Update-SqlFormatterOptions.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$guid = '{a2f2b3c4-1111-4222-3333-444455556666}'
$noBom = New-Object System.Text.UTF8Encoding($false)

# Rangos de los enteros (los mismos que documenta SSMS).
$ranges = @{
    IndentationSize                = @(1, 8)
    LeadingCommaSpaceCount         = @(0, 1)
    NumNewlinesAfterStatement      = @(0, 10)
    NumNewlinesAfterBatchStatement = @(0, 10)
    NumNewlinesAfterBatches        = @(0, 10)
}

# --- ScriptDOM (la versión de Directory.Packages.props, desde la caché de NuGet) ---
[xml]$packages = Get-Content (Join-Path $repo 'Directory.Packages.props')
$version = ($packages.Project.ItemGroup.PackageVersion | Where-Object Include -eq 'Microsoft.SqlServer.TransactSql.ScriptDom').Version
$dll = Join-Path $env:USERPROFILE ".nuget\packages\microsoft.sqlserver.transactsql.scriptdom\$version\lib\net472\Microsoft.SqlServer.TransactSql.ScriptDom.dll"
if (-not (Test-Path $dll)) { throw "No está $dll. Haz 'dotnet restore' primero." }

$assembly = [Reflection.Assembly]::LoadFrom($dll)
$optionsType = $assembly.GetType('Microsoft.SqlServer.TransactSql.ScriptDom.SqlScriptGeneratorOptions')
$properties = @{}
$optionsType.GetProperties() | Where-Object CanWrite | ForEach-Object { $properties[$_.Name] = $_ }

# --- Especificación ---
$rows = @(Get-Content (Join-Path $PSScriptRoot 'SqlFormatterOptions.tsv') -Encoding UTF8 |
    Where-Object { $_ -and -not $_.StartsWith('#') } |
    ForEach-Object {
        $p = $_ -split "`t"
        if ($p.Count -ne 6) { throw "Línea inválida: $_" }
        [pscustomobject]@{ Name = $p[0]; Group = $p[1]; Default = $p[2]; Key = $p[3]; English = $p[4]; Spanish = $p[5] }
    })

$missing = @($properties.Keys | Where-Object { $_ -notin $rows.Name })
$extra = @($rows.Name | Where-Object { -not $properties.ContainsKey($_) })
if ($missing.Count -or $extra.Count) {
    throw "SqlFormatterOptions.tsv no coincide con ScriptDOM $version. Faltan: $($missing -join ', ') / Sobran: $($extra -join ', ')"
}

# GUID de UIContext existentes: se conservan para no cambiar la visibilidad de opciones ya publicadas.
$contextsPath = Join-Path $repo 'src\YelcoBot.CodeSweep.Infrastructure.VisualStudio\Settings\SqlFormatterUIContexts.cs'
$existingGuids = @{}
if (Test-Path $contextsPath) {
    [regex]::Matches((Get-Content $contextsPath -Raw), '\["(\w+)"\] = new Guid\("([0-9a-fA-F-]+)"\)') | ForEach-Object { $existingGuids[$_.Groups[1].Value] = $_.Groups[2].Value }
}

function Get-EnumTypeName([Type]$type) {
    # SqlVersion → SqlVersionValue, KeywordCasing → SqlKeywordCasing (sin "SqlSql…").
    if ($type.Name.StartsWith('Sql')) { return "$($type.Name)Value" }
    return "Sql$($type.Name)"
}

function Get-PropertyName([string]$name) {
    # Página clásica: SqlKeywordCasing, pero SqlVersion (no "SqlSqlVersion").
    if ($name.StartsWith('Sql')) { return $name }
    return "Sql$name"
}

# Sangría de registration.json (puede estar reformateado): la de una propiedad y la de sus campos.
$registrationPath = Join-Path $repo 'src\YelcoBot.CodeSweep\CodeSweep.registration.json'
$registration = [IO.File]::ReadAllText($registrationPath)
$indent = [regex]::Match($registration, '\n([ \t]+)"codeSweep\.general\.cleanupOnSave": \{\r?\n([ \t]+)"')
if (-not $indent.Success) { throw 'No se pudo detectar la sangría de CodeSweep.registration.json.' }
$pi = $indent.Groups[1].Value
$ii = $indent.Groups[2].Value

$json = New-Object Text.StringBuilder
$catalog = New-Object Text.StringBuilder
$contexts = New-Object Text.StringBuilder
$classic = New-Object Text.StringBuilder
$classicMap = New-Object Text.StringBuilder
$titles = New-Object Collections.Generic.List[string]
$enums = [ordered]@{}
$order = @{}
$nl = "`r`n"

foreach ($row in $rows) {
    $type = $properties[$row.Name].PropertyType
    $camel = $row.Name.Substring(0, 1).ToLower() + $row.Name.Substring(1)
    $contextGuid = if ($existingGuids.ContainsKey($row.Name)) { $existingGuids[$row.Name] } else { [guid]::NewGuid().ToString() }
    if (-not $order.ContainsKey($row.Group)) { $order[$row.Group] = 0 }
    $index = $order[$row.Group]; $order[$row.Group]++

    # registration.json
    [void]$json.Append("$pi`"codeSweep.sqlFormatter.$($row.Group).$camel`": {$nl")
    if ($type -eq [bool]) {
        if ($row.Default -notin @('true', 'false')) { throw "Default inválido $($row.Name)=$($row.Default) (true/false)" }
        [void]$json.Append("$ii`"type`": `"boolean`",$nl"); $jsonDefault = $row.Default
    }
    elseif ($type -eq [int]) {
        [int]$null = $row.Default
        [void]$json.Append("$ii`"type`": `"integer`",$nl"); $jsonDefault = $row.Default
    }
    else {
        $names = [Enum]::GetNames($type)
        if ($row.Default -notin $names) { throw "Default inválido $($row.Name)=$($row.Default) (valores: $($names -join ', '))" }
        [void]$json.Append("$ii`"type`": `"string`",$nl")
        $enumList = ($names | ForEach-Object { '"' + $_ + '"' }) -join ', '
        [void]$json.Append("$ii`"enum`": [ $enumList ],$nl")
        $jsonDefault = "`"$($row.Default)`""
        $enums[$type.Name] = $names
    }
    [void]$json.Append("$ii`"title`": `"@Setting_SqlFmt_$($row.Name);$guid`",$nl")
    [void]$json.Append("$ii`"description`": `"editorconfig: $($row.Key)`",$nl")
    [void]$json.Append("$ii`"default`": $jsonDefault,$nl")
    if ($ranges.ContainsKey($row.Name)) {
        [void]$json.Append("$ii`"minimum`": $($ranges[$row.Name][0]),$nl$ii`"maximum`": $($ranges[$row.Name][1]),$nl")
    }
    [void]$json.Append("$ii`"order`": $index,$nl")
    [void]$json.Append("$ii`"visibleWhen`": `"`${uiContext:$contextGuid} != 'true'`"$nl")
    [void]$json.Append("$pi},$nl")

    # C#
    [void]$catalog.Append("            new SqlFormatterOption(`"$($row.Name)`", `"$($row.Group)`", `"$($row.Default)`", `"$($row.Key)`"),$nl")
    [void]$contexts.Append("            [`"$($row.Name)`"] = new Guid(`"$contextGuid`"),$nl")

    $csType = if ($type -eq [bool]) { 'bool' } elseif ($type -eq [int]) { 'int' } else { Get-EnumTypeName $type }
    $csDefault = if ($type -eq [bool] -or $type -eq [int]) { $row.Default } else { "$csType.$($row.Default)" }
    [void]$classic.Append("        [Category(SqlFormatter), DisplayName(`"$($row.English.Replace('"', '\"'))`"), Description(`"editorconfig: $($row.Key)`")]$nl")
    [void]$classic.Append("        [DefaultValue($csDefault)]$nl")
    [void]$classic.Append("        public $csType $(Get-PropertyName $row.Name) { get; set; } = $csDefault;$nl$nl")
    $value = if ($type -eq [bool]) { "$(Get-PropertyName $row.Name) ? `"true`" : `"false`"" } else { "$(Get-PropertyName $row.Name).ToString()" }
    [void]$classicMap.Append("                [`"$($row.Name)`"] = $value,$nl")

    $titles.Add("Setting_SqlFmt_$($row.Name)`t$($row.English)`t$($row.Spanish)")
}

$enumCode = New-Object Text.StringBuilder
foreach ($name in $enums.Keys) {
    $typeName = Get-EnumTypeName ($properties.Values | Where-Object { $_.PropertyType.Name -eq $name } | Select-Object -First 1).PropertyType
    [void]$enumCode.Append("    /// <summary>Mismos valores que ScriptDOM ($name).</summary>$nl    public enum $typeName$nl    {$nl")
    [void]$enumCode.Append((($enums[$name] | ForEach-Object { "        $_" }) -join ",$nl") + "$nl    }$nl$nl")
}

$generatedBy = 'Generado por tools/SqlFormatter/Update-SqlFormatterOptions.ps1 desde SqlFormatterOptions.tsv; no editar a mano.'

# --- SqlFormatterCatalog.cs ---
$catalogFile = @"
namespace YelcoBot.CodeSweep.Domain.Options
{
    /// <summary>Una opción del formateador T-SQL (propiedad de ScriptDOM SqlScriptGeneratorOptions).</summary>
    public sealed class SqlFormatterOption
    {
        public SqlFormatterOption(string name, string group, string defaultValue, string editorConfigKey)
        {
            Name = name;
            Group = group;
            DefaultValue = defaultValue;
            EditorConfigKey = editorConfigKey;
        }

        /// <summary>Nombre de la propiedad de ScriptDOM (KeywordCasing).</summary>
        public string Name { get; }

        /// <summary>Grupo, igual que en SSMS: general, alignment, formatting, indentation, multiline, newLine, spacing.</summary>
        public string Group { get; }

        /// <summary>Valor por defecto de CodeSweep (se usa cuando ni el .editorconfig ni el producto lo definen).</summary>
        public string DefaultValue { get; }

        /// <summary>Clave en .editorconfig, sección [*.sql] (keyword_casing), como la documenta SSMS.</summary>
        public string EditorConfigKey { get; }
    }

    /// <summary>
    /// Las opciones del formateador T-SQL (todas las de ScriptDOM $version) con los valores por defecto de CodeSweep.
    /// Prioridad de cada valor: .editorconfig [*.sql] → opciones del producto (VS / SSMS) si la tiene → opciones de CodeSweep.
    /// $generatedBy
    /// </summary>
    public static class SqlFormatterCatalog
    {
        public static readonly IReadOnlyList<SqlFormatterOption> Options = new[]
        {
$($catalog.ToString().TrimEnd())
        };
    }
}
"@
[IO.File]::WriteAllText((Join-Path $repo 'src\YelcoBot.CodeSweep.Domain\Options\SqlFormatterCatalog.cs'), ($catalogFile -replace "`r?`n", $nl) + $nl, $noBom)

# --- SqlFormatterUIContexts.cs ---
$contextsFile = @"
namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings
{
    /// <summary>
    /// Un UIContext por opción del formateador T-SQL. El paquete activa el de cada opción que el producto ya tiene
    /// en Tools → Options (por ejemplo, las sqlFormatter.* de SSMS), y CodeSweep.registration.json la oculta
    /// con "visibleWhen". $generatedBy
    /// </summary>
    internal static class SqlFormatterUIContexts
    {
        public static readonly IReadOnlyDictionary<string, Guid> ByOption = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
        {
$($contexts.ToString().TrimEnd())
        };
    }
}
"@
[IO.File]::WriteAllText($contextsPath, ($contextsFile -replace "`r?`n", $nl) + $nl, $noBom)

# --- ClassicOptions.SqlFormatter.cs ---
$classicFile = @"
using System.ComponentModel;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings
{
    /// <summary>
    /// Formateador T-SQL en la página clásica (VS 2022). VS no tiene opciones de formato SQL propias: se muestran todas.
    /// $generatedBy
    /// </summary>
    public partial class ClassicOptions
    {
        private const string SqlFormatter = "9. SQL formatter (ScriptDOM)";

$($classic.ToString().TrimEnd())

        /// <summary>Propiedad de ScriptDOM → valor.</summary>
        private Dictionary<string, string> GetSqlFormatterValues()
        {
            return new Dictionary<string, string>
            {
$($classicMap.ToString().TrimEnd())
            };
        }
    }

$($enumCode.ToString().TrimEnd())
}
"@
[IO.File]::WriteAllText((Join-Path $repo 'src\YelcoBot.CodeSweep.Infrastructure.VisualStudio\Settings\ClassicOptions.SqlFormatter.cs'), ($classicFile -replace "`r?`n", $nl) + $nl, $noBom)

# --- CodeSweep.registration.json: reemplazar el bloque del formateador (entre su encabezado y el de "C# y VB") ---
$regNl = if ($registration.Contains("`r`n")) { "`r`n" } else { "`n" }
$startMatch = [regex]::Match($registration, '(?m)^[ \t]*// =+[ \t]*\r?\n[ \t]*// Formateador SQL \(ScriptDOM\)')
$endMatch = [regex]::Match($registration, '(?m)^[ \t]*// =+[ \t]*\r?\n[ \t]*// C# y VB')
if (-not $startMatch.Success -or -not $endMatch.Success -or $endMatch.Index -lt $startMatch.Index) {
    throw 'No se encontró el bloque "Formateador SQL (ScriptDOM)" en CodeSweep.registration.json.'
}
$start = $startMatch.Index
$end = $endMatch.Index

$separator = "$pi// ============================================="
$block = "$separator$regNl$pi// Formateador SQL (ScriptDOM): solo las opciones que el producto no tiene (visibleWhen por opción)$regNl$separator$regNl" +
    ($json.ToString() -replace "`r?`n", $regNl) + $regNl
$registration = $registration.Substring(0, $start) + $block + $registration.Substring($end)
[IO.File]::WriteAllText($registrationPath, $registration, $noBom)

# --- Títulos (VSPackage.tsv) y .resx ---
$tsvPath = Join-Path $repo 'tools\Localization\VSPackage.tsv'
$tsv = @(Get-Content $tsvPath -Encoding UTF8 | Where-Object { -not $_.StartsWith('Setting_SqlFmt_') -and $_ -ne '' }) + $titles
[IO.File]::WriteAllLines($tsvPath, $tsv, $noBom)
& (Join-Path $repo 'tools\Localization\Update-Resources.ps1')

# --- CacheTag: VS vuelve a leer registration.json ---
$pkgdefPath = Join-Path $repo 'src\YelcoBot.CodeSweep\CodeSweep.Settings.pkgdef'
$pkgdef = [IO.File]::ReadAllText($pkgdefPath)
$match = [regex]::Match($pkgdef, '"CacheTag"=qword:([0-9a-fA-F]+)')
$next = [Convert]::ToInt64($match.Groups[1].Value, 16) + 1
[IO.File]::WriteAllText($pkgdefPath, $pkgdef.Replace($match.Value, '"CacheTag"=qword:' + $next.ToString('x8')), $noBom)

Write-Host "OK: $($rows.Count) opciones de ScriptDOM $version; CacheTag → $($next.ToString('x8'))."
