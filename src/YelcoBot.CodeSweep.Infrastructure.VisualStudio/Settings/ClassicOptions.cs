using System.ComponentModel;
using Community.VisualStudio.Toolkit;
using YelcoBot.CodeSweep.Domain.Options;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings
{
    /// <summary>
    /// Opciones para VS 2022 (Tools → Options clásico). En VS 2026 no se muestra: la reemplaza la página moderna
    /// (CodeSweep.registration.json declara "legacyOptionPageId" con el GUID de esta página).
    /// </summary>
    public partial class ClassicOptions : BaseOptionModel<ClassicOptions>
    {
        private const string General = "1. General";
        private const string Rules = "2. Roslyn (C# and VB)";
        private const string UnusedMethods = "2.1 Unused methods (C# and VB)";
        private const string Whitespace = "3. Whitespace (all files)";
        private const string CSharpVb = "4. C# and VB";
        private const string Markup = "5. Markup (Web Forms, Razor, HTML, XML, XAML)";
        private const string FileTypes = "6. File Types";
        private const string Exclusions = "7. Exclusions";

        [Category(General), DisplayName("Automatic cleanup on save"), Description("Clean each document right before it is saved.")]
        [DefaultValue(false)]
        public bool CleanupOnSave { get; set; } = false;

        [Category(Rules), DisplayName("Remove unused local variables"), Description("C#: CS0168 / CS0219. VB: BC42024 / BC42099. Skipped when the file has compile errors.")]
        [DefaultValue(true)]
        public bool RemoveUnusedLocalVariables { get; set; } = true;

        [Category(Rules), DisplayName("Remove unused usings / Imports"), Description("C#: CS8019. VB: BC50000 / BC50001. Skipped when the file has compile errors.")]
        [DefaultValue(true)]
        public bool RemoveUnusedUsings { get; set; } = true;

        [Category(Rules), DisplayName("Sort usings / Imports"), Description("System.* first, then alphabetical.")]
        [DefaultValue(true)]
        public bool SortUsings { get; set; } = true;

        [Category(Rules), DisplayName("Format document"), Description("Roslyn formatting; respects your .editorconfig.")]
        [DefaultValue(true)]
        public bool FormatDocument { get; set; } = true;

        [Category(UnusedMethods), DisplayName("Remove unused methods"), Description("Removes methods that nothing in the solution references, for the access levels below. Never removes override, abstract, virtual, extern or partial methods, interface implementations, Main or pattern methods. Skipped when the file has compile errors.")]
        [DefaultValue(false)]
        public bool RemoveUnusedMethods { get; set; } = false;

        [Category(UnusedMethods), DisplayName("Private methods"), Description("Effective access counts: a public method of a private nested class is private, and protected in a sealed class is private.")]
        [DefaultValue(true)]
        public bool UnusedMethodsIncludePrivate { get; set; } = true;

        [Category(UnusedMethods), DisplayName("Private protected methods"), Description("VB: Private Protected.")]
        [DefaultValue(true)]
        public bool UnusedMethodsIncludePrivateProtected { get; set; } = true;

        [Category(UnusedMethods), DisplayName("Internal methods"), Description("VB: Friend. Skipped when InternalsVisibleTo points to an assembly that is not in the solution.")]
        [DefaultValue(false)]
        public bool UnusedMethodsIncludeInternal { get; set; } = false;

        [Category(UnusedMethods), DisplayName("Protected methods"), Description("A derived class in another assembly may use them.")]
        [DefaultValue(false)]
        public bool UnusedMethodsIncludeProtected { get; set; } = false;

        [Category(UnusedMethods), DisplayName("Protected internal methods"), Description("VB: Protected Friend.")]
        [DefaultValue(false)]
        public bool UnusedMethodsIncludeProtectedInternal { get; set; } = false;

        [Category(UnusedMethods), DisplayName("Public methods"), Description("Only for applications: in a library, the code that uses them is not in the solution.")]
        [DefaultValue(false)]
        public bool UnusedMethodsIncludePublic { get; set; } = false;

        [Category(UnusedMethods), DisplayName("Protect methods referenced from markup"), Description("XAML Click=\"…\", Web Forms OnClick=\"…\", Razor @onclick=\"…\", Ribbon XML onAction=\"…\".")]
        [DefaultValue(true)]
        public bool UnusedMethodsProtectMarkupReferences { get; set; } = true;

        [Category(UnusedMethods), DisplayName("Markup extensions"), Description("Files scanned for method names. Separate with ';'.")]
        [DefaultValue(SweepOptions.DefaultUnusedMethodsMarkupExtensions)]
        public string UnusedMethodsMarkupExtensions { get; set; } = SweepOptions.DefaultUnusedMethodsMarkupExtensions;

        [Category(UnusedMethods), DisplayName("Protect event handlers"), Description("(object sender, EventArgs e) signature, and VB methods with a Handles clause.")]
        [DefaultValue(true)]
        public bool UnusedMethodsProtectEventHandlers { get; set; } = true;

        [Category(UnusedMethods), DisplayName("Protect naming conventions"), Description("Page_*, Application_*, Session_*, OnGet* / OnPost*, ShouldSerializeX / ResetX.")]
        [DefaultValue(true)]
        public bool UnusedMethodsProtectNamingConventions { get; set; } = true;

        [Category(UnusedMethods), DisplayName("Protect framework entry points"), Description("Controllers, hubs, Razor pages, Blazor components, Web Forms pages, Global.asax, web services, Unity messages.")]
        [DefaultValue(true)]
        public bool UnusedMethodsProtectFrameworkEntryPoints { get; set; } = true;

        [Category(UnusedMethods), DisplayName("Protect methods with attributes"), Description("[TestMethod], [HttpGet], [WebMethod]… Informative ones ([Obsolete], [SuppressMessage]…) do not protect.")]
        [DefaultValue(true)]
        public bool UnusedMethodsProtectAttributed { get; set; } = true;

        [Category(UnusedMethods), DisplayName("Protect names used in strings"), Description("Reflection (GetMethod(\"Name\")), Invoke(\"Name\"), SelectMethod = \"Name\".")]
        [DefaultValue(true)]
        public bool UnusedMethodsProtectStringReferences { get; set; } = true;

        [Category(Whitespace), DisplayName("Remove consecutive blank lines"), Description("Keeps at most 'Maximum consecutive blank lines' in a row.")]
        [DefaultValue(true)]
        public bool RemoveConsecutiveBlankLines { get; set; } = true;

        [Category(Whitespace), DisplayName("Maximum consecutive blank lines"), Description("Blank lines allowed in a row (0 removes them all).")]
        [DefaultValue(1)]
        public int MaxConsecutiveBlankLines { get; set; } = 1;

        [Category(Whitespace), DisplayName("Remove blank lines at the start of the file")]
        [DefaultValue(true)]
        public bool RemoveLeadingBlankLines { get; set; } = true;

        [Category(Whitespace), DisplayName("Remove blank lines at the end of the file")]
        [DefaultValue(true)]
        public bool RemoveTrailingBlankLines { get; set; } = true;

        [Category(Whitespace), DisplayName("Remove trailing whitespace")]
        [DefaultValue(true)]
        public bool TrimTrailingWhitespace { get; set; } = true;

        [Category(Whitespace), DisplayName("Exactly one newline at the end of the file"), Description("Adds it when missing and removes extra ones.")]
        [DefaultValue(true)]
        public bool SingleFinalNewline { get; set; } = true;

        [Category(CSharpVb), DisplayName("Remove blank lines after opening a block"), Description("After '{' in C#; after Sub, If, For, Class… in VB.")]
        [DefaultValue(true)]
        public bool RemoveBlankLinesAfterOpenBlock { get; set; } = true;

        [Category(CSharpVb), DisplayName("Remove blank lines before closing a block"), Description("Before '}' in C#; before End, Next, Loop, Else, Catch… in VB.")]
        [DefaultValue(true)]
        public bool RemoveBlankLinesBeforeCloseBlock { get; set; } = true;

        [Category(CSharpVb), DisplayName("Remove blank lines after attributes"), Description("[HttpPost] / <WebMethod()> stays right above its member.")]
        [DefaultValue(true)]
        public bool RemoveBlankLinesAfterAttributes { get; set; } = true;

        [Category(CSharpVb), DisplayName("Remove blank lines between chained calls"), Description("Lines that start with '.Method()' in C#.")]
        [DefaultValue(true)]
        public bool RemoveBlankLinesBetweenChainedCalls { get; set; } = true;

        [Category(CSharpVb), DisplayName("Blank line between members"), Description("Adds one when missing, for the member types below.")]
        [DefaultValue(true)]
        public bool BlankLineBetweenMembers { get; set; } = true;

        [Category(CSharpVb), DisplayName("Member types separated by a blank line"), Description("Separate with ';'. Available: Fields, Constructors, Properties, Methods, Events, Enums, Types.")]
        [DefaultValue("Methods;Properties;Constructors;Types;Enums;Events")]
        public string BlankLineBetweenMemberKinds { get; set; } = "Methods;Properties;Constructors;Types;Enums;Events";

        [Category(CSharpVb), DisplayName("Blank line around regions"), Description("Before and after #region / #endregion (#Region / #End Region).")]
        [DefaultValue(false)]
        public bool BlankLineAroundRegions { get; set; } = false;

        [Category(CSharpVb), DisplayName("Blank line before each case"), Description("Every case / Case except the first one.")]
        [DefaultValue(false)]
        public bool BlankLineBeforeCase { get; set; } = false;

        [Category(CSharpVb), DisplayName("Blank line before single-line comments"), Description("Not after opening a block, another comment, a directive or a case label.")]
        [DefaultValue(true)]
        public bool BlankLineBeforeSingleLineComments { get; set; } = true;

        [Category(CSharpVb), DisplayName("Blank line after usings / Imports")]
        [DefaultValue(true)]
        public bool BlankLineAfterUsings { get; set; } = true;

        [Category(CSharpVb), DisplayName("Remove all regions"), Description("Removes #region / #endregion and keeps their content.")]
        [DefaultValue(false)]
        public bool RemoveAllRegions { get; set; } = false;

        [Category(CSharpVb), DisplayName("Add the region name to #endregion"), Description("C#: #endregion Name. VB: #End Region ' Name.")]
        [DefaultValue(false)]
        public bool UpdateEndRegionText { get; set; } = false;

        [Category(CSharpVb), DisplayName("Remove empty regions")]
        [DefaultValue(true)]
        public bool RemoveEmptyRegions { get; set; } = true;

        [Category(CSharpVb), DisplayName("Reorganize members by type and access"), Description("Fields keep their relative order. Structs, [StructLayout] types and types with directives between members are not touched.")]
        [DefaultValue(false)]
        public bool ReorganizeMembers { get; set; } = false;

        [Category(CSharpVb), DisplayName("Member type order"), Description("Separate with ';'. Missing types go last.")]
        [DefaultValue("Fields;Constructors;Properties;Events;Methods;Enums;Types")]
        public string MemberKindOrder { get; set; } = "Fields;Constructors;Properties;Events;Methods;Enums;Types";

        [Category(CSharpVb), DisplayName("Access order"), Description("Separate with ';'. VB: Friend = internal.")]
        [DefaultValue("public;internal;protected internal;protected;private protected;private")]
        public string AccessOrder { get; set; } = "public;internal;protected internal;protected;private protected;private";

        [Category(CSharpVb), DisplayName("Accessors all single-line or all multi-line"), Description("C# only. Single-line when every accessor has at most one statement.")]
        [DefaultValue(false)]
        public bool UniformAccessors { get; set; } = false;

        [Category(CSharpVb), DisplayName("Wrap comments at line width"), Description("Splits single-line comments longer than the width. Never joins lines.")]
        [DefaultValue(false)]
        public bool WrapComments { get; set; } = false;

        [Category(CSharpVb), DisplayName("Line width"), Description("Columns (a tab counts as 4).")]
        [DefaultValue(120)]
        public int CommentWrapColumn { get; set; } = 120;

        [Category(CSharpVb), DisplayName("Space after // and '"), Description("//Text → // Text.")]
        [DefaultValue(false)]
        public bool SpaceAfterCommentPrefix { get; set; } = false;

        [Category(Markup), DisplayName("Remove blank lines right inside a tag"), Description("After <div> and before </div>. Never inside <pre>, <textarea>, <script> or CDATA.")]
        [DefaultValue(true)]
        public bool RemoveBlankLinesInsideTags { get; set; } = true;

        [Category(Markup), DisplayName("Remove empty comments <!-- -->")]
        [DefaultValue(true)]
        public bool RemoveEmptyComments { get; set; } = true;

        [Category(FileTypes), DisplayName("C# (.cs)")]
        [DefaultValue(true)]
        public bool IncludeCSharp { get; set; } = true;

        [Category(FileTypes), DisplayName("Visual Basic (.vb)")]
        [DefaultValue(true)]
        public bool IncludeVisualBasic { get; set; } = true;

        [Category(FileTypes), DisplayName("Web Forms (.aspx, .ascx, .master, .asax)")]
        [DefaultValue(true)]
        public bool IncludeWebForms { get; set; } = true;

        [Category(FileTypes), DisplayName("Regenerate Web Forms designer after formatting"), Description("Keeps .designer.cs in sync with the markup. Web Application projects only.")]
        [DefaultValue(true)]
        public bool RegenerateWebFormsDesigner { get; set; } = true;

        [Category(FileTypes), DisplayName("Razor (.cshtml, .vbhtml, .razor)")]
        [DefaultValue(true)]
        public bool IncludeRazor { get; set; } = true;

        [Category(FileTypes), DisplayName("HTML (.html, .htm)")]
        [DefaultValue(true)]
        public bool IncludeHtml { get; set; } = true;

        [Category(FileTypes), DisplayName("XML / Config (.xml, .config, .webinfo, .props)")]
        [DefaultValue(true)]
        public bool IncludeXmlConfig { get; set; } = true;

        [Category(FileTypes), DisplayName("XAML (.xaml)")]
        [DefaultValue(true)]
        public bool IncludeXaml { get; set; } = true;

        [Category(FileTypes), DisplayName("Styles (.css, .less, .scss)")]
        [DefaultValue(true)]
        public bool IncludeStyles { get; set; } = true;

        [Category(FileTypes), DisplayName("Scripts (.js, .ts)")]
        [DefaultValue(true)]
        public bool IncludeScripts { get; set; } = true;

        [Category(FileTypes), DisplayName("JSON (.json)")]
        [DefaultValue(true)]
        public bool IncludeJson { get; set; } = true;

        [Category(FileTypes), DisplayName("SQL (.sql)"), Description("Cleaned in the background, without opening editors.")]
        [DefaultValue(true)]
        public bool IncludeSql { get; set; } = true;

        [Category(FileTypes), DisplayName("Format T-SQL"), Description("ScriptDOM, the same engine as the SSMS 22.7+ formatter. Options from .editorconfig [*.sql], with the same keys as SSMS.")]
        [DefaultValue(true)]
        public bool FormatSql { get; set; } = true;

        [Category(FileTypes), DisplayName("Additional file extensions"), Description("Separate with ';' (e.g. .skin;.sitemap).")]
        [DefaultValue("")]
        public string AdditionalFileExtensions { get; set; } = string.Empty;

        [Category(Exclusions), DisplayName("Exclude generated code"), Description("*.designer.cs, *.g.cs, obj\\, <auto-generated>.")]
        [DefaultValue(true)]
        public bool IgnoreGeneratedCode { get; set; } = true;

        [Category(Exclusions), DisplayName("Exclude T4 generated code")]
        [DefaultValue(true)]
        public bool ExcludeT4GeneratedCode { get; set; } = true;

        [Category(Exclusions), DisplayName("Exclude paths (regular expressions)"), Description("Separate with ';'.")]
        [DefaultValue(SweepOptions.DefaultExcludePatterns)]
        public string ExcludePatterns { get; set; } = SweepOptions.DefaultExcludePatterns;

        public SweepOptions ToSweepOptions() => new SweepOptions
        {
            CleanupOnSave = CleanupOnSave,
            EnableRemoveUnusedLocalVariables = RemoveUnusedLocalVariables,
            EnableRemoveUnusedUsings = RemoveUnusedUsings,
            EnableSortUsings = SortUsings,
            EnableFormatDocument = FormatDocument,
            EnableRemoveUnusedMethods = RemoveUnusedMethods,
            UnusedMethodsIncludePrivate = UnusedMethodsIncludePrivate,
            UnusedMethodsIncludePrivateProtected = UnusedMethodsIncludePrivateProtected,
            UnusedMethodsIncludeInternal = UnusedMethodsIncludeInternal,
            UnusedMethodsIncludeProtected = UnusedMethodsIncludeProtected,
            UnusedMethodsIncludeProtectedInternal = UnusedMethodsIncludeProtectedInternal,
            UnusedMethodsIncludePublic = UnusedMethodsIncludePublic,
            UnusedMethodsProtectMarkupReferences = UnusedMethodsProtectMarkupReferences,
            UnusedMethodsMarkupExtensions = UnusedMethodsMarkupExtensions ?? string.Empty,
            UnusedMethodsProtectEventHandlers = UnusedMethodsProtectEventHandlers,
            UnusedMethodsProtectNamingConventions = UnusedMethodsProtectNamingConventions,
            UnusedMethodsProtectFrameworkEntryPoints = UnusedMethodsProtectFrameworkEntryPoints,
            UnusedMethodsProtectAttributed = UnusedMethodsProtectAttributed,
            UnusedMethodsProtectStringReferences = UnusedMethodsProtectStringReferences,
            EnableRemoveConsecutiveBlankLines = RemoveConsecutiveBlankLines,
            MaxConsecutiveBlankLines = MaxConsecutiveBlankLines,
            EnableRemoveLeadingBlankLines = RemoveLeadingBlankLines,
            EnableRemoveTrailingBlankLines = RemoveTrailingBlankLines,
            EnableTrimTrailingWhitespace = TrimTrailingWhitespace,
            EnableSingleFinalNewline = SingleFinalNewline,
            EnableRemoveBlankLinesAfterOpenBlock = RemoveBlankLinesAfterOpenBlock,
            EnableRemoveBlankLinesBeforeCloseBlock = RemoveBlankLinesBeforeCloseBlock,
            EnableRemoveBlankLinesAfterAttributes = RemoveBlankLinesAfterAttributes,
            EnableRemoveBlankLinesBetweenChainedCalls = RemoveBlankLinesBetweenChainedCalls,
            EnableBlankLineBetweenMembers = BlankLineBetweenMembers,
            BlankLineBetweenMemberKinds = BlankLineBetweenMemberKinds ?? string.Empty,
            EnableBlankLineAroundRegions = BlankLineAroundRegions,
            EnableBlankLineBeforeCase = BlankLineBeforeCase,
            EnableBlankLineBeforeSingleLineComments = BlankLineBeforeSingleLineComments,
            EnableBlankLineAfterUsings = BlankLineAfterUsings,
            EnableRemoveAllRegions = RemoveAllRegions,
            EnableUpdateEndRegionText = UpdateEndRegionText,
            EnableRemoveEmptyRegions = RemoveEmptyRegions,
            EnableReorganizeMembers = ReorganizeMembers,
            MemberKindOrder = MemberKindOrder ?? string.Empty,
            AccessOrder = AccessOrder ?? string.Empty,
            EnableUniformAccessors = UniformAccessors,
            EnableWrapComments = WrapComments,
            CommentWrapColumn = CommentWrapColumn,
            EnableSpaceAfterCommentPrefix = SpaceAfterCommentPrefix,
            EnableRemoveBlankLinesInsideTags = RemoveBlankLinesInsideTags,
            EnableRemoveEmptyComments = RemoveEmptyComments,
            IncludeCSharp = IncludeCSharp,
            IncludeVisualBasic = IncludeVisualBasic,
            IncludeWebForms = IncludeWebForms,
            RegenerateWebFormsDesigner = RegenerateWebFormsDesigner,
            IncludeRazor = IncludeRazor,
            IncludeHtml = IncludeHtml,
            IncludeXmlConfig = IncludeXmlConfig,
            IncludeXaml = IncludeXaml,
            IncludeStyles = IncludeStyles,
            IncludeScripts = IncludeScripts,
            IncludeJson = IncludeJson,
            IncludeSql = IncludeSql,
            EnableFormatSql = FormatSql,
            SqlFormatter = GetSqlFormatterValues(),
            AdditionalFileExtensions = AdditionalFileExtensions ?? string.Empty,
            IgnoreGeneratedCode = IgnoreGeneratedCode,
            ExcludeT4GeneratedCode = ExcludeT4GeneratedCode,
            ExcludePatterns = ExcludePatterns ?? string.Empty
        };
    }
}
