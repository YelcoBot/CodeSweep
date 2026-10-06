# Changelog

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
