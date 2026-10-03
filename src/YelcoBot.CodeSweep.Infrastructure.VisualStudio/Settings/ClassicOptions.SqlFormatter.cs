using System.ComponentModel;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings
{
    /// <summary>
    /// Formateador T-SQL en la página clásica (VS 2022). VS no tiene opciones de formato SQL propias: se muestran todas.
    /// Generado por tools/SqlFormatter/Update-SqlFormatterOptions.ps1 desde SqlFormatterOptions.tsv; no editar a mano.
    /// </summary>
    public partial class ClassicOptions
    {
        private const string SqlFormatter = "9. SQL formatter (ScriptDOM)";

        [Category(SqlFormatter), DisplayName("SQL version"), Description("editorconfig: sql_version")]
        [DefaultValue(SqlVersionValue.Sql170)]
        public SqlVersionValue SqlVersion { get; set; } = SqlVersionValue.Sql170;

        [Category(SqlFormatter), DisplayName("SQL engine type"), Description("editorconfig: sql_engine_type")]
        [DefaultValue(SqlEngineTypeValue.All)]
        public SqlEngineTypeValue SqlEngineType { get; set; } = SqlEngineTypeValue.All;

        [Category(SqlFormatter), DisplayName("Allow external library paths"), Description("editorconfig: allow_external_library_paths")]
        [DefaultValue(true)]
        public bool SqlAllowExternalLibraryPaths { get; set; } = true;

        [Category(SqlFormatter), DisplayName("Allow external language paths"), Description("editorconfig: allow_external_language_paths")]
        [DefaultValue(true)]
        public bool SqlAllowExternalLanguagePaths { get; set; } = true;

        [Category(SqlFormatter), DisplayName("Align clause bodies"), Description("editorconfig: align_clause_bodies")]
        [DefaultValue(false)]
        public bool SqlAlignClauseBodies { get; set; } = false;

        [Category(SqlFormatter), DisplayName("Align column definition fields"), Description("editorconfig: align_column_definition_fields")]
        [DefaultValue(true)]
        public bool SqlAlignColumnDefinitionFields { get; set; } = true;

        [Category(SqlFormatter), DisplayName("Align SET clause items"), Description("editorconfig: align_set_clause_item")]
        [DefaultValue(true)]
        public bool SqlAlignSetClauseItem { get; set; } = true;

        [Category(SqlFormatter), DisplayName("Clause body alignment"), Description("editorconfig: clause_body_alignment")]
        [DefaultValue(SqlClauseBodyAlignment.Indented)]
        public SqlClauseBodyAlignment SqlClauseBodyAlignment { get; set; } = SqlClauseBodyAlignment.Indented;

        [Category(SqlFormatter), DisplayName("AS keyword on its own line"), Description("editorconfig: as_keyword_on_own_line")]
        [DefaultValue(true)]
        public bool SqlAsKeywordOnOwnLine { get; set; } = true;

        [Category(SqlFormatter), DisplayName("Keyword casing"), Description("editorconfig: keyword_casing")]
        [DefaultValue(SqlKeywordCasing.Uppercase)]
        public SqlKeywordCasing SqlKeywordCasing { get; set; } = SqlKeywordCasing.Uppercase;

        [Category(SqlFormatter), DisplayName("Built-in function casing"), Description("editorconfig: built_in_function_casing")]
        [DefaultValue(SqlBuiltInFunctionCasing.Uppercase)]
        public SqlBuiltInFunctionCasing SqlBuiltInFunctionCasing { get; set; } = SqlBuiltInFunctionCasing.Uppercase;

        [Category(SqlFormatter), DisplayName("Identifier casing"), Description("editorconfig: identifier_casing")]
        [DefaultValue(SqlIdentifierCasing.Preserve)]
        public SqlIdentifierCasing SqlIdentifierCasing { get; set; } = SqlIdentifierCasing.Preserve;

        [Category(SqlFormatter), DisplayName("Identifier brackets"), Description("editorconfig: identifier_bracketing")]
        [DefaultValue(SqlIdentifierBracketing.Preserve)]
        public SqlIdentifierBracketing SqlIdentifierBracketing { get; set; } = SqlIdentifierBracketing.Preserve;

        [Category(SqlFormatter), DisplayName("Column alias style"), Description("editorconfig: column_alias_style")]
        [DefaultValue(SqlColumnAliasStyle.AsKeyword)]
        public SqlColumnAliasStyle SqlColumnAliasStyle { get; set; } = SqlColumnAliasStyle.AsKeyword;

        [Category(SqlFormatter), DisplayName("Comma placement"), Description("editorconfig: comma_placement")]
        [DefaultValue(SqlCommaPlacement.Trailing)]
        public SqlCommaPlacement SqlCommaPlacement { get; set; } = SqlCommaPlacement.Trailing;

        [Category(SqlFormatter), DisplayName("Spaces after leading comma"), Description("editorconfig: leading_comma_space_count")]
        [DefaultValue(1)]
        public int SqlLeadingCommaSpaceCount { get; set; } = 1;

        [Category(SqlFormatter), DisplayName("Include semicolons"), Description("editorconfig: include_semicolons")]
        [DefaultValue(true)]
        public bool SqlIncludeSemicolons { get; set; } = true;

        [Category(SqlFormatter), DisplayName("Terminate block statements"), Description("editorconfig: terminate_block_statements")]
        [DefaultValue(true)]
        public bool SqlTerminateBlockStatements { get; set; } = true;

        [Category(SqlFormatter), DisplayName("Preserve comments"), Description("editorconfig: preserve_comments")]
        [DefaultValue(true)]
        public bool SqlPreserveComments { get; set; } = true;

        [Category(SqlFormatter), DisplayName("Keep trailing GO"), Description("editorconfig: persist_trailing_go")]
        [DefaultValue(true)]
        public bool SqlPersistTrailingGo { get; set; } = true;

        [Category(SqlFormatter), DisplayName("Indentation size"), Description("editorconfig: indentation_size")]
        [DefaultValue(4)]
        public int SqlIndentationSize { get; set; } = 4;

        [Category(SqlFormatter), DisplayName("Indentation mode"), Description("editorconfig: indentation_mode")]
        [DefaultValue(SqlIndentationMode.Tabs)]
        public SqlIndentationMode SqlIndentationMode { get; set; } = SqlIndentationMode.Tabs;

        [Category(SqlFormatter), DisplayName("Indent SET clause"), Description("editorconfig: indent_set_clause")]
        [DefaultValue(true)]
        public bool SqlIndentSetClause { get; set; } = true;

        [Category(SqlFormatter), DisplayName("Indent VIEW body"), Description("editorconfig: indent_view_body")]
        [DefaultValue(true)]
        public bool SqlIndentViewBody { get; set; } = true;

        [Category(SqlFormatter), DisplayName("SELECT columns on separate lines"), Description("editorconfig: multiline_select_elements_list")]
        [DefaultValue(true)]
        public bool SqlMultilineSelectElementsList { get; set; } = true;

        [Category(SqlFormatter), DisplayName("WHERE predicates on separate lines"), Description("editorconfig: multiline_where_predicates_list")]
        [DefaultValue(true)]
        public bool SqlMultilineWherePredicatesList { get; set; } = true;

        [Category(SqlFormatter), DisplayName("GROUP BY elements on separate lines"), Description("editorconfig: multiline_group_by_elements_list")]
        [DefaultValue(true)]
        public bool SqlMultilineGroupByElementsList { get; set; } = true;

        [Category(SqlFormatter), DisplayName("HAVING predicates on separate lines"), Description("editorconfig: multiline_having_predicates_list")]
        [DefaultValue(true)]
        public bool SqlMultilineHavingPredicatesList { get; set; } = true;

        [Category(SqlFormatter), DisplayName("ORDER BY elements on separate lines"), Description("editorconfig: multiline_order_by_elements_list")]
        [DefaultValue(false)]
        public bool SqlMultilineOrderByElementsList { get; set; } = false;

        [Category(SqlFormatter), DisplayName("PARTITION BY elements on separate lines"), Description("editorconfig: multiline_partition_by_elements_list")]
        [DefaultValue(false)]
        public bool SqlMultilinePartitionByElementsList { get; set; } = false;

        [Category(SqlFormatter), DisplayName("IN values on separate lines"), Description("editorconfig: multiline_in_values_list")]
        [DefaultValue(false)]
        public bool SqlMultilineInValuesList { get; set; } = false;

        [Category(SqlFormatter), DisplayName("VIEW columns on separate lines"), Description("editorconfig: multiline_view_columns_list")]
        [DefaultValue(true)]
        public bool SqlMultilineViewColumnsList { get; set; } = true;

        [Category(SqlFormatter), DisplayName("SET items on separate lines"), Description("editorconfig: multiline_set_clause_items")]
        [DefaultValue(true)]
        public bool SqlMultilineSetClauseItems { get; set; } = true;

        [Category(SqlFormatter), DisplayName("INSERT columns on separate lines"), Description("editorconfig: multiline_insert_targets_list")]
        [DefaultValue(true)]
        public bool SqlMultilineInsertTargetsList { get; set; } = true;

        [Category(SqlFormatter), DisplayName("INSERT sources on separate lines"), Description("editorconfig: multiline_insert_sources_list")]
        [DefaultValue(true)]
        public bool SqlMultilineInsertSourcesList { get; set; } = true;

        [Category(SqlFormatter), DisplayName("Procedure parameters on separate lines"), Description("editorconfig: multiline_procedure_parameters_list")]
        [DefaultValue(true)]
        public bool SqlMultilineProcedureParametersList { get; set; } = true;

        [Category(SqlFormatter), DisplayName("Nested function call parameters on separate lines"), Description("editorconfig: multiline_nested_function_calls")]
        [DefaultValue(false)]
        public bool SqlMultilineNestedFunctionCalls { get; set; } = false;

        [Category(SqlFormatter), DisplayName("WITH / OPTION options on separate lines"), Description("editorconfig: multiline_with_options_list")]
        [DefaultValue(false)]
        public bool SqlMultilineWithOptionsList { get; set; } = false;

        [Category(SqlFormatter), DisplayName("New line before FROM"), Description("editorconfig: new_line_before_from_clause")]
        [DefaultValue(true)]
        public bool SqlNewLineBeforeFromClause { get; set; } = true;

        [Category(SqlFormatter), DisplayName("New line before WHERE"), Description("editorconfig: new_line_before_where_clause")]
        [DefaultValue(true)]
        public bool SqlNewLineBeforeWhereClause { get; set; } = true;

        [Category(SqlFormatter), DisplayName("New line before GROUP BY"), Description("editorconfig: new_line_before_group_by_clause")]
        [DefaultValue(true)]
        public bool SqlNewLineBeforeGroupByClause { get; set; } = true;

        [Category(SqlFormatter), DisplayName("New line before ORDER BY"), Description("editorconfig: new_line_before_order_by_clause")]
        [DefaultValue(true)]
        public bool SqlNewLineBeforeOrderByClause { get; set; } = true;

        [Category(SqlFormatter), DisplayName("New line before HAVING"), Description("editorconfig: new_line_before_having_clause")]
        [DefaultValue(true)]
        public bool SqlNewLineBeforeHavingClause { get; set; } = true;

        [Category(SqlFormatter), DisplayName("New line before WINDOW"), Description("editorconfig: new_line_before_window_clause")]
        [DefaultValue(true)]
        public bool SqlNewLineBeforeWindowClause { get; set; } = true;

        [Category(SqlFormatter), DisplayName("New line before JOIN"), Description("editorconfig: new_line_before_join_clause")]
        [DefaultValue(true)]
        public bool SqlNewLineBeforeJoinClause { get; set; } = true;

        [Category(SqlFormatter), DisplayName("New line after JOIN"), Description("editorconfig: new_line_after_join_keyword")]
        [DefaultValue(false)]
        public bool SqlNewLineAfterJoinKeyword { get; set; } = false;

        [Category(SqlFormatter), DisplayName("New line before ON"), Description("editorconfig: new_line_before_on_clause")]
        [DefaultValue(true)]
        public bool SqlNewLineBeforeOnClause { get; set; } = true;

        [Category(SqlFormatter), DisplayName("New line before OFFSET"), Description("editorconfig: new_line_before_offset_clause")]
        [DefaultValue(true)]
        public bool SqlNewLineBeforeOffsetClause { get; set; } = true;

        [Category(SqlFormatter), DisplayName("New line before OUTPUT"), Description("editorconfig: new_line_before_output_clause")]
        [DefaultValue(true)]
        public bool SqlNewLineBeforeOutputClause { get; set; } = true;

        [Category(SqlFormatter), DisplayName("New line before '(' in multi-line lists"), Description("editorconfig: new_line_before_open_parenthesis_in_multiline_list")]
        [DefaultValue(true)]
        public bool SqlNewLineBeforeOpenParenthesisInMultilineList { get; set; } = true;

        [Category(SqlFormatter), DisplayName("New line before ')' in multi-line lists"), Description("editorconfig: new_line_before_close_parenthesis_in_multiline_list")]
        [DefaultValue(true)]
        public bool SqlNewLineBeforeCloseParenthesisInMultilineList { get; set; } = true;

        [Category(SqlFormatter), DisplayName("Index definitions on multiple lines"), Description("editorconfig: newline_formatted_index_definition")]
        [DefaultValue(true)]
        public bool SqlNewLineFormattedIndexDefinition { get; set; } = true;

        [Category(SqlFormatter), DisplayName("CHECK constraints on multiple lines"), Description("editorconfig: newline_formatted_check_constraint")]
        [DefaultValue(true)]
        public bool SqlNewlineFormattedCheckConstraint { get; set; } = true;

        [Category(SqlFormatter), DisplayName("New lines after each statement"), Description("editorconfig: num_newlines_after_statement")]
        [DefaultValue(2)]
        public int SqlNumNewlinesAfterStatement { get; set; } = 2;

        [Category(SqlFormatter), DisplayName("New lines after a batch statement"), Description("editorconfig: num_newlines_after_batch_statement")]
        [DefaultValue(1)]
        public int SqlNumNewlinesAfterBatchStatement { get; set; } = 1;

        [Category(SqlFormatter), DisplayName("New lines after each batch (GO)"), Description("editorconfig: num_newlines_after_batches")]
        [DefaultValue(2)]
        public int SqlNumNewlinesAfterBatches { get; set; } = 2;

        [Category(SqlFormatter), DisplayName("Space between data type and parameters"), Description("editorconfig: space_between_data_type_and_parameters")]
        [DefaultValue(false)]
        public bool SqlSpaceBetweenDataTypeAndParameters { get; set; } = false;

        [Category(SqlFormatter), DisplayName("Space between parameters in data types"), Description("editorconfig: space_between_parameters_in_data_type")]
        [DefaultValue(true)]
        public bool SqlSpaceBetweenParametersInDataType { get; set; } = true;

        /// <summary>Propiedad de ScriptDOM → valor.</summary>
        private Dictionary<string, string> GetSqlFormatterValues()
        {
            return new Dictionary<string, string>
            {
                ["SqlVersion"] = SqlVersion.ToString(),
                ["SqlEngineType"] = SqlEngineType.ToString(),
                ["AllowExternalLibraryPaths"] = SqlAllowExternalLibraryPaths ? "true" : "false",
                ["AllowExternalLanguagePaths"] = SqlAllowExternalLanguagePaths ? "true" : "false",
                ["AlignClauseBodies"] = SqlAlignClauseBodies ? "true" : "false",
                ["AlignColumnDefinitionFields"] = SqlAlignColumnDefinitionFields ? "true" : "false",
                ["AlignSetClauseItem"] = SqlAlignSetClauseItem ? "true" : "false",
                ["ClauseBodyAlignment"] = SqlClauseBodyAlignment.ToString(),
                ["AsKeywordOnOwnLine"] = SqlAsKeywordOnOwnLine ? "true" : "false",
                ["KeywordCasing"] = SqlKeywordCasing.ToString(),
                ["BuiltInFunctionCasing"] = SqlBuiltInFunctionCasing.ToString(),
                ["IdentifierCasing"] = SqlIdentifierCasing.ToString(),
                ["IdentifierBracketing"] = SqlIdentifierBracketing.ToString(),
                ["ColumnAliasStyle"] = SqlColumnAliasStyle.ToString(),
                ["CommaPlacement"] = SqlCommaPlacement.ToString(),
                ["LeadingCommaSpaceCount"] = SqlLeadingCommaSpaceCount.ToString(),
                ["IncludeSemicolons"] = SqlIncludeSemicolons ? "true" : "false",
                ["TerminateBlockStatements"] = SqlTerminateBlockStatements ? "true" : "false",
                ["PreserveComments"] = SqlPreserveComments ? "true" : "false",
                ["PersistTrailingGo"] = SqlPersistTrailingGo ? "true" : "false",
                ["IndentationSize"] = SqlIndentationSize.ToString(),
                ["IndentationMode"] = SqlIndentationMode.ToString(),
                ["IndentSetClause"] = SqlIndentSetClause ? "true" : "false",
                ["IndentViewBody"] = SqlIndentViewBody ? "true" : "false",
                ["MultilineSelectElementsList"] = SqlMultilineSelectElementsList ? "true" : "false",
                ["MultilineWherePredicatesList"] = SqlMultilineWherePredicatesList ? "true" : "false",
                ["MultilineGroupByElementsList"] = SqlMultilineGroupByElementsList ? "true" : "false",
                ["MultilineHavingPredicatesList"] = SqlMultilineHavingPredicatesList ? "true" : "false",
                ["MultilineOrderByElementsList"] = SqlMultilineOrderByElementsList ? "true" : "false",
                ["MultilinePartitionByElementsList"] = SqlMultilinePartitionByElementsList ? "true" : "false",
                ["MultilineInValuesList"] = SqlMultilineInValuesList ? "true" : "false",
                ["MultilineViewColumnsList"] = SqlMultilineViewColumnsList ? "true" : "false",
                ["MultilineSetClauseItems"] = SqlMultilineSetClauseItems ? "true" : "false",
                ["MultilineInsertTargetsList"] = SqlMultilineInsertTargetsList ? "true" : "false",
                ["MultilineInsertSourcesList"] = SqlMultilineInsertSourcesList ? "true" : "false",
                ["MultilineProcedureParametersList"] = SqlMultilineProcedureParametersList ? "true" : "false",
                ["MultilineNestedFunctionCalls"] = SqlMultilineNestedFunctionCalls ? "true" : "false",
                ["MultilineWithOptionsList"] = SqlMultilineWithOptionsList ? "true" : "false",
                ["NewLineBeforeFromClause"] = SqlNewLineBeforeFromClause ? "true" : "false",
                ["NewLineBeforeWhereClause"] = SqlNewLineBeforeWhereClause ? "true" : "false",
                ["NewLineBeforeGroupByClause"] = SqlNewLineBeforeGroupByClause ? "true" : "false",
                ["NewLineBeforeOrderByClause"] = SqlNewLineBeforeOrderByClause ? "true" : "false",
                ["NewLineBeforeHavingClause"] = SqlNewLineBeforeHavingClause ? "true" : "false",
                ["NewLineBeforeWindowClause"] = SqlNewLineBeforeWindowClause ? "true" : "false",
                ["NewLineBeforeJoinClause"] = SqlNewLineBeforeJoinClause ? "true" : "false",
                ["NewLineAfterJoinKeyword"] = SqlNewLineAfterJoinKeyword ? "true" : "false",
                ["NewLineBeforeOnClause"] = SqlNewLineBeforeOnClause ? "true" : "false",
                ["NewLineBeforeOffsetClause"] = SqlNewLineBeforeOffsetClause ? "true" : "false",
                ["NewLineBeforeOutputClause"] = SqlNewLineBeforeOutputClause ? "true" : "false",
                ["NewLineBeforeOpenParenthesisInMultilineList"] = SqlNewLineBeforeOpenParenthesisInMultilineList ? "true" : "false",
                ["NewLineBeforeCloseParenthesisInMultilineList"] = SqlNewLineBeforeCloseParenthesisInMultilineList ? "true" : "false",
                ["NewLineFormattedIndexDefinition"] = SqlNewLineFormattedIndexDefinition ? "true" : "false",
                ["NewlineFormattedCheckConstraint"] = SqlNewlineFormattedCheckConstraint ? "true" : "false",
                ["NumNewlinesAfterStatement"] = SqlNumNewlinesAfterStatement.ToString(),
                ["NumNewlinesAfterBatchStatement"] = SqlNumNewlinesAfterBatchStatement.ToString(),
                ["NumNewlinesAfterBatches"] = SqlNumNewlinesAfterBatches.ToString(),
                ["SpaceBetweenDataTypeAndParameters"] = SqlSpaceBetweenDataTypeAndParameters ? "true" : "false",
                ["SpaceBetweenParametersInDataType"] = SqlSpaceBetweenParametersInDataType ? "true" : "false",
            };
        }
    }

    /// <summary>Mismos valores que ScriptDOM (SqlVersion).</summary>
    public enum SqlVersionValue
    {
        Sql90,
        Sql80,
        Sql100,
        Sql110,
        Sql120,
        Sql130,
        Sql140,
        Sql150,
        Sql160,
        Sql170,
        SqlFabricDW,
        Sql180
    }

    /// <summary>Mismos valores que ScriptDOM (SqlEngineType).</summary>
    public enum SqlEngineTypeValue
    {
        All,
        Standalone,
        SqlAzure
    }

    /// <summary>Mismos valores que ScriptDOM (ClauseBodyAlignment).</summary>
    public enum SqlClauseBodyAlignment
    {
        Aligned,
        Indented
    }

    /// <summary>Mismos valores que ScriptDOM (KeywordCasing).</summary>
    public enum SqlKeywordCasing
    {
        Lowercase,
        Uppercase,
        PascalCase
    }

    /// <summary>Mismos valores que ScriptDOM (BuiltInFunctionCasing).</summary>
    public enum SqlBuiltInFunctionCasing
    {
        Preserve,
        Uppercase,
        Lowercase,
        PascalCase
    }

    /// <summary>Mismos valores que ScriptDOM (IdentifierCasing).</summary>
    public enum SqlIdentifierCasing
    {
        Preserve,
        Uppercase,
        Lowercase,
        PascalCase
    }

    /// <summary>Mismos valores que ScriptDOM (IdentifierBracketing).</summary>
    public enum SqlIdentifierBracketing
    {
        Preserve,
        IncludeBrackets,
        ExcludeBrackets
    }

    /// <summary>Mismos valores que ScriptDOM (ColumnAliasStyle).</summary>
    public enum SqlColumnAliasStyle
    {
        AsKeyword,
        EqualsSign,
        Preserve
    }

    /// <summary>Mismos valores que ScriptDOM (CommaPlacement).</summary>
    public enum SqlCommaPlacement
    {
        Trailing,
        Leading
    }

    /// <summary>Mismos valores que ScriptDOM (IndentationMode).</summary>
    public enum SqlIndentationMode
    {
        Spaces,
        Tabs
    }
}
