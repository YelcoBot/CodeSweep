# CodeSweep

Fast code cleanup for **Visual Studio 2022 / 2026** (C#, VB, markup, T-SQL) and **SQL Server Management Studio 22.7+** (T-SQL).

- **C# and VB**: cleaned with Roslyn in the background and in parallel, without opening files.
- **T-SQL**: formatted with ScriptDOM (the engine behind the SSMS formatter), also without opening files.
- **ASPX, Razor, HTML, XML, XAML, CSS, JS, JSON…**: formatted with the Visual Studio editor.
- **Every file type**: blank lines, trailing whitespace and the final newline.

Marketplace: [CodeSweep](https://marketplace.visualstudio.com/items?itemName=yelcobot.CodeSweep)

## Documentation

| Document | Content |
|---|---|
| [REGLAS.md](REGLAS.md) | Every cleanup rule, with examples and defaults (Spanish) |
| [ESTRUCTURA.md](ESTRUCTURA.md) | Projects, layers, routing and how the VSIX is built (Spanish) |
| [CHANGELOG.md](CHANGELOG.md) | Release notes |

## Build

Requirements: Visual Studio 2026 (or 2022 17.14+) with the *Visual Studio extension development* workload.

```powershell
msbuild CodeSweep.slnx -restore -p:Configuration=Release
# VSIX: src/YelcoBot.CodeSweep/bin/Release/net481/YelcoBot.CodeSweep.vsix
```

- **Version:** fixed at `1.0.0` in `source.extension.vsixmanifest` for local builds; the pipeline sets the real one when it publishes.
- **Install locally:** use the `VSIXInstaller.exe` of the product you want. Double-clicking the `.vsix` may open the SSMS installer, which only offers SSMS.
- **Debug:** press F5 to use the experimental instance, so you don't touch the version installed from the Marketplace.

## Generated files

Run with PowerShell 7 from the repository root:

| Script | Regenerates |
|---|---|
| `tools/Localization/Update-Resources.ps1` | `Strings*.resx` and `VSPackage*.resx` from `Strings.tsv` / `VSPackage.tsv` (English / Spanish) |
| `tools/SqlFormatter/Update-SqlFormatterOptions.ps1` | The 58 T-SQL formatter options (catalog, UI contexts, VS 2022 page, `CodeSweep.registration.json`, titles, CacheTag) from `SqlFormatterOptions.tsv` |

After updating ScriptDOM in `Directory.Packages.props`, run `Update-SqlFormatterOptions.ps1`: it fails if ScriptDOM added or removed an option that isn't in the TSV.

## Known limitations

- **`include_semicolons = false` has no effect:** ScriptDOM 180.117 adds the `;` anyway. This comes from the library, not from CodeSweep.
- **XML / `.config`:** they can't be formatted in the background. CodeSweep asks before opening the editor.
- **SSMS has no JSON or XML formatter:** in SSMS, CodeSweep only cleans `.sql`.
