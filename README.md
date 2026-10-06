# CodeSweep

Fast code cleanup for **Visual Studio 2022 / 2026** (C#, VB, markup, T-SQL) and **SQL Server Management Studio 22.7+** (T-SQL).

- **C# and VB**: cleaned with Roslyn in the background and in parallel, without opening files.
- **T-SQL**: formatted with ScriptDOM (the engine behind the SSMS formatter), also without opening files.
- **ASPX, Razor, HTML, XML, XAML, CSS, JS, JSON…**: formatted with the Visual Studio editor.
- **Every file type**: blank lines, trailing whitespace and the final newline.

## Install

| Product | Where |
|---|---|
| Visual Studio 2022 / 2026 | [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=yelcobot.CodeSweep) (updates from *Extensions → Manage Extensions*) |
| SQL Server Management Studio 22.7+ | [GitHub Releases](https://github.com/YelcoBot/CodeSweep/releases): download the `.vsix` and run it with SSMS closed. It also installs in Visual Studio |

The Visual Studio Marketplace rejects packages that target SSMS, so there are two packages built from the same code: the Marketplace one (Visual Studio only) and the GitHub Releases one (Visual Studio + SSMS).

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

- **Version:** `source.extension.vsixmanifest` stays at `1.0.0`; the pipeline sets the real version when it publishes. For local builds, create `src/YelcoBot.CodeSweep/version.local.props` (ignored by git) with `<Project><PropertyGroup><LocalVsixVersion>1.10.1</LocalVsixVersion></PropertyGroup></Project>` and bump it: only the VSIX gets that version, never the manifest in the repo.
- **SSMS package:** the manifest in the repo targets Visual Studio and SSMS, so a local build installs in both and is the one for GitHub Releases. The pipeline removes the SSMS target only from the Marketplace build, which doesn't accept it.
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
