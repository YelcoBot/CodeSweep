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
    /// Las opciones del formateador T-SQL (todas las de ScriptDOM 180.117.0) con los valores por defecto de CodeSweep.
    /// Prioridad de cada valor: .editorconfig [*.sql] → opciones del producto (VS / SSMS) si la tiene → opciones de CodeSweep.
    /// Generado por tools/SqlFormatter/Update-SqlFormatterOptions.ps1 desde SqlFormatterOptions.tsv; no editar a mano.
    /// </summary>
    public static class SqlFormatterCatalog
    {
        public static readonly IReadOnlyList<SqlFormatterOption> Options = new[]
        {
            new SqlFormatterOption("SqlVersion", "general", "Sql170", "sql_version"),
            new SqlFormatterOption("SqlEngineType", "general", "All", "sql_engine_type"),
            new SqlFormatterOption("AllowExternalLibraryPaths", "general", "true", "allow_external_library_paths"),
            new SqlFormatterOption("AllowExternalLanguagePaths", "general", "true", "allow_external_language_paths"),
            new SqlFormatterOption("AlignClauseBodies", "alignment", "false", "align_clause_bodies"),
            new SqlFormatterOption("AlignColumnDefinitionFields", "alignment", "true", "align_column_definition_fields"),
            new SqlFormatterOption("AlignSetClauseItem", "alignment", "true", "align_set_clause_item"),
            new SqlFormatterOption("ClauseBodyAlignment", "alignment", "Indented", "clause_body_alignment"),
            new SqlFormatterOption("AsKeywordOnOwnLine", "formatting", "true", "as_keyword_on_own_line"),
            new SqlFormatterOption("KeywordCasing", "formatting", "Uppercase", "keyword_casing"),
            new SqlFormatterOption("BuiltInFunctionCasing", "formatting", "Uppercase", "built_in_function_casing"),
            new SqlFormatterOption("IdentifierCasing", "formatting", "Preserve", "identifier_casing"),
            new SqlFormatterOption("IdentifierBracketing", "formatting", "Preserve", "identifier_bracketing"),
            new SqlFormatterOption("ColumnAliasStyle", "formatting", "AsKeyword", "column_alias_style"),
            new SqlFormatterOption("CommaPlacement", "formatting", "Trailing", "comma_placement"),
            new SqlFormatterOption("LeadingCommaSpaceCount", "formatting", "1", "leading_comma_space_count"),
            new SqlFormatterOption("IncludeSemicolons", "formatting", "true", "include_semicolons"),
            new SqlFormatterOption("TerminateBlockStatements", "formatting", "true", "terminate_block_statements"),
            new SqlFormatterOption("PreserveComments", "formatting", "true", "preserve_comments"),
            new SqlFormatterOption("PersistTrailingGo", "formatting", "true", "persist_trailing_go"),
            new SqlFormatterOption("IndentationSize", "indentation", "4", "indentation_size"),
            new SqlFormatterOption("IndentationMode", "indentation", "Tabs", "indentation_mode"),
            new SqlFormatterOption("IndentSetClause", "indentation", "true", "indent_set_clause"),
            new SqlFormatterOption("IndentViewBody", "indentation", "true", "indent_view_body"),
            new SqlFormatterOption("MultilineSelectElementsList", "multiline", "true", "multiline_select_elements_list"),
            new SqlFormatterOption("MultilineWherePredicatesList", "multiline", "true", "multiline_where_predicates_list"),
            new SqlFormatterOption("MultilineGroupByElementsList", "multiline", "true", "multiline_group_by_elements_list"),
            new SqlFormatterOption("MultilineHavingPredicatesList", "multiline", "true", "multiline_having_predicates_list"),
            new SqlFormatterOption("MultilineOrderByElementsList", "multiline", "false", "multiline_order_by_elements_list"),
            new SqlFormatterOption("MultilinePartitionByElementsList", "multiline", "false", "multiline_partition_by_elements_list"),
            new SqlFormatterOption("MultilineInValuesList", "multiline", "false", "multiline_in_values_list"),
            new SqlFormatterOption("MultilineViewColumnsList", "multiline", "true", "multiline_view_columns_list"),
            new SqlFormatterOption("MultilineSetClauseItems", "multiline", "true", "multiline_set_clause_items"),
            new SqlFormatterOption("MultilineInsertTargetsList", "multiline", "true", "multiline_insert_targets_list"),
            new SqlFormatterOption("MultilineInsertSourcesList", "multiline", "true", "multiline_insert_sources_list"),
            new SqlFormatterOption("MultilineProcedureParametersList", "multiline", "true", "multiline_procedure_parameters_list"),
            new SqlFormatterOption("MultilineNestedFunctionCalls", "multiline", "false", "multiline_nested_function_calls"),
            new SqlFormatterOption("MultilineWithOptionsList", "multiline", "false", "multiline_with_options_list"),
            new SqlFormatterOption("NewLineBeforeFromClause", "newLine", "true", "new_line_before_from_clause"),
            new SqlFormatterOption("NewLineBeforeWhereClause", "newLine", "true", "new_line_before_where_clause"),
            new SqlFormatterOption("NewLineBeforeGroupByClause", "newLine", "true", "new_line_before_group_by_clause"),
            new SqlFormatterOption("NewLineBeforeOrderByClause", "newLine", "true", "new_line_before_order_by_clause"),
            new SqlFormatterOption("NewLineBeforeHavingClause", "newLine", "true", "new_line_before_having_clause"),
            new SqlFormatterOption("NewLineBeforeWindowClause", "newLine", "true", "new_line_before_window_clause"),
            new SqlFormatterOption("NewLineBeforeJoinClause", "newLine", "true", "new_line_before_join_clause"),
            new SqlFormatterOption("NewLineAfterJoinKeyword", "newLine", "false", "new_line_after_join_keyword"),
            new SqlFormatterOption("NewLineBeforeOnClause", "newLine", "true", "new_line_before_on_clause"),
            new SqlFormatterOption("NewLineBeforeOffsetClause", "newLine", "true", "new_line_before_offset_clause"),
            new SqlFormatterOption("NewLineBeforeOutputClause", "newLine", "true", "new_line_before_output_clause"),
            new SqlFormatterOption("NewLineBeforeOpenParenthesisInMultilineList", "newLine", "true", "new_line_before_open_parenthesis_in_multiline_list"),
            new SqlFormatterOption("NewLineBeforeCloseParenthesisInMultilineList", "newLine", "true", "new_line_before_close_parenthesis_in_multiline_list"),
            new SqlFormatterOption("NewLineFormattedIndexDefinition", "newLine", "true", "newline_formatted_index_definition"),
            new SqlFormatterOption("NewlineFormattedCheckConstraint", "newLine", "true", "newline_formatted_check_constraint"),
            new SqlFormatterOption("NumNewlinesAfterStatement", "newLine", "2", "num_newlines_after_statement"),
            new SqlFormatterOption("NumNewlinesAfterBatchStatement", "newLine", "1", "num_newlines_after_batch_statement"),
            new SqlFormatterOption("NumNewlinesAfterBatches", "newLine", "2", "num_newlines_after_batches"),
            new SqlFormatterOption("SpaceBetweenDataTypeAndParameters", "spacing", "false", "space_between_data_type_and_parameters"),
            new SqlFormatterOption("SpaceBetweenParametersInDataType", "spacing", "true", "space_between_parameters_in_data_type"),
        };
    }
}
