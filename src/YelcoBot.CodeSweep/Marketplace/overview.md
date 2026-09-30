# CodeSweep

Fast, Roslyn-based code cleanup for Visual Studio 2022 and 2026.

CodeSweep cleans your C# code **in the background and in parallel, without opening files**. Files that Roslyn doesn't support (ASPX, Razor, HTML, XML, XAML…) are formatted with the Visual Studio editor.

## Commands

Menu **Extensions → CodeSweep**:

| Command | What it does |
|---|---|
| **Cleanup Active Document** | Cleans the document you are editing |
| **Cleanup Open Code** | Cleans every open document |
| **Cleanup All Code...** | Cleans the whole solution (asks for confirmation, shows progress) |
| **Automatic Cleanup On Save** | Cleans each document right before it is saved |

## Cleanup rules (C#)

- Remove unused local variables
- Remove unused `using` directives
- Sort `using` directives (`System.*` first)
- Collapse consecutive blank lines
- Format document (respects your `.editorconfig`)

Rules never run on files that don't compile, and generated code (`*.designer.cs`, `*.g.cs`, `obj\`) is always skipped.

## Configuration

**Tools → Options → CodeSweep** lets you enable or disable each rule, the editor-based formatting and the list of file extensions it applies to.
