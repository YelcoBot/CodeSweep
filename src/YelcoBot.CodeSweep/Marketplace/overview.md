# CodeSweep

Fast code cleanup for **Visual Studio 2022 / 2026** (C#, VB, markup, T-SQL) and **SQL Server Management Studio 22.7+** (T-SQL).

> **Using SSMS?** The Visual Studio Marketplace only lists Visual Studio. Download the `.vsix` from [GitHub Releases](https://github.com/YelcoBot/CodeSweep/releases): it installs in SSMS 22.7+ and in Visual Studio.

- **C# and VB** are cleaned with Roslyn **in the background and in parallel, without opening files**.
- **T-SQL** is formatted with **ScriptDOM**, the same engine as the SSMS formatter, also without opening files.
- **ASPX, Razor, HTML, XML, XAML, CSS, JS, JSON…** are formatted with the Visual Studio editor.

## Commands

Menu **Tools → CodeSweep**, plus the Solution Explorer, document tab and code editor context menus:

| Command | What it does |
|---|---|
| **Cleanup Active Document** | Cleans the document you are editing |
| **Cleanup Open Code** | Cleans every open document |
| **Cleanup All Code...** | Cleans the whole solution (asks for confirmation, shows progress) |
| **Cleanup Selected Code** | Cleans the items selected in Solution Explorer |
| **Automatic Cleanup On Save** | Cleans each document right before it is saved |

## C# and VB

- Remove unused local variables, unused `using` / `Imports`, and sort them (`System.*` first)
- Format document (respects your `.editorconfig`)
- Blank lines: remove them after opening and before closing a block, after attributes and between chained calls; add them between members, before comments and after `using` / `Imports`
- Remove empty regions
- Optional (off by default): blank lines around regions and before each `case`, remove all regions, name the `#endregion`, reorganize members by type and access, uniform accessors, wrap long comments, space after `//` and `'`

Rules never run on files that don't compile, and generated code (`*.designer.cs`, `*.g.cs`, `obj\`) is always skipped.

## T-SQL (Visual Studio and SSMS)

- Formatted with **ScriptDOM 180.117** (the latest), even inside SSMS
- **58 formatter options**, with this priority for each one:
  1. `.editorconfig`, section `[*.sql]`, with the same keys as SSMS (`keyword_casing`, `indentation_size`…)
  2. The product's own options (SSMS **SQL Formatter**)
  3. CodeSweep's options
- In SSMS, CodeSweep only shows the formatter options SSMS doesn't have; in Visual Studio it shows all of them
- Scripts with syntax errors or SQLCMD (`:setvar`) are never formatted, and a result that would lose comments is discarded

## Every file type

- Collapse consecutive blank lines (configurable maximum)
- Remove blank lines at the start and end of the file
- Remove trailing whitespace
- Exactly one newline at the end of the file

Strings, template strings, disabled code, `<pre>`, `<textarea>`, `<script>` and CDATA are never touched.

## Markup

- Remove blank lines right inside a tag
- Remove empty `<!-- -->` comments

## Configuration

**Tools → Options → CodeSweep**: every rule can be turned on or off, and you choose which file types are cleaned. Available in English and Spanish.
