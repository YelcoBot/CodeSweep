# Changelog

## 1.12.0

### New

- **Remove unused methods** (C# and VB, off by default): removes methods that nothing in the solution references. Tools → Options → CodeSweep → *Unused methods*.
  - One checkbox per access level: private and private protected on by default; internal, protected, protected internal and public off. Effective access counts (a public method of a private nested class is private).
  - Protections, all on by default: methods named in markup (XAML, Web Forms, Razor, Ribbon XML), event handlers (including VB `Handles`), naming conventions (`Page_Load`, `Application_Start`, `OnGet`, `ShouldSerializeX`…), framework entry points (controllers, hubs, Razor pages, Blazor components, Web Forms pages, Unity messages), methods with attributes and names used in strings.
  - Never removes override, abstract, virtual, extern or partial methods, interface implementations, `Main`, pattern methods or names used in inactive `#if` code.
- **`.props` files** (`Directory.Build.props`, `Directory.Packages.props`…) are formatted with the XML / Config group.

## 1.11.4

### New

- **Progress dialog with Cancel**: long cleanups show the Visual Studio wait dialog, with the current file and a Cancel button. Files already cleaned stay cleaned.

### Fixed

- `.sql` files open in the editor were not cleaned since 1.11.3; only closed files were.
- The *CodeSweep Summary* was never shown at the end of a cleanup, and the status bar progress could get stuck.

## 1.11.3

### Changed

- **T-SQL**: `.sql` files are cleaned in parallel, like C# and VB.

## 1.11.2

### New

- **Open Folder** (File → Open → Folder, no solution): every command now works.
  - **Cleanup All Code** cleans the files of the open folder, skipping `bin`, `obj`, `node_modules` and folders that start with a dot.
  - **Cleanup Selected Code** is available in the Folder View context menu, for files, folders and the root folder.
  - C# and VB files that belong to no project are cleaned too. Without a project there are no references, so unused `using` / `Imports` and unused local variables are left alone.
- **SSMS**: the CodeSweep menu is also in the context menu of the query editor.

### Changed

- The *Cleanup All Code* confirmation and progress texts no longer mention only the solution.

## 1.10.0

### New

- **SQL Server Management Studio 22.7+**: CodeSweep now installs in SSMS too. Download it from [GitHub Releases](https://github.com/YelcoBot/CodeSweep/releases) (the Visual Studio Marketplace doesn't accept SSMS packages). In SSMS it only cleans `.sql` files, and Tools → Options hides the other languages.
- **T-SQL formatting** with ScriptDOM 180.117, the engine behind the SSMS formatter, in the background and without opening files.
  - It always uses its own ScriptDOM, even inside SSMS (which ships an older one).
  - 58 formatter options. Each value comes from `.editorconfig` `[*.sql]` (same keys as SSMS) → the product's own options (SSMS *SQL Formatter*) → CodeSweep's options.
  - CodeSweep only shows the options the product doesn't have: all 58 in Visual Studio, only the missing ones in SSMS.
  - Scripts with syntax errors or SQLCMD are not formatted; a result that would lose comments is discarded.
  - Open files (including unsaved SSMS query windows) are changed in the editor; closed files are saved with their original encoding.
- **VB**: removes unused `Imports` and local variables, and sorts `Imports`, like C#.
- **Rules for every file type**, in Roslyn and in the editor: consecutive blank lines (configurable maximum), blank lines at the start and end of the file, trailing whitespace, exactly one final newline. JS/TS template strings, SQL strings, `<pre>`, `<textarea>`, `<script>` and CDATA are never touched.
- **C# and VB rules**:
  - On by default: blank lines after opening and before closing a block, after attributes and between chained calls; a blank line between members, before single-line comments and after `using` / `Imports`; remove empty regions.
  - Off by default: blank lines around regions and before each `case`, remove all regions, name the `#endregion`, reorganize members, uniform accessors, wrap comments, space after `//` and `'`.
- **Markup rules**: remove blank lines right inside a tag and empty `<!-- -->` comments.
- **Tools → Options** reorganized by section (Roslyn, Whitespace, SQL, SQL formatter, C# and VB, Markup, File Types), in English and Spanish.

### Changed

- The *Remove consecutive blank lines* option moved to the **Whitespace** section. If you had turned it off, turn it off again.
