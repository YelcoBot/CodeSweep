namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Settings
{
    /// <summary>
    /// Un UIContext por opción del formateador T-SQL. El paquete activa el de cada opción que el producto ya tiene
    /// en Tools → Options (por ejemplo, las sqlFormatter.* de SSMS), y CodeSweep.registration.json la oculta
    /// con "visibleWhen". Generado por tools/SqlFormatter/Update-SqlFormatterOptions.ps1 desde SqlFormatterOptions.tsv; no editar a mano.
    /// </summary>
    internal static class SqlFormatterUIContexts
    {
        public static readonly IReadOnlyDictionary<string, Guid> ByOption = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
        {
            ["SqlVersion"] = new Guid("f620fc8b-5ed2-4f20-9c55-8ac39211cd59"),
            ["SqlEngineType"] = new Guid("8246c74f-fc5d-4e23-b68a-173b394a4e3b"),
            ["AllowExternalLibraryPaths"] = new Guid("6396cc63-4af7-4ee4-96b8-3cba99048c81"),
            ["AllowExternalLanguagePaths"] = new Guid("57f202b0-7c87-4770-889a-b1d1753d4859"),
            ["AlignClauseBodies"] = new Guid("da79b5c6-d389-4ce3-936c-b187020bf944"),
            ["AlignColumnDefinitionFields"] = new Guid("ed28ef80-ccec-44fa-877f-36b183e2c62b"),
            ["AlignSetClauseItem"] = new Guid("dbfb0e25-9522-4a33-bd5e-3979e5cba62b"),
            ["ClauseBodyAlignment"] = new Guid("256af42c-dd50-4924-95af-742da25ae105"),
            ["AsKeywordOnOwnLine"] = new Guid("15c9341e-d677-4ce9-b038-c0f418aca571"),
            ["KeywordCasing"] = new Guid("cc2da039-4b5b-44ed-bc5f-043714163ca8"),
            ["BuiltInFunctionCasing"] = new Guid("2907b343-e273-4b81-bd51-0cfa357dd2a7"),
            ["IdentifierCasing"] = new Guid("5eef0c48-ffb1-45d1-8458-4e30979c9116"),
            ["IdentifierBracketing"] = new Guid("2d11f173-a5ad-42f0-86f6-195bc80f7659"),
            ["ColumnAliasStyle"] = new Guid("1486169c-61fd-473b-aeb2-1ddb72fede86"),
            ["CommaPlacement"] = new Guid("ac0f9694-99ad-4348-8611-adce8900ba03"),
            ["LeadingCommaSpaceCount"] = new Guid("b53b7ad2-4e2c-483e-a6c4-f1a81117a3b5"),
            ["IncludeSemicolons"] = new Guid("f75d7ce6-fcc6-464b-9698-df7ac6a56baa"),
            ["TerminateBlockStatements"] = new Guid("89e5198d-d89d-41d3-a164-e7a728d3c6e2"),
            ["PreserveComments"] = new Guid("4805cd29-f474-424a-b95d-a6fd5310bb39"),
            ["PersistTrailingGo"] = new Guid("8b09a6f5-3882-4fa1-9750-dc36f1c35ed0"),
            ["IndentationSize"] = new Guid("976c087f-1f95-438e-8dd4-0f3832202dea"),
            ["IndentationMode"] = new Guid("85220ea1-3505-40d4-83c1-fba004f84605"),
            ["IndentSetClause"] = new Guid("7a1c6eca-2775-4b0a-99cf-cd8701a22d33"),
            ["IndentViewBody"] = new Guid("ee42b8bb-b6c3-4853-a08b-c4699e3a5db9"),
            ["MultilineSelectElementsList"] = new Guid("01f86128-8319-4476-8ce9-2bfc98964bc7"),
            ["MultilineWherePredicatesList"] = new Guid("c13cfeb9-e86f-43b1-987b-d8956a00041f"),
            ["MultilineGroupByElementsList"] = new Guid("ff4f7e39-81e6-4c1e-8779-944ae93ce875"),
            ["MultilineHavingPredicatesList"] = new Guid("32b39652-ee03-4195-9659-129a91ce511d"),
            ["MultilineOrderByElementsList"] = new Guid("3394c918-ccd0-4c55-9bf2-58843f435ed5"),
            ["MultilinePartitionByElementsList"] = new Guid("1b2b9ddb-94d3-4e23-a611-3fbb5edfd217"),
            ["MultilineInValuesList"] = new Guid("6af6dcdf-ff58-4dcb-bd83-c8d266c62eb6"),
            ["MultilineViewColumnsList"] = new Guid("cbb76f21-2d7a-491e-a8d0-ff95ad72224c"),
            ["MultilineSetClauseItems"] = new Guid("75196296-b175-4ea3-8a29-c25f39dc0fa4"),
            ["MultilineInsertTargetsList"] = new Guid("dc3bd09b-f7d9-4baf-b86d-b4d36c4d778a"),
            ["MultilineInsertSourcesList"] = new Guid("fac2d96c-e823-4e1f-ae88-e89ae308a9ef"),
            ["MultilineProcedureParametersList"] = new Guid("234f2d3b-2ced-4588-bc10-1cd2eae94a1a"),
            ["MultilineNestedFunctionCalls"] = new Guid("5526c3bd-24e7-40e2-8076-3f334e25cb60"),
            ["MultilineWithOptionsList"] = new Guid("133cef1a-8cc0-4615-94eb-21f90deaf673"),
            ["NewLineBeforeFromClause"] = new Guid("26770cab-df6c-4f09-afb4-a3f7f576705f"),
            ["NewLineBeforeWhereClause"] = new Guid("700cfd22-2af7-450e-9095-c0c0997f1ebd"),
            ["NewLineBeforeGroupByClause"] = new Guid("fddeb590-fa47-4606-8ce8-d4b74e9b4f3c"),
            ["NewLineBeforeOrderByClause"] = new Guid("d4cd9922-24bd-4bfe-9d22-cd539cc37a18"),
            ["NewLineBeforeHavingClause"] = new Guid("f3826edf-0e6f-48ca-8063-9bdf093ad204"),
            ["NewLineBeforeWindowClause"] = new Guid("bd09e386-c147-47d6-b7a1-50b2935ddcf9"),
            ["NewLineBeforeJoinClause"] = new Guid("18c29df5-9f0f-4819-9846-9df4915e38d0"),
            ["NewLineAfterJoinKeyword"] = new Guid("f8486d1e-fbae-4cd0-b472-d84172ac836c"),
            ["NewLineBeforeOnClause"] = new Guid("e36420c5-1f06-423b-b091-50c79eb339b0"),
            ["NewLineBeforeOffsetClause"] = new Guid("d7a96e6f-9e12-4b25-a3b2-459cc4d3d53a"),
            ["NewLineBeforeOutputClause"] = new Guid("469899cb-6367-4b73-9658-55fd0c826893"),
            ["NewLineBeforeOpenParenthesisInMultilineList"] = new Guid("ff5bec62-cf90-4dfd-aa79-1e8fa30cd5e8"),
            ["NewLineBeforeCloseParenthesisInMultilineList"] = new Guid("1d478a22-4832-4777-b708-9f52dbb17112"),
            ["NewLineFormattedIndexDefinition"] = new Guid("f041aec1-ad79-485d-baee-fe8d3b5f6653"),
            ["NewlineFormattedCheckConstraint"] = new Guid("192492f3-6aae-41a9-9ff8-2610b389a99e"),
            ["NumNewlinesAfterStatement"] = new Guid("c0d09238-7b0a-4381-8d03-2413a91b7e8f"),
            ["NumNewlinesAfterBatchStatement"] = new Guid("15c39110-8fcd-4543-a288-c876f93b8e7d"),
            ["NumNewlinesAfterBatches"] = new Guid("5cd628a3-edc1-4d5e-b905-7865bf48d877"),
            ["SpaceBetweenDataTypeAndParameters"] = new Guid("19a5a4df-eb0c-4bfd-b916-877907d3d0e2"),
            ["SpaceBetweenParametersInDataType"] = new Guid("b3fff56f-dbd5-408a-a405-aae6aaff2d0c"),
        };
    }
}
